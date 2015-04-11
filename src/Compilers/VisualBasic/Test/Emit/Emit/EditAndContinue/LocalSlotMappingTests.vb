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
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()
        For index As Integer = 1 To 1
            Console.WriteLine(1)
        Next
        For index As Integer = 1 To 2
            Console.WriteLine(2)
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            v0.VerifyIL("C.M", "
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (Integer V_0,
                Integer V_1, //index
                Boolean V_2,
                Integer V_3,
                Integer V_4) //index
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.1
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0009:  nop
  IL_000a:  ldloc.1
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.1
  IL_0010:  cgt
  IL_0012:  ldc.i4.0
  IL_0013:  ceq
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  brtrue.s   IL_0003
  IL_0019:  ldc.i4.1
  IL_001a:  stloc.s    V_4
  IL_001c:  ldc.i4.2
  IL_001d:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0022:  nop
  IL_0023:  ldloc.s    V_4
  IL_0025:  ldc.i4.1
  IL_0026:  add.ovf
  IL_0027:  stloc.s    V_4
  IL_0029:  ldloc.s    V_4
  IL_002b:  ldc.i4.2
  IL_002c:  cgt
  IL_002e:  ldc.i4.0
  IL_002f:  ceq
  IL_0031:  stloc.2
  IL_0032:  ldloc.2
  IL_0033:  brtrue.s   IL_001c
  IL_0035:  ret
}
")
            v0.VerifyPdb("C.M", "
<symbols>
  <files>
    <file id=""1"" name=""a.vb"" language=""3a12d0b8-c26c-11d0-b442-00a0244a1dd2"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""9E,  5, 67, 8D, 19, 11, 32, B1, 10, EF, 60, 66, 68, 44, D1, 36, E9, 39, 7D, 4C, "" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""13"" offset=""0"" />
          <slot kind=""0"" offset=""4"" />
          <slot kind=""temp"" />
          <slot kind=""13"" offset=""87"" />
          <slot kind=""0"" offset=""91"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""3"" startColumn=""5"" endLine=""3"" endColumn=""12"" document=""1"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""9"" endLine=""4"" endColumn=""38"" document=""1"" />
        <entry offset=""0x3"" startLine=""5"" startColumn=""13"" endLine=""5"" endColumn=""33"" document=""1"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""13"" document=""1"" />
        <entry offset=""0xe"" hidden=""true"" document=""1"" />
        <entry offset=""0x19"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""38"" document=""1"" />
        <entry offset=""0x1c"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""33"" document=""1"" />
        <entry offset=""0x23"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""13"" document=""1"" />
        <entry offset=""0x29"" hidden=""true"" document=""1"" />
        <entry offset=""0x35"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""12"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x36"">
        <namespace name=""System"" importlevel=""file"" />
        <currentnamespace name="""" />
        <scope startOffset=""0x1"" endOffset=""0x18"">
          <local name=""index"" il_index=""1"" il_start=""0x1"" il_end=""0x18"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x19"" endOffset=""0x34"">
          <local name=""index"" il_index=""4"" il_start=""0x19"" il_end=""0x34"" attributes=""0"" />
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
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

            ' check that all user-defined and long-lived synthesized local slots are reused
            diff1.VerifyIL("C.M", "
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init ([int] V_0,
                Integer V_1, //index
                [bool] V_2,
                [int] V_3,
                Integer V_4, //index
                Integer V_5,
                Boolean V_6,
                Integer V_7)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.1
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0009:  nop
  IL_000a:  ldloc.1
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.1
  IL_0010:  cgt
  IL_0012:  ldc.i4.0
  IL_0013:  ceq
  IL_0015:  stloc.s    V_6
  IL_0017:  ldloc.s    V_6
  IL_0019:  brtrue.s   IL_0003
  IL_001b:  ldc.i4.1
  IL_001c:  stloc.s    V_4
  IL_001e:  ldc.i4.2
  IL_001f:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0024:  nop
  IL_0025:  ldloc.s    V_4
  IL_0027:  ldc.i4.1
  IL_0028:  add.ovf
  IL_0029:  stloc.s    V_4
  IL_002b:  ldloc.s    V_4
  IL_002d:  ldc.i4.2
  IL_002e:  cgt
  IL_0030:  ldc.i4.0
  IL_0031:  ceq
  IL_0033:  stloc.s    V_6
  IL_0035:  ldloc.s    V_6
  IL_0037:  brtrue.s   IL_001e
  IL_0039:  ret
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

            Dim debug = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim release = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.ReleaseDll)

            CompileAndVerify(debug).VerifyPdb("C.M", "
<symbols>
  <files>
    <file id=""1"" name=""a.vb"" language=""3a12d0b8-c26c-11d0-b442-00a0244a1dd2"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""B1, 88, 10, 98, B9, 30, FE, B8, AD, 46, 3F,  5, 46, 9B, AF, A9, 4F, CB, 65, B1, "" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""4"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""12"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""18"" document=""1"" />
        <entry offset=""0x9"" hidden=""true"" document=""1"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x1c"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""12"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1d"">
        <importsforward declaringType=""C"" methodName=""F"" />
      </scope>
    </method>
  </methods>
</symbols>
")

            CompileAndVerify(release).VerifyPdb("C.M", "
<symbols>
  <files>
    <file id=""1"" name=""a.vb"" language=""3a12d0b8-c26c-11d0-b442-00a0244a1dd2"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""B1, 88, 10, 98, B9, 30, FE, B8, AD, 46, 3F,  5, 46, 9B, AF, A9, 4F, CB, 65, B1, "" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""18"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x9"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x13"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""12"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <importsforward declaringType=""C"" methodName=""F"" />
      </scope>
    </method>
  </methods>
</symbols>
")

        End Sub

        <Fact>
        Public Sub ForEach()
            Dim source =
<compilation>
    <file name="a.vb">
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
        For Each x In F1()
            For Each y As Object In F2()
            Next
        Next
        For Each x In F4()
            For Each y In F3()
            Next
            For Each z In F2()
            Next
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source)

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
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size      339 (0x153)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [object] V_1,
                [unchanged] V_2,
                Object V_3, //y
                [bool] V_4,
                [unchanged] V_5,
                [object] V_6,
                [unchanged] V_7,
                [object] V_8,
                [unchanged] V_9,
                [object] V_10,
                System.Collections.IEnumerator V_11,
                Object V_12, //x
                System.Collections.Generic.List(Of Object).Enumerator V_13,
                Boolean V_14,
                System.Collections.Generic.List(Of Object).Enumerator V_15,
                Object V_16, //x
                System.Collections.IEnumerator V_17,
                Object V_18, //y
                System.Collections.Generic.List(Of Object).Enumerator V_19,
                Object V_20) //z
  IL_0000:  nop
  .try
  {
    IL_0001:  ldarg.0
    IL_0002:  call       ""Function C.F1() As System.Collections.IEnumerable""
    IL_0007:  callvirt   ""Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator""
    IL_000c:  stloc.s    V_11
    IL_000e:  br.s       IL_005a
    IL_0010:  ldloc.s    V_11
    IL_0012:  callvirt   ""Function System.Collections.IEnumerator.get_Current() As Object""
    IL_0017:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_001c:  stloc.s    V_12
    .try
    {
      IL_001e:  ldarg.0
      IL_001f:  call       ""Function C.F2() As System.Collections.Generic.List(Of Object)""
      IL_0024:  callvirt   ""Function System.Collections.Generic.List(Of Object).GetEnumerator() As System.Collections.Generic.List(Of Object).Enumerator""
      IL_0029:  stloc.s    V_13
      IL_002b:  br.s       IL_003b
      IL_002d:  ldloca.s   V_13
      IL_002f:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.get_Current() As Object""
      IL_0034:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
      IL_0039:  stloc.3
      IL_003a:  nop
      IL_003b:  ldloca.s   V_13
      IL_003d:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.MoveNext() As Boolean""
      IL_0042:  stloc.s    V_14
      IL_0044:  ldloc.s    V_14
      IL_0046:  brtrue.s   IL_002d
      IL_0048:  leave.s    IL_0059
    }
    finally
    {
      IL_004a:  ldloca.s   V_13
      IL_004c:  constrained. ""System.Collections.Generic.List(Of Object).Enumerator""
      IL_0052:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_0057:  nop
      IL_0058:  endfinally
    }
    IL_0059:  nop
    IL_005a:  ldloc.s    V_11
    IL_005c:  callvirt   ""Function System.Collections.IEnumerator.MoveNext() As Boolean""
    IL_0061:  stloc.s    V_14
    IL_0063:  ldloc.s    V_14
    IL_0065:  brtrue.s   IL_0010
    IL_0067:  leave.s    IL_0087
  }
  finally
  {
    IL_0069:  ldloc.s    V_11
    IL_006b:  isinst     ""System.IDisposable""
    IL_0070:  ldnull
    IL_0071:  ceq
    IL_0073:  stloc.s    V_14
    IL_0075:  ldloc.s    V_14
    IL_0077:  brtrue.s   IL_0086
    IL_0079:  ldloc.s    V_11
    IL_007b:  isinst     ""System.IDisposable""
    IL_0080:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0085:  nop
    IL_0086:  endfinally
  }
  IL_0087:  nop
  .try
  {
    IL_0088:  ldarg.0
    IL_0089:  call       ""Function C.F4() As System.Collections.Generic.List(Of Object)""
    IL_008e:  callvirt   ""Function System.Collections.Generic.List(Of Object).GetEnumerator() As System.Collections.Generic.List(Of Object).Enumerator""
    IL_0093:  stloc.s    V_15
    IL_0095:  br         IL_0131
    IL_009a:  ldloca.s   V_15
    IL_009c:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.get_Current() As Object""
    IL_00a1:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_00a6:  stloc.s    V_16
    .try
    {
      IL_00a8:  ldarg.0
      IL_00a9:  call       ""Function C.F3() As System.Collections.IEnumerable""
      IL_00ae:  callvirt   ""Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator""
      IL_00b3:  stloc.s    V_17
      IL_00b5:  br.s       IL_00c6
      IL_00b7:  ldloc.s    V_17
      IL_00b9:  callvirt   ""Function System.Collections.IEnumerator.get_Current() As Object""
      IL_00be:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
      IL_00c3:  stloc.s    V_18
      IL_00c5:  nop
      IL_00c6:  ldloc.s    V_17
      IL_00c8:  callvirt   ""Function System.Collections.IEnumerator.MoveNext() As Boolean""
      IL_00cd:  stloc.s    V_14
      IL_00cf:  ldloc.s    V_14
      IL_00d1:  brtrue.s   IL_00b7
      IL_00d3:  leave.s    IL_00f3
    }
    finally
    {
      IL_00d5:  ldloc.s    V_17
      IL_00d7:  isinst     ""System.IDisposable""
      IL_00dc:  ldnull
      IL_00dd:  ceq
      IL_00df:  stloc.s    V_14
      IL_00e1:  ldloc.s    V_14
      IL_00e3:  brtrue.s   IL_00f2
      IL_00e5:  ldloc.s    V_17
      IL_00e7:  isinst     ""System.IDisposable""
      IL_00ec:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_00f1:  nop
      IL_00f2:  endfinally
    }
    IL_00f3:  nop
    .try
    {
      IL_00f4:  ldarg.0
      IL_00f5:  call       ""Function C.F2() As System.Collections.Generic.List(Of Object)""
      IL_00fa:  callvirt   ""Function System.Collections.Generic.List(Of Object).GetEnumerator() As System.Collections.Generic.List(Of Object).Enumerator""
      IL_00ff:  stloc.s    V_19
      IL_0101:  br.s       IL_0112
      IL_0103:  ldloca.s   V_19
      IL_0105:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.get_Current() As Object""
      IL_010a:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
      IL_010f:  stloc.s    V_20
      IL_0111:  nop
      IL_0112:  ldloca.s   V_19
      IL_0114:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.MoveNext() As Boolean""
      IL_0119:  stloc.s    V_14
      IL_011b:  ldloc.s    V_14
      IL_011d:  brtrue.s   IL_0103
      IL_011f:  leave.s    IL_0130
    }
    finally
    {
      IL_0121:  ldloca.s   V_19
      IL_0123:  constrained. ""System.Collections.Generic.List(Of Object).Enumerator""
      IL_0129:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_012e:  nop
      IL_012f:  endfinally
    }
    IL_0130:  nop
    IL_0131:  ldloca.s   V_15
    IL_0133:  call       ""Function System.Collections.Generic.List(Of Object).Enumerator.MoveNext() As Boolean""
    IL_0138:  stloc.s    V_14
    IL_013a:  ldloc.s    V_14
    IL_013c:  brtrue     IL_009a
    IL_0141:  leave.s    IL_0152
  }
  finally
  {
    IL_0143:  ldloca.s   V_15
    IL_0145:  constrained. ""System.Collections.Generic.List(Of Object).Enumerator""
    IL_014b:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0150:  nop
    IL_0151:  endfinally
  }
  IL_0152:  ret
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
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (Object V_0,
                System.IDisposable V_1,
                Boolean V_2)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""Function C.F() As System.IDisposable""
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldnull
    IL_000a:  stloc.0
    IL_000b:  leave.s    IL_001e
  }
  finally
  {
    IL_000d:  nop
    IL_000e:  ldloc.1
    IL_000f:  ldnull
    IL_0010:  ceq
    IL_0012:  stloc.2
    IL_0013:  ldloc.2
    IL_0014:  brtrue.s   IL_001d
    IL_0016:  ldloc.1
    IL_0017:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_001c:  nop
    IL_001d:  endfinally
  }
  IL_001e:  ldloc.0
  IL_001f:  ret
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
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""17"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""21"" endLine=""11"" endColumn=""30"" document=""1"" />
        <entry offset=""0x9"" startLine=""12"" startColumn=""25"" endLine=""12"" endColumn=""39"" document=""1"" />
        <entry offset=""0xd"" startLine=""13"" startColumn=""21"" endLine=""13"" endColumn=""30"" document=""1"" />
        <entry offset=""0x1e"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""29"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x20"">
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
  // Code size      210 (0xd2)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
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
  IL_0026:  br         IL_00c5
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
    IL_006a:  leave.s    IL_00d0
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
    IL_007e:  leave.s    IL_00d0
    IL_0080:  ldarg.0
    IL_0081:  ldc.i4.m1
    IL_0082:  dup
    IL_0083:  stloc.1
    IL_0084:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_0089:  leave.s    IL_00b3
  }
  finally
  {
    IL_008b:  ldloc.1
    IL_008c:  ldc.i4.0
    IL_008d:  clt
    IL_008f:  ldc.i4.0
    IL_0090:  ceq
    IL_0092:  stloc.2
    IL_0093:  ldloc.2
    IL_0094:  brtrue.s   IL_00b2
    IL_0096:  nop
    IL_0097:  ldarg.0
    IL_0098:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
    IL_009d:  ldnull
    IL_009e:  ceq
    IL_00a0:  stloc.2
    IL_00a1:  ldloc.2
    IL_00a2:  brtrue.s   IL_00b0
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
    IL_00aa:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_00af:  nop
    IL_00b0:  br.s       IL_00b2
    IL_00b2:  endfinally
  }
  IL_00b3:  ldarg.0
  IL_00b4:  ldc.i4.2
  IL_00b5:  stfld      ""C.VB$StateMachine_2_M.$Current As Integer""
  IL_00ba:  ldarg.0
  IL_00bb:  ldc.i4.3
  IL_00bc:  dup
  IL_00bd:  stloc.1
  IL_00be:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_00c3:  ldc.i4.1
  IL_00c4:  ret
  IL_00c5:  ldarg.0
  IL_00c6:  ldc.i4.m1
  IL_00c7:  dup
  IL_00c8:  stloc.1
  IL_00c9:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_00ce:  ldc.i4.0
  IL_00cf:  ret
  IL_00d0:  ldloc.0
  IL_00d1:  ret
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
          <slot kind=""temp"" />
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
        <entry offset=""0x96"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""18"" document=""1"" />
        <entry offset=""0xb2"" hidden=""true"" document=""1"" />
        <entry offset=""0xb3"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""16"" document=""1"" />
        <entry offset=""0xce"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""17"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xd2"">
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
  // Code size      252 (0xfc)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                Boolean V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                C.VB$StateMachine_2_M V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_008c
    IL_000f:  nop
    IL_0010:  nop
    IL_0011:  ldarg.0
    IL_0012:  ldarg.0
    IL_0013:  ldfld      ""C.VB$StateMachine_2_M.$VB$Me As C""
    IL_0018:  callvirt   ""Function C.F() As System.IDisposable""
    IL_001d:  stfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
    .try
    {
      IL_0022:  leave.s    IL_004c
    }
    finally
    {
      IL_0024:  ldloc.1
      IL_0025:  ldc.i4.0
      IL_0026:  clt
      IL_0028:  ldc.i4.0
      IL_0029:  ceq
      IL_002b:  stloc.3
      IL_002c:  ldloc.3
      IL_002d:  brtrue.s   IL_004b
      IL_002f:  nop
      IL_0030:  ldarg.0
      IL_0031:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
      IL_0036:  ldnull
      IL_0037:  ceq
      IL_0039:  stloc.3
      IL_003a:  ldloc.3
      IL_003b:  brtrue.s   IL_0049
      IL_003d:  ldarg.0
      IL_003e:  ldfld      ""C.VB$StateMachine_2_M.$S0 As System.IDisposable""
      IL_0043:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_0048:  nop
      IL_0049:  br.s       IL_004b
      IL_004b:  endfinally
    }
    IL_004c:  nop
    IL_004d:  ldc.i4.s   10
    IL_004f:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0054:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0059:  stloc.s    V_4
    IL_005b:  ldloca.s   V_4
    IL_005d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0062:  stloc.3
    IL_0063:  ldloc.3
    IL_0064:  brtrue.s   IL_00ab
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.0
    IL_0068:  dup
    IL_0069:  stloc.1
    IL_006a:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_006f:  ldarg.0
    IL_0070:  ldloc.s    V_4
    IL_0072:  stfld      ""C.VB$StateMachine_2_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""C.VB$StateMachine_2_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_007d:  ldloca.s   V_4
    IL_007f:  ldarg.0
    IL_0080:  stloc.s    V_5
    IL_0082:  ldloca.s   V_5
    IL_0084:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_2_M)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_2_M)""
    IL_0089:  nop
    IL_008a:  leave.s    IL_00fb
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.m1
    IL_008e:  dup
    IL_008f:  stloc.1
    IL_0090:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_0095:  ldarg.0
    IL_0096:  ldfld      ""C.VB$StateMachine_2_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_009b:  stloc.s    V_4
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""C.VB$StateMachine_2_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00a3:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00a9:  br.s       IL_00ab
    IL_00ab:  ldloca.s   V_4
    IL_00ad:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_00b2:  pop
    IL_00b3:  ldloca.s   V_4
    IL_00b5:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00bb:  ldc.i4.2
    IL_00bc:  stloc.0
    IL_00bd:  leave.s    IL_00e4
  }
  catch System.Exception
  {
    IL_00bf:  dup
    IL_00c0:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_00c5:  stloc.s    V_6
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.s   -2
    IL_00ca:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
    IL_00cf:  ldarg.0
    IL_00d0:  ldflda     ""C.VB$StateMachine_2_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00d5:  ldloc.s    V_6
    IL_00d7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00dc:  nop
    IL_00dd:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00e2:  leave.s    IL_00fb
  }
  IL_00e4:  ldarg.0
  IL_00e5:  ldc.i4.s   -2
  IL_00e7:  dup
  IL_00e8:  stloc.1
  IL_00e9:  stfld      ""C.VB$StateMachine_2_M.$State As Integer""
  IL_00ee:  ldarg.0
  IL_00ef:  ldflda     ""C.VB$StateMachine_2_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00f4:  ldloc.0
  IL_00f5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00fa:  nop
  IL_00fb:  ret
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
          <slot kind=""temp"" />
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
        <entry offset=""0x2f"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x4b"" hidden=""true"" document=""1"" />
        <entry offset=""0x4c"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""34"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0xbb"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""17"" document=""1"" />
        <entry offset=""0xbf"" hidden=""true"" document=""1"" />
        <entry offset=""0xc7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe4"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""17"" document=""1"" />
        <entry offset=""0xee"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xfc"">
        <importsforward declaringType=""C"" methodName=""F"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x6f"" resume=""0x8c"" declaringType=""C+VB$StateMachine_2_M"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
")

        End Sub

    End Class
End Namespace
