' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.CodeGen
    Public Class WinRTCollectionTests
        Inherits BasicTestBase

        Private _legacyRefs As MetadataReference() = Nothing

        Public ReadOnly Property LegacyRefs As MetadataReference()
            Get
                If _legacyRefs Is Nothing Then
                    Dim listRefs = New List(Of MetadataReference)(WinRtRefs.Length + 2)
                    listRefs.AddRange(WinRtRefs)
                    listRefs.Add(AssemblyMetadata.CreateFromImage(TestResources.WinRt.Windows_Languages_WinRTTest).GetReference(display:="WinRTTest"))
                    listRefs.Add(AssemblyMetadata.CreateFromImage(TestMetadata.ResourcesNet451.SystemCore).GetReference(display:="SystemCore"))
                    _legacyRefs = listRefs.ToArray()
                End If
                Return _legacyRefs
            End Get
        End Property

        <Fact, WorkItem(762316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762316")>
        Public Sub InheritFromTypeWithProjections()
            Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
Imports Windows.UI.Xaml
 
Public NotInheritable Class BehaviorCollection 
    Inherits DependencyObjectCollection
 
    Private c As Integer
 
    Public Sub BehaviorCollection()
        c = Me.Count
    End Sub
  
    Public Function GetItem(i As Integer) As Object
        Return Me(i)
    End Function
End Class]]></file>
            </compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(source, WinRtRefs)
            comp.AssertNoDiagnostics()
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.WinRTNeedsWindowsDesktop)>
        Public Sub IVectorProjectionTests()
            Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
Imports System
Imports Windows.Data.Json

Public Class A
    Shared Sub Main()
        Dim jsonArray = New JsonArray()
        Dim a = JsonValue.CreateStringValue("a")
        jsonArray.Add(a)
        Dim b = JsonValue.CreateStringValue("b")
        jsonArray.Insert(0, b)
        jsonArray.Remove(b)
        Console.WriteLine(jsonArray.Contains(b))
        Console.WriteLine(jsonArray.IndexOf(a))
        jsonArray.RemoveAt(0)
        Console.WriteLine(jsonArray.Count)
        jsonArray.Add(b)
        For Each json In jsonArray
            Console.WriteLine(json.GetString())        
        Next
        Console.WriteLine(jsonArray.Count)
        jsonArray.Clear()
        Console.WriteLine(jsonArray.Count)
    End Sub
End Class]]></file>
            </compilation>

            Dim expectedOutput =
            <![CDATA[
False
0
0
b
1
0
]]>


            Dim verifier = CompileAndVerifyOnWin8Only(
                source,
                expectedOutput,
                allReferences:=WinRtRefs)

            verifier.VerifyIL("A.Main", <![CDATA[
{
  // Code size      174 (0xae)
  .maxstack  3
  .locals init (Windows.Data.Json.JsonArray V_0, //jsonArray
  Windows.Data.Json.JsonValue V_1, //a
  Windows.Data.Json.JsonValue V_2, //b
  System.Collections.Generic.IEnumerator(Of Windows.Data.Json.IJsonValue) V_3)
  IL_0000:  newobj     "Sub Windows.Data.Json.JsonArray..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldstr      "a"
  IL_000b:  call       "Function Windows.Data.Json.JsonValue.CreateStringValue(String) As Windows.Data.Json.JsonValue"
  IL_0010:  stloc.1
  IL_0011:  ldloc.0
  IL_0012:  ldloc.1
  IL_0013:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).Add(Windows.Data.Json.IJsonValue)"
  IL_0018:  ldstr      "b"
  IL_001d:  call       "Function Windows.Data.Json.JsonValue.CreateStringValue(String) As Windows.Data.Json.JsonValue"
  IL_0022:  stloc.2
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldloc.2
  IL_0026:  callvirt   "Sub System.Collections.Generic.IList(Of Windows.Data.Json.IJsonValue).Insert(Integer, Windows.Data.Json.IJsonValue)"
  IL_002b:  ldloc.0
  IL_002c:  ldloc.2
  IL_002d:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).Remove(Windows.Data.Json.IJsonValue) As Boolean"
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldloc.2
  IL_0035:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).Contains(Windows.Data.Json.IJsonValue) As Boolean"
  IL_003a:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_003f:  ldloc.0
  IL_0040:  ldloc.1
  IL_0041:  callvirt   "Function System.Collections.Generic.IList(Of Windows.Data.Json.IJsonValue).IndexOf(Windows.Data.Json.IJsonValue) As Integer"
  IL_0046:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004b:  ldloc.0
  IL_004c:  ldc.i4.0
  IL_004d:  callvirt   "Sub System.Collections.Generic.IList(Of Windows.Data.Json.IJsonValue).RemoveAt(Integer)"
  IL_0052:  ldloc.0
  IL_0053:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).get_Count() As Integer"
  IL_0058:  call       "Sub System.Console.WriteLine(Integer)"
  IL_005d:  ldloc.0
  IL_005e:  ldloc.2
  IL_005f:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).Add(Windows.Data.Json.IJsonValue)"
  .try
{
  IL_0064:  ldloc.0
  IL_0065:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Windows.Data.Json.IJsonValue).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Windows.Data.Json.IJsonValue)"
  IL_006a:  stloc.3
  IL_006b:  br.s       IL_007d
  IL_006d:  ldloc.3
  IL_006e:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Windows.Data.Json.IJsonValue).get_Current() As Windows.Data.Json.IJsonValue"
  IL_0073:  callvirt   "Function Windows.Data.Json.IJsonValue.GetString() As String"
  IL_0078:  call       "Sub System.Console.WriteLine(String)"
  IL_007d:  ldloc.3
  IL_007e:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0083:  brtrue.s   IL_006d
  IL_0085:  leave.s    IL_0091
}
  finally
{
  IL_0087:  ldloc.3
  IL_0088:  brfalse.s  IL_0090
  IL_008a:  ldloc.3
  IL_008b:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0090:  endfinally
}
  IL_0091:  ldloc.0
  IL_0092:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).get_Count() As Integer"
  IL_0097:  call       "Sub System.Console.WriteLine(Integer)"
  IL_009c:  ldloc.0
  IL_009d:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).Clear()"
  IL_00a2:  ldloc.0
  IL_00a3:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).get_Count() As Integer"
  IL_00a8:  call       "Sub System.Console.WriteLine(Integer)"
  IL_00ad:  ret
}
]]>.Value)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.WinRTNeedsWindowsDesktop)>
        Public Sub IVectorViewProjectionTests()

            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System
Imports Windows.Foundation

Public Class A
    Public Shared Sub Main()
        Dim results = New WwwFormUrlDecoder("?param1=test")
        Console.WriteLine(results(0).Name + results(0).Value)
    End Sub
End Class]]></file>
            </compilation>

            Dim expectedOut = "param1test"
            Dim verifier = CompileAndVerifyOnWin8Only(
                source,
                expectedOutput:=expectedOut,
                allReferences:=WinRtRefs)

            verifier.VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (Windows.Foundation.WwwFormUrlDecoder V_0) //results
  IL_0000:  ldstr      "?param1=test"
  IL_0005:  newobj     "Sub Windows.Foundation.WwwFormUrlDecoder..ctor(String)"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  callvirt   "Function System.Collections.Generic.IReadOnlyList(Of Windows.Foundation.IWwwFormUrlDecoderEntry).get_Item(Integer) As Windows.Foundation.IWwwFormUrlDecoderEntry"
  IL_0012:  callvirt   "Function Windows.Foundation.IWwwFormUrlDecoderEntry.get_Name() As String"
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4.0
  IL_0019:  callvirt   "Function System.Collections.Generic.IReadOnlyList(Of Windows.Foundation.IWwwFormUrlDecoderEntry).get_Item(Integer) As Windows.Foundation.IWwwFormUrlDecoderEntry"
  IL_001e:  callvirt   "Function Windows.Foundation.IWwwFormUrlDecoderEntry.get_Value() As String"
  IL_0023:  call       "Function String.Concat(String, String) As String"
  IL_0028:  call       "Sub System.Console.WriteLine(String)"
  IL_002d:  ret
}]]>.Value)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.WinRTNeedsWindowsDesktop)>
        Public Sub IVectorLinqQueryTest()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Windows.Data.Json

Class A
    Shared Sub Main()
        Dim j = New JsonArray()
        j.Add(JsonValue.CreateStringValue("include"))
        Dim s = From i In j Where i.GetString() = "include" Select i
        System.Console.WriteLine(s.Count)
    End Sub
End Class                   
                    </file>
                </compilation>

            Dim output = "1"

            Dim comp = CompileAndVerifyOnWin8Only(source,
                                                  expectedOutput:=output,
                                                  references:=LegacyRefs)
            comp.VerifyIL("A.Main", <![CDATA[
{
  // Code size      114 (0x72)
  .maxstack  3
  IL_0000:  newobj     "Sub Windows.Data.Json.JsonArray..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "include"
  IL_000b:  call       "Function Windows.Data.Json.JsonValue.CreateStringValue(String) As Windows.Data.Json.JsonValue"
  IL_0010:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Data.Json.IJsonValue).Add(Windows.Data.Json.IJsonValue)"
  IL_0015:  ldsfld     "A._Closure$__.$I1-0 As System.Func(Of Windows.Data.Json.IJsonValue, Boolean)"
  IL_001a:  brfalse.s  IL_0023
  IL_001c:  ldsfld     "A._Closure$__.$I1-0 As System.Func(Of Windows.Data.Json.IJsonValue, Boolean)"
  IL_0021:  br.s       IL_0039
  IL_0023:  ldsfld     "A._Closure$__.$I As A._Closure$__"
  IL_0028:  ldftn      "Function A._Closure$__._Lambda$__1-0(Windows.Data.Json.IJsonValue) As Boolean"
  IL_002e:  newobj     "Sub System.Func(Of Windows.Data.Json.IJsonValue, Boolean)..ctor(Object, System.IntPtr)"
  IL_0033:  dup
  IL_0034:  stsfld     "A._Closure$__.$I1-0 As System.Func(Of Windows.Data.Json.IJsonValue, Boolean)"
  IL_0039:  call       "Function System.Linq.Enumerable.Where(Of Windows.Data.Json.IJsonValue)(System.Collections.Generic.IEnumerable(Of Windows.Data.Json.IJsonValue), System.Func(Of Windows.Data.Json.IJsonValue, Boolean)) As System.Collections.Generic.IEnumerable(Of Windows.Data.Json.IJsonValue)"
  IL_003e:  ldsfld     "A._Closure$__.$I1-1 As System.Func(Of Windows.Data.Json.IJsonValue, Windows.Data.Json.IJsonValue)"
  IL_0043:  brfalse.s  IL_004c
  IL_0045:  ldsfld     "A._Closure$__.$I1-1 As System.Func(Of Windows.Data.Json.IJsonValue, Windows.Data.Json.IJsonValue)"
  IL_004a:  br.s       IL_0062
  IL_004c:  ldsfld     "A._Closure$__.$I As A._Closure$__"
  IL_0051:  ldftn      "Function A._Closure$__._Lambda$__1-1(Windows.Data.Json.IJsonValue) As Windows.Data.Json.IJsonValue"
  IL_0057:  newobj     "Sub System.Func(Of Windows.Data.Json.IJsonValue, Windows.Data.Json.IJsonValue)..ctor(Object, System.IntPtr)"
  IL_005c:  dup
  IL_005d:  stsfld     "A._Closure$__.$I1-1 As System.Func(Of Windows.Data.Json.IJsonValue, Windows.Data.Json.IJsonValue)"
  IL_0062:  call       "Function System.Linq.Enumerable.Select(Of Windows.Data.Json.IJsonValue, Windows.Data.Json.IJsonValue)(System.Collections.Generic.IEnumerable(Of Windows.Data.Json.IJsonValue), System.Func(Of Windows.Data.Json.IJsonValue, Windows.Data.Json.IJsonValue)) As System.Collections.Generic.IEnumerable(Of Windows.Data.Json.IJsonValue)"
  IL_0067:  call       "Function System.Linq.Enumerable.Count(Of Windows.Data.Json.IJsonValue)(System.Collections.Generic.IEnumerable(Of Windows.Data.Json.IJsonValue)) As Integer"
  IL_006c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0071:  ret
}
]]>.Value)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.WinRTNeedsWindowsDesktop)>
        Public Sub IMapProjectionTests()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System
Imports System.Collections.Generic
Imports Windows.ApplicationModel.DataTransfer

Public Class A
    Public Shared Sub Main()
        Dim dataPackage = New DataPackage()
        Dim dpps = dataPackage.Properties
        dpps.Add(New KeyValuePair(Of String, Object)("testKey1", "testValue1"))
        Console.Out.WriteLine(dpps.ContainsKey("testKey1"))
        Console.Out.WriteLine(dpps.Item("testKey1"))
        dpps.Add("testKey2", "testValue2")
        Dim tv2 As Object = Nothing
        dpps.TryGetValue("testKey2", tv2)
        Console.Out.WriteLine(tv2)
        dpps.Item("testKey2") = "testValue3"
        dpps.Remove("testKey1")
        Dim valsEnumerator = dpps.Values.GetEnumerator()
        Dim keysEnumerator = dpps.Keys.GetEnumerator()
        While keysEnumerator.MoveNext() And valsEnumerator.MoveNext()
            Console.Out.WriteLine(keysEnumerator.Current & valsEnumerator.Current.ToString())
        End While
    End Sub
End Class]]>
                </file>
            </compilation>

            Dim expectedOut =
            <![CDATA[True
testValue1
testValue2
testKey2testValue3]]>

            Dim verifier = CompileAndVerifyOnWin8Only(
                source,
                expectedOutput:=expectedOut,
                allReferences:=WinRtRefs)

            verifier.VerifyIL("A.Main", <![CDATA[
{
  // Code size      229 (0xe5)
  .maxstack  3
  .locals init (Windows.ApplicationModel.DataTransfer.DataPackagePropertySet V_0, //dpps
  Object V_1, //tv2
  System.Collections.Generic.IEnumerator(Of Object) V_2, //valsEnumerator
  System.Collections.Generic.IEnumerator(Of String) V_3) //keysEnumerator
  IL_0000:  newobj     "Sub Windows.ApplicationModel.DataTransfer.DataPackage..ctor()"
  IL_0005:  callvirt   "Function Windows.ApplicationModel.DataTransfer.DataPackage.get_Properties() As Windows.ApplicationModel.DataTransfer.DataPackagePropertySet"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldstr      "testKey1"
  IL_0011:  ldstr      "testValue1"
  IL_0016:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of String, Object)..ctor(String, Object)"
  IL_001b:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, Object)).Add(System.Collections.Generic.KeyValuePair(Of String, Object))"
  IL_0020:  call       "Function System.Console.get_Out() As System.IO.TextWriter"
  IL_0025:  ldloc.0
  IL_0026:  ldstr      "testKey1"
  IL_002b:  callvirt   "Function System.Collections.Generic.IDictionary(Of String, Object).ContainsKey(String) As Boolean"
  IL_0030:  callvirt   "Sub System.IO.TextWriter.WriteLine(Boolean)"
  IL_0035:  call       "Function System.Console.get_Out() As System.IO.TextWriter"
  IL_003a:  ldloc.0
  IL_003b:  ldstr      "testKey1"
  IL_0040:  callvirt   "Function System.Collections.Generic.IDictionary(Of String, Object).get_Item(String) As Object"
  IL_0045:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004a:  callvirt   "Sub System.IO.TextWriter.WriteLine(Object)"
  IL_004f:  ldloc.0
  IL_0050:  ldstr      "testKey2"
  IL_0055:  ldstr      "testValue2"
  IL_005a:  callvirt   "Sub System.Collections.Generic.IDictionary(Of String, Object).Add(String, Object)"
  IL_005f:  ldnull
  IL_0060:  stloc.1
  IL_0061:  ldloc.0
  IL_0062:  ldstr      "testKey2"
  IL_0067:  ldloca.s   V_1
  IL_0069:  callvirt   "Function System.Collections.Generic.IDictionary(Of String, Object).TryGetValue(String, ByRef Object) As Boolean"
  IL_006e:  pop
  IL_006f:  call       "Function System.Console.get_Out() As System.IO.TextWriter"
  IL_0074:  ldloc.1
  IL_0075:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_007a:  callvirt   "Sub System.IO.TextWriter.WriteLine(Object)"
  IL_007f:  ldloc.0
  IL_0080:  ldstr      "testKey2"
  IL_0085:  ldstr      "testValue3"
  IL_008a:  callvirt   "Sub System.Collections.Generic.IDictionary(Of String, Object).set_Item(String, Object)"
  IL_008f:  ldloc.0
  IL_0090:  ldstr      "testKey1"
  IL_0095:  callvirt   "Function System.Collections.Generic.IDictionary(Of String, Object).Remove(String) As Boolean"
  IL_009a:  pop
  IL_009b:  ldloc.0
  IL_009c:  callvirt   "Function System.Collections.Generic.IDictionary(Of String, Object).get_Values() As System.Collections.Generic.ICollection(Of Object)"
  IL_00a1:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Object).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Object)"
  IL_00a6:  stloc.2
  IL_00a7:  ldloc.0
  IL_00a8:  callvirt   "Function System.Collections.Generic.IDictionary(Of String, Object).get_Keys() As System.Collections.Generic.ICollection(Of String)"
  IL_00ad:  callvirt   "Function System.Collections.Generic.IEnumerable(Of String).GetEnumerator() As System.Collections.Generic.IEnumerator(Of String)"
  IL_00b2:  stloc.3
  IL_00b3:  br.s       IL_00d5
  IL_00b5:  call       "Function System.Console.get_Out() As System.IO.TextWriter"
  IL_00ba:  ldloc.3
  IL_00bb:  callvirt   "Function System.Collections.Generic.IEnumerator(Of String).get_Current() As String"
  IL_00c0:  ldloc.2
  IL_00c1:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Object).get_Current() As Object"
  IL_00c6:  callvirt   "Function Object.ToString() As String"
  IL_00cb:  call       "Function String.Concat(String, String) As String"
  IL_00d0:  callvirt   "Sub System.IO.TextWriter.WriteLine(String)"
  IL_00d5:  ldloc.3
  IL_00d6:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_00db:  ldloc.2
  IL_00dc:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_00e1:  and
  IL_00e2:  brtrue.s   IL_00b5
  IL_00e4:  ret
}]]>.Value)
        End Sub

        <Fact()>
        Public Sub MultipleInterfaceMethodConflictTests()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
Imports Windows.Data.Json
Imports Windows.Foundation

Public Class A
    Public Shared Sub Main()
        Dim en = New JsonArray().GetEnumerator()
        en = new WwwFormUrlDecoder("?param1=test").GetEnumerator()
    End Sub
End Class]]>
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(source, references:=WinRtRefs)
            ' JsonArray implements both IEnumerable and IList, which both have a GetEnumerator
            ' method. We can't know which interface method to call, so we shouldn't emit a
            ' GetEnumerator method at all.
            CompilationUtils.AssertTheseDiagnostics(
                comp,
            <expected>
                <![CDATA[
BC30456: 'GetEnumerator' is not a member of 'JsonArray'.
        Dim en = New JsonArray().GetEnumerator()
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'GetEnumerator' is not a member of 'WwwFormUrlDecoder'.
        en = new WwwFormUrlDecoder("?param1=test").GetEnumerator()
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
            </expected>)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest01()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Public Class AllMembers
    Private Shared FailedCount As Integer = 0
    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected Then
            FailedCount += 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: 0, Actual: 1", expected, actual)
        return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount += 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: 0, Actual: 1", expected, actual)
        return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestIIterableMembers()
        Console.WriteLine("===  IIterableFloat  ===")
        Dim i = new IIterableFloat()
        i.ClearFlag()

        Dim enumerator = DirectCast(i, IEnumerable(Of Single)).GetEnumerator()
        ValidateMethod(i.GetFlagState(), TestMethodCalled.IIterable_First)
    End Sub

    Shared Sub Main()
        TestIIterableMembers()

        Console.WriteLine(FailedCount)
    End Sub
End Class
]]>
                    </file>
                </compilation>


            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestIIterableMembers", <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldstr      "===  IIterableFloat  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IIterableFloat..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.IIterableFloat.ClearFlag()"
  IL_0015:  dup
  IL_0016:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Single).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Single)"
  IL_001b:  pop
  IL_001c:  callvirt   "Function Windows.Languages.WinRTTest.IIterableFloat.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0021:  ldc.i4.1
  IL_0022:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0027:  pop
  IL_0028:  ret
}]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest02()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected Then
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If Not actual.ToString().Equals(expected.ToString()) Then
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString().Equals(expected.ToString())
    End Function

    Shared Sub TestIVectorIntMembers()
        Console.WriteLine("===  IVectorInt  ===")
        Dim v = New IVectorInt()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(v(0), 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0), 1)
        ValidateValue(arr(1), 0)
        v.ClearFlag()
        Dim count As Integer = v.Count
        Dim enumerator As IEnumerator(Of Integer) = DirectCast(v, IEnumerable(Of Integer)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        Dim index As Integer = 0
        For Each e In v
            index = index + 1
            ValidateValue(e, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue(v(1), 2)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As Integer = v(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 1)
        v.ClearFlag()
        val = v(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 2)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(v.Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(v.Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(v.Count, 0)
    End Sub

    Shared Sub TestIVectorStructMembers()
        Console.WriteLine("===  IVectorStruct  ===")
        Dim v = New IVectorStruct()
        Dim ud = New UserDefinedStruct() With {.Id = 1}
        v.ClearFlag()
        v.Add(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(v(0).Id, 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As UserDefinedStruct() = New UserDefinedStruct() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0).Id, ud.Id)
        v.ClearFlag()
        Dim count As Integer = v.Count
        Dim enumerator As IEnumerator(Of UserDefinedStruct) = DirectCast(v, IEnumerable(Of UserDefinedStruct)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size)
        enumerator.MoveNext()
        ValidateValue((enumerator.Current).Id, 1)
        Dim index As Integer = 0
        For Each e In v
            index = index + 1
            ValidateValue(e.Id, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, New UserDefinedStruct() With {.Id = 4})
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue(v(1).Id, 4)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val = v(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val.Id, ud.Id)
        v.ClearFlag()
        val = v(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val.Id, 4)
        v.ClearFlag()
        v.Remove(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(v.Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(v.Count, 0)
        v.Add(ud)
        v.Add(New UserDefinedStruct() With {.Id = 4})
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(v.Count, 0)
    End Sub

    Shared Sub TestIVectorUintStructMembers()
        Console.WriteLine("===  IVectorUintStruct  ===")
        Dim v = New IVectorUintStruct()
        v.ClearFlag()
        v.Add(7)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue((TryCast(v, IList(Of UInteger)))(0), 7)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(7)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim rez = DirectCast(v, IList(Of UInteger)).IndexOf(7)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 5)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue((TryCast(v, IList(Of UInteger)))(1), 5)
        v.ClearFlag()
        Dim isReadOnly As Boolean = DirectCast(v, IList(Of UInteger)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As UInteger = TryCast(v, IList(Of UInteger))(0)
        ValidateValue(val, 7)
        v.ClearFlag()
        val = DirectCast(v, IList(Of UInteger))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 5)
        v.ClearFlag()
        v.Remove(5)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 1)
        Try
            v.ClearFlag()
            DirectCast(v, IList(Of UInteger)).RemoveAt(0)
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
            ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 0)
        Catch exce As Exception
            Console.WriteLine("RemoveAt")
            Console.WriteLine(exce.Message)
        End Try

        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        DirectCast(v, IList(Of UInteger)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 0)
        Dim ud = New UserDefinedStruct() With {.Id = 1}
        v.ClearFlag()
        v.Add(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct))(0).Id, 1)
        v.ClearFlag()
        b = v.Contains(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim count As Integer = DirectCast(v, IList(Of UserDefinedStruct)).Count
        Dim enumerator As IEnumerator(Of UserDefinedStruct) = DirectCast(v, IList(Of UserDefinedStruct)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetMany)
        enumerator.MoveNext()
        ValidateValue(enumerator.Current.Id, 1)
        v.ClearFlag()
        rez = v.IndexOf(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, New UserDefinedStruct() With {.Id = 4})
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct))(1).Id, 4)
        v.ClearFlag()
        isReadOnly = DirectCast(v, IList(Of UserDefinedStruct)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val2 = DirectCast(v, IList(Of UserDefinedStruct))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val2.Id, ud.Id)
        v.ClearFlag()
        val2 = DirectCast(v, IList(Of UserDefinedStruct))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val2.Id, 4)
        v.ClearFlag()
        v.Remove(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct)).Count, 1)
        v.ClearFlag()
        DirectCast(v, IList(Of UserDefinedStruct)).RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct)).Count, 0)
        v.Add(ud)
        v.Add(New UserDefinedStruct() With {.Id = 4})
        v.ClearFlag()
        DirectCast(v, IList(Of UserDefinedStruct)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct)).Count, 0)
    End Sub

    Shared Sub TestIVectorUintFloatMembers()
        Console.WriteLine("===  IVectorUintIVectorFloat  ===")
        Dim v = New IVectorUintIVectorFloat()
        v.ClearFlag()
        v.Add(7)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 1)
        Try
            ValidateValue(DirectCast(v, IList(Of UInteger))(0), 7)
        Catch exc As ArgumentException
            Console.WriteLine(exc.Message)
        End Try

        v.ClearFlag()
        Dim b As Boolean = v.Contains(7)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim rez = DirectCast(v, IList(Of UInteger)).IndexOf(7)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.Insert(1, 5)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        Try
            ValidateValue(DirectCast(v, IList(Of UInteger))(1), 5)
        Catch exc As ArgumentException
            Console.WriteLine(exc.Message)
        End Try

        v.ClearFlag()
        Dim isReadOnly As Boolean = DirectCast(v, IList(Of UInteger)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        Try
            v.ClearFlag()
            Dim val = (TryCast(v, IList(Of UInteger)))(0)
            ValidateValue(val, 7)
            v.ClearFlag()
            val = DirectCast(v, IList(Of UInteger))(1)
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
            ValidateValue(val, 5)
        Catch exce As Exception
            Console.WriteLine("Indexing")
            Console.WriteLine(exce.Message)
        End Try

        v.ClearFlag()
        v.Remove(5)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 1)
        Try
            v.ClearFlag()
            DirectCast(v, IList(Of UInteger)).RemoveAt(0)
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
            ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 0)
        Catch exce As Exception
            Console.WriteLine("RemoveAt")
            Console.WriteLine(exce.Message)
        End Try

        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        DirectCast(v, IList(Of UInteger)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of UInteger)).Count, 0)
        v.ClearFlag()
        Dim one As Single = 1
        v.Add(one)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(DirectCast(v, IList(Of Single))(0), one)
        v.ClearFlag()
        b = v.Contains(one)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        rez = v.IndexOf(one)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, Convert.ToSingle(4))
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue(DirectCast(v, IList(Of Single))(1), 4)
        v.ClearFlag()
        isReadOnly = DirectCast(v, IList(Of Single)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val2 = DirectCast(v, IList(Of Single))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val2, one)
        v.ClearFlag()
        val2 = DirectCast(v, IList(Of Single))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val2, 4)
        v.ClearFlag()
        v.Remove(one)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Single)).Count, 1)
        v.ClearFlag()
        DirectCast(v, IList(Of Single)).RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Single)).Count, 0)
        v.Add(one)
        v.ClearFlag()
        DirectCast(v, IList(Of Single)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of Single)).Count, 0)
    End Sub

    Shared Sub TestIVectorIntIMapIntIntMembers()
        Console.WriteLine("===  IVectorIntIMapIntInt  ===")
        Dim v = New IVectorIntIMapIntInt()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue((TryCast(v, IList(Of Integer)))(0), 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0), 1)
        ValidateValue(arr(1), 0)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue((TryCast(v, IList(Of Integer)))(1), 2)
        v.ClearFlag()
        Dim isReadOnly As Boolean = DirectCast(v, IList(Of Integer)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As Integer = TryCast(v, IList(Of Integer))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 1)
        v.ClearFlag()
        val = DirectCast(v, IList(Of Integer))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 2)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 0)
        Dim m = v
        m.ClearFlag()
        m.Add(1, 2)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 1)
        m.ClearFlag()
        Dim key As Boolean = m.ContainsKey(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        m.ClearFlag()
        Dim val2 As Integer = TryCast(v, IDictionary(Of Integer, Integer))(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(val2, 2)
        m.ClearFlag()
        Dim keys = m.Keys
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim values = m.Values
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim outVal As Integer
        Dim success As Boolean = DirectCast(m, IDictionary(Of Integer, Integer)).TryGetValue(1, outVal)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(success, True)
        ValidateValue(outVal, 2)
        m.ClearFlag()
        m.Add(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 2)
        m.ClearFlag()
        Dim contains As Boolean = m.Contains(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(contains, True)
        m.ClearFlag()
        contains = m.Contains(New KeyValuePair(Of Integer, Integer)(8, 9))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(contains, False)
        m.ClearFlag()
        Dim remove As Boolean = DirectCast(m, IDictionary(Of Integer, Integer)).Remove(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 1)
        ValidateValue(remove, True)
        m.ClearFlag()
        Dim count As Integer = DirectCast(m, IDictionary(Of Integer, Integer)).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
        ValidateValue(count, 1)
        m.ClearFlag()
        isReadOnly = DirectCast(m, IDictionary(Of Integer, Integer)).IsReadOnly
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        m.ClearFlag()
        Dim rez2 = m.Remove(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(rez2, True)
        m.ClearFlag()
        rez2 = m.Remove(New KeyValuePair(Of Integer, Integer)(2, 3))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(rez2, False)
        m.Add(1, 2)
        m.Add(2, 3)
        m.ClearFlag()
        DirectCast(m, IDictionary(Of Integer, Integer)).Clear()
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 0)
    End Sub

    Shared Sub TestIVectorExplicitAddMembers()
        Dim v As IVectorExplicitAdd = New IVectorExplicitAdd()
        v.ClearFlag()
        DirectCast(v, IMemberAdd).Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add)
        v.ClearFlag()
        DirectCast(v, IMemberAdd).Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(v(0), 1)
    End Sub

    Shared Sub TestIVectorViewMembers()
        Dim v = New IVectorViewInt()
        v.ClearFlag()
        Dim count As Integer = v.Count
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVectorView_get_Size)
    End Sub

    Shared Sub TestIVectorUIntIVectorViewIntMembers()
        Console.WriteLine("===  IVectorUintIVectorViewInt  ===")
        Dim v = New IVectorUintIVectorViewInt()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue((TryCast(v, IList(Of UInteger)))(0), 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As UInteger() = New UInteger() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0), 1)
        ValidateValue(arr(1), 0)
        v.ClearFlag()
        Dim count As UInteger = Convert.ToUInt32((TryCast(v, IList(Of UInteger))).Count)
        Dim enumerator As IEnumerator(Of UInteger) = DirectCast(v, IEnumerable(Of UInteger)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        Dim index As UInteger = 0
        For Each e In v
            index = index + 1
            ValidateValue(e, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue((TryCast(v, IList(Of UInteger)))(1), 2)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As UInteger = (TryCast(v, IList(Of UInteger)))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 1)
        v.ClearFlag()
        val = DirectCast(v, IList(Of UInteger))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 2)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue((TryCast(v, IList(Of UInteger))).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue((TryCast(v, IList(Of UInteger))).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue((TryCast(v, IList(Of UInteger))).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        ValidateValue(DirectCast(v, IReadOnlyList(Of UInteger)).Count, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size)
        v.ClearFlag()
        ValidateValue(DirectCast(v, IReadOnlyList(Of UInteger))(0), 1)
        ValidateValue(DirectCast(v, IReadOnlyList(Of UInteger))(1), 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVectorView_GetAt)
    End Sub

    Shared Sub TestIVectorIntIVectorViewUintMembers()
        Console.WriteLine("===  IVectorIntIVectorViewUint  ===")
        Dim v = New IVectorIntIVectorViewUint()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(TryCast(v, IList(Of Integer))(0), 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0), 1)
        ValidateValue(arr(1), 0)
        v.ClearFlag()
        Dim count As UInteger = Convert.ToUInt32(DirectCast(v, IList(Of Integer)).Count)
        Dim enumerator As IEnumerator(Of Integer) = DirectCast(v, IList(Of Integer)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        Dim index As UInteger = 0
        For Each e In DirectCast(v, IList(Of Integer))
            index = index + 1
            ValidateValue(e, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue(TryCast(v, IList(Of Integer))(1), 2)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As UInteger = Convert.ToUInt32(TryCast(v, IList(Of Integer))(0))
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 1)
        v.ClearFlag()
        val = Convert.ToUInt32(DirectCast(v, IList(Of Integer))(1))
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 2)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        ValidateValue(DirectCast(v, IReadOnlyList(Of UInteger))(0), 0)
        ValidateValue(DirectCast(v, IReadOnlyList(Of UInteger))(1), 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
    End Sub

    Shared Sub TestIVectorStructIVectorViewStructMembers()
        Console.WriteLine("===  IVectorStructIVectorViewStruct  ===")
        Dim v = New IVectorStructIVectorViewStruct()
        Dim ud = New UserDefinedStruct() With {.Id = 1}
        v.ClearFlag()
        v.Add(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(TryCast(v, IList(Of UserDefinedStruct))(0).Id, 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As UserDefinedStruct() = New UserDefinedStruct() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0).Id, ud.Id)
        v.ClearFlag()
        Dim count As Integer = (TryCast(v, IList(Of UserDefinedStruct))).Count
        Dim enumerator As IEnumerator(Of UserDefinedStruct) = DirectCast(v, IEnumerable(Of UserDefinedStruct)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size)
        enumerator.MoveNext()
        ValidateValue((enumerator.Current).Id, 1)
        Dim index As Integer = 0
        For Each e In v
            index = index + 1
            ValidateValue(e.Id, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, New UserDefinedStruct() With {.Id = 4})
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue((TryCast(v, IList(Of UserDefinedStruct)))(1).Id, 4)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val = (TryCast(v, IList(Of UserDefinedStruct)))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val.Id, ud.Id)
        v.ClearFlag()
        val = DirectCast(v, IList(Of UserDefinedStruct))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val.Id, 4)
        v.ClearFlag()
        v.Remove(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue((TryCast(v, IList(Of UserDefinedStruct))).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue((TryCast(v, IList(Of UserDefinedStruct))).Count, 0)
        v.Add(ud)
        v.Add(New UserDefinedStruct() With {.Id = 4})
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(TryCast(v, IList(Of UserDefinedStruct)).Count, 0)
        v.Add(ud)
        v.Add(New UserDefinedStruct() With {.Id = 4})
        v.ClearFlag()
        ValidateValue(DirectCast(v, IReadOnlyList(Of UserDefinedStruct))(0).Id, ud.Id)
        ValidateValue(DirectCast(v, IReadOnlyList(Of UserDefinedStruct))(1).Id, 4)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
    End Sub

    Shared Function Main() As Integer
        TestIVectorIntMembers()
        TestIVectorStructMembers()
        TestIVectorUintStructMembers()
        TestIVectorUintFloatMembers()
        TestIVectorIntIMapIntIntMembers()
        TestIVectorExplicitAddMembers()
        TestIVectorViewMembers()
        TestIVectorUIntIVectorViewIntMembers()
        TestIVectorIntIVectorViewUintMembers()
        TestIVectorStructIVectorViewStructMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:=LegacyRefs)
            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub



        <Fact()>
        Public Sub LegacyCollectionTest03()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestIMapIntIntMembers()
        Console.WriteLine("===  IMapIntInt  ===")
        Dim m = New IMapIntInt()
        m.ClearFlag()
        m.Add(1, 2)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(m.Count, 1)
        m.ClearFlag()
        Dim key As Boolean = m.ContainsKey(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        m.ClearFlag()
        Dim val As Integer = m.Item(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(val, 2)
        m.ClearFlag()
        Dim keys = m.Keys
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim values = m.Values
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim outVal As Integer
        Dim success As Boolean =(DirectCast(m, IDictionary(Of Integer, Integer))).TryGetValue(1, outVal)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(outVal, 2)
        ValidateValue(success, True)
        m.ClearFlag()
        m.Add(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(m.Count, 2)
        m.ClearFlag()
        Dim contains As Boolean = m.Contains(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(contains, True)
        m.ClearFlag()
        contains = m.Contains(New KeyValuePair(Of Integer, Integer)(8, 9))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(contains, False)
        m.ClearFlag()
        Dim remove As Boolean = m.Remove(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(m.Count, 1)
        ValidateValue(remove, True)
        m.ClearFlag()
        Dim count As Integer = m.Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
        ValidateValue(count, 1)
        m.ClearFlag()
        Dim isReadOnly As Boolean = m.IsReadOnly
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        m.ClearFlag()
        Dim rez = m.Remove(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(rez, True)
        m.ClearFlag()
        rez = m.Remove(New KeyValuePair(Of Integer, Integer)(2, 3))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(rez, False)
        m.Add(1, 2)
        m.Add(2, 3)
        m.ClearFlag()
        m.Clear()
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear)
        ValidateValue(m.Count, 0)
    End Sub

    Shared Sub TestIMapIntStructMembers()
        Console.WriteLine("===  IMapIntStruct  ===")
        Dim m = New IMapIntStruct()
        Dim ud = New UserDefinedStruct() With {.Id = 10}
        m.ClearFlag()
        m.Add(1, ud)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(m.Count, 1)
        m.ClearFlag()
        Dim key As Boolean = m.ContainsKey(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        m.ClearFlag()
        Dim val As UserDefinedStruct = m(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(val.Id, 10)
        m.ClearFlag()
        Dim keys = m.Keys
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim values = m.Values
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim outVal As UserDefinedStruct
        Dim success As Boolean = (DirectCast(m, IDictionary(Of Integer, UserDefinedStruct))).TryGetValue(1, outVal)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(outVal.Id, ud.Id)
        ValidateValue(success, True)
        m.ClearFlag()
        m.Add(New KeyValuePair(Of Integer, UserDefinedStruct)(3, New UserDefinedStruct() With {.Id = 4}))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(m.Count, 2)
        m.ClearFlag()
        Dim contains As Boolean = m.Contains(New KeyValuePair(Of Integer, UserDefinedStruct)(1, ud))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(contains, True)
        m.ClearFlag()
        contains = m.Contains(New KeyValuePair(Of Integer, UserDefinedStruct)(8, New UserDefinedStruct()))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(contains, False)
        m.ClearFlag()
        Dim remove As Boolean = m.Remove(3)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(m.Count, 1)
        ValidateValue(remove, True)
        m.ClearFlag()
        Dim count As Integer = m.Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
        ValidateValue(count, 1)
        m.ClearFlag()
        Dim isReadOnly As Boolean = m.IsReadOnly
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        m.ClearFlag()
        Dim rez = m.Remove(New KeyValuePair(Of Integer, UserDefinedStruct)(1, ud))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(rez, True)
        ValidateValue(m.Count, 0)
        m.Add(1, ud)
        m.Add(2, ud)
        m.ClearFlag()
        m.Clear()
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear)
        ValidateValue(m.Count, 0)
    End Sub

    Shared Sub TestIMapExplicitAddMembers()
        Dim v As IMapExplicitAdd = New IMapExplicitAdd()
        v.ClearFlag()
        DirectCast(v, IMemberAdd2Args).Add(1, 1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add)
        v.ClearFlag()
        DirectCast(v, IMemberAdd2Args).Add(2, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add)
        v.ClearFlag()
        DirectCast(v, IDictionary(Of Integer, Integer)).Add(3, 3)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(v.Count, 3)
    End Sub

    Shared Sub TestIMapViewMembers()
        Dim m = New IMapViewIntInt()
        m.ClearFlag()
        Dim count As Integer = m.Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
    End Sub

    Shared Sub TestIMapIntIMapViewIntStructMembers()
        Console.WriteLine("===  IMapIMapViewIntStruct  ===")
        Dim m = New IMapIMapViewIntStruct()
        Dim ud = New UserDefinedStruct() With {.Id = 10}
        m.ClearFlag()
        m.Add(1, ud)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue((TryCast(m, IDictionary(Of Integer, UserDefinedStruct))).Count, 1)
        m.ClearFlag()
        Dim key As Boolean =DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).ContainsKey(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        m.ClearFlag()
        Dim val As UserDefinedStruct =DirectCast(m, IDictionary(Of Integer, UserDefinedStruct))(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(val.Id, 10)
        m.ClearFlag()
        Dim keys =DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Keys
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim values =DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Values
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim outVal As UserDefinedStruct
        Dim success As Boolean =DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).TryGetValue(1, outVal)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(success, True)
        m.ClearFlag()
        m.Add(New KeyValuePair(Of Integer, UserDefinedStruct)(3, New UserDefinedStruct() With {.Id = 4}))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue((TryCast(m, IDictionary(Of Integer, UserDefinedStruct))).Count, 2)
        m.ClearFlag()
        Dim contains As Boolean = m.Contains(New KeyValuePair(Of Integer, UserDefinedStruct)(1, ud))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(contains, True)
        m.ClearFlag()
        contains = m.Contains(New KeyValuePair(Of Integer, UserDefinedStruct)(8, New UserDefinedStruct()))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(contains, False)
        m.ClearFlag()
        Dim remove As Boolean = m.Remove(3)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue((TryCast(m, IDictionary(Of Integer, UserDefinedStruct))).Count, 1)
        ValidateValue(remove, True)
        m.ClearFlag()
        Dim count As Integer =(TryCast(m, IDictionary(Of Integer, UserDefinedStruct))).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
        ValidateValue(count, 1)
        m.ClearFlag()
        Dim isReadOnly As Boolean = m.IsReadOnly
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        m.ClearFlag()
        Dim rez = m.Remove(New KeyValuePair(Of Integer, UserDefinedStruct)(1, ud))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(rez, True)
        ValidateValue((TryCast(m, IDictionary(Of Integer, UserDefinedStruct))).Count, 0)
        m.Add(1, ud)
        m.Add(2, ud)
        m.ClearFlag()
        m.Clear()
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear)
        ValidateValue((TryCast(m, IDictionary(Of Integer, UserDefinedStruct))).Count, 0)
        m.ClearFlag()
        count =DirectCast(m, IReadOnlyDictionary(Of Integer, UserDefinedStruct)).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
    End Sub

    Shared Function Main() As Integer
        TestIMapIntIntMembers()
        TestIMapIntStructMembers()
        TestIMapExplicitAddMembers()
        TestIMapViewMembers()
        TestIMapIntIMapViewIntStructMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            verifier.VerifyIL("AllMembers.TestIMapIntIntMembers", <![CDATA[
{
  // Code size      756 (0x2f4)
  .maxstack  4
  .locals init (Integer V_0, //val
  Integer V_1, //outVal
  Boolean V_2, //success
  Boolean V_3, //contains
  Boolean V_4, //remove
  Integer V_5, //count
  Boolean V_6, //isReadOnly
  Boolean V_7) //rez
  IL_0000:  ldstr      "===  IMapIntInt  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IMapIntInt..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0015:  dup
  IL_0016:  ldc.i4.1
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_001d:  dup
  IL_001e:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0023:  ldc.i4.s   21
  IL_0025:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_002a:  pop
  IL_002b:  dup
  IL_002c:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_0031:  box        "Integer"
  IL_0036:  ldc.i4.1
  IL_0037:  box        "Integer"
  IL_003c:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0041:  pop
  IL_0042:  dup
  IL_0043:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0048:  dup
  IL_0049:  ldc.i4.1
  IL_004a:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).ContainsKey(Integer) As Boolean"
  IL_004f:  pop
  IL_0050:  dup
  IL_0051:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0056:  ldc.i4.s   19
  IL_0058:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_005d:  pop
  IL_005e:  dup
  IL_005f:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0064:  dup
  IL_0065:  ldc.i4.1
  IL_0066:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).get_Item(Integer) As Integer"
  IL_006b:  stloc.0
  IL_006c:  dup
  IL_006d:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0072:  ldc.i4.s   17
  IL_0074:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0079:  pop
  IL_007a:  ldloc.0
  IL_007b:  box        "Integer"
  IL_0080:  ldc.i4.2
  IL_0081:  box        "Integer"
  IL_0086:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_008b:  pop
  IL_008c:  dup
  IL_008d:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0092:  dup
  IL_0093:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).get_Keys() As System.Collections.Generic.ICollection(Of Integer)"
  IL_0098:  pop
  IL_0099:  dup
  IL_009a:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_009f:  ldc.i4.0
  IL_00a0:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00a5:  pop
  IL_00a6:  dup
  IL_00a7:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_00ac:  dup
  IL_00ad:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).get_Values() As System.Collections.Generic.ICollection(Of Integer)"
  IL_00b2:  pop
  IL_00b3:  dup
  IL_00b4:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00b9:  ldc.i4.0
  IL_00ba:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00bf:  pop
  IL_00c0:  dup
  IL_00c1:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_00c6:  dup
  IL_00c7:  ldc.i4.1
  IL_00c8:  ldloca.s   V_1
  IL_00ca:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).TryGetValue(Integer, ByRef Integer) As Boolean"
  IL_00cf:  stloc.2
  IL_00d0:  dup
  IL_00d1:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00d6:  ldc.i4.s   17
  IL_00d8:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00dd:  pop
  IL_00de:  ldloc.1
  IL_00df:  box        "Integer"
  IL_00e4:  ldc.i4.2
  IL_00e5:  box        "Integer"
  IL_00ea:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00ef:  pop
  IL_00f0:  ldloc.2
  IL_00f1:  box        "Boolean"
  IL_00f6:  ldc.i4.1
  IL_00f7:  box        "Boolean"
  IL_00fc:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0101:  pop
  IL_0102:  dup
  IL_0103:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0108:  dup
  IL_0109:  ldc.i4.3
  IL_010a:  ldc.i4.4
  IL_010b:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0110:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Add(System.Collections.Generic.KeyValuePair(Of Integer, Integer))"
  IL_0115:  dup
  IL_0116:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_011b:  ldc.i4.s   21
  IL_011d:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0122:  pop
  IL_0123:  dup
  IL_0124:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_0129:  box        "Integer"
  IL_012e:  ldc.i4.2
  IL_012f:  box        "Integer"
  IL_0134:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0139:  pop
  IL_013a:  dup
  IL_013b:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0140:  dup
  IL_0141:  ldc.i4.3
  IL_0142:  ldc.i4.4
  IL_0143:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0148:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_014d:  stloc.3
  IL_014e:  dup
  IL_014f:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0154:  ldc.i4.s   17
  IL_0156:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_015b:  pop
  IL_015c:  ldloc.3
  IL_015d:  box        "Boolean"
  IL_0162:  ldc.i4.1
  IL_0163:  box        "Boolean"
  IL_0168:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_016d:  pop
  IL_016e:  dup
  IL_016f:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0174:  dup
  IL_0175:  ldc.i4.8
  IL_0176:  ldc.i4.s   9
  IL_0178:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_017d:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_0182:  stloc.3
  IL_0183:  dup
  IL_0184:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0189:  ldc.i4.s   19
  IL_018b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0190:  pop
  IL_0191:  ldloc.3
  IL_0192:  box        "Boolean"
  IL_0197:  ldc.i4.0
  IL_0198:  box        "Boolean"
  IL_019d:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01a2:  pop
  IL_01a3:  dup
  IL_01a4:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_01a9:  dup
  IL_01aa:  ldc.i4.1
  IL_01ab:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).Remove(Integer) As Boolean"
  IL_01b0:  stloc.s    V_4
  IL_01b2:  dup
  IL_01b3:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01b8:  ldc.i4.s   22
  IL_01ba:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01bf:  pop
  IL_01c0:  dup
  IL_01c1:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_01c6:  box        "Integer"
  IL_01cb:  ldc.i4.1
  IL_01cc:  box        "Integer"
  IL_01d1:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01d6:  pop
  IL_01d7:  ldloc.s    V_4
  IL_01d9:  box        "Boolean"
  IL_01de:  ldc.i4.1
  IL_01df:  box        "Boolean"
  IL_01e4:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01e9:  pop
  IL_01ea:  dup
  IL_01eb:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_01f0:  dup
  IL_01f1:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_01f6:  stloc.s    V_5
  IL_01f8:  dup
  IL_01f9:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01fe:  ldc.i4.s   18
  IL_0200:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0205:  pop
  IL_0206:  ldloc.s    V_5
  IL_0208:  box        "Integer"
  IL_020d:  ldc.i4.1
  IL_020e:  box        "Integer"
  IL_0213:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0218:  pop
  IL_0219:  dup
  IL_021a:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_021f:  dup
  IL_0220:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_IsReadOnly() As Boolean"
  IL_0225:  stloc.s    V_6
  IL_0227:  dup
  IL_0228:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_022d:  ldc.i4.0
  IL_022e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0233:  pop
  IL_0234:  ldloc.s    V_6
  IL_0236:  box        "Boolean"
  IL_023b:  ldc.i4.0
  IL_023c:  box        "Boolean"
  IL_0241:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0246:  pop
  IL_0247:  dup
  IL_0248:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_024d:  dup
  IL_024e:  ldc.i4.3
  IL_024f:  ldc.i4.4
  IL_0250:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0255:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Remove(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_025a:  stloc.s    V_7
  IL_025c:  dup
  IL_025d:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0262:  ldc.i4.s   22
  IL_0264:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0269:  pop
  IL_026a:  ldloc.s    V_7
  IL_026c:  box        "Boolean"
  IL_0271:  ldc.i4.1
  IL_0272:  box        "Boolean"
  IL_0277:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_027c:  pop
  IL_027d:  dup
  IL_027e:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0283:  dup
  IL_0284:  ldc.i4.2
  IL_0285:  ldc.i4.3
  IL_0286:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_028b:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Remove(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_0290:  stloc.s    V_7
  IL_0292:  dup
  IL_0293:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0298:  ldc.i4.s   19
  IL_029a:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_029f:  pop
  IL_02a0:  ldloc.s    V_7
  IL_02a2:  box        "Boolean"
  IL_02a7:  ldc.i4.0
  IL_02a8:  box        "Boolean"
  IL_02ad:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02b2:  pop
  IL_02b3:  dup
  IL_02b4:  ldc.i4.1
  IL_02b5:  ldc.i4.2
  IL_02b6:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_02bb:  dup
  IL_02bc:  ldc.i4.2
  IL_02bd:  ldc.i4.3
  IL_02be:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_02c3:  dup
  IL_02c4:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_02c9:  dup
  IL_02ca:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Clear()"
  IL_02cf:  dup
  IL_02d0:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02d5:  ldc.i4.s   23
  IL_02d7:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02dc:  pop
  IL_02dd:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_02e2:  box        "Integer"
  IL_02e7:  ldc.i4.0
  IL_02e8:  box        "Integer"
  IL_02ed:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02f2:  pop
  IL_02f3:  ret
}
]]>.Value)

            verifier.VerifyIL("AllMembers.TestIMapViewMembers", <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.IMapViewIntInt..ctor()"
  IL_0005:  dup
  IL_0006:  callvirt   "Sub Windows.Languages.WinRTTest.IMapViewIntInt.ClearFlag()"
  IL_000b:  dup
  IL_000c:  callvirt   "Function System.Collections.Generic.IReadOnlyCollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_0011:  pop
  IL_0012:  callvirt   "Function Windows.Languages.WinRTTest.IMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0017:  ldc.i4.s   18
  IL_0019:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_001e:  pop
  IL_001f:  ret
}
]]>.Value)
            verifier.VerifyIL("AllMembers.TestIMapIntIMapViewIntStructMembers", <![CDATA[
{
  // Code size      790 (0x316)
  .maxstack  5
  .locals init (Windows.Languages.WinRTTest.UserDefinedStruct V_0, //ud
  Windows.Languages.WinRTTest.UserDefinedStruct V_1, //val
  Windows.Languages.WinRTTest.UserDefinedStruct V_2, //outVal
  Boolean V_3, //success
  Boolean V_4, //contains
  Boolean V_5, //remove
  Integer V_6, //count
  Boolean V_7, //isReadOnly
  Boolean V_8, //rez
  Windows.Languages.WinRTTest.UserDefinedStruct V_9)
  IL_0000:  ldstr      "===  IMapIMapViewIntStruct  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct..ctor()"
  IL_000f:  ldloca.s   V_9
  IL_0011:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_0017:  ldloca.s   V_9
  IL_0019:  ldc.i4.s   10
  IL_001b:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0020:  ldloc.s    V_9
  IL_0022:  stloc.0
  IL_0023:  dup
  IL_0024:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldloc.0
  IL_002c:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Add(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0031:  dup
  IL_0032:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0037:  ldc.i4.s   21
  IL_0039:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_003e:  pop
  IL_003f:  dup
  IL_0040:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_0045:  box        "Integer"
  IL_004a:  ldc.i4.1
  IL_004b:  box        "Integer"
  IL_0050:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0055:  pop
  IL_0056:  dup
  IL_0057:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_005c:  dup
  IL_005d:  ldc.i4.1
  IL_005e:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).ContainsKey(Integer) As Boolean"
  IL_0063:  pop
  IL_0064:  dup
  IL_0065:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_006a:  ldc.i4.s   19
  IL_006c:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0071:  pop
  IL_0072:  dup
  IL_0073:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0078:  dup
  IL_0079:  ldc.i4.1
  IL_007a:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).get_Item(Integer) As Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_007f:  stloc.1
  IL_0080:  dup
  IL_0081:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0086:  ldc.i4.s   17
  IL_0088:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_008d:  pop
  IL_008e:  ldloc.1
  IL_008f:  ldfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0094:  box        "UInteger"
  IL_0099:  ldc.i4.s   10
  IL_009b:  box        "Integer"
  IL_00a0:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00a5:  pop
  IL_00a6:  dup
  IL_00a7:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_00ac:  dup
  IL_00ad:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).get_Keys() As System.Collections.Generic.ICollection(Of Integer)"
  IL_00b2:  pop
  IL_00b3:  dup
  IL_00b4:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00b9:  ldc.i4.0
  IL_00ba:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00bf:  pop
  IL_00c0:  dup
  IL_00c1:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_00c6:  dup
  IL_00c7:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).get_Values() As System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_00cc:  pop
  IL_00cd:  dup
  IL_00ce:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00d3:  ldc.i4.0
  IL_00d4:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00d9:  pop
  IL_00da:  dup
  IL_00db:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_00e0:  dup
  IL_00e1:  ldc.i4.1
  IL_00e2:  ldloca.s   V_2
  IL_00e4:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).TryGetValue(Integer, ByRef Windows.Languages.WinRTTest.UserDefinedStruct) As Boolean"
  IL_00e9:  stloc.3
  IL_00ea:  dup
  IL_00eb:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00f0:  ldc.i4.s   17
  IL_00f2:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00f7:  pop
  IL_00f8:  ldloc.3
  IL_00f9:  box        "Boolean"
  IL_00fe:  ldc.i4.1
  IL_00ff:  box        "Boolean"
  IL_0104:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0109:  pop
  IL_010a:  dup
  IL_010b:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0110:  dup
  IL_0111:  ldc.i4.3
  IL_0112:  ldloca.s   V_9
  IL_0114:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_011a:  ldloca.s   V_9
  IL_011c:  ldc.i4.4
  IL_011d:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0122:  ldloc.s    V_9
  IL_0124:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0129:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Add(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct))"
  IL_012e:  dup
  IL_012f:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0134:  ldc.i4.s   21
  IL_0136:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_013b:  pop
  IL_013c:  dup
  IL_013d:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_0142:  box        "Integer"
  IL_0147:  ldc.i4.2
  IL_0148:  box        "Integer"
  IL_014d:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0152:  pop
  IL_0153:  dup
  IL_0154:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0159:  dup
  IL_015a:  ldc.i4.1
  IL_015b:  ldloc.0
  IL_015c:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0161:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)) As Boolean"
  IL_0166:  stloc.s    V_4
  IL_0168:  dup
  IL_0169:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_016e:  ldc.i4.s   17
  IL_0170:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0175:  pop
  IL_0176:  ldloc.s    V_4
  IL_0178:  box        "Boolean"
  IL_017d:  ldc.i4.1
  IL_017e:  box        "Boolean"
  IL_0183:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0188:  pop
  IL_0189:  dup
  IL_018a:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_018f:  dup
  IL_0190:  ldc.i4.8
  IL_0191:  ldloca.s   V_9
  IL_0193:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_0199:  ldloc.s    V_9
  IL_019b:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_01a0:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)) As Boolean"
  IL_01a5:  stloc.s    V_4
  IL_01a7:  dup
  IL_01a8:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01ad:  ldc.i4.s   19
  IL_01af:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01b4:  pop
  IL_01b5:  ldloc.s    V_4
  IL_01b7:  box        "Boolean"
  IL_01bc:  ldc.i4.0
  IL_01bd:  box        "Boolean"
  IL_01c2:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01c7:  pop
  IL_01c8:  dup
  IL_01c9:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_01ce:  dup
  IL_01cf:  ldc.i4.3
  IL_01d0:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Remove(Integer) As Boolean"
  IL_01d5:  stloc.s    V_5
  IL_01d7:  dup
  IL_01d8:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01dd:  ldc.i4.s   22
  IL_01df:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01e4:  pop
  IL_01e5:  dup
  IL_01e6:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_01eb:  box        "Integer"
  IL_01f0:  ldc.i4.1
  IL_01f1:  box        "Integer"
  IL_01f6:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01fb:  pop
  IL_01fc:  ldloc.s    V_5
  IL_01fe:  box        "Boolean"
  IL_0203:  ldc.i4.1
  IL_0204:  box        "Boolean"
  IL_0209:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_020e:  pop
  IL_020f:  dup
  IL_0210:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0215:  dup
  IL_0216:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_021b:  stloc.s    V_6
  IL_021d:  dup
  IL_021e:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0223:  ldc.i4.s   18
  IL_0225:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_022a:  pop
  IL_022b:  ldloc.s    V_6
  IL_022d:  box        "Integer"
  IL_0232:  ldc.i4.1
  IL_0233:  box        "Integer"
  IL_0238:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_023d:  pop
  IL_023e:  dup
  IL_023f:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0244:  dup
  IL_0245:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_IsReadOnly() As Boolean"
  IL_024a:  stloc.s    V_7
  IL_024c:  dup
  IL_024d:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0252:  ldc.i4.0
  IL_0253:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0258:  pop
  IL_0259:  ldloc.s    V_7
  IL_025b:  box        "Boolean"
  IL_0260:  ldc.i4.0
  IL_0261:  box        "Boolean"
  IL_0266:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_026b:  pop
  IL_026c:  dup
  IL_026d:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0272:  dup
  IL_0273:  ldc.i4.1
  IL_0274:  ldloc.0
  IL_0275:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_027a:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Remove(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)) As Boolean"
  IL_027f:  stloc.s    V_8
  IL_0281:  dup
  IL_0282:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0287:  ldc.i4.s   22
  IL_0289:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_028e:  pop
  IL_028f:  ldloc.s    V_8
  IL_0291:  box        "Boolean"
  IL_0296:  ldc.i4.1
  IL_0297:  box        "Boolean"
  IL_029c:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02a1:  pop
  IL_02a2:  dup
  IL_02a3:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_02a8:  box        "Integer"
  IL_02ad:  ldc.i4.0
  IL_02ae:  box        "Integer"
  IL_02b3:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02b8:  pop
  IL_02b9:  dup
  IL_02ba:  ldc.i4.1
  IL_02bb:  ldloc.0
  IL_02bc:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Add(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_02c1:  dup
  IL_02c2:  ldc.i4.2
  IL_02c3:  ldloc.0
  IL_02c4:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Add(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_02c9:  dup
  IL_02ca:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_02cf:  dup
  IL_02d0:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Clear()"
  IL_02d5:  dup
  IL_02d6:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02db:  ldc.i4.s   23
  IL_02dd:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02e2:  pop
  IL_02e3:  dup
  IL_02e4:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_02e9:  box        "Integer"
  IL_02ee:  ldc.i4.0
  IL_02ef:  box        "Integer"
  IL_02f4:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02f9:  pop
  IL_02fa:  dup
  IL_02fb:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()"
  IL_0300:  dup
  IL_0301:  callvirt   "Function System.Collections.Generic.IReadOnlyCollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_0306:  stloc.s    V_6
  IL_0308:  callvirt   "Function Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_030d:  ldc.i4.s   18
  IL_030f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0314:  pop
  IL_0315:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest04()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers
    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestIVectorIntIVectorViewIntIMapIntIntIMapViewIntIntMembers()
        Dim v = New IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(TryCast(v, IList(Of Integer))(0), 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0), 1)
        ValidateValue(arr(1), 0)
        v.ClearFlag()
        Dim count As Integer = DirectCast(v, IList(Of Integer)).Count
        Dim enumerator As IEnumerator(Of Integer) = DirectCast(v, IList(Of Integer)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        Dim index As Integer = 0
        For Each e In DirectCast(v, IList(Of Integer))
            index = index + 1
            ValidateValue(e, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue(TryCast(v, IList(Of Integer))(1), 2)
        v.ClearFlag()
        Dim isReadOnly As Boolean = DirectCast(v, IList(Of Integer)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As Integer = Convert.ToUInt32(TryCast(v, IList(Of Integer))(0))
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 1)
        v.ClearFlag()
        val = DirectCast(v, IList(Of Integer))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 2)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of Integer)).Count, 0)
        v.ClearFlag()
        count = DirectCast(v, IReadOnlyList(Of Integer)).Count
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size)
        Dim m = v
        m.ClearFlag()
        m.Add(1, 2)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 1)
        m.ClearFlag()
        Dim key As Boolean = DirectCast(m, IDictionary(Of Integer, Integer)).ContainsKey(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        m.ClearFlag()
        val = Convert.ToInt32((TryCast(m, IDictionary(Of Integer, Integer)))(1))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(val, 2)
        m.ClearFlag()
        Dim keys = DirectCast(m, IDictionary(Of Integer, Integer)).Keys
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim values = DirectCast(m, IDictionary(Of Integer, Integer)).Values
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim outVal As Integer
        Dim success As Boolean = DirectCast(m, IDictionary(Of Integer, Integer)).TryGetValue(1, outVal)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(outVal, 2)
        ValidateValue(success, True)
        m.ClearFlag()
        m.Add(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 2)
        m.ClearFlag()
        Dim contains As Boolean = m.Contains(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(contains, True)
        m.ClearFlag()
        contains = m.Contains(New KeyValuePair(Of Integer, Integer)(8, 9))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(contains, False)
        m.ClearFlag()
        Dim remove As Boolean = DirectCast(m, IDictionary(Of Integer, Integer)).Remove(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 1)
        ValidateValue(remove, True)
        m.ClearFlag()
        count = DirectCast(m, IDictionary(Of Integer, Integer)).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
        ValidateValue(count, 1)
        m.ClearFlag()
        isReadOnly = DirectCast(m, IDictionary(Of Integer, Integer)).IsReadOnly
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        m.ClearFlag()
        Dim rez2 = m.Remove(New KeyValuePair(Of Integer, Integer)(3, 4))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(rez2, True)
        m.ClearFlag()
        rez2 = m.Remove(New KeyValuePair(Of Integer, Integer)(2, 3))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(rez2, False)
        m.Add(1, 2)
        m.Add(2, 3)
        m.ClearFlag()
        DirectCast(m, IDictionary(Of Integer, Integer)).Clear()
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, Integer)).Count, 0)
        m.ClearFlag()
        count = DirectCast(m, IReadOnlyDictionary(Of Integer, Integer)).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IVector_get_Size)
    End Sub

    Shared Sub TestIVectorStructIVectorViewStructIMapIntStructIMapViewIntStructMembers()
        Dim v = New IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct()
        Dim ud = New UserDefinedStruct() With {.Id = 1}
        v.ClearFlag()
        v.Add(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As UserDefinedStruct() = New UserDefinedStruct() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0).Id, ud.Id)
        v.ClearFlag()
        Dim count As Integer = DirectCast(v, IList(Of UserDefinedStruct)).Count
        Dim enumerator As IEnumerator(Of UserDefinedStruct) = DirectCast(v, IList(Of UserDefinedStruct)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        enumerator.MoveNext()
        ValidateValue((enumerator.Current).Id, 1)
        Dim index As Integer = 0
        For Each e In DirectCast(v, IList(Of UserDefinedStruct))
            index = index + 1
            ValidateValue(e.Id, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, New UserDefinedStruct() With {.Id = 4})
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        v.ClearFlag()
        Dim isReadOnly As Boolean = DirectCast(v, IList(Of UserDefinedStruct)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val =(TryCast(v, IList(Of UserDefinedStruct)))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        v.ClearFlag()
        val = DirectCast(v, IList(Of UserDefinedStruct))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        v.ClearFlag()
        v.Remove(ud)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct)).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct)).Count, 0)
        v.Add(ud)
        v.Add(New UserDefinedStruct() With {.Id = 4})
        v.ClearFlag()
        DirectCast(v, IList(Of UserDefinedStruct)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue(DirectCast(v, IList(Of UserDefinedStruct)).Count, 0)
        v.ClearFlag()
        count = DirectCast(v, IReadOnlyList(Of UserDefinedStruct)).Count
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size)
        Dim m = v
        ud = New UserDefinedStruct() With {.Id = 10}
        m.ClearFlag()
        m.Add(1, ud)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Count, 1)
        m.ClearFlag()
        Dim key As Boolean = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).ContainsKey(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        m.ClearFlag()
        val = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct))(1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        m.ClearFlag()
        Dim keys = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Keys
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim values = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Values
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        m.ClearFlag()
        Dim outVal As UserDefinedStruct
        Dim success As Boolean = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).TryGetValue(1, outVal)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(success, True)
        m.ClearFlag()
        m.Add(New KeyValuePair(Of Integer, UserDefinedStruct)(3, New UserDefinedStruct() With {.Id = 4}))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Count, 2)
        m.ClearFlag()
        Dim contains As Boolean = m.Contains(New KeyValuePair(Of Integer, UserDefinedStruct)(1, ud))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup)
        ValidateValue(contains, True)
        m.ClearFlag()
        contains = m.Contains(New KeyValuePair(Of Integer, UserDefinedStruct)(8, New UserDefinedStruct()))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        ValidateValue(contains, False)
        m.ClearFlag()
        m.ClearFlag()
        count = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
        ValidateValue(count, 1)
        m.ClearFlag()
        isReadOnly = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).IsReadOnly
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        m.ClearFlag()
        m.Remove(New KeyValuePair(Of Integer, UserDefinedStruct)(1, ud))
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Count, 0)
        m.Add(1, ud)
        m.Add(2, ud)
        m.ClearFlag()
        DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Clear()
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear)
        ValidateValue(DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Count, 0)
        m.ClearFlag()
        count = DirectCast(m, IDictionary(Of Integer, UserDefinedStruct)).Count
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size)
    End Sub

    Shared Function Main() As Integer
        TestIVectorIntIVectorViewIntIMapIntIntIMapViewIntIntMembers()
        TestIVectorStructIVectorViewStructIMapIntStructIMapViewIntStructMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestIVectorIntIVectorViewIntIMapIntIntIMapViewIntIntMembers", <![CDATA[
{
  // Code size     1496 (0x5d8)
  .maxstack  4
  .locals init (Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt V_0, //v
                Integer() V_1, //arr
                Integer V_2, //count
                Integer V_3, //index
                Boolean V_4, //isReadOnly
                Integer V_5, //val
                Integer V_6, //outVal
                Boolean V_7, //success
                Boolean V_8, //contains
                Boolean V_9, //remove
                Boolean V_10, //rez2
                System.Collections.Generic.IEnumerator(Of Integer) V_11)
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0013:  ldloc.0
  IL_0014:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0019:  ldc.i4.s   9
  IL_001b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.0
  IL_0023:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_0028:  box        "Integer"
  IL_002d:  ldc.i4.1
  IL_002e:  box        "Integer"
  IL_0033:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.1
  IL_0041:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Contains(Integer) As Boolean"
  IL_0046:  ldloc.0
  IL_0047:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_004c:  ldc.i4.5
  IL_004d:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0052:  pop
  IL_0053:  box        "Boolean"
  IL_0058:  ldc.i4.1
  IL_0059:  box        "Boolean"
  IL_005e:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0063:  pop
  IL_0064:  ldloc.0
  IL_0065:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_006a:  ldc.i4.0
  IL_006b:  newarr     "Integer"
  IL_0070:  stloc.1
  IL_0071:  ldloc.0
  IL_0072:  ldloc.1
  IL_0073:  ldc.i4.0
  IL_0074:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).CopyTo(Integer(), Integer)"
  IL_0079:  ldloc.0
  IL_007a:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_007f:  ldc.i4.2
  IL_0080:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0085:  pop
  IL_0086:  ldloc.1
  IL_0087:  ldc.i4.0
  IL_0088:  ldelem.i4
  IL_0089:  box        "Integer"
  IL_008e:  ldc.i4.1
  IL_008f:  box        "Integer"
  IL_0094:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0099:  pop
  IL_009a:  ldloc.1
  IL_009b:  ldc.i4.1
  IL_009c:  ldelem.i4
  IL_009d:  box        "Integer"
  IL_00a2:  ldc.i4.0
  IL_00a3:  box        "Integer"
  IL_00a8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00ad:  pop
  IL_00ae:  ldloc.0
  IL_00af:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_00b4:  ldloc.0
  IL_00b5:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_00ba:  stloc.2
  IL_00bb:  ldloc.0
  IL_00bc:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_00c1:  pop
  IL_00c2:  ldloc.0
  IL_00c3:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00c8:  ldc.i4.1
  IL_00c9:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00ce:  pop
  IL_00cf:  ldc.i4.0
  IL_00d0:  stloc.3
  .try
  {
    IL_00d1:  ldloc.0
    IL_00d2:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_00d7:  stloc.s    V_11
    IL_00d9:  br.s       IL_00f7
    IL_00db:  ldloc.s    V_11
    IL_00dd:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
    IL_00e2:  ldloc.3
    IL_00e3:  ldc.i4.1
    IL_00e4:  add.ovf
    IL_00e5:  stloc.3
    IL_00e6:  box        "Integer"
    IL_00eb:  ldloc.3
    IL_00ec:  box        "Integer"
    IL_00f1:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
    IL_00f6:  pop
    IL_00f7:  ldloc.s    V_11
    IL_00f9:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_00fe:  brtrue.s   IL_00db
    IL_0100:  leave.s    IL_010e
  }
  finally
  {
    IL_0102:  ldloc.s    V_11
    IL_0104:  brfalse.s  IL_010d
    IL_0106:  ldloc.s    V_11
    IL_0108:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_010d:  endfinally
  }
  IL_010e:  ldloc.3
  IL_010f:  box        "Integer"
  IL_0114:  ldc.i4.1
  IL_0115:  box        "Integer"
  IL_011a:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_011f:  pop
  IL_0120:  ldloc.0
  IL_0121:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0126:  ldloc.0
  IL_0127:  ldc.i4.1
  IL_0128:  callvirt   "Function System.Collections.Generic.IList(Of Integer).IndexOf(Integer) As Integer"
  IL_012d:  ldloc.0
  IL_012e:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0133:  ldc.i4.5
  IL_0134:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0139:  pop
  IL_013a:  box        "Integer"
  IL_013f:  ldc.i4.0
  IL_0140:  box        "Integer"
  IL_0145:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_014a:  pop
  IL_014b:  ldloc.0
  IL_014c:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0151:  ldloc.0
  IL_0152:  ldc.i4.1
  IL_0153:  ldc.i4.2
  IL_0154:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).Insert(Integer, Integer)"
  IL_0159:  ldloc.0
  IL_015a:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_015f:  ldc.i4.7
  IL_0160:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0165:  pop
  IL_0166:  ldloc.0
  IL_0167:  ldc.i4.1
  IL_0168:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_016d:  box        "Integer"
  IL_0172:  ldc.i4.2
  IL_0173:  box        "Integer"
  IL_0178:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_017d:  pop
  IL_017e:  ldloc.0
  IL_017f:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0184:  ldloc.0
  IL_0185:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_IsReadOnly() As Boolean"
  IL_018a:  stloc.s    V_4
  IL_018c:  ldloc.0
  IL_018d:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0192:  ldc.i4.0
  IL_0193:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0198:  pop
  IL_0199:  ldloc.s    V_4
  IL_019b:  box        "Boolean"
  IL_01a0:  ldc.i4.0
  IL_01a1:  box        "Boolean"
  IL_01a6:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01ab:  pop
  IL_01ac:  ldloc.0
  IL_01ad:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_01b2:  ldloc.0
  IL_01b3:  ldc.i4.0
  IL_01b4:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_01b9:  call       "Function System.Convert.ToUInt32(Integer) As UInteger"
  IL_01be:  conv.ovf.i4.un
  IL_01bf:  stloc.s    V_5
  IL_01c1:  ldloc.0
  IL_01c2:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01c7:  ldc.i4.2
  IL_01c8:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01cd:  pop
  IL_01ce:  ldloc.s    V_5
  IL_01d0:  box        "Integer"
  IL_01d5:  ldc.i4.1
  IL_01d6:  box        "Integer"
  IL_01db:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01e0:  pop
  IL_01e1:  ldloc.0
  IL_01e2:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_01e7:  ldloc.0
  IL_01e8:  ldc.i4.1
  IL_01e9:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_01ee:  stloc.s    V_5
  IL_01f0:  ldloc.0
  IL_01f1:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01f6:  ldc.i4.2
  IL_01f7:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01fc:  pop
  IL_01fd:  ldloc.s    V_5
  IL_01ff:  box        "Integer"
  IL_0204:  ldc.i4.2
  IL_0205:  box        "Integer"
  IL_020a:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_020f:  pop
  IL_0210:  ldloc.0
  IL_0211:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0216:  ldloc.0
  IL_0217:  ldc.i4.1
  IL_0218:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Remove(Integer) As Boolean"
  IL_021d:  pop
  IL_021e:  ldloc.0
  IL_021f:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0224:  ldc.i4.8
  IL_0225:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_022a:  pop
  IL_022b:  ldloc.0
  IL_022c:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_0231:  box        "Integer"
  IL_0236:  ldc.i4.1
  IL_0237:  box        "Integer"
  IL_023c:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0241:  pop
  IL_0242:  ldloc.0
  IL_0243:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0248:  ldloc.0
  IL_0249:  ldc.i4.0
  IL_024a:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).RemoveAt(Integer)"
  IL_024f:  ldloc.0
  IL_0250:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0255:  ldc.i4.8
  IL_0256:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_025b:  pop
  IL_025c:  ldloc.0
  IL_025d:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_0262:  box        "Integer"
  IL_0267:  ldc.i4.0
  IL_0268:  box        "Integer"
  IL_026d:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0272:  pop
  IL_0273:  ldloc.0
  IL_0274:  ldc.i4.1
  IL_0275:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_027a:  ldloc.0
  IL_027b:  ldc.i4.2
  IL_027c:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0281:  ldloc.0
  IL_0282:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0287:  ldloc.0
  IL_0288:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Clear()"
  IL_028d:  ldloc.0
  IL_028e:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0293:  ldc.i4.s   11
  IL_0295:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_029a:  pop
  IL_029b:  ldloc.0
  IL_029c:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_02a1:  box        "Integer"
  IL_02a6:  ldc.i4.0
  IL_02a7:  box        "Integer"
  IL_02ac:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02b1:  pop
  IL_02b2:  ldloc.0
  IL_02b3:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_02b8:  ldloc.0
  IL_02b9:  callvirt   "Function System.Collections.Generic.IReadOnlyCollection(Of Integer).get_Count() As Integer"
  IL_02be:  stloc.2
  IL_02bf:  ldloc.0
  IL_02c0:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02c5:  ldc.i4.3
  IL_02c6:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02cb:  pop
  IL_02cc:  ldloc.0
  IL_02cd:  dup
  IL_02ce:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_02d3:  dup
  IL_02d4:  ldc.i4.1
  IL_02d5:  ldc.i4.2
  IL_02d6:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_02db:  dup
  IL_02dc:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02e1:  ldc.i4.s   21
  IL_02e3:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02e8:  pop
  IL_02e9:  dup
  IL_02ea:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_02ef:  box        "Integer"
  IL_02f4:  ldc.i4.1
  IL_02f5:  box        "Integer"
  IL_02fa:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02ff:  pop
  IL_0300:  dup
  IL_0301:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0306:  dup
  IL_0307:  ldc.i4.1
  IL_0308:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).ContainsKey(Integer) As Boolean"
  IL_030d:  pop
  IL_030e:  dup
  IL_030f:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0314:  ldc.i4.s   19
  IL_0316:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_031b:  pop
  IL_031c:  dup
  IL_031d:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0322:  dup
  IL_0323:  ldc.i4.1
  IL_0324:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).get_Item(Integer) As Integer"
  IL_0329:  call       "Function System.Convert.ToInt32(Integer) As Integer"
  IL_032e:  stloc.s    V_5
  IL_0330:  dup
  IL_0331:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0336:  ldc.i4.s   17
  IL_0338:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_033d:  pop
  IL_033e:  ldloc.s    V_5
  IL_0340:  box        "Integer"
  IL_0345:  ldc.i4.2
  IL_0346:  box        "Integer"
  IL_034b:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0350:  pop
  IL_0351:  dup
  IL_0352:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0357:  dup
  IL_0358:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).get_Keys() As System.Collections.Generic.ICollection(Of Integer)"
  IL_035d:  pop
  IL_035e:  dup
  IL_035f:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0364:  ldc.i4.0
  IL_0365:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_036a:  pop
  IL_036b:  dup
  IL_036c:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0371:  dup
  IL_0372:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).get_Values() As System.Collections.Generic.ICollection(Of Integer)"
  IL_0377:  pop
  IL_0378:  dup
  IL_0379:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_037e:  ldc.i4.0
  IL_037f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0384:  pop
  IL_0385:  dup
  IL_0386:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_038b:  dup
  IL_038c:  ldc.i4.1
  IL_038d:  ldloca.s   V_6
  IL_038f:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).TryGetValue(Integer, ByRef Integer) As Boolean"
  IL_0394:  stloc.s    V_7
  IL_0396:  dup
  IL_0397:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_039c:  ldc.i4.s   17
  IL_039e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_03a3:  pop
  IL_03a4:  ldloc.s    V_6
  IL_03a6:  box        "Integer"
  IL_03ab:  ldc.i4.2
  IL_03ac:  box        "Integer"
  IL_03b1:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_03b6:  pop
  IL_03b7:  ldloc.s    V_7
  IL_03b9:  box        "Boolean"
  IL_03be:  ldc.i4.1
  IL_03bf:  box        "Boolean"
  IL_03c4:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_03c9:  pop
  IL_03ca:  dup
  IL_03cb:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_03d0:  dup
  IL_03d1:  ldc.i4.3
  IL_03d2:  ldc.i4.4
  IL_03d3:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_03d8:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Add(System.Collections.Generic.KeyValuePair(Of Integer, Integer))"
  IL_03dd:  dup
  IL_03de:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_03e3:  ldc.i4.s   21
  IL_03e5:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_03ea:  pop
  IL_03eb:  dup
  IL_03ec:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_03f1:  box        "Integer"
  IL_03f6:  ldc.i4.2
  IL_03f7:  box        "Integer"
  IL_03fc:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0401:  pop
  IL_0402:  dup
  IL_0403:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0408:  dup
  IL_0409:  ldc.i4.3
  IL_040a:  ldc.i4.4
  IL_040b:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0410:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_0415:  stloc.s    V_8
  IL_0417:  dup
  IL_0418:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_041d:  ldc.i4.s   17
  IL_041f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0424:  pop
  IL_0425:  ldloc.s    V_8
  IL_0427:  box        "Boolean"
  IL_042c:  ldc.i4.1
  IL_042d:  box        "Boolean"
  IL_0432:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0437:  pop
  IL_0438:  dup
  IL_0439:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_043e:  dup
  IL_043f:  ldc.i4.8
  IL_0440:  ldc.i4.s   9
  IL_0442:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0447:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_044c:  stloc.s    V_8
  IL_044e:  dup
  IL_044f:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0454:  ldc.i4.s   19
  IL_0456:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_045b:  pop
  IL_045c:  ldloc.s    V_8
  IL_045e:  box        "Boolean"
  IL_0463:  ldc.i4.0
  IL_0464:  box        "Boolean"
  IL_0469:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_046e:  pop
  IL_046f:  dup
  IL_0470:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0475:  dup
  IL_0476:  ldc.i4.1
  IL_0477:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Integer).Remove(Integer) As Boolean"
  IL_047c:  stloc.s    V_9
  IL_047e:  dup
  IL_047f:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0484:  ldc.i4.s   22
  IL_0486:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_048b:  pop
  IL_048c:  dup
  IL_048d:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_0492:  box        "Integer"
  IL_0497:  ldc.i4.1
  IL_0498:  box        "Integer"
  IL_049d:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_04a2:  pop
  IL_04a3:  ldloc.s    V_9
  IL_04a5:  box        "Boolean"
  IL_04aa:  ldc.i4.1
  IL_04ab:  box        "Boolean"
  IL_04b0:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_04b5:  pop
  IL_04b6:  dup
  IL_04b7:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_04bc:  dup
  IL_04bd:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_04c2:  stloc.2
  IL_04c3:  dup
  IL_04c4:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_04c9:  ldc.i4.s   18
  IL_04cb:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_04d0:  pop
  IL_04d1:  ldloc.2
  IL_04d2:  box        "Integer"
  IL_04d7:  ldc.i4.1
  IL_04d8:  box        "Integer"
  IL_04dd:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_04e2:  pop
  IL_04e3:  dup
  IL_04e4:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_04e9:  dup
  IL_04ea:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_IsReadOnly() As Boolean"
  IL_04ef:  stloc.s    V_4
  IL_04f1:  dup
  IL_04f2:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_04f7:  ldc.i4.0
  IL_04f8:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_04fd:  pop
  IL_04fe:  ldloc.s    V_4
  IL_0500:  box        "Boolean"
  IL_0505:  ldc.i4.0
  IL_0506:  box        "Boolean"
  IL_050b:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0510:  pop
  IL_0511:  dup
  IL_0512:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0517:  dup
  IL_0518:  ldc.i4.3
  IL_0519:  ldc.i4.4
  IL_051a:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_051f:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Remove(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_0524:  stloc.s    V_10
  IL_0526:  dup
  IL_0527:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_052c:  ldc.i4.s   22
  IL_052e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0533:  pop
  IL_0534:  ldloc.s    V_10
  IL_0536:  box        "Boolean"
  IL_053b:  ldc.i4.1
  IL_053c:  box        "Boolean"
  IL_0541:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0546:  pop
  IL_0547:  dup
  IL_0548:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_054d:  dup
  IL_054e:  ldc.i4.2
  IL_054f:  ldc.i4.3
  IL_0550:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0555:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Remove(System.Collections.Generic.KeyValuePair(Of Integer, Integer)) As Boolean"
  IL_055a:  stloc.s    V_10
  IL_055c:  dup
  IL_055d:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0562:  ldc.i4.s   19
  IL_0564:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0569:  pop
  IL_056a:  ldloc.s    V_10
  IL_056c:  box        "Boolean"
  IL_0571:  ldc.i4.0
  IL_0572:  box        "Boolean"
  IL_0577:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_057c:  pop
  IL_057d:  dup
  IL_057e:  ldc.i4.1
  IL_057f:  ldc.i4.2
  IL_0580:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_0585:  dup
  IL_0586:  ldc.i4.2
  IL_0587:  ldc.i4.3
  IL_0588:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_058d:  dup
  IL_058e:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_0593:  dup
  IL_0594:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).Clear()"
  IL_0599:  dup
  IL_059a:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_059f:  ldc.i4.s   23
  IL_05a1:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_05a6:  pop
  IL_05a7:  dup
  IL_05a8:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_05ad:  box        "Integer"
  IL_05b2:  ldc.i4.0
  IL_05b3:  box        "Integer"
  IL_05b8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_05bd:  pop
  IL_05be:  dup
  IL_05bf:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()"
  IL_05c4:  dup
  IL_05c5:  callvirt   "Function System.Collections.Generic.IReadOnlyCollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_05ca:  stloc.2
  IL_05cb:  callvirt   "Function Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_05d0:  ldc.i4.3
  IL_05d1:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_05d6:  pop
  IL_05d7:  ret
}
]]>.Value)

            verifier.VerifyIL("AllMembers.TestIVectorStructIVectorViewStructIMapIntStructIMapViewIntStructMembers", <![CDATA[
{
  // Code size     1378 (0x562)
  .maxstack  5
  .locals init (Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct V_0, //v
                Windows.Languages.WinRTTest.UserDefinedStruct V_1, //ud
                Windows.Languages.WinRTTest.UserDefinedStruct() V_2, //arr
                Integer V_3, //count
                Integer V_4, //index
                Boolean V_5, //isReadOnly
                Windows.Languages.WinRTTest.UserDefinedStruct V_6, //outVal
                Boolean V_7, //success
                Boolean V_8, //contains
                Windows.Languages.WinRTTest.UserDefinedStruct V_9,
                System.Collections.Generic.IEnumerator(Of Windows.Languages.WinRTTest.UserDefinedStruct) V_10)
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_9
  IL_0008:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_000e:  ldloca.s   V_9
  IL_0010:  ldc.i4.1
  IL_0011:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0016:  ldloc.s    V_9
  IL_0018:  stloc.1
  IL_0019:  ldloc.0
  IL_001a:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_001f:  ldloc.0
  IL_0020:  ldloc.1
  IL_0021:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).Add(Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0026:  ldloc.0
  IL_0027:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_002c:  ldc.i4.s   9
  IL_002e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0033:  pop
  IL_0034:  ldloc.0
  IL_0035:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_003a:  ldloc.0
  IL_003b:  ldloc.1
  IL_003c:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).Contains(Windows.Languages.WinRTTest.UserDefinedStruct) As Boolean"
  IL_0041:  ldloc.0
  IL_0042:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0047:  ldc.i4.5
  IL_0048:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_004d:  pop
  IL_004e:  box        "Boolean"
  IL_0053:  ldc.i4.1
  IL_0054:  box        "Boolean"
  IL_0059:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_005e:  pop
  IL_005f:  ldloc.0
  IL_0060:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0065:  ldc.i4.0
  IL_0066:  newarr     "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_006b:  stloc.2
  IL_006c:  ldloc.0
  IL_006d:  ldloc.2
  IL_006e:  ldc.i4.0
  IL_006f:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).CopyTo(Windows.Languages.WinRTTest.UserDefinedStruct(), Integer)"
  IL_0074:  ldloc.0
  IL_0075:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_007a:  ldc.i4.2
  IL_007b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0080:  pop
  IL_0081:  ldloc.2
  IL_0082:  ldc.i4.0
  IL_0083:  ldelema    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_0088:  ldfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_008d:  box        "UInteger"
  IL_0092:  ldloc.1
  IL_0093:  ldfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0098:  box        "UInteger"
  IL_009d:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00a2:  pop
  IL_00a3:  ldloc.0
  IL_00a4:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_00a9:  ldloc.0
  IL_00aa:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Count() As Integer"
  IL_00af:  stloc.3
  IL_00b0:  ldloc.0
  IL_00b1:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Windows.Languages.WinRTTest.UserDefinedStruct).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_00b6:  ldloc.0
  IL_00b7:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00bc:  ldc.i4.1
  IL_00bd:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00c2:  pop
  IL_00c3:  dup
  IL_00c4:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_00c9:  pop
  IL_00ca:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Current() As Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_00cf:  ldfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_00d4:  box        "UInteger"
  IL_00d9:  ldc.i4.1
  IL_00da:  box        "Integer"
  IL_00df:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00e4:  pop
  IL_00e5:  ldc.i4.0
  IL_00e6:  stloc.s    V_4
  .try
  {
    IL_00e8:  ldloc.0
    IL_00e9:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Windows.Languages.WinRTTest.UserDefinedStruct).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Windows.Languages.WinRTTest.UserDefinedStruct)"
    IL_00ee:  stloc.s    V_10
    IL_00f0:  br.s       IL_0116
    IL_00f2:  ldloc.s    V_10
    IL_00f4:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Current() As Windows.Languages.WinRTTest.UserDefinedStruct"
    IL_00f9:  ldloc.s    V_4
    IL_00fb:  ldc.i4.1
    IL_00fc:  add.ovf
    IL_00fd:  stloc.s    V_4
    IL_00ff:  ldfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
    IL_0104:  box        "UInteger"
    IL_0109:  ldloc.s    V_4
    IL_010b:  box        "Integer"
    IL_0110:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
    IL_0115:  pop
    IL_0116:  ldloc.s    V_10
    IL_0118:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_011d:  brtrue.s   IL_00f2
    IL_011f:  leave.s    IL_012d
  }
  finally
  {
    IL_0121:  ldloc.s    V_10
    IL_0123:  brfalse.s  IL_012c
    IL_0125:  ldloc.s    V_10
    IL_0127:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_012c:  endfinally
  }
  IL_012d:  ldloc.s    V_4
  IL_012f:  box        "Integer"
  IL_0134:  ldc.i4.1
  IL_0135:  box        "Integer"
  IL_013a:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_013f:  pop
  IL_0140:  ldloc.0
  IL_0141:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0146:  ldloc.0
  IL_0147:  ldloc.1
  IL_0148:  callvirt   "Function System.Collections.Generic.IList(Of Windows.Languages.WinRTTest.UserDefinedStruct).IndexOf(Windows.Languages.WinRTTest.UserDefinedStruct) As Integer"
  IL_014d:  ldloc.0
  IL_014e:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0153:  ldc.i4.5
  IL_0154:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0159:  pop
  IL_015a:  box        "Integer"
  IL_015f:  ldc.i4.0
  IL_0160:  box        "Integer"
  IL_0165:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_016a:  pop
  IL_016b:  ldloc.0
  IL_016c:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0171:  ldloc.0
  IL_0172:  ldc.i4.1
  IL_0173:  ldloca.s   V_9
  IL_0175:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_017b:  ldloca.s   V_9
  IL_017d:  ldc.i4.4
  IL_017e:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0183:  ldloc.s    V_9
  IL_0185:  callvirt   "Sub System.Collections.Generic.IList(Of Windows.Languages.WinRTTest.UserDefinedStruct).Insert(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_018a:  ldloc.0
  IL_018b:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0190:  ldc.i4.7
  IL_0191:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0196:  pop
  IL_0197:  ldloc.0
  IL_0198:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_019d:  ldloc.0
  IL_019e:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_IsReadOnly() As Boolean"
  IL_01a3:  stloc.s    V_5
  IL_01a5:  ldloc.0
  IL_01a6:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01ab:  ldc.i4.0
  IL_01ac:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01b1:  pop
  IL_01b2:  ldloc.s    V_5
  IL_01b4:  box        "Boolean"
  IL_01b9:  ldc.i4.0
  IL_01ba:  box        "Boolean"
  IL_01bf:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01c4:  pop
  IL_01c5:  ldloc.0
  IL_01c6:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_01cb:  ldloc.0
  IL_01cc:  ldc.i4.0
  IL_01cd:  callvirt   "Function System.Collections.Generic.IList(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Item(Integer) As Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_01d2:  pop
  IL_01d3:  ldloc.0
  IL_01d4:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01d9:  ldc.i4.2
  IL_01da:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01df:  pop
  IL_01e0:  ldloc.0
  IL_01e1:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_01e6:  ldloc.0
  IL_01e7:  ldc.i4.1
  IL_01e8:  callvirt   "Function System.Collections.Generic.IList(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Item(Integer) As Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_01ed:  pop
  IL_01ee:  ldloc.0
  IL_01ef:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01f4:  ldc.i4.2
  IL_01f5:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01fa:  pop
  IL_01fb:  ldloc.0
  IL_01fc:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0201:  ldloc.0
  IL_0202:  ldloc.1
  IL_0203:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).Remove(Windows.Languages.WinRTTest.UserDefinedStruct) As Boolean"
  IL_0208:  pop
  IL_0209:  ldloc.0
  IL_020a:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_020f:  ldc.i4.8
  IL_0210:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0215:  pop
  IL_0216:  ldloc.0
  IL_0217:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Count() As Integer"
  IL_021c:  box        "Integer"
  IL_0221:  ldc.i4.1
  IL_0222:  box        "Integer"
  IL_0227:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_022c:  pop
  IL_022d:  ldloc.0
  IL_022e:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0233:  ldloc.0
  IL_0234:  ldc.i4.0
  IL_0235:  callvirt   "Sub System.Collections.Generic.IList(Of Windows.Languages.WinRTTest.UserDefinedStruct).RemoveAt(Integer)"
  IL_023a:  ldloc.0
  IL_023b:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0240:  ldc.i4.8
  IL_0241:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0246:  pop
  IL_0247:  ldloc.0
  IL_0248:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Count() As Integer"
  IL_024d:  box        "Integer"
  IL_0252:  ldc.i4.0
  IL_0253:  box        "Integer"
  IL_0258:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_025d:  pop
  IL_025e:  ldloc.0
  IL_025f:  ldloc.1
  IL_0260:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).Add(Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0265:  ldloc.0
  IL_0266:  ldloca.s   V_9
  IL_0268:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_026e:  ldloca.s   V_9
  IL_0270:  ldc.i4.4
  IL_0271:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_0276:  ldloc.s    V_9
  IL_0278:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).Add(Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_027d:  ldloc.0
  IL_027e:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0283:  ldloc.0
  IL_0284:  callvirt   "Sub System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).Clear()"
  IL_0289:  ldloc.0
  IL_028a:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_028f:  ldc.i4.s   11
  IL_0291:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0296:  pop
  IL_0297:  ldloc.0
  IL_0298:  callvirt   "Function System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Count() As Integer"
  IL_029d:  box        "Integer"
  IL_02a2:  ldc.i4.0
  IL_02a3:  box        "Integer"
  IL_02a8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_02ad:  pop
  IL_02ae:  ldloc.0
  IL_02af:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_02b4:  ldloc.0
  IL_02b5:  callvirt   "Function System.Collections.Generic.IReadOnlyCollection(Of Windows.Languages.WinRTTest.UserDefinedStruct).get_Count() As Integer"
  IL_02ba:  stloc.3
  IL_02bb:  ldloc.0
  IL_02bc:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02c1:  ldc.i4.3
  IL_02c2:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02c7:  pop
  IL_02c8:  ldloc.0
  IL_02c9:  ldloca.s   V_9
  IL_02cb:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_02d1:  ldloca.s   V_9
  IL_02d3:  ldc.i4.s   10
  IL_02d5:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_02da:  ldloc.s    V_9
  IL_02dc:  stloc.1
  IL_02dd:  dup
  IL_02de:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_02e3:  dup
  IL_02e4:  ldc.i4.1
  IL_02e5:  ldloc.1
  IL_02e6:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Add(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_02eb:  dup
  IL_02ec:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02f1:  ldc.i4.s   21
  IL_02f3:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02f8:  pop
  IL_02f9:  dup
  IL_02fa:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_02ff:  box        "Integer"
  IL_0304:  ldc.i4.1
  IL_0305:  box        "Integer"
  IL_030a:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_030f:  pop
  IL_0310:  dup
  IL_0311:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0316:  dup
  IL_0317:  ldc.i4.1
  IL_0318:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).ContainsKey(Integer) As Boolean"
  IL_031d:  pop
  IL_031e:  dup
  IL_031f:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0324:  ldc.i4.s   19
  IL_0326:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_032b:  pop
  IL_032c:  dup
  IL_032d:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0332:  dup
  IL_0333:  ldc.i4.1
  IL_0334:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).get_Item(Integer) As Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_0339:  pop
  IL_033a:  dup
  IL_033b:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0340:  ldc.i4.s   17
  IL_0342:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0347:  pop
  IL_0348:  dup
  IL_0349:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_034e:  dup
  IL_034f:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).get_Keys() As System.Collections.Generic.ICollection(Of Integer)"
  IL_0354:  pop
  IL_0355:  dup
  IL_0356:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_035b:  ldc.i4.0
  IL_035c:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0361:  pop
  IL_0362:  dup
  IL_0363:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0368:  dup
  IL_0369:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).get_Values() As System.Collections.Generic.ICollection(Of Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_036e:  pop
  IL_036f:  dup
  IL_0370:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0375:  ldc.i4.0
  IL_0376:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_037b:  pop
  IL_037c:  dup
  IL_037d:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0382:  dup
  IL_0383:  ldc.i4.1
  IL_0384:  ldloca.s   V_6
  IL_0386:  callvirt   "Function System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).TryGetValue(Integer, ByRef Windows.Languages.WinRTTest.UserDefinedStruct) As Boolean"
  IL_038b:  stloc.s    V_7
  IL_038d:  dup
  IL_038e:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0393:  ldc.i4.s   17
  IL_0395:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_039a:  pop
  IL_039b:  ldloc.s    V_7
  IL_039d:  box        "Boolean"
  IL_03a2:  ldc.i4.1
  IL_03a3:  box        "Boolean"
  IL_03a8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_03ad:  pop
  IL_03ae:  dup
  IL_03af:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_03b4:  dup
  IL_03b5:  ldc.i4.3
  IL_03b6:  ldloca.s   V_9
  IL_03b8:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_03be:  ldloca.s   V_9
  IL_03c0:  ldc.i4.4
  IL_03c1:  stfld      "Windows.Languages.WinRTTest.UserDefinedStruct.Id As UInteger"
  IL_03c6:  ldloc.s    V_9
  IL_03c8:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_03cd:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Add(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct))"
  IL_03d2:  dup
  IL_03d3:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_03d8:  ldc.i4.s   21
  IL_03da:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_03df:  pop
  IL_03e0:  dup
  IL_03e1:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_03e6:  box        "Integer"
  IL_03eb:  ldc.i4.2
  IL_03ec:  box        "Integer"
  IL_03f1:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_03f6:  pop
  IL_03f7:  dup
  IL_03f8:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_03fd:  dup
  IL_03fe:  ldc.i4.1
  IL_03ff:  ldloc.1
  IL_0400:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0405:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)) As Boolean"
  IL_040a:  stloc.s    V_8
  IL_040c:  dup
  IL_040d:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0412:  ldc.i4.s   17
  IL_0414:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0419:  pop
  IL_041a:  ldloc.s    V_8
  IL_041c:  box        "Boolean"
  IL_0421:  ldc.i4.1
  IL_0422:  box        "Boolean"
  IL_0427:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_042c:  pop
  IL_042d:  dup
  IL_042e:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0433:  dup
  IL_0434:  ldc.i4.8
  IL_0435:  ldloca.s   V_9
  IL_0437:  initobj    "Windows.Languages.WinRTTest.UserDefinedStruct"
  IL_043d:  ldloc.s    V_9
  IL_043f:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0444:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Contains(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)) As Boolean"
  IL_0449:  stloc.s    V_8
  IL_044b:  dup
  IL_044c:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0451:  ldc.i4.s   19
  IL_0453:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0458:  pop
  IL_0459:  ldloc.s    V_8
  IL_045b:  box        "Boolean"
  IL_0460:  ldc.i4.0
  IL_0461:  box        "Boolean"
  IL_0466:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_046b:  pop
  IL_046c:  dup
  IL_046d:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0472:  dup
  IL_0473:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_0478:  dup
  IL_0479:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_047e:  stloc.3
  IL_047f:  dup
  IL_0480:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0485:  ldc.i4.s   18
  IL_0487:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_048c:  pop
  IL_048d:  ldloc.3
  IL_048e:  box        "Integer"
  IL_0493:  ldc.i4.1
  IL_0494:  box        "Integer"
  IL_0499:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_049e:  pop
  IL_049f:  dup
  IL_04a0:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_04a5:  dup
  IL_04a6:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_IsReadOnly() As Boolean"
  IL_04ab:  stloc.s    V_5
  IL_04ad:  dup
  IL_04ae:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_04b3:  ldc.i4.0
  IL_04b4:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_04b9:  pop
  IL_04ba:  ldloc.s    V_5
  IL_04bc:  box        "Boolean"
  IL_04c1:  ldc.i4.0
  IL_04c2:  box        "Boolean"
  IL_04c7:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_04cc:  pop
  IL_04cd:  dup
  IL_04ce:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_04d3:  dup
  IL_04d4:  ldc.i4.1
  IL_04d5:  ldloc.1
  IL_04d6:  newobj     "Sub System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)..ctor(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_04db:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Remove(System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)) As Boolean"
  IL_04e0:  pop
  IL_04e1:  dup
  IL_04e2:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_04e7:  ldc.i4.s   22
  IL_04e9:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_04ee:  pop
  IL_04ef:  dup
  IL_04f0:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_04f5:  box        "Integer"
  IL_04fa:  ldc.i4.0
  IL_04fb:  box        "Integer"
  IL_0500:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0505:  pop
  IL_0506:  dup
  IL_0507:  ldc.i4.1
  IL_0508:  ldloc.1
  IL_0509:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Add(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_050e:  dup
  IL_050f:  ldc.i4.2
  IL_0510:  ldloc.1
  IL_0511:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct).Add(Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_0516:  dup
  IL_0517:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_051c:  dup
  IL_051d:  callvirt   "Sub System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).Clear()"
  IL_0522:  dup
  IL_0523:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0528:  ldc.i4.s   23
  IL_052a:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_052f:  pop
  IL_0530:  dup
  IL_0531:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_0536:  box        "Integer"
  IL_053b:  ldc.i4.0
  IL_053c:  box        "Integer"
  IL_0541:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0546:  pop
  IL_0547:  dup
  IL_0548:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()"
  IL_054d:  dup
  IL_054e:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)).get_Count() As Integer"
  IL_0553:  stloc.3
  IL_0554:  callvirt   "Function Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0559:  ldc.i4.s   18
  IL_055b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0560:  pop
  IL_0561:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest05()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestISimpleInterfaceImplMembers()
        Dim v As ISimpleInterfaceImpl = New ISimpleInterfaceImpl()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue((TryCast(v, IList(Of Integer)))(0), 1)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(b, True)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(arr(0), 1)
        ValidateValue(arr(1), 0)
        v.ClearFlag()
        Dim count As Integer =(TryCast(v, IList(Of Integer))).Count
        Dim enumerator As IEnumerator(Of Integer) = DirectCast(v, IEnumerable(Of Integer)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        Dim index As Integer = 0
        For Each e In v
            index = index + 1
            ValidateValue(e, index)
        Next

        ValidateValue(index, 1)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez, 0)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        ValidateValue((TryCast(v, IList(Of Integer)))(1), 2)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        ValidateValue(isReadOnly, False)
        v.ClearFlag()
        Dim val As Integer =(TryCast(v, IList(Of Integer)))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 1)
        v.ClearFlag()
        val = DirectCast(v, IList(Of Integer))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        ValidateValue(val, 2)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue((TryCast(v, IList(Of Integer))).Count, 1)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt)
        ValidateValue((TryCast(v, IList(Of Integer))).Count, 0)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear)
        ValidateValue((TryCast(v, IList(Of Integer))).Count, 0)
    End Sub

    Shared Function Main() As Integer
        TestISimpleInterfaceImplMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestISimpleInterfaceImplMembers", <![CDATA[
{
  // Code size      662 (0x296)
  .maxstack  3
  .locals init (Windows.Languages.WinRTTest.ISimpleInterfaceImpl V_0, //v
  Integer() V_1, //arr
  Integer V_2, //index
  System.Collections.Generic.IEnumerator(Of Integer) V_3)
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0013:  ldloc.0
  IL_0014:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0019:  ldc.i4.s   9
  IL_001b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.0
  IL_0023:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_0028:  box        "Integer"
  IL_002d:  ldc.i4.1
  IL_002e:  box        "Integer"
  IL_0033:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.1
  IL_0041:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Contains(Integer) As Boolean"
  IL_0046:  ldloc.0
  IL_0047:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_004c:  ldc.i4.5
  IL_004d:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0052:  pop
  IL_0053:  box        "Boolean"
  IL_0058:  ldc.i4.1
  IL_0059:  box        "Boolean"
  IL_005e:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0063:  pop
  IL_0064:  ldloc.0
  IL_0065:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_006a:  ldc.i4.0
  IL_006b:  newarr     "Integer"
  IL_0070:  stloc.1
  IL_0071:  ldloc.0
  IL_0072:  ldloc.1
  IL_0073:  ldc.i4.0
  IL_0074:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).CopyTo(Integer(), Integer)"
  IL_0079:  ldloc.0
  IL_007a:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_007f:  ldc.i4.2
  IL_0080:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0085:  pop
  IL_0086:  ldloc.1
  IL_0087:  ldc.i4.0
  IL_0088:  ldelem.i4
  IL_0089:  box        "Integer"
  IL_008e:  ldc.i4.1
  IL_008f:  box        "Integer"
  IL_0094:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0099:  pop
  IL_009a:  ldloc.1
  IL_009b:  ldc.i4.1
  IL_009c:  ldelem.i4
  IL_009d:  box        "Integer"
  IL_00a2:  ldc.i4.0
  IL_00a3:  box        "Integer"
  IL_00a8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00ad:  pop
  IL_00ae:  ldloc.0
  IL_00af:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_00b4:  ldloc.0
  IL_00b5:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_00ba:  pop
  IL_00bb:  ldloc.0
  IL_00bc:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_00c1:  pop
  IL_00c2:  ldloc.0
  IL_00c3:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00c8:  ldc.i4.1
  IL_00c9:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00ce:  pop
  IL_00cf:  ldc.i4.0
  IL_00d0:  stloc.2
  .try
{
  IL_00d1:  ldloc.0
  IL_00d2:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_00d7:  stloc.3
  IL_00d8:  br.s       IL_00f5
  IL_00da:  ldloc.3
  IL_00db:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
  IL_00e0:  ldloc.2
  IL_00e1:  ldc.i4.1
  IL_00e2:  add.ovf
  IL_00e3:  stloc.2
  IL_00e4:  box        "Integer"
  IL_00e9:  ldloc.2
  IL_00ea:  box        "Integer"
  IL_00ef:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00f4:  pop
  IL_00f5:  ldloc.3
  IL_00f6:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_00fb:  brtrue.s   IL_00da
  IL_00fd:  leave.s    IL_0109
}
  finally
{
  IL_00ff:  ldloc.3
  IL_0100:  brfalse.s  IL_0108
  IL_0102:  ldloc.3
  IL_0103:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0108:  endfinally
}
  IL_0109:  ldloc.2
  IL_010a:  box        "Integer"
  IL_010f:  ldc.i4.1
  IL_0110:  box        "Integer"
  IL_0115:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_011a:  pop
  IL_011b:  ldloc.0
  IL_011c:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_0121:  ldloc.0
  IL_0122:  ldc.i4.1
  IL_0123:  callvirt   "Function System.Collections.Generic.IList(Of Integer).IndexOf(Integer) As Integer"
  IL_0128:  ldloc.0
  IL_0129:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_012e:  ldc.i4.5
  IL_012f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0134:  pop
  IL_0135:  box        "Integer"
  IL_013a:  ldc.i4.0
  IL_013b:  box        "Integer"
  IL_0140:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0145:  pop
  IL_0146:  ldloc.0
  IL_0147:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_014c:  ldloc.0
  IL_014d:  ldc.i4.1
  IL_014e:  ldc.i4.2
  IL_014f:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).Insert(Integer, Integer)"
  IL_0154:  ldloc.0
  IL_0155:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_015a:  ldc.i4.7
  IL_015b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0160:  pop
  IL_0161:  ldloc.0
  IL_0162:  ldc.i4.1
  IL_0163:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_0168:  box        "Integer"
  IL_016d:  ldc.i4.2
  IL_016e:  box        "Integer"
  IL_0173:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0178:  pop
  IL_0179:  ldloc.0
  IL_017a:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_017f:  ldloc.0
  IL_0180:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_IsReadOnly() As Boolean"
  IL_0185:  ldloc.0
  IL_0186:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_018b:  ldc.i4.0
  IL_018c:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0191:  pop
  IL_0192:  box        "Boolean"
  IL_0197:  ldc.i4.0
  IL_0198:  box        "Boolean"
  IL_019d:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01a2:  pop
  IL_01a3:  ldloc.0
  IL_01a4:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_01a9:  ldloc.0
  IL_01aa:  ldc.i4.0
  IL_01ab:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_01b0:  ldloc.0
  IL_01b1:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01b6:  ldc.i4.2
  IL_01b7:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01bc:  pop
  IL_01bd:  box        "Integer"
  IL_01c2:  ldc.i4.1
  IL_01c3:  box        "Integer"
  IL_01c8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01cd:  pop
  IL_01ce:  ldloc.0
  IL_01cf:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_01d4:  ldloc.0
  IL_01d5:  ldc.i4.1
  IL_01d6:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_01db:  ldloc.0
  IL_01dc:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01e1:  ldc.i4.2
  IL_01e2:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01e7:  pop
  IL_01e8:  box        "Integer"
  IL_01ed:  ldc.i4.2
  IL_01ee:  box        "Integer"
  IL_01f3:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_01f8:  pop
  IL_01f9:  ldloc.0
  IL_01fa:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_01ff:  ldloc.0
  IL_0200:  ldc.i4.1
  IL_0201:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Remove(Integer) As Boolean"
  IL_0206:  pop
  IL_0207:  ldloc.0
  IL_0208:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_020d:  ldc.i4.8
  IL_020e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0213:  pop
  IL_0214:  ldloc.0
  IL_0215:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_021a:  box        "Integer"
  IL_021f:  ldc.i4.1
  IL_0220:  box        "Integer"
  IL_0225:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_022a:  pop
  IL_022b:  ldloc.0
  IL_022c:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_0231:  ldloc.0
  IL_0232:  ldc.i4.0
  IL_0233:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).RemoveAt(Integer)"
  IL_0238:  ldloc.0
  IL_0239:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_023e:  ldc.i4.8
  IL_023f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0244:  pop
  IL_0245:  ldloc.0
  IL_0246:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_024b:  box        "Integer"
  IL_0250:  ldc.i4.0
  IL_0251:  box        "Integer"
  IL_0256:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_025b:  pop
  IL_025c:  ldloc.0
  IL_025d:  ldc.i4.1
  IL_025e:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0263:  ldloc.0
  IL_0264:  ldc.i4.2
  IL_0265:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_026a:  ldloc.0
  IL_026b:  callvirt   "Sub Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()"
  IL_0270:  ldloc.0
  IL_0271:  callvirt   "Function Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0276:  ldc.i4.s   11
  IL_0278:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_027d:  pop
  IL_027e:  ldloc.0
  IL_027f:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_0284:  box        "Integer"
  IL_0289:  ldc.i4.0
  IL_028a:  box        "Integer"
  IL_028f:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0294:  pop
  IL_0295:  ret
}
]]>.Value)
        End Sub



        <Fact()>
        Public Sub LegacyCollectionTest06()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestCollectionInitializers()
        Dim v = New IVectorInt() From {1, 2, 3, 4, 5}
        ValidateValue(v.Count, 5)
        Dim m = New IMapIntInt() From {{1, 2}, {2, 3}}
        ValidateValue(m.Count, 2)
        Dim t = New Dictionary(Of Integer, IVectorInt)() From {{1, New IVectorInt() From {1, 2, 3}}, {2, New IVectorInt() From {4, 5, 6}}}
        ValidateValue(t(1)(2), 3)
        ValidateValue(t(2)(2), 6)
    End Sub

    Shared Function Main() As Integer
        TestCollectionInitializers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestCollectionInitializers", <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  6
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0013:  dup
  IL_0014:  ldc.i4.3
  IL_0015:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_001a:  dup
  IL_001b:  ldc.i4.4
  IL_001c:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0021:  dup
  IL_0022:  ldc.i4.5
  IL_0023:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0028:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_002d:  box        "Integer"
  IL_0032:  ldc.i4.5
  IL_0033:  box        "Integer"
  IL_0038:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_003d:  pop
  IL_003e:  newobj     "Sub Windows.Languages.WinRTTest.IMapIntInt..ctor()"
  IL_0043:  dup
  IL_0044:  ldc.i4.1
  IL_0045:  ldc.i4.2
  IL_0046:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_004b:  dup
  IL_004c:  ldc.i4.2
  IL_004d:  ldc.i4.3
  IL_004e:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_0053:  callvirt   "Function System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, Integer)).get_Count() As Integer"
  IL_0058:  box        "Integer"
  IL_005d:  ldc.i4.2
  IL_005e:  box        "Integer"
  IL_0063:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0068:  pop
  IL_0069:  newobj     "Sub System.Collections.Generic.Dictionary(Of Integer, Windows.Languages.WinRTTest.IVectorInt)..ctor()"
  IL_006e:  dup
  IL_006f:  ldc.i4.1
  IL_0070:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_0075:  dup
  IL_0076:  ldc.i4.1
  IL_0077:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_007c:  dup
  IL_007d:  ldc.i4.2
  IL_007e:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0083:  dup
  IL_0084:  ldc.i4.3
  IL_0085:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_008a:  callvirt   "Sub System.Collections.Generic.Dictionary(Of Integer, Windows.Languages.WinRTTest.IVectorInt).Add(Integer, Windows.Languages.WinRTTest.IVectorInt)"
  IL_008f:  dup
  IL_0090:  ldc.i4.2
  IL_0091:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_0096:  dup
  IL_0097:  ldc.i4.4
  IL_0098:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_009d:  dup
  IL_009e:  ldc.i4.5
  IL_009f:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_00a4:  dup
  IL_00a5:  ldc.i4.6
  IL_00a6:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_00ab:  callvirt   "Sub System.Collections.Generic.Dictionary(Of Integer, Windows.Languages.WinRTTest.IVectorInt).Add(Integer, Windows.Languages.WinRTTest.IVectorInt)"
  IL_00b0:  dup
  IL_00b1:  ldc.i4.1
  IL_00b2:  callvirt   "Function System.Collections.Generic.Dictionary(Of Integer, Windows.Languages.WinRTTest.IVectorInt).get_Item(Integer) As Windows.Languages.WinRTTest.IVectorInt"
  IL_00b7:  ldc.i4.2
  IL_00b8:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_00bd:  box        "Integer"
  IL_00c2:  ldc.i4.3
  IL_00c3:  box        "Integer"
  IL_00c8:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00cd:  pop
  IL_00ce:  ldc.i4.2
  IL_00cf:  callvirt   "Function System.Collections.Generic.Dictionary(Of Integer, Windows.Languages.WinRTTest.IVectorInt).get_Item(Integer) As Windows.Languages.WinRTTest.IVectorInt"
  IL_00d4:  ldc.i4.2
  IL_00d5:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_00da:  box        "Integer"
  IL_00df:  ldc.i4.6
  IL_00e0:  box        "Integer"
  IL_00e5:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00ea:  pop
  IL_00eb:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest07()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestExpressionTreeCompiler()
        Dim v = New IVectorInt()
        Try
            Console.WriteLine("Dev11:205875")
            ValidateValue(True, True)
            Dim expr As Expression(Of Action(Of Integer)) = Sub(val) v.Add(val)
            v.ClearFlag()
            expr.Compile()(1)
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        Catch e As Exception
            Console.WriteLine("ExprTree compiler")
            Console.WriteLine(e.Message)
        End Try
    End Sub

    Shared Function Main() As Integer
        TestExpressionTreeCompiler()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestExpressionTreeCompiler", <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  6
  .locals init (AllMembers._Closure$__5-0 V_0, //$VB$Closure_0
                System.Linq.Expressions.ParameterExpression V_1,
                System.Exception V_2) //e
  IL_0000:  newobj     "Sub AllMembers._Closure$__5-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_000c:  stfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  .try
  {
    IL_0011:  ldstr      "Dev11:205875"
    IL_0016:  call       "Sub System.Console.WriteLine(String)"
    IL_001b:  ldc.i4.1
    IL_001c:  box        "Boolean"
    IL_0021:  ldc.i4.1
    IL_0022:  box        "Boolean"
    IL_0027:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
    IL_002c:  pop
    IL_002d:  ldtoken    "Integer"
    IL_0032:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
    IL_0037:  ldstr      "val"
    IL_003c:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
    IL_0041:  stloc.1
    IL_0042:  ldloc.0
    IL_0043:  ldtoken    "AllMembers._Closure$__5-0"
    IL_0048:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
    IL_004d:  call       "Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression"
    IL_0052:  ldtoken    "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
    IL_0057:  call       "Function System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle) As System.Reflection.FieldInfo"
    IL_005c:  call       "Function System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo) As System.Linq.Expressions.MemberExpression"
    IL_0061:  ldtoken    "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
    IL_0066:  ldtoken    "System.Collections.Generic.ICollection(Of Integer)"
    IL_006b:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle) As System.Reflection.MethodBase"
    IL_0070:  castclass  "System.Reflection.MethodInfo"
    IL_0075:  ldc.i4.1
    IL_0076:  newarr     "System.Linq.Expressions.Expression"
    IL_007b:  dup
    IL_007c:  ldc.i4.0
    IL_007d:  ldloc.1
    IL_007e:  stelem.ref
    IL_007f:  call       "Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression"
    IL_0084:  ldc.i4.1
    IL_0085:  newarr     "System.Linq.Expressions.ParameterExpression"
    IL_008a:  dup
    IL_008b:  ldc.i4.0
    IL_008c:  ldloc.1
    IL_008d:  stelem.ref
    IL_008e:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Action(Of Integer))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Action(Of Integer))"
    IL_0093:  ldloc.0
    IL_0094:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
    IL_0099:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorInt.ClearFlag()"
    IL_009e:  callvirt   "Function System.Linq.Expressions.Expression(Of System.Action(Of Integer)).Compile() As System.Action(Of Integer)"
    IL_00a3:  ldc.i4.1
    IL_00a4:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
    IL_00a9:  ldloc.0
    IL_00aa:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
    IL_00af:  callvirt   "Function Windows.Languages.WinRTTest.IVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
    IL_00b4:  ldc.i4.s   9
    IL_00b6:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
    IL_00bb:  pop
    IL_00bc:  leave.s    IL_00e1
  }
  catch System.Exception
  {
    IL_00be:  dup
    IL_00bf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c4:  stloc.2
    IL_00c5:  ldstr      "ExprTree compiler"
    IL_00ca:  call       "Sub System.Console.WriteLine(String)"
    IL_00cf:  ldloc.2
    IL_00d0:  callvirt   "Function System.Exception.get_Message() As String"
    IL_00d5:  call       "Sub System.Console.WriteLine(String)"
    IL_00da:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00df:  leave.s    IL_00e1
  }
  IL_00e1:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest09()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestLINQ()
        Dim v = New IVectorInt() From {1, 2, 3, 4, 5}
        ValidateValue(v.Count, 5)
        v.ClearFlag()
        Dim rez = From e In New Integer() {2, 4, 6, 10, 12} Where v.Contains(e) Select e
        rez = rez.ToList()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        ValidateValue(rez.Count(), 2)
        ValidateValue(rez.ToArray()(0), 2)
        ValidateValue(rez.ToArray()(1), 4)
        rez = From e In v Where e Mod 2 = 0 Select e
        rez = rez.ToList()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        ValidateValue(rez.Count(), 2)
        Try
            Console.WriteLine("Dev11:205875")
            ValidateValue(False, False)
        Catch e As ArgumentException
            Console.WriteLine("TestLINQ")
            Console.WriteLine(e.Message)
        End Try
    End Sub

    Shared Function Main() As Integer
        TestLINQ()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                options:=TestOptions.ReleaseExe.WithModuleName("MODULE"),
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestLINQ", <![CDATA[
{
  // Code size      460 (0x1cc)
  .maxstack  4
  .locals init (AllMembers._Closure$__5-0 V_0, //$VB$Closure_0
                System.ArgumentException V_1) //e
  IL_0000:  newobj     "Sub AllMembers._Closure$__5-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_001a:  dup
  IL_001b:  ldc.i4.3
  IL_001c:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0021:  dup
  IL_0022:  ldc.i4.4
  IL_0023:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0028:  dup
  IL_0029:  ldc.i4.5
  IL_002a:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_002f:  stfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  IL_0034:  ldloc.0
  IL_0035:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  IL_003a:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_003f:  box        "Integer"
  IL_0044:  ldc.i4.5
  IL_0045:  box        "Integer"
  IL_004a:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_004f:  pop
  IL_0050:  ldloc.0
  IL_0051:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  IL_0056:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorInt.ClearFlag()"
  IL_005b:  ldc.i4.5
  IL_005c:  newarr     "Integer"
  IL_0061:  dup
  IL_0062:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.A4161100F9A38A73DDA6BAB5DE1C8D59C39708CBBBF384A489FEA6385940EBFE"
  IL_0067:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_006c:  ldloc.0
  IL_006d:  ldftn      "Function AllMembers._Closure$__5-0._Lambda$__0(Integer) As Boolean"
  IL_0073:  newobj     "Sub System.Func(Of Integer, Boolean)..ctor(Object, System.IntPtr)"
  IL_0078:  call       "Function System.Linq.Enumerable.Where(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer), System.Func(Of Integer, Boolean)) As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_007d:  ldsfld     "AllMembers._Closure$__.$I5-1 As System.Func(Of Integer, Integer)"
  IL_0082:  brfalse.s  IL_008b
  IL_0084:  ldsfld     "AllMembers._Closure$__.$I5-1 As System.Func(Of Integer, Integer)"
  IL_0089:  br.s       IL_00a1
  IL_008b:  ldsfld     "AllMembers._Closure$__.$I As AllMembers._Closure$__"
  IL_0090:  ldftn      "Function AllMembers._Closure$__._Lambda$__5-1(Integer) As Integer"
  IL_0096:  newobj     "Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_009b:  dup
  IL_009c:  stsfld     "AllMembers._Closure$__.$I5-1 As System.Func(Of Integer, Integer)"
  IL_00a1:  call       "Function System.Linq.Enumerable.Select(Of Integer, Integer)(System.Collections.Generic.IEnumerable(Of Integer), System.Func(Of Integer, Integer)) As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_00a6:  call       "Function System.Linq.Enumerable.ToList(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As System.Collections.Generic.List(Of Integer)"
  IL_00ab:  ldloc.0
  IL_00ac:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  IL_00b1:  callvirt   "Function Windows.Languages.WinRTTest.IVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00b6:  ldc.i4.5
  IL_00b7:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00bc:  pop
  IL_00bd:  dup
  IL_00be:  call       "Function System.Linq.Enumerable.Count(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As Integer"
  IL_00c3:  box        "Integer"
  IL_00c8:  ldc.i4.2
  IL_00c9:  box        "Integer"
  IL_00ce:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00d3:  pop
  IL_00d4:  dup
  IL_00d5:  call       "Function System.Linq.Enumerable.ToArray(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As Integer()"
  IL_00da:  ldc.i4.0
  IL_00db:  ldelem.i4
  IL_00dc:  box        "Integer"
  IL_00e1:  ldc.i4.2
  IL_00e2:  box        "Integer"
  IL_00e7:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_00ec:  pop
  IL_00ed:  call       "Function System.Linq.Enumerable.ToArray(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As Integer()"
  IL_00f2:  ldc.i4.1
  IL_00f3:  ldelem.i4
  IL_00f4:  box        "Integer"
  IL_00f9:  ldc.i4.4
  IL_00fa:  box        "Integer"
  IL_00ff:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0104:  pop
  IL_0105:  ldloc.0
  IL_0106:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  IL_010b:  ldsfld     "AllMembers._Closure$__.$I5-2 As System.Func(Of Integer, Boolean)"
  IL_0110:  brfalse.s  IL_0119
  IL_0112:  ldsfld     "AllMembers._Closure$__.$I5-2 As System.Func(Of Integer, Boolean)"
  IL_0117:  br.s       IL_012f
  IL_0119:  ldsfld     "AllMembers._Closure$__.$I As AllMembers._Closure$__"
  IL_011e:  ldftn      "Function AllMembers._Closure$__._Lambda$__5-2(Integer) As Boolean"
  IL_0124:  newobj     "Sub System.Func(Of Integer, Boolean)..ctor(Object, System.IntPtr)"
  IL_0129:  dup
  IL_012a:  stsfld     "AllMembers._Closure$__.$I5-2 As System.Func(Of Integer, Boolean)"
  IL_012f:  call       "Function System.Linq.Enumerable.Where(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer), System.Func(Of Integer, Boolean)) As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0134:  ldsfld     "AllMembers._Closure$__.$I5-3 As System.Func(Of Integer, Integer)"
  IL_0139:  brfalse.s  IL_0142
  IL_013b:  ldsfld     "AllMembers._Closure$__.$I5-3 As System.Func(Of Integer, Integer)"
  IL_0140:  br.s       IL_0158
  IL_0142:  ldsfld     "AllMembers._Closure$__.$I As AllMembers._Closure$__"
  IL_0147:  ldftn      "Function AllMembers._Closure$__._Lambda$__5-3(Integer) As Integer"
  IL_014d:  newobj     "Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_0152:  dup
  IL_0153:  stsfld     "AllMembers._Closure$__.$I5-3 As System.Func(Of Integer, Integer)"
  IL_0158:  call       "Function System.Linq.Enumerable.Select(Of Integer, Integer)(System.Collections.Generic.IEnumerable(Of Integer), System.Func(Of Integer, Integer)) As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_015d:  call       "Function System.Linq.Enumerable.ToList(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As System.Collections.Generic.List(Of Integer)"
  IL_0162:  ldloc.0
  IL_0163:  ldfld      "AllMembers._Closure$__5-0.$VB$Local_v As Windows.Languages.WinRTTest.IVectorInt"
  IL_0168:  callvirt   "Function Windows.Languages.WinRTTest.IVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_016d:  ldc.i4.1
  IL_016e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0173:  pop
  IL_0174:  call       "Function System.Linq.Enumerable.Count(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As Integer"
  IL_0179:  box        "Integer"
  IL_017e:  ldc.i4.2
  IL_017f:  box        "Integer"
  IL_0184:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0189:  pop
  .try
  {
    IL_018a:  ldstr      "Dev11:205875"
    IL_018f:  call       "Sub System.Console.WriteLine(String)"
    IL_0194:  ldc.i4.0
    IL_0195:  box        "Boolean"
    IL_019a:  ldc.i4.0
    IL_019b:  box        "Boolean"
    IL_01a0:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
    IL_01a5:  pop
    IL_01a6:  leave.s    IL_01cb
  }
  catch System.ArgumentException
  {
    IL_01a8:  dup
    IL_01a9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_01ae:  stloc.1
    IL_01af:  ldstr      "TestLINQ"
    IL_01b4:  call       "Sub System.Console.WriteLine(String)"
    IL_01b9:  ldloc.1
    IL_01ba:  callvirt   "Function System.ArgumentException.get_Message() As String"
    IL_01bf:  call       "Sub System.Console.WriteLine(String)"
    IL_01c4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_01c9:  leave.s    IL_01cb
  }
  IL_01cb:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest10()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestNamedArguments()
        Dim v = New IVectorInt()
        v.ClearFlag()
        v.Add(item:=1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(v.Count, 1)
        Dim m = New IMapIntInt()
        m.ClearFlag()
        m.Add(key:=1, value:=1)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
        m.ClearFlag()
        m.Add(2, value:=2)
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert)
    End Sub

    Shared Function Main() As Integer
        TestNamedArguments()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestNamedArguments", <![CDATA[
{
  // Code size      115 (0x73)
  .maxstack  4
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_0005:  dup
  IL_0006:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorInt.ClearFlag()"
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0012:  dup
  IL_0013:  callvirt   "Function Windows.Languages.WinRTTest.IVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0018:  ldc.i4.s   9
  IL_001a:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_001f:  pop
  IL_0020:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_0025:  box        "Integer"
  IL_002a:  ldc.i4.1
  IL_002b:  box        "Integer"
  IL_0030:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_0035:  pop
  IL_0036:  newobj     "Sub Windows.Languages.WinRTTest.IMapIntInt..ctor()"
  IL_003b:  dup
  IL_003c:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_0041:  dup
  IL_0042:  ldc.i4.1
  IL_0043:  ldc.i4.1
  IL_0044:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_0049:  dup
  IL_004a:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_004f:  ldc.i4.s   21
  IL_0051:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0056:  pop
  IL_0057:  dup
  IL_0058:  callvirt   "Sub Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()"
  IL_005d:  dup
  IL_005e:  ldc.i4.2
  IL_005f:  ldc.i4.2
  IL_0060:  callvirt   "Sub System.Collections.Generic.IDictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_0065:  callvirt   "Function Windows.Languages.WinRTTest.IMapIntInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_006a:  ldc.i4.s   21
  IL_006c:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0071:  pop
  IL_0072:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest11()
            Dim source =
    <compilation>
        <file name="c.vb">
            <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Private Shared Function ValidateValue(actual As Object, expected As Object) As Boolean
        Dim temp = Console.ForegroundColor
        If actual.ToString() <> expected.ToString()
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual.ToString() = expected.ToString()
    End Function

    Shared Sub TestNullableArgs()
        Console.WriteLine("===  IVectorInt - nullable ===")
        Dim v = New IVectorInt()
        v.ClearFlag()
        Dim x As Integer? = 1
        v.Add(x.Value)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        ValidateValue(v(0), 1)
    End Sub

    Shared Function Main() As Integer
        TestNullableArgs()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
        </file>
    </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestNullableArgs", <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (Integer? V_0) //x
  IL_0000:  ldstr      "===  IVectorInt - nullable ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IVectorInt..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.IVectorInt.ClearFlag()"
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldc.i4.1
  IL_0018:  call       "Sub Integer?..ctor(Integer)"
  IL_001d:  dup
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       "Function Integer?.get_Value() As Integer"
  IL_0025:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_002a:  dup
  IL_002b:  callvirt   "Function Windows.Languages.WinRTTest.IVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0030:  ldc.i4.s   9
  IL_0032:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0037:  pop
  IL_0038:  ldc.i4.0
  IL_0039:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_003e:  box        "Integer"
  IL_0043:  ldc.i4.1
  IL_0044:  box        "Integer"
  IL_0049:  call       "Function AllMembers.ValidateValue(Object, Object) As Boolean"
  IL_004e:  pop
  IL_004f:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest12()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Collections
Imports System.Collections.Generic
Imports Windows.Foundation.Collections

Namespace Test

    Public Class R
        Implements IObservableVector(Of Integer)

        Public Shared Sub Main()
        End Sub

        Public Event VectorChanged As VectorChangedEventHandler(Of Integer) _
            Implements IObservableVector(Of Integer).VectorChanged

        Public Function IndexOf(item As Integer) As Integer _
            Implements IObservableVector(Of Integer).IndexOf
            Throw New NotImplementedException()
        End Function

        Public Sub Insert(index As Integer, item As Integer) _
            Implements IObservableVector(Of Integer).Insert
            Throw New NotImplementedException()
        End Sub

        Public Sub RemoveAt(index As Integer) _
            Implements IObservableVector(Of Integer).RemoveAt
            Throw New NotImplementedException()
        End Sub

        Default Property Item(index As Integer) As Integer _
            Implements IList(Of Integer).Item
            Get
                Throw New NotImplementedException()
            End Get

            Set(value As Integer)
                Throw New NotImplementedException()
            End Set
        End Property

        Public Sub Add(item As Integer) _
            Implements IObservableVector(Of Integer).Add
            Throw New NotImplementedException()
        End Sub

        Public Sub Clear() _
            Implements IObservableVector(Of Integer).Clear
            Throw New NotImplementedException()
        End Sub

        Public Function Contains(item As Integer) As Boolean _
            Implements IObservableVector(Of Integer).Contains
            Throw New NotImplementedException()
        End Function

        Public Sub CopyTo(array As Integer(), arrayIndex As Integer) _
            Implements IObservableVector(Of Integer).CopyTo
            Throw New NotImplementedException()
        End Sub

        Public ReadOnly Property Count As Integer _
            Implements IObservableVector(Of Integer).Count
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public ReadOnly Property IsReadOnly As Boolean _
            Implements IObservableVector(Of Integer).IsReadOnly
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Function Remove(item As Integer) As Boolean _
            Implements IObservableVector(Of Integer).Remove
            Throw New NotImplementedException()
        End Function

        Public Function GetEnumerator() As IEnumerator(Of Integer) _
            Implements IObservableVector(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
]]>
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:=LegacyRefs)
            CompilationUtils.AssertTheseDiagnostics(comp)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest13()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub TestIBindableVectorMembers()
        Console.WriteLine("===  IBindableVectorSimple  ===")
        Dim v = New IBindableVectorSimple()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        Dim count As Integer = v.Count
        Dim enumerator As IEnumerator = DirectCast(v, IEnumerable).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableIterable_First)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_InsertAt)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        v.ClearFlag()
        Dim val As Object = v(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt)
        v.ClearFlag()
        val = v(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear)
    End Sub

    Shared Function Main() As Integer
        TestIBindableVectorMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestIBindableVectorMembers", <![CDATA[
{
  // Code size      421 (0x1a5)
  .maxstack  3
  .locals init (Windows.Languages.WinRTTest.IBindableVectorSimple V_0, //v
  Integer() V_1) //arr
  IL_0000:  ldstr      "===  IBindableVectorSimple  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IBindableVectorSimple..ctor()"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  box        "Integer"
  IL_001d:  callvirt   "Function System.Collections.IList.Add(Object) As Integer"
  IL_0022:  pop
  IL_0023:  ldloc.0
  IL_0024:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0029:  ldc.i4.s   28
  IL_002b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0030:  pop
  IL_0031:  ldloc.0
  IL_0032:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_0037:  ldloc.0
  IL_0038:  ldc.i4.1
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Function System.Collections.IList.Contains(Object) As Boolean"
  IL_0043:  pop
  IL_0044:  ldloc.0
  IL_0045:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_004a:  ldc.i4.s   30
  IL_004c:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0051:  pop
  IL_0052:  ldloc.0
  IL_0053:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_0058:  ldc.i4.0
  IL_0059:  newarr     "Integer"
  IL_005e:  stloc.1
  IL_005f:  ldloc.0
  IL_0060:  ldloc.1
  IL_0061:  ldc.i4.0
  IL_0062:  callvirt   "Sub System.Collections.ICollection.CopyTo(System.Array, Integer)"
  IL_0067:  ldloc.0
  IL_0068:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_006d:  ldc.i4.s   28
  IL_006f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0074:  pop
  IL_0075:  ldloc.0
  IL_0076:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_007b:  ldloc.0
  IL_007c:  callvirt   "Function System.Collections.ICollection.get_Count() As Integer"
  IL_0081:  pop
  IL_0082:  ldloc.0
  IL_0083:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0088:  pop
  IL_0089:  ldloc.0
  IL_008a:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_008f:  ldc.i4.s   26
  IL_0091:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0096:  pop
  IL_0097:  ldloc.0
  IL_0098:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_009d:  ldloc.0
  IL_009e:  ldc.i4.1
  IL_009f:  box        "Integer"
  IL_00a4:  callvirt   "Function System.Collections.IList.IndexOf(Object) As Integer"
  IL_00a9:  pop
  IL_00aa:  ldloc.0
  IL_00ab:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00b0:  ldc.i4.s   30
  IL_00b2:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00b7:  pop
  IL_00b8:  ldloc.0
  IL_00b9:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_00be:  ldloc.0
  IL_00bf:  ldc.i4.1
  IL_00c0:  ldc.i4.2
  IL_00c1:  box        "Integer"
  IL_00c6:  callvirt   "Sub System.Collections.IList.Insert(Integer, Object)"
  IL_00cb:  ldloc.0
  IL_00cc:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00d1:  ldc.i4.s   32
  IL_00d3:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00d8:  pop
  IL_00d9:  ldloc.0
  IL_00da:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_00df:  ldloc.0
  IL_00e0:  callvirt   "Function System.Collections.IList.get_IsReadOnly() As Boolean"
  IL_00e5:  pop
  IL_00e6:  ldloc.0
  IL_00e7:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00ec:  ldc.i4.0
  IL_00ed:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00f2:  pop
  IL_00f3:  ldloc.0
  IL_00f4:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_00f9:  ldloc.0
  IL_00fa:  ldc.i4.0
  IL_00fb:  callvirt   "Function System.Collections.IList.get_Item(Integer) As Object"
  IL_0100:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0105:  pop
  IL_0106:  ldloc.0
  IL_0107:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_010c:  ldc.i4.s   27
  IL_010e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0113:  pop
  IL_0114:  ldloc.0
  IL_0115:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_011a:  ldloc.0
  IL_011b:  ldc.i4.1
  IL_011c:  callvirt   "Function System.Collections.IList.get_Item(Integer) As Object"
  IL_0121:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0126:  pop
  IL_0127:  ldloc.0
  IL_0128:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_012d:  ldc.i4.s   27
  IL_012f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0134:  pop
  IL_0135:  ldloc.0
  IL_0136:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_013b:  ldloc.0
  IL_013c:  ldc.i4.1
  IL_013d:  box        "Integer"
  IL_0142:  callvirt   "Sub System.Collections.IList.Remove(Object)"
  IL_0147:  ldloc.0
  IL_0148:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_014d:  ldc.i4.s   30
  IL_014f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0154:  pop
  IL_0155:  ldloc.0
  IL_0156:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_015b:  ldloc.0
  IL_015c:  ldc.i4.0
  IL_015d:  callvirt   "Sub System.Collections.IList.RemoveAt(Integer)"
  IL_0162:  ldloc.0
  IL_0163:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0168:  ldc.i4.s   33
  IL_016a:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_016f:  pop
  IL_0170:  ldloc.0
  IL_0171:  ldc.i4.1
  IL_0172:  box        "Integer"
  IL_0177:  callvirt   "Function System.Collections.IList.Add(Object) As Integer"
  IL_017c:  pop
  IL_017d:  ldloc.0
  IL_017e:  ldc.i4.2
  IL_017f:  box        "Integer"
  IL_0184:  callvirt   "Function System.Collections.IList.Add(Object) As Integer"
  IL_0189:  pop
  IL_018a:  ldloc.0
  IL_018b:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()"
  IL_0190:  ldloc.0
  IL_0191:  callvirt   "Sub System.Collections.IList.Clear()"
  IL_0196:  ldloc.0
  IL_0197:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_019c:  ldc.i4.s   36
  IL_019e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01a3:  pop
  IL_01a4:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest14()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub TestIBindableIterableMembers()
        Console.WriteLine("===  IBindableIterableSimple  ===")
        Dim v = New IBindableIterableSimple()
        v.ClearFlag()
        v.GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableIterable_First)
    End Sub

    Shared Function Main() As Integer
        TestIBindableIterableMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestIBindableIterableMembers", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldstr      "===  IBindableIterableSimple  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IBindableIterableSimple..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableIterableSimple.ClearFlag()"
  IL_0015:  dup
  IL_0016:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_001b:  pop
  IL_001c:  callvirt   "Function Windows.Languages.WinRTTest.IBindableIterableSimple.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0021:  ldc.i4.s   26
  IL_0023:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0028:  pop
  IL_0029:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest15()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub TestIBindableVectorIVectorIntMembers()
        Console.WriteLine("===  IBindableVectorIVectorIntSimple  ===")
        Dim v = New IBindableVectorIVectorInt()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        Dim count As Integer = DirectCast(v, IList).Count
        Dim enumerator As IEnumerator = DirectCast(v, IEnumerable).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_InsertAt)
        v.ClearFlag()
        Dim isReadOnly As Boolean = DirectCast(v, IList).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        v.ClearFlag()
        Dim val As Object = DirectCast(v, IList)(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt)
        v.ClearFlag()
        val = DirectCast(v, IList)(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        DirectCast(v, IList).RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        DirectCast(v, IList).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear)
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append)
        v.ClearFlag()
        b = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        v.ClearFlag()
        arr = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        count = DirectCast(v, IList(Of Integer)).Count
        Dim enumerator2 As IEnumerator(Of Integer) = DirectCast(v, IList(Of Integer)).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        v.ClearFlag()
        rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt)
        v.ClearFlag()
        isReadOnly = DirectCast(v, IList(Of Integer)).IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        v.ClearFlag()
        val = DirectCast(v, IList(Of Integer))(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        v.ClearFlag()
        val = DirectCast(v, IList(Of Integer))(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        DirectCast(v, IList(Of Integer)).Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear)
    End Sub

    Shared Function Main() As Integer
        TestIBindableVectorIVectorIntMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestIBindableVectorIVectorIntMembers", <![CDATA[
{
  // Code size      743 (0x2e7)
  .maxstack  3
  .locals init (Windows.Languages.WinRTTest.IBindableVectorIVectorInt V_0, //v
  Integer() V_1) //arr
  IL_0000:  ldstr      "===  IBindableVectorIVectorIntSimple  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt..ctor()"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_001d:  ldloc.0
  IL_001e:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0023:  ldc.i4.s   28
  IL_0025:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0031:  ldloc.0
  IL_0032:  ldc.i4.1
  IL_0033:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Contains(Integer) As Boolean"
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_003f:  ldc.i4.s   30
  IL_0041:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0046:  pop
  IL_0047:  ldloc.0
  IL_0048:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_004d:  ldc.i4.0
  IL_004e:  newarr     "Integer"
  IL_0053:  stloc.1
  IL_0054:  ldloc.0
  IL_0055:  ldloc.1
  IL_0056:  ldc.i4.0
  IL_0057:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).CopyTo(Integer(), Integer)"
  IL_005c:  ldloc.0
  IL_005d:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0062:  ldc.i4.s   28
  IL_0064:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0069:  pop
  IL_006a:  ldloc.0
  IL_006b:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0070:  ldloc.0
  IL_0071:  callvirt   "Function System.Collections.ICollection.get_Count() As Integer"
  IL_0076:  pop
  IL_0077:  ldloc.0
  IL_0078:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_007d:  pop
  IL_007e:  ldloc.0
  IL_007f:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0084:  ldc.i4.1
  IL_0085:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_008a:  pop
  IL_008b:  ldloc.0
  IL_008c:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0091:  ldloc.0
  IL_0092:  ldc.i4.1
  IL_0093:  callvirt   "Function System.Collections.Generic.IList(Of Integer).IndexOf(Integer) As Integer"
  IL_0098:  pop
  IL_0099:  ldloc.0
  IL_009a:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_009f:  ldc.i4.s   30
  IL_00a1:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00a6:  pop
  IL_00a7:  ldloc.0
  IL_00a8:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_00ad:  ldloc.0
  IL_00ae:  ldc.i4.1
  IL_00af:  ldc.i4.2
  IL_00b0:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).Insert(Integer, Integer)"
  IL_00b5:  ldloc.0
  IL_00b6:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00bb:  ldc.i4.s   32
  IL_00bd:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00c2:  pop
  IL_00c3:  ldloc.0
  IL_00c4:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_00c9:  ldloc.0
  IL_00ca:  callvirt   "Function System.Collections.IList.get_IsReadOnly() As Boolean"
  IL_00cf:  pop
  IL_00d0:  ldloc.0
  IL_00d1:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00d6:  ldc.i4.0
  IL_00d7:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00dc:  pop
  IL_00dd:  ldloc.0
  IL_00de:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_00e3:  ldloc.0
  IL_00e4:  ldc.i4.0
  IL_00e5:  callvirt   "Function System.Collections.IList.get_Item(Integer) As Object"
  IL_00ea:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_00ef:  pop
  IL_00f0:  ldloc.0
  IL_00f1:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00f6:  ldc.i4.s   27
  IL_00f8:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00fd:  pop
  IL_00fe:  ldloc.0
  IL_00ff:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0104:  ldloc.0
  IL_0105:  ldc.i4.1
  IL_0106:  callvirt   "Function System.Collections.IList.get_Item(Integer) As Object"
  IL_010b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0110:  pop
  IL_0111:  ldloc.0
  IL_0112:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0117:  ldc.i4.s   27
  IL_0119:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_011e:  pop
  IL_011f:  ldloc.0
  IL_0120:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0125:  ldloc.0
  IL_0126:  ldc.i4.1
  IL_0127:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Remove(Integer) As Boolean"
  IL_012c:  pop
  IL_012d:  ldloc.0
  IL_012e:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0133:  ldc.i4.s   30
  IL_0135:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_013a:  pop
  IL_013b:  ldloc.0
  IL_013c:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0141:  ldloc.0
  IL_0142:  ldc.i4.0
  IL_0143:  callvirt   "Sub System.Collections.IList.RemoveAt(Integer)"
  IL_0148:  ldloc.0
  IL_0149:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_014e:  ldc.i4.s   33
  IL_0150:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0155:  pop
  IL_0156:  ldloc.0
  IL_0157:  ldc.i4.1
  IL_0158:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_015d:  ldloc.0
  IL_015e:  ldc.i4.2
  IL_015f:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_0164:  ldloc.0
  IL_0165:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_016a:  ldloc.0
  IL_016b:  callvirt   "Sub System.Collections.IList.Clear()"
  IL_0170:  ldloc.0
  IL_0171:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0176:  ldc.i4.s   36
  IL_0178:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_017d:  pop
  IL_017e:  ldloc.0
  IL_017f:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0184:  ldloc.0
  IL_0185:  ldc.i4.1
  IL_0186:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_018b:  ldloc.0
  IL_018c:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0191:  ldc.i4.s   9
  IL_0193:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0198:  pop
  IL_0199:  ldloc.0
  IL_019a:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_019f:  ldloc.0
  IL_01a0:  ldc.i4.1
  IL_01a1:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Contains(Integer) As Boolean"
  IL_01a6:  pop
  IL_01a7:  ldloc.0
  IL_01a8:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01ad:  ldc.i4.5
  IL_01ae:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01b3:  pop
  IL_01b4:  ldloc.0
  IL_01b5:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_01ba:  ldc.i4.0
  IL_01bb:  newarr     "Integer"
  IL_01c0:  stloc.1
  IL_01c1:  ldloc.0
  IL_01c2:  ldloc.1
  IL_01c3:  ldc.i4.0
  IL_01c4:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).CopyTo(Integer(), Integer)"
  IL_01c9:  ldloc.0
  IL_01ca:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01cf:  ldc.i4.s   28
  IL_01d1:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01d6:  pop
  IL_01d7:  ldloc.0
  IL_01d8:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_01dd:  ldloc.0
  IL_01de:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_Count() As Integer"
  IL_01e3:  pop
  IL_01e4:  ldloc.0
  IL_01e5:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_01ea:  pop
  IL_01eb:  ldloc.0
  IL_01ec:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01f1:  ldc.i4.1
  IL_01f2:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01f7:  pop
  IL_01f8:  ldloc.0
  IL_01f9:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_01fe:  ldloc.0
  IL_01ff:  ldc.i4.1
  IL_0200:  callvirt   "Function System.Collections.Generic.IList(Of Integer).IndexOf(Integer) As Integer"
  IL_0205:  pop
  IL_0206:  ldloc.0
  IL_0207:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_020c:  ldc.i4.5
  IL_020d:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0212:  pop
  IL_0213:  ldloc.0
  IL_0214:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0219:  ldloc.0
  IL_021a:  ldc.i4.1
  IL_021b:  ldc.i4.2
  IL_021c:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).Insert(Integer, Integer)"
  IL_0221:  ldloc.0
  IL_0222:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0227:  ldc.i4.7
  IL_0228:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_022d:  pop
  IL_022e:  ldloc.0
  IL_022f:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_0234:  ldloc.0
  IL_0235:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).get_IsReadOnly() As Boolean"
  IL_023a:  pop
  IL_023b:  ldloc.0
  IL_023c:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0241:  ldc.i4.0
  IL_0242:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0247:  pop
  IL_0248:  ldloc.0
  IL_0249:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_024e:  ldloc.0
  IL_024f:  ldc.i4.0
  IL_0250:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_0255:  box        "Integer"
  IL_025a:  pop
  IL_025b:  ldloc.0
  IL_025c:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0261:  ldc.i4.2
  IL_0262:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0267:  pop
  IL_0268:  ldloc.0
  IL_0269:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_026e:  ldloc.0
  IL_026f:  ldc.i4.1
  IL_0270:  callvirt   "Function System.Collections.Generic.IList(Of Integer).get_Item(Integer) As Integer"
  IL_0275:  box        "Integer"
  IL_027a:  pop
  IL_027b:  ldloc.0
  IL_027c:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0281:  ldc.i4.2
  IL_0282:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0287:  pop
  IL_0288:  ldloc.0
  IL_0289:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_028e:  ldloc.0
  IL_028f:  ldc.i4.1
  IL_0290:  callvirt   "Function System.Collections.Generic.ICollection(Of Integer).Remove(Integer) As Boolean"
  IL_0295:  pop
  IL_0296:  ldloc.0
  IL_0297:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_029c:  ldc.i4.5
  IL_029d:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02a2:  pop
  IL_02a3:  ldloc.0
  IL_02a4:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_02a9:  ldloc.0
  IL_02aa:  ldc.i4.0
  IL_02ab:  callvirt   "Sub System.Collections.Generic.IList(Of Integer).RemoveAt(Integer)"
  IL_02b0:  ldloc.0
  IL_02b1:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02b6:  ldc.i4.s   33
  IL_02b8:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02bd:  pop
  IL_02be:  ldloc.0
  IL_02bf:  ldc.i4.1
  IL_02c0:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_02c5:  ldloc.0
  IL_02c6:  ldc.i4.2
  IL_02c7:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Add(Integer)"
  IL_02cc:  ldloc.0
  IL_02cd:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()"
  IL_02d2:  ldloc.0
  IL_02d3:  callvirt   "Sub System.Collections.Generic.ICollection(Of Integer).Clear()"
  IL_02d8:  ldloc.0
  IL_02d9:  callvirt   "Function Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_02de:  ldc.i4.s   36
  IL_02e0:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_02e5:  pop
  IL_02e6:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest16()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub TestIBindableIterableIIterableMembers()
        Console.WriteLine("===  IBindableIterableIIterable  ===")
        Dim v = New IBindableIterableIIterable()
        v.ClearFlag()
        DirectCast(v, IEnumerable).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
    End Sub

    Shared Function Main() As Integer
        TestIBindableIterableIIterableMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.TestIBindableIterableIIterableMembers", <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldstr      "===  IBindableIterableIIterable  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.IBindableIterableIIterable..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.IBindableIterableIIterable.ClearFlag()"
  IL_0015:  dup
  IL_0016:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_001b:  pop
  IL_001c:  callvirt   "Function Windows.Languages.WinRTTest.IBindableIterableIIterable.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0021:  ldc.i4.1
  IL_0022:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0027:  pop
  IL_0028:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest17()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected Then
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub INotifyCollectionAndBindableVectorMembers()
        Console.WriteLine("===  INotifyCollectionAndBindableVectorClass  ===")
        Dim v = New INotifyCollectionAndBindableVectorClass()
        v.ClearFlag()
        v.Add(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        Dim b As Boolean = v.Contains(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        Dim arr As Integer() = New Integer() {}
        v.CopyTo(arr, 0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size)
        v.ClearFlag()
        Dim count As Integer = v.Count
        Dim enumerator As IEnumerator = DirectCast(v, IEnumerable).GetEnumerator()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableIterable_First)
        v.ClearFlag()
        Dim rez = v.IndexOf(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        v.Insert(1, 2)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_InsertAt)
        v.ClearFlag()
        Dim isReadOnly As Boolean = v.IsReadOnly
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet)
        v.ClearFlag()
        Dim val As Object = v(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt)
        v.ClearFlag()
        val = v(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt)
        v.ClearFlag()
        v.Remove(1)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf)
        v.ClearFlag()
        v.RemoveAt(0)
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt)
        v.Add(1)
        v.Add(2)
        v.ClearFlag()
        v.Clear()
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear)
        v.ClearFlag()
        Dim dele = New System.Collections.Specialized.NotifyCollectionChangedEventHandler(AddressOf v_CollectionChanged)
        AddHandler v.CollectionChanged, dele
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Add_CollectionChanged)
        v.ClearFlag()
        RemoveHandler v.CollectionChanged, dele
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Remove_CollectionChanged)
    End Sub

    Shared Sub v_PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Shared Sub v_CollectionChanged(sender As Object, e As System.Collections.Specialized.NotifyCollectionChangedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Shared Function Main() As Integer
        INotifyCollectionAndBindableVectorMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>


            Dim verifier = CompileAndVerify(
                source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.INotifyCollectionAndBindableVectorMembers", <![CDATA[
{
  // Code size      488 (0x1e8)
  .maxstack  3
  .locals init (Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass V_0, //v
  Integer() V_1, //arr
  System.Collections.Specialized.NotifyCollectionChangedEventHandler V_2) //dele
  IL_0000:  ldstr      "===  INotifyCollectionAndBindableVectorClass  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass..ctor()"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  box        "Integer"
  IL_001d:  callvirt   "Function System.Collections.IList.Add(Object) As Integer"
  IL_0022:  pop
  IL_0023:  ldloc.0
  IL_0024:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0029:  ldc.i4.s   28
  IL_002b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0030:  pop
  IL_0031:  ldloc.0
  IL_0032:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_0037:  ldloc.0
  IL_0038:  ldc.i4.1
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Function System.Collections.IList.Contains(Object) As Boolean"
  IL_0043:  pop
  IL_0044:  ldloc.0
  IL_0045:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_004a:  ldc.i4.s   30
  IL_004c:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0051:  pop
  IL_0052:  ldloc.0
  IL_0053:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_0058:  ldc.i4.0
  IL_0059:  newarr     "Integer"
  IL_005e:  stloc.1
  IL_005f:  ldloc.0
  IL_0060:  ldloc.1
  IL_0061:  ldc.i4.0
  IL_0062:  callvirt   "Sub System.Collections.ICollection.CopyTo(System.Array, Integer)"
  IL_0067:  ldloc.0
  IL_0068:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_006d:  ldc.i4.s   28
  IL_006f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0074:  pop
  IL_0075:  ldloc.0
  IL_0076:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_007b:  ldloc.0
  IL_007c:  callvirt   "Function System.Collections.ICollection.get_Count() As Integer"
  IL_0081:  pop
  IL_0082:  ldloc.0
  IL_0083:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0088:  pop
  IL_0089:  ldloc.0
  IL_008a:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_008f:  ldc.i4.s   26
  IL_0091:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0096:  pop
  IL_0097:  ldloc.0
  IL_0098:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_009d:  ldloc.0
  IL_009e:  ldc.i4.1
  IL_009f:  box        "Integer"
  IL_00a4:  callvirt   "Function System.Collections.IList.IndexOf(Object) As Integer"
  IL_00a9:  pop
  IL_00aa:  ldloc.0
  IL_00ab:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00b0:  ldc.i4.s   30
  IL_00b2:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00b7:  pop
  IL_00b8:  ldloc.0
  IL_00b9:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_00be:  ldloc.0
  IL_00bf:  ldc.i4.1
  IL_00c0:  ldc.i4.2
  IL_00c1:  box        "Integer"
  IL_00c6:  callvirt   "Sub System.Collections.IList.Insert(Integer, Object)"
  IL_00cb:  ldloc.0
  IL_00cc:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00d1:  ldc.i4.s   32
  IL_00d3:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00d8:  pop
  IL_00d9:  ldloc.0
  IL_00da:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_00df:  ldloc.0
  IL_00e0:  callvirt   "Function System.Collections.IList.get_IsReadOnly() As Boolean"
  IL_00e5:  pop
  IL_00e6:  ldloc.0
  IL_00e7:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_00ec:  ldc.i4.0
  IL_00ed:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_00f2:  pop
  IL_00f3:  ldloc.0
  IL_00f4:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_00f9:  ldloc.0
  IL_00fa:  ldc.i4.0
  IL_00fb:  callvirt   "Function System.Collections.IList.get_Item(Integer) As Object"
  IL_0100:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0105:  pop
  IL_0106:  ldloc.0
  IL_0107:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_010c:  ldc.i4.s   27
  IL_010e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0113:  pop
  IL_0114:  ldloc.0
  IL_0115:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_011a:  ldloc.0
  IL_011b:  ldc.i4.1
  IL_011c:  callvirt   "Function System.Collections.IList.get_Item(Integer) As Object"
  IL_0121:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0126:  pop
  IL_0127:  ldloc.0
  IL_0128:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_012d:  ldc.i4.s   27
  IL_012f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0134:  pop
  IL_0135:  ldloc.0
  IL_0136:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_013b:  ldloc.0
  IL_013c:  ldc.i4.1
  IL_013d:  box        "Integer"
  IL_0142:  callvirt   "Sub System.Collections.IList.Remove(Object)"
  IL_0147:  ldloc.0
  IL_0148:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_014d:  ldc.i4.s   30
  IL_014f:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0154:  pop
  IL_0155:  ldloc.0
  IL_0156:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_015b:  ldloc.0
  IL_015c:  ldc.i4.0
  IL_015d:  callvirt   "Sub System.Collections.IList.RemoveAt(Integer)"
  IL_0162:  ldloc.0
  IL_0163:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0168:  ldc.i4.s   33
  IL_016a:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_016f:  pop
  IL_0170:  ldloc.0
  IL_0171:  ldc.i4.1
  IL_0172:  box        "Integer"
  IL_0177:  callvirt   "Function System.Collections.IList.Add(Object) As Integer"
  IL_017c:  pop
  IL_017d:  ldloc.0
  IL_017e:  ldc.i4.2
  IL_017f:  box        "Integer"
  IL_0184:  callvirt   "Function System.Collections.IList.Add(Object) As Integer"
  IL_0189:  pop
  IL_018a:  ldloc.0
  IL_018b:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_0190:  ldloc.0
  IL_0191:  callvirt   "Sub System.Collections.IList.Clear()"
  IL_0196:  ldloc.0
  IL_0197:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_019c:  ldc.i4.s   36
  IL_019e:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01a3:  pop
  IL_01a4:  ldloc.0
  IL_01a5:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_01aa:  ldnull
  IL_01ab:  ldftn      "Sub AllMembers.v_CollectionChanged(Object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)"
  IL_01b1:  newobj     "Sub System.Collections.Specialized.NotifyCollectionChangedEventHandler..ctor(Object, System.IntPtr)"
  IL_01b6:  stloc.2
  IL_01b7:  ldloc.0
  IL_01b8:  ldloc.2
  IL_01b9:  callvirt   "Sub System.Collections.Specialized.INotifyCollectionChanged.add_CollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventHandler)"
  IL_01be:  ldloc.0
  IL_01bf:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01c4:  ldc.i4.s   37
  IL_01c6:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01cb:  pop
  IL_01cc:  ldloc.0
  IL_01cd:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()"
  IL_01d2:  ldloc.0
  IL_01d3:  ldloc.2
  IL_01d4:  callvirt   "Sub System.Collections.Specialized.INotifyCollectionChanged.remove_CollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventHandler)"
  IL_01d9:  ldloc.0
  IL_01da:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_01df:  ldc.i4.s   38
  IL_01e1:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_01e6:  pop
  IL_01e7:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LegacyCollectionTest18()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub INotifyCollectionChangedMembers()
        Console.WriteLine("===  INotifyCollectionChangedClass  ===")
        Dim v = New INotifyCollectionChangedClass()
        v.ClearFlag()
        Dim dele = New System.Collections.Specialized.NotifyCollectionChangedEventHandler(AddressOf v_CollectionChanged)
        AddHandler v.CollectionChanged, dele
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Add_CollectionChanged)
        v.ClearFlag()
        RemoveHandler v.CollectionChanged, dele
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Remove_CollectionChanged)
    End Sub

    Shared Sub v_PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Shared Sub v_CollectionChanged(sender As Object, e As System.Collections.Specialized.NotifyCollectionChangedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Shared Function Main() As Integer
        INotifyCollectionChangedMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>


            Dim verifier = CompileAndVerify(
                source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.INotifyCollectionChangedMembers", <![CDATA[
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (System.Collections.Specialized.NotifyCollectionChangedEventHandler V_0) //dele
  IL_0000:  ldstr      "===  INotifyCollectionChangedClass  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.INotifyCollectionChangedClass..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionChangedClass.ClearFlag()"
  IL_0015:  ldnull
  IL_0016:  ldftn      "Sub AllMembers.v_CollectionChanged(Object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)"
  IL_001c:  newobj     "Sub System.Collections.Specialized.NotifyCollectionChangedEventHandler..ctor(Object, System.IntPtr)"
  IL_0021:  stloc.0
  IL_0022:  dup
  IL_0023:  ldloc.0
  IL_0024:  callvirt   "Sub System.Collections.Specialized.INotifyCollectionChanged.add_CollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventHandler)"
  IL_0029:  dup
  IL_002a:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionChangedClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_002f:  ldc.i4.s   37
  IL_0031:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0036:  pop
  IL_0037:  dup
  IL_0038:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyCollectionChangedClass.ClearFlag()"
  IL_003d:  dup
  IL_003e:  ldloc.0
  IL_003f:  callvirt   "Sub System.Collections.Specialized.INotifyCollectionChanged.remove_CollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventHandler)"
  IL_0044:  callvirt   "Function Windows.Languages.WinRTTest.INotifyCollectionChangedClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0049:  ldc.i4.s   38
  IL_004b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0050:  pop
  IL_0051:  ret
}
]]>.Value)
        End Sub

        <Fact>
        Public Sub LegacyCollectionTest20()
            Dim source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports Windows.Languages.WinRTTest
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Linq.Expressions
Imports System
Imports System.Linq
Imports System.Collections

Class AllMembers

    Private Shared FailedCount As Integer = 0

    Private Shared Function ValidateMethod(actual As TestMethodCalled, expected As TestMethodCalled) As Boolean
        Dim temp = Console.ForegroundColor
        If actual <> expected
            FailedCount = FailedCount + 1
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("FAIL:  ")
        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("PASS:  ")
        End If

        Console.ForegroundColor = temp
        Console.WriteLine("Expected: {0}, Actual: {1}", expected, actual)
        Return actual = expected
    End Function

    Shared Sub IPropertyChangedMembers()
        Console.WriteLine("===  INotifyCollectionChangedClass  ===")
        Dim v = New INotifyPropertyChangedClass()
        v.ClearFlag()
        Dim pdeleg = New System.ComponentModel.PropertyChangedEventHandler(AddressOf v_PropertyChanged)
        AddHandler v.PropertyChanged, pdeleg
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyPropertyChanged_Add_PropertyChanged)
        v.ClearFlag()
        RemoveHandler v.PropertyChanged, pdeleg
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyPropertyChanged_Remove_PropertyChanged)
    End Sub

    Shared Sub v_PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Shared Function Main() As Integer
        IPropertyChangedMembers()
        Console.WriteLine(FailedCount)
        Return FailedCount
    End Function
End Class
]]>
                    </file>
                </compilation>


            Dim verifier = CompileAndVerify(
                source,
                references:=LegacyRefs,
                verify:=Verification.Fails)
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("AllMembers.IPropertyChangedMembers", <![CDATA[
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (System.ComponentModel.PropertyChangedEventHandler V_0) //pdeleg
  IL_0000:  ldstr      "===  INotifyCollectionChangedClass  ==="
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  newobj     "Sub Windows.Languages.WinRTTest.INotifyPropertyChangedClass..ctor()"
  IL_000f:  dup
  IL_0010:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyPropertyChangedClass.ClearFlag()"
  IL_0015:  ldnull
  IL_0016:  ldftn      "Sub AllMembers.v_PropertyChanged(Object, System.ComponentModel.PropertyChangedEventArgs)"
  IL_001c:  newobj     "Sub System.ComponentModel.PropertyChangedEventHandler..ctor(Object, System.IntPtr)"
  IL_0021:  stloc.0
  IL_0022:  dup
  IL_0023:  ldloc.0
  IL_0024:  callvirt   "Sub System.ComponentModel.INotifyPropertyChanged.add_PropertyChanged(System.ComponentModel.PropertyChangedEventHandler)"
  IL_0029:  dup
  IL_002a:  callvirt   "Function Windows.Languages.WinRTTest.INotifyPropertyChangedClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_002f:  ldc.i4.s   39
  IL_0031:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0036:  pop
  IL_0037:  dup
  IL_0038:  callvirt   "Sub Windows.Languages.WinRTTest.INotifyPropertyChangedClass.ClearFlag()"
  IL_003d:  dup
  IL_003e:  ldloc.0
  IL_003f:  callvirt   "Sub System.ComponentModel.INotifyPropertyChanged.remove_PropertyChanged(System.ComponentModel.PropertyChangedEventHandler)"
  IL_0044:  callvirt   "Function Windows.Languages.WinRTTest.INotifyPropertyChangedClass.GetFlagState() As Windows.Languages.WinRTTest.TestMethodCalled"
  IL_0049:  ldc.i4.s   40
  IL_004b:  call       "Function AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled) As Boolean"
  IL_0050:  pop
  IL_0051:  ret
}
]]>.Value)
        End Sub

        <Fact>
        Public Sub MultipleDefaultProperties()
            Dim src =
                <compilation>
                    <file name="a.vb">
Imports System
Imports System.Linq
Imports Windows.Languages.WinRTTest

Class A
    Shared Sub Main()
        Dim mmv = new IMapIMapViewIntStruct()
        Dim x = mmv(1)
    End Sub                        
End Class
                    </file>
                </compilation>

            Dim comp = CompileAndVerify(src,
                                        references:=LegacyRefs,
                                        verify:=Verification.Fails,
                                        options:=TestOptions.ReleaseExe)

            comp.VerifyIL("A.Main", <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  newobj     "Sub Windows.Languages.WinRTTest.IMapIMapViewIntStruct..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  call       "Function System.Linq.Enumerable.ElementAtOrDefault(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct))(System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)), Integer) As System.Collections.Generic.KeyValuePair(Of Integer, Windows.Languages.WinRTTest.UserDefinedStruct)"
  IL_000b:  pop
  IL_000c:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub WinRTCompilationReference()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Namespace Test

    Public Class C
        Implements IEnumerable(Of Integer)

        Function GetEnumerator() As IEnumerator _
            Implements IEnumerable.GetEnumerator
            Return Nothing
        End Function

        Public Function GetEnumerator2() As IEnumerator(Of Integer) _
            Implements IEnumerable(Of Integer).GetEnumerator
            Return Nothing
        End Function
    End Class
End Namespace
]]></file>
                         </compilation>


            Dim verifier As CompilationVerifier = CompileAndVerify(source,
                options:=TestOptions.ReleaseWinMD,
                references:=WinRtRefs)

            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("Test.C.GetEnumerator()", <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}

]]>.Value)

            Dim compRef = verifier.Compilation.EmitToImageReference(expectedWarnings:={Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System")})
            Dim allRefs = New List(Of MetadataReference)(WinRtRefs)
            allRefs.Add(compRef)
            source =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports System
Imports Test

Namespace Test2

    Public Class D

        Public Shared Sub Main(args As String())
            Dim c = New C()
            Dim e = c.GetEnumerator()
        End Sub
    End Class
End Namespace
]]>
                    </file>
                </compilation>

            verifier = CompileAndVerify(source,
                references:=allRefs.ToArray())
            AssertNoErrorsOrWarnings(verifier)
            verifier.VerifyIL("Test2.D.Main", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  newobj     "Sub Test.C..ctor()"
  IL_0005:  callvirt   "Function Test.C.GetEnumerator() As System.Collections.IEnumerator"
  IL_000a:  pop
  IL_000b:  ret
}
]]>.Value)
        End Sub

        Private Shared Sub AssertNoErrorsOrWarnings(verifier As CompilationVerifier)
            verifier.Diagnostics.AsEnumerable().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()
        End Sub

        <Fact, WorkItem(1034461, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034461")>
        Public Sub Bug1034461()
            Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
Imports Windows.Data.Json

Public Class Class1

    Sub Test()
        Dim jsonObj = New JsonObject()
        jsonObj.Add("firstEntry", Nothing)
    End Sub
End Class
]]></file>
            </compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(source, WinRtRefs)
            comp.AssertNoDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim add = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.IdentifierName AndAlso DirectCast(n, IdentifierNameSyntax).Identifier.ValueText = "Add").Single()
            Dim addMethod = model.GetSymbolInfo(add).Symbol
            Assert.Equal("Sub System.Collections.Generic.IDictionary(Of System.String, Windows.Data.Json.IJsonValue).Add(key As System.String, value As Windows.Data.Json.IJsonValue)", addMethod.ToTestDisplayString())

            Dim jsonObj = DirectCast(add.Parent, MemberAccessExpressionSyntax).Expression

            Dim jsonObjType = model.GetTypeInfo(jsonObj).Type
            Assert.Equal("Windows.Data.Json.JsonObject", jsonObjType.ToTestDisplayString())

            Assert.True(model.LookupNames(add.SpanStart, jsonObjType).Contains("Add"))
            Assert.True(model.LookupSymbols(add.SpanStart, jsonObjType, "Add").Contains(addMethod))
            Assert.True(model.LookupSymbols(add.SpanStart, jsonObjType).Contains(addMethod))
        End Sub

    End Class
End Namespace
