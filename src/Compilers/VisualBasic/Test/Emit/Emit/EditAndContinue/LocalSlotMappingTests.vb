' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class LocalSlotMappingTests
        Inherits EditAndContinueTestBase

        <Fact>
        Public Sub OutOfOrderUserLocals()
            Dim source = MarkedSource("
Imports System
Class C
    Sub M()
        For <N:0>index</N:0> As Integer = 1 To 1
            Console.WriteLine(1)
        Next
        For <N:1>index</N:1> As Integer = 1 To 2
            Console.WriteLine(2)
        Next
    End Sub
End Class
")

            Dim compilation0 = CreateCompilationWithReferences({source.Tree}, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            v0.VerifyIL("C.M", "
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (Integer V_0, //index
                Integer V_1) //index
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
 -IL_0003:  ldc.i4.1
  IL_0004:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0009:  nop
 -IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.0
 ~IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  ble.s      IL_0003
 -IL_0012:  ldc.i4.1
  IL_0013:  stloc.1
 -IL_0014:  ldc.i4.2
  IL_0015:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_001a:  nop
 -IL_001b:  ldloc.1
  IL_001c:  ldc.i4.1
  IL_001d:  add.ovf
  IL_001e:  stloc.1
 ~IL_001f:  ldloc.1
  IL_0020:  ldc.i4.2
  IL_0021:  ble.s      IL_0014
 -IL_0023:  ret
}
", sequencePoints:="C.M")

            v0.VerifyPdb("C.M", "
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""9"" />
          <slot kind=""0"" offset=""107"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""12"" document=""0"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""49"" document=""0"" />
        <entry offset=""0x3"" startLine=""6"" startColumn=""13"" endLine=""6"" endColumn=""33"" document=""0"" />
        <entry offset=""0xa"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""13"" document=""0"" />
        <entry offset=""0xe"" hidden=""true"" document=""0"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""49"" document=""0"" />
        <entry offset=""0x14"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""33"" document=""0"" />
        <entry offset=""0x1b"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""13"" document=""0"" />
        <entry offset=""0x1f"" hidden=""true"" document=""0"" />
        <entry offset=""0x23"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""12"" document=""0"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x24"">
        <namespace name=""System"" importlevel=""file"" />
        <currentnamespace name="""" />
        <scope startOffset=""0x1"" endOffset=""0x11"">
          <local name=""index"" il_index=""0"" il_start=""0x1"" il_end=""0x11"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x12"" endOffset=""0x22"">
          <local name=""index"" il_index=""1"" il_start=""0x12"" il_end=""0x22"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
")
            Dim symReader = v0.CreateSymReader()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C.M")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), Function(handle) symReader.GetEncMethodDebugInfo(handle))

            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source, source), preserveLocalVariables:=True)))

            ' check that all user-defined and long-lived synthesized local slots are reused
            diff1.VerifyIL("C.M", "
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (Integer V_0, //index
                Integer V_1) //index
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0009:  nop
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  ble.s      IL_0003
  IL_0012:  ldc.i4.1
  IL_0013:  stloc.1
  IL_0014:  ldc.i4.2
  IL_0015:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_001a:  nop
  IL_001b:  ldloc.1
  IL_001c:  ldc.i4.1
  IL_001d:  add.ovf
  IL_001e:  stloc.1
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.2
  IL_0021:  ble.s      IL_0014
  IL_0023:  ret
}
")
        End Sub

        ' <summary>
        ' Enc debug info Is only present in debug builds.
        ' </summary>
        <Fact>
        Public Sub DebugOnly()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Function F() As System.IDisposable
        Return Nothing
    End Function

    Sub M()
        Using F()
        End Using
    End Sub
End Class
    </file>
</compilation>

            Dim debug = CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim release = CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.ReleaseDll)

            CompileAndVerify(debug).VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="B1, 88, 10, 98, B9, 30, FE, B8, AD, 46, 3F,  5, 46, 9B, AF, A9, 4F, CB, 65, B1, "/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="4" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="7" startColumn="5" endLine="7" endColumn="12" document="1"/>
                <entry offset="0x1" startLine="8" startColumn="9" endLine="8" endColumn="18" document="1"/>
                <entry offset="0x9" hidden="true" document="1"/>
                <entry offset="0xb" startLine="9" startColumn="9" endLine="9" endColumn="18" document="1"/>
                <entry offset="0x17" startLine="10" startColumn="5" endLine="10" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <importsforward declaringType="C" methodName="F"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(release).VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="B1, 88, 10, 98, B9, 30, FE, B8, AD, 46, 3F,  5, 46, 9B, AF, A9, 4F, CB, 65, B1, "/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="9" endLine="8" endColumn="18" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
                <entry offset="0x9" startLine="9" startColumn="9" endLine="9" endColumn="18" document="1"/>
                <entry offset="0x13" startLine="10" startColumn="5" endLine="10" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x14">
                <importsforward declaringType="C" methodName="F"/>
            </scope>
        </method>
    </methods>
</symbols>
            )

        End Sub

        <Fact>
        Public Sub ForEach()
            Dim source = MarkedSource("
Imports System.Collections
Imports System.Collections.Generic

Class C
    Function F1() As IEnumerable
        Return Nothing
    End Function

    Function F2() As List(Of Object)
        Return Nothing
    End Function

    Function F3() As IEnumerable
        Return Nothing
    End Function

    Function F4() As List(Of Object)
        Return Nothing
    End Function

    Sub M()
        <N:4><N:0>For Each x In F1()</N:0>

            <N:3><N:1>For Each <N:2>y</N:2> As Object In F2()</N:1> : Next</N:3>

        Next</N:4>

        <N:8><N:5>For Each x In F4()</N:5>
            <N:9><N:6>For Each y In F3()</N:6> : Next</N:9>

            <N:10><N:7>For Each z In F2()</N:7> : Next</N:10>
        Next</N:8>
    End Sub
End Class
")

            Dim compilation0 = CreateCompilationWithMscorlib({source.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source.Tree)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C.M")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider())

            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source, source), preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size      318 (0x13e)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
                Object V_1, //x
                System.Collections.Generic.List(Of Object).Enumerator V_2,
                Object V_3, //y
                Boolean V_4,
                Boolean V_5,
                System.Collections.Generic.List(Of Object).Enumerator V_6,
                Object V_7, //x
                System.Collections.IEnumerator V_8,
                Object V_9, //y
                Boolean V_10,
                System.Collections.Generic.List(Of Object).Enumerator V_11,
                Object V_12, //z
                Boolean V_13,
                Boolean V_14)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldarg.0
    IL_0002:  call       ""Function C.F1() As System.Collections.IEnumerable""
    IL_0007:  callvirt   ""Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator""
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_0056
    IL_000f:  ldloc.0
    IL_0010:  callvirt   ""Function System.Collections.IEnumerator.get_Current() As Object""
    IL_0015:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_001a:  stloc.1
    .try
    {
      IL_001b:  ldarg.0
      IL_001c:  call       ""Function C.F2() As System.Collections.Generic.List(Of Object)""
      IL_0021:  callvirt   ""Function System.Collections.Generic.List(Of Object).GetEnumerator() As System.Collections.Generic.List(Of Object).Enumerator""
      IL_0026:  stloc.2
      IL_0027:  br.s       IL_0037
      IL_0029:  ldloca.s   V_2
      IL_002b:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.get_Current() As Object""
      IL_0030:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
      IL_0035:  stloc.3
      IL_0036:  nop
      IL_0037:  ldloca.s   V_2
      IL_0039:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.MoveNext() As Boolean""
      IL_003e:  stloc.s    V_4
      IL_0040:  ldloc.s    V_4
      IL_0042:  brtrue.s   IL_0029
      IL_0044:  leave.s    IL_0055
    }
    finally
    {
      IL_0046:  ldloca.s   V_2
      IL_0048:  constrained. ""System.Collections.Generic.List(Of Object).Enumerator""
      IL_004e:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_0053:  nop
      IL_0054:  endfinally
    }
    IL_0055:  nop
    IL_0056:  ldloc.0
    IL_0057:  callvirt   ""Function System.Collections.IEnumerator.MoveNext() As Boolean""
    IL_005c:  stloc.s    V_5
    IL_005e:  ldloc.s    V_5
    IL_0060:  brtrue.s   IL_000f
    IL_0062:  leave.s    IL_0079
  }
  finally
  {
    IL_0064:  ldloc.0
    IL_0065:  isinst     ""System.IDisposable""
    IL_006a:  brfalse.s  IL_0078
    IL_006c:  ldloc.0
    IL_006d:  isinst     ""System.IDisposable""
    IL_0072:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0077:  nop
    IL_0078:  endfinally
  }
  IL_0079:  nop
  .try
  {
    IL_007a:  ldarg.0
    IL_007b:  call       ""Function C.F4() As System.Collections.Generic.List(Of Object)""
    IL_0080:  callvirt   ""Function System.Collections.Generic.List(Of Object).GetEnumerator() As System.Collections.Generic.List(Of Object).Enumerator""
    IL_0085:  stloc.s    V_6
    IL_0087:  br         IL_011c
    IL_008c:  ldloca.s   V_6
    IL_008e:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.get_Current() As Object""
    IL_0093:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_0098:  stloc.s    V_7
    .try
    {
      IL_009a:  ldarg.0
      IL_009b:  call       ""Function C.F3() As System.Collections.IEnumerable""
      IL_00a0:  callvirt   ""Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator""
      IL_00a5:  stloc.s    V_8
      IL_00a7:  br.s       IL_00b8
      IL_00a9:  ldloc.s    V_8
      IL_00ab:  callvirt   ""Function System.Collections.IEnumerator.get_Current() As Object""
      IL_00b0:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
      IL_00b5:  stloc.s    V_9
      IL_00b7:  nop
      IL_00b8:  ldloc.s    V_8
      IL_00ba:  callvirt   ""Function System.Collections.IEnumerator.MoveNext() As Boolean""
      IL_00bf:  stloc.s    V_10
      IL_00c1:  ldloc.s    V_10
      IL_00c3:  brtrue.s   IL_00a9
      IL_00c5:  leave.s    IL_00de
    }
    finally
    {
      IL_00c7:  ldloc.s    V_8
      IL_00c9:  isinst     ""System.IDisposable""
      IL_00ce:  brfalse.s  IL_00dd
      IL_00d0:  ldloc.s    V_8
      IL_00d2:  isinst     ""System.IDisposable""
      IL_00d7:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_00dc:  nop
      IL_00dd:  endfinally
    }
    IL_00de:  nop
    .try
    {
      IL_00df:  ldarg.0
      IL_00e0:  call       ""Function C.F2() As System.Collections.Generic.List(Of Object)""
      IL_00e5:  callvirt   ""Function System.Collections.Generic.List(Of Object).GetEnumerator() As System.Collections.Generic.List(Of Object).Enumerator""
      IL_00ea:  stloc.s    V_11
      IL_00ec:  br.s       IL_00fd
      IL_00ee:  ldloca.s   V_11
      IL_00f0:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.get_Current() As Object""
      IL_00f5:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
      IL_00fa:  stloc.s    V_12
      IL_00fc:  nop
      IL_00fd:  ldloca.s   V_11
      IL_00ff:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.MoveNext() As Boolean""
      IL_0104:  stloc.s    V_13
      IL_0106:  ldloc.s    V_13
      IL_0108:  brtrue.s   IL_00ee
      IL_010a:  leave.s    IL_011b
    }
    finally
    {
      IL_010c:  ldloca.s   V_11
      IL_010e:  constrained. ""System.Collections.Generic.List(Of Object).Enumerator""
      IL_0114:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_0119:  nop
      IL_011a:  endfinally
    }
    IL_011b:  nop
    IL_011c:  ldloca.s   V_6
    IL_011e:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.MoveNext() As Boolean""
    IL_0123:  stloc.s    V_14
    IL_0125:  ldloc.s    V_14
    IL_0127:  brtrue     IL_008c
    IL_012c:  leave.s    IL_013d
  }
  finally
  {
    IL_012e:  ldloca.s   V_6
    IL_0130:  constrained. ""System.Collections.Generic.List(Of Object).Enumerator""
    IL_0136:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_013b:  nop
    IL_013c:  endfinally
  }
  IL_013d:  ret
}
")
        End Sub

        <Fact>
        Public Sub SynthesizedVariablesInLambdas1()
            Dim source =
            <compilation>
                <file name="a.vb">
Imports System.Collections
Imports System.Collections.Generic
Class C
    Function F() As System.IDisposable
        Return Nothing
    End Function
    Sub M()
        Using F()
            Dim g =
                Function()
                    Using F()
                        Return Nothing
                    End Using
                End Function
        End Using
    End Sub
End Class
    </file>
            </compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            v0.VerifyIL("C._Lambda$__2-0()", "
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Object V_0,
                System.IDisposable V_1)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""Function C.F() As System.IDisposable""
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldnull
    IL_000a:  stloc.0
    IL_000b:  leave.s    IL_0019
  }
  finally
  {
    IL_000d:  nop
    IL_000e:  ldloc.1
    IL_000f:  brfalse.s  IL_0018
    IL_0011:  ldloc.1
    IL_0012:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0017:  nop
    IL_0018:  endfinally
  }
  IL_0019:  ldloc.0
  IL_001a:  ret
}
")
            v0.VerifyPdb("C._Lambda$__2-0", "
<symbols>
  <files>
    <file id=""1"" name=""a.vb"" language=""3a12d0b8-c26c-11d0-b442-00a0244a1dd2"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""CB, 10, 23, 23, 67, CE, AD, BE, 85, D1, 57, F2, D2, CB, 12, A0,  4, 4F, 66, C7, "" />
  </files>
  <methods>
    <method containingType=""C"" name=""_Lambda$__2-0"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""-1"" />
          <slot kind=""4"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""17"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""21"" endLine=""11"" endColumn=""30"" document=""1"" />
        <entry offset=""0x9"" startLine=""12"" startColumn=""25"" endLine=""12"" endColumn=""39"" document=""1"" />
        <entry offset=""0xd"" startLine=""13"" startColumn=""21"" endLine=""13"" endColumn=""30"" document=""1"" />
        <entry offset=""0x19"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""29"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1b"">
        <importsforward declaringType=""C"" methodName=""F"" />
      </scope>
    </method>
  </methods>
</symbols>
")

#If TODO Then ' identify the lambda in a semantic edit 
            Dim debugInfoProvider = v0.CreatePdbInfoProvider()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C.M")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), Function(handle) debugInfoProvider.GetEncMethodDebugInfo(handle))

            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

            ' check that all user-defined and long-lived synthesized local slots are reused
            diff1.VerifyIL("C._Lambda$__1", "
")
#End If
        End Sub

        <Fact>
        Public Sub SynthesizedVariablesInIterator()
            Dim source =
            <compilation>
                <file name="a.vb">
Imports System.Collections
Imports System.Collections.Generic
Class C
    Function F() As System.IDisposable
        Return Nothing
    End Function

    Iterator Function M() As IEnumerable(Of Integer)
        Using F()
            Yield 1
        End Using
        Yield 2
    End Function
End Class
    </file>
            </compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            v0.VerifyIL("C.VB$StateMachine_2_M.MoveNext()", "
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001f,
        IL_0022,
        IL_0022,
        IL_0025)
  IL_001d:  br.s       IL_002b
  IL_001f:  nop
  IL_0020:  br.s       IL_002d
  IL_0022:  nop
  IL_0023:  br.s       IL_004a
  IL_0025:  nop
  IL_0026:  br         IL_00b9
  IL_002b:  ldc.i4.0
  IL_002c:  ret
  IL_002d:  ldarg.0
  IL_002e:  ldc.i4.m1
  IL_002f:  dup
  IL_0030:  stloc.1
  IL_0031:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_0036:  nop
  IL_0037:  nop
  IL_0038:  nop
  IL_0039:  ldarg.0
  IL_003a:  ldarg.0
  IL_003b:  ldfld      ""C.VB$StateMachine_2_M.$VB$Me As C""
  IL_0040:  callvirt   ""Function C.F() As System.IDisposable""
  IL_0045:  stfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
  IL_004a:  nop
  .try
  {
    IL_004b:  ldloc.1
    IL_004c:  ldc.i4.1
    IL_004d:  beq.s      IL_0057
    IL_004f:  br.s       IL_0051
    IL_0051:  ldloc.1
    IL_0052:  ldc.i4.2
    IL_0053:  beq.s      IL_005a
    IL_0055:  br.s       IL_005d
    IL_0057:  nop
    IL_0058:  br.s       IL_0080
    IL_005a:  nop
    IL_005b:  br.s       IL_005f
    IL_005d:  br.s       IL_006c
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.1
    IL_0063:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_0068:  ldc.i4.1
    IL_0069:  stloc.0
    IL_006a:  leave.s    IL_00c4
    IL_006c:  ldarg.0
    IL_006d:  ldc.i4.1
    IL_006e:  stfld      ""C.VB$StateMachine_2_M.$Current As Integer""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.1
    IL_0075:  dup
    IL_0076:  stloc.1
    IL_0077:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_007c:  ldc.i4.1
    IL_007d:  stloc.0
    IL_007e:  leave.s    IL_00c4
    IL_0080:  ldarg.0
    IL_0081:  ldc.i4.m1
    IL_0082:  dup
    IL_0083:  stloc.1
    IL_0084:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_0089:  leave.s    IL_00a7
  }
  finally
  {
    IL_008b:  ldloc.1
    IL_008c:  ldc.i4.0
    IL_008d:  bge.s      IL_00a6
    IL_008f:  nop
    IL_0090:  ldarg.0
    IL_0091:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
    IL_0096:  brfalse.s  IL_00a4
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
    IL_009e:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_00a3:  nop
    IL_00a4:  br.s       IL_00a6
    IL_00a6:  endfinally
  }
  IL_00a7:  ldarg.0
  IL_00a8:  ldc.i4.2
  IL_00a9:  stfld      ""C.VB$StateMachine_2_M.$Current As Integer""
  IL_00ae:  ldarg.0
  IL_00af:  ldc.i4.3
  IL_00b0:  dup
  IL_00b1:  stloc.1
  IL_00b2:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_00b7:  ldc.i4.1
  IL_00b8:  ret
  IL_00b9:  ldarg.0
  IL_00ba:  ldc.i4.m1
  IL_00bb:  dup
  IL_00bc:  stloc.1
  IL_00bd:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_00c2:  ldc.i4.0
  IL_00c3:  ret
  IL_00c4:  ldloc.0
  IL_00c5:  ret
}
")
            v0.VerifyPdb("C+VB$StateMachine_2_M.MoveNext", "
<symbols>
  <files>
    <file id=""1"" name=""a.vb"" language=""3a12d0b8-c26c-11d0-b442-00a0244a1dd2"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum="" E, DD, DB, BF, A5, 4D, 75, 50, 39, C6, 6C, D8, 6D, 49, 1B, 2A, 56, 79, F8, E8, "" />
  </files>
  <methods>
    <method containingType=""C+VB$StateMachine_2_M"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""20"" offset=""-1"" />
          <slot kind=""27"" offset=""-1"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x36"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""53"" document=""1"" />
        <entry offset=""0x37"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""53"" document=""1"" />
        <entry offset=""0x38"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x4a"" hidden=""true"" document=""1"" />
        <entry offset=""0x4b"" hidden=""true"" document=""1"" />
        <entry offset=""0x6c"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""20"" document=""1"" />
        <entry offset=""0x89"" hidden=""true"" document=""1"" />
        <entry offset=""0x8b"" hidden=""true"" document=""1"" />
        <entry offset=""0x8f"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""18"" document=""1"" />
        <entry offset=""0xa6"" hidden=""true"" document=""1"" />
        <entry offset=""0xa7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""16"" document=""1"" />
        <entry offset=""0xc2"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""17"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xc6"">
        <importsforward declaringType=""C"" methodName=""F"" />
      </scope>
    </method>
  </methods>
</symbols>
")

        End Sub

        <Fact>
        Public Sub SynthesizedVariablesInAsyncMethod()
            Dim source =
            <compilation>
                <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function F() As System.IDisposable
        Return Nothing
    End Function

    Async Function M() As Task(Of Integer)
        Using F()
        End Using
        Await Task.FromResult(10)
        Return 2
    End Function
End Class
                </file>
            </compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            v0.VerifyIL("C.VB$StateMachine_2_M.MoveNext()", "
{
  // Code size      235 (0xeb)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_2_M V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_007c
    IL_000f:  nop
    IL_0010:  nop
    IL_0011:  ldarg.0
    IL_0012:  ldarg.0
    IL_0013:  ldfld      ""C.VB$StateMachine_2_M.$VB$Me As C""
    IL_0018:  callvirt   ""Function C.F() As System.IDisposable""
    IL_001d:  stfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
    .try
    {
      IL_0022:  leave.s    IL_0040
    }
    finally
    {
      IL_0024:  ldloc.1
      IL_0025:  ldc.i4.0
      IL_0026:  bge.s      IL_003f
      IL_0028:  nop
      IL_0029:  ldarg.0
      IL_002a:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
      IL_002f:  brfalse.s  IL_003d
      IL_0031:  ldarg.0
      IL_0032:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
      IL_0037:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_003c:  nop
      IL_003d:  br.s       IL_003f
      IL_003f:  endfinally
    }
    IL_0040:  nop
    IL_0041:  ldc.i4.s   10
    IL_0043:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0048:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_004d:  stloc.3
    IL_004e:  ldloca.s   V_3
    IL_0050:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0055:  brtrue.s   IL_009a
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.0
    IL_0059:  dup
    IL_005a:  stloc.1
    IL_005b:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_0060:  ldarg.0
    IL_0061:  ldloc.3
    IL_0062:  stfld      ""C.VB$StateMachine_2_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""C.VB$StateMachine_2_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_006d:  ldloca.s   V_3
    IL_006f:  ldarg.0
    IL_0070:  stloc.s    V_4
    IL_0072:  ldloca.s   V_4
    IL_0074:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_2_M)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_2_M)""
    IL_0079:  nop
    IL_007a:  leave.s    IL_00ea
    IL_007c:  ldarg.0
    IL_007d:  ldc.i4.m1
    IL_007e:  dup
    IL_007f:  stloc.1
    IL_0080:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_0085:  ldarg.0
    IL_0086:  ldfld      ""C.VB$StateMachine_2_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_008b:  stloc.3
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""C.VB$StateMachine_2_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0092:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0098:  br.s       IL_009a
    IL_009a:  ldloca.s   V_3
    IL_009c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_00a1:  pop
    IL_00a2:  ldloca.s   V_3
    IL_00a4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00aa:  ldc.i4.2
    IL_00ab:  stloc.0
    IL_00ac:  leave.s    IL_00d3
  }
  catch System.Exception
  {
    IL_00ae:  dup
    IL_00af:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_00b4:  stloc.s    V_5
    IL_00b6:  ldarg.0
    IL_00b7:  ldc.i4.s   -2
    IL_00b9:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_00be:  ldarg.0
    IL_00bf:  ldflda     ""C.VB$StateMachine_2_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00c4:  ldloc.s    V_5
    IL_00c6:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00cb:  nop
    IL_00cc:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00d1:  leave.s    IL_00ea
  }
  IL_00d3:  ldarg.0
  IL_00d4:  ldc.i4.s   -2
  IL_00d6:  dup
  IL_00d7:  stloc.1
  IL_00d8:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_00dd:  ldarg.0
  IL_00de:  ldflda     ""C.VB$StateMachine_2_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00e3:  ldloc.0
  IL_00e4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00e9:  nop
  IL_00ea:  ret
}
")

            v0.VerifyPdb("C+VB$StateMachine_2_M.MoveNext", "
<symbols>
  <files>
    <file id=""1"" name=""a.vb"" language=""3a12d0b8-c26c-11d0-b442-00a0244a1dd2"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""30, DD, 7D, 76, D3, C3, 98, A6, 4F, 3D, 96, F9, 8C, 84, 5B, EC, EC, 10, 83, C7, "" />
  </files>
  <methods>
    <method containingType=""C+VB$StateMachine_2_M"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""20"" offset=""-1"" />
          <slot kind=""27"" offset=""-1"" />
          <slot kind=""0"" offset=""-1"" />
          <slot kind=""33"" offset=""38"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xf"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""43"" document=""1"" />
        <entry offset=""0x10"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""18"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x28"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x3f"" hidden=""true"" document=""1"" />
        <entry offset=""0x40"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""34"" document=""1"" />
        <entry offset=""0x4e"" hidden=""true"" document=""1"" />
        <entry offset=""0xaa"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""17"" document=""1"" />
        <entry offset=""0xae"" hidden=""true"" document=""1"" />
        <entry offset=""0xb6"" hidden=""true"" document=""1"" />
        <entry offset=""0xd3"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""17"" document=""1"" />
        <entry offset=""0xdd"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xeb"">
        <importsforward declaringType=""C"" methodName=""F"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x60"" resume=""0x7c"" declaringType=""C+VB$StateMachine_2_M"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
")

        End Sub

    End Class
End Namespace
