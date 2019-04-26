// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class WinRTCollectionTests : CSharpTestBase
    {
        public static MetadataReference[] LegacyRefs
        { get; }
        =
        {
            AssemblyMetadata.CreateFromImage(TestResources.WinRt.Windows_Languages_WinRTTest).GetReference(display: "WinRTTest"),
            AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Core).GetReference(display: "SystemCore")
        };


        [Fact, WorkItem(762316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762316")]
        public void InheritFromTypeWithProjections()
        {
            var source = @"
using Windows.UI.Xaml;
 
public sealed class BehaviorCollection : DependencyObjectCollection
{
  private int count;
 
  public BehaviorCollection()
    {
        count = this.Count;
    }
  
  public object GetItem(int i)
    {
     return this[i];
    }
}";
            var comp = CreateEmptyCompilation(source, references: WinRtRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IVectorProjectionTests()
        {
            var source =
@"using System;
using Windows.Data.Json;

public class Class1
{
    public static void Main(string[] args)
    {
        var jsonArray = new JsonArray();
        var a = JsonValue.CreateStringValue(""a"");
        jsonArray.Add(a);
        var b = JsonValue.CreateStringValue(""b"");
        jsonArray.Insert(0, b);
        jsonArray.Remove(b);
        Console.WriteLine(jsonArray.Contains(b));
        Console.WriteLine(jsonArray.IndexOf(a));
        jsonArray.RemoveAt(0);
        Console.WriteLine(jsonArray.Count);
        jsonArray.Add(b);
        foreach (var json in jsonArray)
        {
            Console.WriteLine(json.GetString());
        }
        Console.WriteLine(jsonArray.Count);
        jsonArray.Clear();
        Console.WriteLine(jsonArray.Count);
    }
}";
            string expectedOutput =
@"False
0
0
b
1
0";

            var verifier = this.CompileAndVerifyOnWin8Only(source, expectedOutput: expectedOutput);

            verifier.VerifyIL("Class1.Main",
@"{
// Code size      174 (0xae)
  .maxstack  3
  .locals init (Windows.Data.Json.JsonArray V_0, //jsonArray
  Windows.Data.Json.JsonValue V_1, //a
  Windows.Data.Json.JsonValue V_2, //b
  System.Collections.Generic.IEnumerator<Windows.Data.Json.IJsonValue> V_3)
  IL_0000:  newobj     ""Windows.Data.Json.JsonArray..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""a""
  IL_000b:  call       ""Windows.Data.Json.JsonValue Windows.Data.Json.JsonValue.CreateStringValue(string)""
  IL_0010:  stloc.1
  IL_0011:  ldloc.0
  IL_0012:  ldloc.1
  IL_0013:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Add(Windows.Data.Json.IJsonValue)""
  IL_0018:  ldstr      ""b""
  IL_001d:  call       ""Windows.Data.Json.JsonValue Windows.Data.Json.JsonValue.CreateStringValue(string)""
  IL_0022:  stloc.2
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldloc.2
  IL_0026:  callvirt   ""void System.Collections.Generic.IList<Windows.Data.Json.IJsonValue>.Insert(int, Windows.Data.Json.IJsonValue)""
  IL_002b:  ldloc.0
  IL_002c:  ldloc.2
  IL_002d:  callvirt   ""bool System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Remove(Windows.Data.Json.IJsonValue)""
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldloc.2
  IL_0035:  callvirt   ""bool System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Contains(Windows.Data.Json.IJsonValue)""
  IL_003a:  call       ""void System.Console.WriteLine(bool)""
  IL_003f:  ldloc.0
  IL_0040:  ldloc.1
  IL_0041:  callvirt   ""int System.Collections.Generic.IList<Windows.Data.Json.IJsonValue>.IndexOf(Windows.Data.Json.IJsonValue)""
  IL_0046:  call       ""void System.Console.WriteLine(int)""
  IL_004b:  ldloc.0
  IL_004c:  ldc.i4.0
  IL_004d:  callvirt   ""void System.Collections.Generic.IList<Windows.Data.Json.IJsonValue>.RemoveAt(int)""
  IL_0052:  ldloc.0
  IL_0053:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Count.get""
  IL_0058:  call       ""void System.Console.WriteLine(int)""
  IL_005d:  ldloc.0
  IL_005e:  ldloc.2
  IL_005f:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Add(Windows.Data.Json.IJsonValue)""
  IL_0064:  ldloc.0
  IL_0065:  callvirt   ""System.Collections.Generic.IEnumerator<Windows.Data.Json.IJsonValue> System.Collections.Generic.IEnumerable<Windows.Data.Json.IJsonValue>.GetEnumerator()""
  IL_006a:  stloc.3
  .try
{
  IL_006b:  br.s       IL_007d
  IL_006d:  ldloc.3
  IL_006e:  callvirt   ""Windows.Data.Json.IJsonValue System.Collections.Generic.IEnumerator<Windows.Data.Json.IJsonValue>.Current.get""
  IL_0073:  callvirt   ""string Windows.Data.Json.IJsonValue.GetString()""
  IL_0078:  call       ""void System.Console.WriteLine(string)""
  IL_007d:  ldloc.3
  IL_007e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0083:  brtrue.s   IL_006d
  IL_0085:  leave.s    IL_0091
}
  finally
{
  IL_0087:  ldloc.3
  IL_0088:  brfalse.s  IL_0090
  IL_008a:  ldloc.3
  IL_008b:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0090:  endfinally
}
  IL_0091:  ldloc.0
  IL_0092:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Count.get""
  IL_0097:  call       ""void System.Console.WriteLine(int)""
  IL_009c:  ldloc.0
  IL_009d:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Clear()""
  IL_00a2:  ldloc.0
  IL_00a3:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Data.Json.IJsonValue>.Count.get""
  IL_00a8:  call       ""void System.Console.WriteLine(int)""
  IL_00ad:  ret
}
");
        }

        [Fact]
        public void IVectorViewProjectionTests()
        {
            var source =
@"using System;
using Windows.Foundation;

public class Class1
{
    public static void Main(string[] args)
    {
        var results = new WwwFormUrlDecoder(""?param1=test"");
        Console.Out.WriteLine(results[0].Name + results[0].Value);
    }
}";
            var expectedOut = "param1test";
            var verifier = this.CompileAndVerifyOnWin8Only(
                source,
                expectedOutput: expectedOut);

            verifier.VerifyIL("Class1.Main",
@"{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (Windows.Foundation.WwwFormUrlDecoder V_0) //results
  IL_0000:  ldstr      ""?param1=test""
  IL_0005:  newobj     ""Windows.Foundation.WwwFormUrlDecoder..ctor(string)""
  IL_000a:  stloc.0
  IL_000b:  call       ""System.IO.TextWriter System.Console.Out.get""
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  callvirt   ""Windows.Foundation.IWwwFormUrlDecoderEntry System.Collections.Generic.IReadOnlyList<Windows.Foundation.IWwwFormUrlDecoderEntry>.this[int].get""
  IL_0017:  callvirt   ""string Windows.Foundation.IWwwFormUrlDecoderEntry.Name.get""
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  callvirt   ""Windows.Foundation.IWwwFormUrlDecoderEntry System.Collections.Generic.IReadOnlyList<Windows.Foundation.IWwwFormUrlDecoderEntry>.this[int].get""
  IL_0023:  callvirt   ""string Windows.Foundation.IWwwFormUrlDecoderEntry.Value.get""
  IL_0028:  call       ""string string.Concat(string, string)""
  IL_002d:  callvirt   ""void System.IO.TextWriter.WriteLine(string)""
  IL_0032:  ret
}");
        }

        [Fact]
        public void IMapProjectionTests()
        {
            var source =
@"using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

public class Class1
{
    public static void Main(string[] args)
    {
        var dataPackage = new DataPackage();
        var dpps = dataPackage.Properties;
        dpps.Add(new KeyValuePair<string, object>(""testKey1"", ""testValue1""));
        Console.Out.WriteLine(dpps.ContainsKey(""testKey1""));
        Console.Out.WriteLine(dpps[""testKey1""]);
        dpps.Add(""testKey2"", ""testValue2"");
        object tv2;
        dpps.TryGetValue(""testKey2"", out tv2);
        Console.Out.WriteLine(tv2);
        dpps[""testKey2""] = ""testValue3"";
        dpps.Remove(""testKey1"");
        var valsEnumerator = dpps.Values.GetEnumerator();
        var keysEnumerator = dpps.Keys.GetEnumerator();
        while (keysEnumerator.MoveNext() && valsEnumerator.MoveNext())
        {
            Console.Out.WriteLine(keysEnumerator.Current + valsEnumerator.Current);
        }
    }
}";

            var expectedOut =
@"True
testValue1
testValue2
testKey2testValue3
";
            var verifier = this.CompileAndVerifyOnWin8Only(
                source,
                expectedOutput: expectedOut);

            verifier.VerifyIL("Class1.Main",
@"{
  // Code size      225 (0xe1)
  .maxstack  4
  .locals init (Windows.ApplicationModel.DataTransfer.DataPackagePropertySet V_0, //dpps
  object V_1, //tv2
  System.Collections.Generic.IEnumerator<object> V_2, //valsEnumerator
  System.Collections.Generic.IEnumerator<string> V_3) //keysEnumerator
  IL_0000:  newobj     ""Windows.ApplicationModel.DataTransfer.DataPackage..ctor()""
  IL_0005:  callvirt   ""Windows.ApplicationModel.DataTransfer.DataPackagePropertySet Windows.ApplicationModel.DataTransfer.DataPackage.Properties.get""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldstr      ""testKey1""
  IL_0011:  ldstr      ""testValue1""
  IL_0016:  newobj     ""System.Collections.Generic.KeyValuePair<string, object>..ctor(string, object)""
  IL_001b:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>.Add(System.Collections.Generic.KeyValuePair<string, object>)""
  IL_0020:  call       ""System.IO.TextWriter System.Console.Out.get""
  IL_0025:  ldloc.0
  IL_0026:  ldstr      ""testKey1""
  IL_002b:  callvirt   ""bool System.Collections.Generic.IDictionary<string, object>.ContainsKey(string)""
  IL_0030:  callvirt   ""void System.IO.TextWriter.WriteLine(bool)""
  IL_0035:  call       ""System.IO.TextWriter System.Console.Out.get""
  IL_003a:  ldloc.0
  IL_003b:  ldstr      ""testKey1""
  IL_0040:  callvirt   ""object System.Collections.Generic.IDictionary<string, object>.this[string].get""
  IL_0045:  callvirt   ""void System.IO.TextWriter.WriteLine(object)""
  IL_004a:  ldloc.0
  IL_004b:  ldstr      ""testKey2""
  IL_0050:  ldstr      ""testValue2""
  IL_0055:  callvirt   ""void System.Collections.Generic.IDictionary<string, object>.Add(string, object)""
  IL_005a:  ldloc.0
  IL_005b:  ldstr      ""testKey2""
  IL_0060:  ldloca.s   V_1
  IL_0062:  callvirt   ""bool System.Collections.Generic.IDictionary<string, object>.TryGetValue(string, out object)""
  IL_0067:  pop
  IL_0068:  call       ""System.IO.TextWriter System.Console.Out.get""
  IL_006d:  ldloc.1
  IL_006e:  callvirt   ""void System.IO.TextWriter.WriteLine(object)""
  IL_0073:  ldloc.0
  IL_0074:  ldstr      ""testKey2""
  IL_0079:  ldstr      ""testValue3""
  IL_007e:  callvirt   ""void System.Collections.Generic.IDictionary<string, object>.this[string].set""
  IL_0083:  ldloc.0
  IL_0084:  ldstr      ""testKey1""
  IL_0089:  callvirt   ""bool System.Collections.Generic.IDictionary<string, object>.Remove(string)""
  IL_008e:  pop
  IL_008f:  ldloc.0
  IL_0090:  callvirt   ""System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<string, object>.Values.get""
  IL_0095:  callvirt   ""System.Collections.Generic.IEnumerator<object> System.Collections.Generic.IEnumerable<object>.GetEnumerator()""
  IL_009a:  stloc.2
  IL_009b:  ldloc.0
  IL_009c:  callvirt   ""System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<string, object>.Keys.get""
  IL_00a1:  callvirt   ""System.Collections.Generic.IEnumerator<string> System.Collections.Generic.IEnumerable<string>.GetEnumerator()""
  IL_00a6:  stloc.3
  IL_00a7:  br.s       IL_00d0
  IL_00a9:  call       ""System.IO.TextWriter System.Console.Out.get""
  IL_00ae:  ldloc.3
  IL_00af:  callvirt   ""string System.Collections.Generic.IEnumerator<string>.Current.get""
  IL_00b4:  ldloc.2
  IL_00b5:  callvirt   ""object System.Collections.Generic.IEnumerator<object>.Current.get""
  IL_00ba:  dup
  IL_00bb:  brtrue.s   IL_00c1
  IL_00bd:  pop
  IL_00be:  ldnull
  IL_00bf:  br.s       IL_00c6
  IL_00c1:  callvirt   ""string object.ToString()""
  IL_00c6:  call       ""string string.Concat(string, string)""
  IL_00cb:  callvirt   ""void System.IO.TextWriter.WriteLine(string)""
  IL_00d0:  ldloc.3
  IL_00d1:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_00d6:  brfalse.s  IL_00e0
  IL_00d8:  ldloc.2
  IL_00d9:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_00de:  brtrue.s   IL_00a9
  IL_00e0:  ret
}");
        }

        // TODO: There are no suitable winmd members to test the IMapView projections,
        // a custom winmd will have to be used after winmd references are implemented

        [Fact]
        public void MultipleInterfaceMethodConflictTests()
        {
            var source =
@"using Windows.Data.Json;
using Windows.Foundation;

public class Class1
{
    public static void Main(string[] args)
    {
        var en = new JsonArray().GetEnumerator();
        en = new WwwFormUrlDecoder(""?param1=test"").GetEnumerator();
    }
}";
            var comp = CreateEmptyCompilation(source, references: WinRtRefs);
            // JsonArray implements both IEnumerable and IList, which both have a GetEnumerator
            // method. We can't know which interface method to call, so we shouldn't emit a
            // GetEnumerator method at all.
            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "GetEnumerator")
                .WithArguments("Windows.Data.Json.JsonArray", "GetEnumerator"),
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "GetEnumerator")
                .WithArguments("Windows.Foundation.WwwFormUrlDecoder", "GetEnumerator"));
        }

        [Fact]
        public void LegacyCollectionTest01()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestIIterableMembers()
    {
        Console.WriteLine(""===  IIterableFloat  ==="");
        var i = new IIterableFloat();
        i.ClearFlag();

        IEnumerator<float> enumerator = ((IEnumerable<float>)i).GetEnumerator();
        ValidateMethod(i.GetFlagState(), TestMethodCalled.IIterable_First);
    }

    static int Main()
    {
        TestIIterableMembers();
        
        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestIIterableMembers",
@"{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldstr      ""===  IIterableFloat  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IIterableFloat..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IIterableFloat.ClearFlag()""
  IL_0015:  dup
  IL_0016:  callvirt   ""System.Collections.Generic.IEnumerator<float> System.Collections.Generic.IEnumerable<float>.GetEnumerator()""
  IL_001b:  pop
  IL_001c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IIterableFloat.GetFlagState()""
  IL_0021:  ldc.i4.1
  IL_0022:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0027:  pop
  IL_0028:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest02()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestIVectorIntMembers()
    {
        Console.WriteLine(""===  IVectorInt  ==="");
        var v = new IVectorInt();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(v[0], 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0], 1);
        ValidateValue(arr[1], 0); //there should be nothing there! :)
        //GetEnumerator
        v.ClearFlag();
        int count = v.Count;
        IEnumerator<int> enumerator = ((IEnumerable<int>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        int index = 0;
        foreach (var e in v)
        {
            index = index + 1;
            ValidateValue(e, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue(v[1], 2);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        int val = v[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 1);
        v.ClearFlag();
        val = v[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 2);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(v.Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(v.Count, 0);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(v.Count, 0);
    }

    static void TestIVectorStructMembers()
    {
        Console.WriteLine(""===  IVectorStruct  ==="");
        var v = new IVectorStruct();
        var ud = new UserDefinedStruct()
        {
            Id = 1
        }

        ;
        //Add
        v.ClearFlag();
        v.Add(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(v[0].Id, 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        UserDefinedStruct[] arr = new UserDefinedStruct[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0].Id, ud.Id);
        //GetEnumerator
        v.ClearFlag();
        int count = v.Count;
        IEnumerator<UserDefinedStruct> enumerator = ((IEnumerable<UserDefinedStruct>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size);
        enumerator.MoveNext();
        ValidateValue((enumerator.Current).Id, 1);
        int index = 0;
        foreach (var e in v)
        {
            index = index + 1;
            ValidateValue(e.Id, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, new UserDefinedStruct()
        {
            Id = 4
        }

        );
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue(v[1].Id, 4);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        var val = v[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val.Id, ud.Id);
        v.ClearFlag();
        val = v[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val.Id, 4);
        //Remove
        v.ClearFlag();
        v.Remove(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(v.Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(v.Count, 0);
        //Clear
        v.Add(ud);
        v.Add(new UserDefinedStruct()
        {
            Id = 4
        }

        );
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(v.Count, 0);
    }

    static void TestIVectorUintStructMembers()
    {
        Console.WriteLine(""===  IVectorUintStruct  ==="");
        var v = new IVectorUintStruct();
        //Add
        v.ClearFlag();
        v.Add(7);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<uint>)[0], 7);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(7);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //IndexOf
        v.ClearFlag();
        var rez = ((IList<uint>)v).IndexOf(7);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0);
        //Insert
        v.ClearFlag();
        v.Insert(1, 5);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<uint>)[1], 5);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = ((IList<uint>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        uint val = ((uint)(v as IList<uint>)[0]);
        ValidateValue(val, 7);
        v.ClearFlag();
        val = ((IList<uint>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 5);
        //Remove 
        v.ClearFlag();
        v.Remove(5);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<uint>)v).Count, 1);
        //RemoveAt
        try
        {
            v.ClearFlag();
            ((IList<uint>)v).RemoveAt(0);
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
            ValidateValue(((IList<uint>)v).Count, 0);
        }
        catch (Exception exce)
        {
            Console.WriteLine(""RemoveAt"");
            Console.WriteLine(exce.Message);
        }

        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ((IList<uint>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<uint>)v).Count, 0);
        var ud = new UserDefinedStruct()
        {
            Id = 1
        }

        ;
        //Add
        v.ClearFlag();
        v.Add(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(((IList<UserDefinedStruct>)v)[0].Id, 1);
        //Contains
        v.ClearFlag();
        b = v.Contains(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //'CopyTo
        //v.ClearFlag()
        //Dim arr As UserDefinedStruct()
        //ReDim arr(10)
        //v.CopyTo(arr, 0)
        //ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        //ValidateValue(arr[0].Id, ud.Id)
        //GetEnumerator
        v.ClearFlag();
        int count = ((IList<UserDefinedStruct>)v).Count;
        IEnumerator<UserDefinedStruct> enumerator = ((IList<UserDefinedStruct>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetMany);
        enumerator.MoveNext();
        ValidateValue((enumerator.Current).Id, 1);
        //IndexOf
        v.ClearFlag();
        rez = v.IndexOf(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, new UserDefinedStruct()
        {
            Id = 4
        }

        );
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue(((IList<UserDefinedStruct>)v)[1].Id, 4);
        //IsReadOnly
        v.ClearFlag();
        isReadOnly = ((IList<UserDefinedStruct>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        var val2 = ((IList<UserDefinedStruct>)v)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val2.Id, ud.Id);
        v.ClearFlag();
        val2 = ((IList<UserDefinedStruct>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val2.Id, 4);
        //Remove
        v.ClearFlag();
        v.Remove(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<UserDefinedStruct>)v).Count, 1);
        //RemoveAt
        v.ClearFlag();
        ((IList<UserDefinedStruct>)v).RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<UserDefinedStruct>)v).Count, 0);
        //Clear
        v.Add(ud);
        v.Add(new UserDefinedStruct()
        {
            Id = 4
        }

        );
        v.ClearFlag();
        ((IList<UserDefinedStruct>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<UserDefinedStruct>)v).Count, 0);
    }

    static void TestIVectorUintFloatMembers()
    {
        Console.WriteLine(""===  IVectorUintIVectorFloat  ==="");
        var v = new IVectorUintIVectorFloat();
        //Add
        v.ClearFlag();
        v.Add(7);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(((IList<uint>)v).Count, 1);
        try
        {
            ValidateValue(((IList<uint>)v)[0], 7);
        }
        catch (ArgumentException exc)
        {
            Console.WriteLine(exc.Message);
        }

        //Contains
        v.ClearFlag();
        bool b = v.Contains(7);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //IndexOf
        v.ClearFlag();
        var rez = ((IList<uint>)v).IndexOf(7);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0);
        //Insertv.ClearFlag()
        v.Insert(1, 5);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        try
        {
            ValidateValue(((IList<uint>)v)[1], 5);
        }
        catch (ArgumentException exc)
        {
            Console.WriteLine(exc.Message);
        }

        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = ((IList<uint>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        try
        {
            v.ClearFlag();
            var val = (v as IList<uint>)[0];
            ValidateValue(val, 7);
            v.ClearFlag();
            val = ((IList<uint>)v)[1];
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
            ValidateValue(val, 5);
        }
        catch (Exception exce)
        {
            Console.WriteLine(""Indexing"");
            Console.WriteLine(exce.Message);
        }

        //Remove 
        v.ClearFlag();
        v.Remove(5);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<uint>)v).Count, 1);
        //RemoveAt
        try
        {
            v.ClearFlag();
            ((IList<uint>)v).RemoveAt(0);
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
            ValidateValue(((IList<uint>)v).Count, 0);
        }
        catch (Exception exce)
        {
            Console.WriteLine(""RemoveAt"");
            Console.WriteLine(exce.Message);
        }

        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ((IList<uint>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<uint>)v).Count, 0);
        //single
        //Add
        v.ClearFlag();
        float one = 1;
        v.Add(one);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(((IList<float>)v)[0], one);
        //Contains
        v.ClearFlag();
        b = v.Contains(one);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //'CopyTo
        //v.ClearFlag()
        //Dim arr As single()
        //ReDim arr(10)
        //v.CopyTo(arr, 0)
        //ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt)
        //ValidateValue(arr[0].Id, ud.Id)
        //IndexOf
        v.ClearFlag();
        rez = v.IndexOf(one);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, (float)4);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue(((IList<float>)v)[1], 4);
        //IsReadOnly
        v.ClearFlag();
        isReadOnly = ((IList<float>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        var val2 = ((IList<float>)v)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val2, one);
        v.ClearFlag();
        val2 = ((IList<float>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val2, 4);
        //Remove
        v.ClearFlag();
        v.Remove(one);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<float>)v).Count, 1);
        //RemoveAt
        v.ClearFlag();
        ((IList<float>)v).RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<float>)v).Count, 0);
        //Clear
        v.Add(one);
        v.ClearFlag();
        ((IList<float>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<float>)v).Count, 0);
    }

    static void TestIVectorIntIMapIntIntMembers()
    {
        Console.WriteLine(""===  IVectorIntIMapIntInt  ==="");
        var v = new IVectorIntIMapIntInt();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<int>)[0], 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0], 1);
        ValidateValue(arr[1], 0); //there should be nothing there! :)
        //'GetEnumerator
        //v.ClearFlag()
        //Dim count As Integer = v.Count
        //Dim enumerator As IEnumerator(Of Integer) = v.GetEnumerator()
        //ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First)
        //Dim index As Integer = 0
        //For Each e In v
        //    index = index + 1
        //    ValidateValue(e, index)
        //Next
        //ValidateValue(index, 1) 'there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<int>)[1], 2);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = ((IList<int>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        int val = ((int)(v as IList<int>)[0]);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 1);
        v.ClearFlag();
        val = ((IList<int>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 2);
        //Remove
        v.ClearFlag();
        ((IList<int>)v).Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<int>)v).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<int>)v).Count, 0);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ((IList<int>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<int>)v).Count, 0);
        var m = v;
        //Add
        m.ClearFlag();
        m.Add(1, 2);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(((IDictionary<int, int>)m).Count, 1);
        //ContainsKey
        m.ClearFlag();
        bool key = m.ContainsKey(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        //Lookup
        m.ClearFlag();
        int val2 = ((int)(v as IDictionary<int, int>)[1]);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(val2, 2);
        //Keys
        m.ClearFlag();
        var keys = m.Keys;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Values
        m.ClearFlag();
        var values = m.Values;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Lookup
        m.ClearFlag();
        int outVal;
        bool success = ((IDictionary<int, int>)m).TryGetValue(1, out outVal);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(success, true);
        ValidateValue(outVal, 2);
        //Add
        m.ClearFlag();
        m.Add(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(((IDictionary<int, int>)m).Count, 2);
        //Contains
        m.ClearFlag();
        bool contains = m.Contains(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(contains, true);
        //non-existent pair
        m.ClearFlag();
        contains = m.Contains(new KeyValuePair<int, int>(8, 9));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(contains, false);
        //Remove
        m.ClearFlag();
        bool remove = ((IDictionary<int, int>)m).Remove(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(((IDictionary<int, int>)m).Count, 1);
        ValidateValue(remove, true);
        //CopyTo
        //m.ClearFlag()
        //Dim arr As KeyValuePair(Of Integer, Integer)()
        //ReDim arr(10)
        //m.CopyTo(arr, 1)
        //ValidateMethod(m.GetFlagState(), TestMethodCalled.IVector_GetAt)
        //ValidateValue(arr[0].Value, 2)
        //Count
        m.ClearFlag();
        int count = ((IDictionary<int, int>)m).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
        ValidateValue(count, 1);
        //isReadOnly
        m.ClearFlag();
        isReadOnly = ((IDictionary<int, int>)m).IsReadOnly;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Remove
        m.ClearFlag();
        var rez2 = m.Remove(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(rez2, true);
        m.ClearFlag();
        rez2 = m.Remove(new KeyValuePair<int, int>(2, 3));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(rez2, false);
        m.Add(1, 2);
        m.Add(2, 3);
        m.ClearFlag();
        ((IDictionary<int, int>)m).Clear();
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear);
        ValidateValue(((IDictionary<int, int>)m).Count, 0);
    }

    static void TestIVectorExplicitAddMembers()
    {
        IVectorExplicitAdd v = new IVectorExplicitAdd();
        //Calling the user defined Add method
        v.ClearFlag();
        ((IMemberAdd)v).Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add);
        v.ClearFlag();
        ((IMemberAdd)v).Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add);
        //Calling the Interface Add's method
        v.ClearFlag();
        ((IList<int>)v).Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(v[0], 1);
    }

    static void TestIVectorViewMembers()
    {
        var v = new IVectorViewInt();
        v.ClearFlag();
        int count = v.Count;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVectorView_get_Size);
    }

    static void TestIVectorUIntIVectorViewIntMembers()
    {
        Console.WriteLine(""===  IVectorUintIVectorViewInt  ==="");
        var v = new IVectorUintIVectorViewInt();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<uint>)[0], 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        uint[] arr = new uint[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0], 1);
        ValidateValue(arr[1], 0); //there should be nothing there! :)
        //GetEnumerator
        v.ClearFlag();
        uint count = ((uint)(v as IList<uint>).Count);
        IEnumerator<uint> enumerator = ((IEnumerable<uint>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        uint index = 0;
        foreach (var e in v)
        {
            index = ((uint)index + 1);
            ValidateValue(e, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<uint>)[1], 2);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        uint val = (v as IList<uint>)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 1);
        v.ClearFlag();
        val = (((IList<uint>)v))[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 2);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((v as IList<uint>).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((v as IList<uint>).Count, 0);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue((v as IList<uint>).Count, 0);
        // IVectorView members
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ValidateValue(((IReadOnlyList<uint>)v).Count, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size);
        v.ClearFlag();
        ValidateValue(((IReadOnlyList<uint>)v)[0], 1);
        ValidateValue(((IReadOnlyList<uint>)v)[1], 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVectorView_GetAt);
    }

    static void TestIVectorIntIVectorViewUintMembers()
    {
        Console.WriteLine(""===  IVectorIntIVectorViewUint  ==="");
        var v = new IVectorIntIVectorViewUint();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<int>)[0], 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0], 1);
        ValidateValue(arr[1], 0); //there should be nothing there! :)
        //GetEnumerator
        v.ClearFlag();
        uint count = ((uint)(((IList<int>)v)).Count);
        IEnumerator<int> enumerator = (((IList<int>)v)).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        uint index = 0;
        foreach (var e in (((IList<int>)v)))
        {
            index = ((uint)index + 1);
            ValidateValue(e, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<int>)[1], 2);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        uint val = ((uint)(v as IList<int>)[0]);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 1);
        v.ClearFlag();
        val = ((uint)(((IList<int>)v))[1]);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 2);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((((IList<int>)v)).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((((IList<int>)v)).Count, 0);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue((((IList<int>)v)).Count, 0);
        // IVectorView members
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ValidateValue(((IReadOnlyList<uint>)v)[0], 0);
        ValidateValue(((IReadOnlyList<uint>)v)[1], 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
    }

    static void TestIVectorStructIVectorViewStructMembers()
    {
        Console.WriteLine(""===  IVectorStructIVectorViewStruct  ==="");
        var v = new IVectorStructIVectorViewStruct();
        var ud = new UserDefinedStruct()
        {
            Id = 1
        }

        ;
        //Add
        v.ClearFlag();
        v.Add(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<UserDefinedStruct>)[0].Id, 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        UserDefinedStruct[] arr = new UserDefinedStruct[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0].Id, ud.Id);
        //GetEnumerator
        v.ClearFlag();
        int count = (v as IList<UserDefinedStruct>).Count;
        IEnumerator<UserDefinedStruct> enumerator = ((IEnumerable<UserDefinedStruct>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size);
        enumerator.MoveNext();
        ValidateValue((enumerator.Current).Id, 1);
        int index = 0;
        foreach (var e in v)
        {
            index = index + 1;
            ValidateValue(e.Id, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, new UserDefinedStruct()
        {
            Id = 4
        }

        );
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<UserDefinedStruct>)[1].Id, 4);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        var val = (v as IList<UserDefinedStruct>)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val.Id, ud.Id);
        v.ClearFlag();
        val = ((IList<UserDefinedStruct>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val.Id, 4);
        //Remove
        v.ClearFlag();
        v.Remove(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((v as IList<UserDefinedStruct>).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((v as IList<UserDefinedStruct>).Count, 0);
        //Clear
        v.Add(ud);
        v.Add(new UserDefinedStruct()
        {
            Id = 4
        }

        );
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue((v as IList<UserDefinedStruct>).Count, 0);
        // IVectorView members
        v.Add(ud);
        v.Add(new UserDefinedStruct()
        {
            Id = 4
        }

        );
        v.ClearFlag();
        ValidateValue(((IReadOnlyList<UserDefinedStruct>)v)[0].Id, ud.Id);
        ValidateValue(((IReadOnlyList<UserDefinedStruct>)v)[1].Id, 4);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
    }

    static int Main()
    {
        TestIVectorIntMembers();
        TestIVectorStructMembers();
        TestIVectorUintStructMembers();
        TestIVectorUintFloatMembers();
        TestIVectorIntIMapIntIntMembers();
        TestIVectorExplicitAddMembers();
        TestIVectorViewMembers();
        TestIVectorUIntIVectorViewIntMembers();
        TestIVectorIntIVectorViewUintMembers();
        TestIVectorStructIVectorViewStructMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var comp = CreateCompilationWithWinRT(source, references: LegacyRefs);
            comp.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
        }

        [Fact]
        public void LegacyCollectionTest03()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestIMapIntIntMembers()
    {
        Console.WriteLine(""===  IMapIntInt  ==="");
        var m = new IMapIntInt();
        //Add
        m.ClearFlag();
        m.Add(1, 2);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(m.Count, 1);
        //ContainsKey
        m.ClearFlag();
        bool key = m.ContainsKey(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        //Lookup
        m.ClearFlag();
        int val = m[1];
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(val, 2);
        //Keys
        m.ClearFlag();
        var keys = m.Keys;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Values
        m.ClearFlag();
        var values = m.Values;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Lookup
        m.ClearFlag();
        int outVal;
        bool success = ((IDictionary<int, int>)m).TryGetValue(1, out outVal);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(outVal, 2);
        ValidateValue(success, true);
        //Add
        m.ClearFlag();
        m.Add(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(m.Count, 2);
        //Contains
        m.ClearFlag();
        bool contains = m.Contains(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(contains, true);
        //non-existent pair
        m.ClearFlag();
        contains = m.Contains(new KeyValuePair<int, int>(8, 9));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(contains, false);
        //Remove
        m.ClearFlag();
        bool remove = m.Remove(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(m.Count, 1);
        ValidateValue(remove, true);
        //Count
        m.ClearFlag();
        int count = m.Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
        ValidateValue(count, 1);
        //isReadOnly
        m.ClearFlag();
        bool isReadOnly = m.IsReadOnly;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Remove
        m.ClearFlag();
        var rez = m.Remove(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(rez, true);
        m.ClearFlag();
        rez = m.Remove(new KeyValuePair<int, int>(2, 3));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(rez, false);
        m.Add(1, 2);
        m.Add(2, 3);
        m.ClearFlag();
        m.Clear();
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear);
        ValidateValue(m.Count, 0);
    }

    static void TestIMapIntStructMembers()
    {
        Console.WriteLine(""===  IMapIntStruct  ==="");
        var m = new IMapIntStruct();
        var ud = new UserDefinedStruct()
        {
            Id = 10
        }

        ;
        //Add
        m.ClearFlag();
        m.Add(1, ud);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(m.Count, 1);
        //ContainsKey
        m.ClearFlag();
        bool key = m.ContainsKey(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        //Lookup
        m.ClearFlag();
        UserDefinedStruct val = m[1];
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(val.Id, 10);
        //Keys
        m.ClearFlag();
        var keys = m.Keys;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Values
        m.ClearFlag();
        var values = m.Values;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Lookup
        m.ClearFlag();
        UserDefinedStruct outVal;
        bool success = ((IDictionary<int, UserDefinedStruct>)m).TryGetValue(1, out outVal);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(outVal.Id, ud.Id);
        ValidateValue(success, true);
        //Add
        m.ClearFlag();
        m.Add(new KeyValuePair<int, UserDefinedStruct>(3, new UserDefinedStruct()
        {
            Id = 4
        }

        ));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(m.Count, 2);
        //Contains
        m.ClearFlag();
        bool contains = m.Contains(new KeyValuePair<int, UserDefinedStruct>(1, ud));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(contains, true);
        //non-existent pair
        m.ClearFlag();
        contains = m.Contains(new KeyValuePair<int, UserDefinedStruct>(8, new UserDefinedStruct()));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(contains, false);
        //Remove
        m.ClearFlag();
        bool remove = m.Remove(3);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(m.Count, 1);
        ValidateValue(remove, true);
        //Count
        m.ClearFlag();
        int count = m.Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
        ValidateValue(count, 1);
        //isReadOnly
        m.ClearFlag();
        bool isReadOnly = m.IsReadOnly;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Remove
        m.ClearFlag();
        var rez = m.Remove(new KeyValuePair<int, UserDefinedStruct>(1, ud));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(rez, true);
        ValidateValue(m.Count, 0);
        //Clear
        m.Add(1, ud);
        m.Add(2, ud);
        m.ClearFlag();
        m.Clear();
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear);
        ValidateValue(m.Count, 0);
    }

    static void TestIMapExplicitAddMembers()
    {
        IMapExplicitAdd v = new IMapExplicitAdd();
        //Calling the user defined Add method
        v.ClearFlag();
        ((IMemberAdd2Args)v).Add(1, 1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add);
        v.ClearFlag();
        ((IMemberAdd2Args)v).Add(2, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.UserDef_Add);
        //Calling the Interface Add's method
        v.ClearFlag();
        ((IDictionary<int, int>)v).Add(3, 3);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(v.Count, 3);
    }

    static void TestIMapViewMembers()
    {
        var m = new IMapViewIntInt();
        m.ClearFlag();
        int count = m.Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
    }

    static void TestIMapIntIMapViewIntStructMembers()
    {
        Console.WriteLine(""===  IMapIMapViewIntStruct  ==="");
        var m = new IMapIMapViewIntStruct();
        var ud = new UserDefinedStruct() { Id = 10 };
        //Add
        m.ClearFlag();
        m.Add(1, ud);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue((m as IDictionary<int, UserDefinedStruct>).Count, 1);
        //ContainsKey
        m.ClearFlag();
        bool key = ((IDictionary<int, UserDefinedStruct>)m).ContainsKey(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        //Lookup
        m.ClearFlag();
        UserDefinedStruct val = ((IDictionary<int, UserDefinedStruct>)m)[1];
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(val.Id, 10);
        //Keys
        m.ClearFlag();
        var keys = ((IDictionary<int, UserDefinedStruct>)m).Keys;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Values
        m.ClearFlag();
        var values = ((IDictionary<int, UserDefinedStruct>)m).Values;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Lookup
        m.ClearFlag();
        UserDefinedStruct outVal;
        bool success = ((IDictionary<int, UserDefinedStruct>)m).TryGetValue(1, out outVal);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(success, true);
        //Add
        m.ClearFlag();
        m.Add(new KeyValuePair<int, UserDefinedStruct>(3, new UserDefinedStruct()
        {
            Id = 4
        }

        ));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue((m as IDictionary<int, UserDefinedStruct>).Count, 2);
        //Contains
        m.ClearFlag();
        bool contains = m.Contains(new KeyValuePair<int, UserDefinedStruct>(1, ud));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(contains, true);
        //non-existent pair
        m.ClearFlag();
        contains = m.Contains(new KeyValuePair<int, UserDefinedStruct>(8, new UserDefinedStruct()));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(contains, false);
        //Remove
        m.ClearFlag();
        bool remove = m.Remove(3);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue((m as IDictionary<int, UserDefinedStruct>).Count, 1);
        ValidateValue(remove, true);
        //Count
        m.ClearFlag();
        int count = (m as IDictionary<int, UserDefinedStruct>).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
        ValidateValue(count, 1);
        //isReadOnly
        m.ClearFlag();
        bool isReadOnly = m.IsReadOnly;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Remove
        m.ClearFlag();
        var rez = m.Remove(new KeyValuePair<int, UserDefinedStruct>(1, ud));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(rez, true);
        ValidateValue((m as IDictionary<int, UserDefinedStruct>).Count, 0);
        //m.ClearFlag()
        //rez = m.Remove(New KeyValuePair(Of Integer, UserDefinedStruct)(3, ud))
        //ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        //ValidateValue(rez, False)
        //Clear
        m.Add(1, ud);
        m.Add(2, ud);
        m.ClearFlag();
        m.Clear();
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear);
        ValidateValue((m as IDictionary<int, UserDefinedStruct>).Count, 0);
        // IMapView members
        m.ClearFlag();
        count = ((IReadOnlyDictionary<int, UserDefinedStruct>)m).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
    }

    static int Main()
    {
        TestIMapIntIntMembers();
        TestIMapIntStructMembers();
        TestIMapExplicitAddMembers();
        TestIMapViewMembers();
        TestIMapIntIMapViewIntStructMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                //FIXME: Can't verify because the metadata adapter isn't implemented yet
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestIMapIntIntMembers",
@"{
  // Code size      756 (0x2f4)
  .maxstack  4
  .locals init (int V_0, //val
  int V_1, //outVal
  bool V_2, //success
  bool V_3, //contains
  bool V_4, //remove
  int V_5, //count
  bool V_6, //isReadOnly
  bool V_7) //rez
  IL_0000:  ldstr      ""===  IMapIntInt  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IMapIntInt..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0015:  dup
  IL_0016:  ldc.i4.1
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_001d:  dup
  IL_001e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0023:  ldc.i4.s   21
  IL_0025:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_002a:  pop
  IL_002b:  dup
  IL_002c:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_0031:  box        ""int""
  IL_0036:  ldc.i4.1
  IL_0037:  box        ""int""
  IL_003c:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0041:  pop
  IL_0042:  dup
  IL_0043:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0048:  dup
  IL_0049:  ldc.i4.1
  IL_004a:  callvirt   ""bool System.Collections.Generic.IDictionary<int, int>.ContainsKey(int)""
  IL_004f:  pop
  IL_0050:  dup
  IL_0051:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0056:  ldc.i4.s   19
  IL_0058:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_005d:  pop
  IL_005e:  dup
  IL_005f:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0064:  dup
  IL_0065:  ldc.i4.1
  IL_0066:  callvirt   ""int System.Collections.Generic.IDictionary<int, int>.this[int].get""
  IL_006b:  stloc.0
  IL_006c:  dup
  IL_006d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0072:  ldc.i4.s   17
  IL_0074:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0079:  pop
  IL_007a:  ldloc.0
  IL_007b:  box        ""int""
  IL_0080:  ldc.i4.2
  IL_0081:  box        ""int""
  IL_0086:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_008b:  pop
  IL_008c:  dup
  IL_008d:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0092:  dup
  IL_0093:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, int>.Keys.get""
  IL_0098:  pop
  IL_0099:  dup
  IL_009a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_009f:  ldc.i4.0
  IL_00a0:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00a5:  pop
  IL_00a6:  dup
  IL_00a7:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_00ac:  dup
  IL_00ad:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, int>.Values.get""
  IL_00b2:  pop
  IL_00b3:  dup
  IL_00b4:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_00b9:  ldc.i4.0
  IL_00ba:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00bf:  pop
  IL_00c0:  dup
  IL_00c1:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_00c6:  dup
  IL_00c7:  ldc.i4.1
  IL_00c8:  ldloca.s   V_1
  IL_00ca:  callvirt   ""bool System.Collections.Generic.IDictionary<int, int>.TryGetValue(int, out int)""
  IL_00cf:  stloc.2
  IL_00d0:  dup
  IL_00d1:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_00d6:  ldc.i4.s   17
  IL_00d8:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00dd:  pop
  IL_00de:  ldloc.1
  IL_00df:  box        ""int""
  IL_00e4:  ldc.i4.2
  IL_00e5:  box        ""int""
  IL_00ea:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00ef:  pop
  IL_00f0:  ldloc.2
  IL_00f1:  box        ""bool""
  IL_00f6:  ldc.i4.1
  IL_00f7:  box        ""bool""
  IL_00fc:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0101:  pop
  IL_0102:  dup
  IL_0103:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0108:  dup
  IL_0109:  ldc.i4.3
  IL_010a:  ldc.i4.4
  IL_010b:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0110:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Add(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_0115:  dup
  IL_0116:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_011b:  ldc.i4.s   21
  IL_011d:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0122:  pop
  IL_0123:  dup
  IL_0124:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_0129:  box        ""int""
  IL_012e:  ldc.i4.2
  IL_012f:  box        ""int""
  IL_0134:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0139:  pop
  IL_013a:  dup
  IL_013b:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0140:  dup
  IL_0141:  ldc.i4.3
  IL_0142:  ldc.i4.4
  IL_0143:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0148:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Contains(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_014d:  stloc.3
  IL_014e:  dup
  IL_014f:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0154:  ldc.i4.s   17
  IL_0156:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_015b:  pop
  IL_015c:  ldloc.3
  IL_015d:  box        ""bool""
  IL_0162:  ldc.i4.1
  IL_0163:  box        ""bool""
  IL_0168:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_016d:  pop
  IL_016e:  dup
  IL_016f:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0174:  dup
  IL_0175:  ldc.i4.8
  IL_0176:  ldc.i4.s   9
  IL_0178:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_017d:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Contains(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_0182:  stloc.3
  IL_0183:  dup
  IL_0184:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0189:  ldc.i4.s   19
  IL_018b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0190:  pop
  IL_0191:  ldloc.3
  IL_0192:  box        ""bool""
  IL_0197:  ldc.i4.0
  IL_0198:  box        ""bool""
  IL_019d:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01a2:  pop
  IL_01a3:  dup
  IL_01a4:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_01a9:  dup
  IL_01aa:  ldc.i4.1
  IL_01ab:  callvirt   ""bool System.Collections.Generic.IDictionary<int, int>.Remove(int)""
  IL_01b0:  stloc.s    V_4
  IL_01b2:  dup
  IL_01b3:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_01b8:  ldc.i4.s   22
  IL_01ba:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01bf:  pop
  IL_01c0:  dup
  IL_01c1:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_01c6:  box        ""int""
  IL_01cb:  ldc.i4.1
  IL_01cc:  box        ""int""
  IL_01d1:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01d6:  pop
  IL_01d7:  ldloc.s    V_4
  IL_01d9:  box        ""bool""
  IL_01de:  ldc.i4.1
  IL_01df:  box        ""bool""
  IL_01e4:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01e9:  pop
  IL_01ea:  dup
  IL_01eb:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_01f0:  dup
  IL_01f1:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_01f6:  stloc.s    V_5
  IL_01f8:  dup
  IL_01f9:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_01fe:  ldc.i4.s   18
  IL_0200:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0205:  pop
  IL_0206:  ldloc.s    V_5
  IL_0208:  box        ""int""
  IL_020d:  ldc.i4.1
  IL_020e:  box        ""int""
  IL_0213:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0218:  pop
  IL_0219:  dup
  IL_021a:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_021f:  dup
  IL_0220:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.IsReadOnly.get""
  IL_0225:  stloc.s    V_6
  IL_0227:  dup
  IL_0228:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_022d:  ldc.i4.0
  IL_022e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0233:  pop
  IL_0234:  ldloc.s    V_6
  IL_0236:  box        ""bool""
  IL_023b:  ldc.i4.0
  IL_023c:  box        ""bool""
  IL_0241:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0246:  pop
  IL_0247:  dup
  IL_0248:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_024d:  dup
  IL_024e:  ldc.i4.3
  IL_024f:  ldc.i4.4
  IL_0250:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0255:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Remove(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_025a:  stloc.s    V_7
  IL_025c:  dup
  IL_025d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0262:  ldc.i4.s   22
  IL_0264:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0269:  pop
  IL_026a:  ldloc.s    V_7
  IL_026c:  box        ""bool""
  IL_0271:  ldc.i4.1
  IL_0272:  box        ""bool""
  IL_0277:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_027c:  pop
  IL_027d:  dup
  IL_027e:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0283:  dup
  IL_0284:  ldc.i4.2
  IL_0285:  ldc.i4.3
  IL_0286:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_028b:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Remove(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_0290:  stloc.s    V_7
  IL_0292:  dup
  IL_0293:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_0298:  ldc.i4.s   19
  IL_029a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_029f:  pop
  IL_02a0:  ldloc.s    V_7
  IL_02a2:  box        ""bool""
  IL_02a7:  ldc.i4.0
  IL_02a8:  box        ""bool""
  IL_02ad:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02b2:  pop
  IL_02b3:  dup
  IL_02b4:  ldc.i4.1
  IL_02b5:  ldc.i4.2
  IL_02b6:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_02bb:  dup
  IL_02bc:  ldc.i4.2
  IL_02bd:  ldc.i4.3
  IL_02be:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_02c3:  dup
  IL_02c4:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_02c9:  dup
  IL_02ca:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Clear()""
  IL_02cf:  dup
  IL_02d0:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_02d5:  ldc.i4.s   23
  IL_02d7:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02dc:  pop
  IL_02dd:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_02e2:  box        ""int""
  IL_02e7:  ldc.i4.0
  IL_02e8:  box        ""int""
  IL_02ed:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02f2:  pop
  IL_02f3:  ret
}");
            verifier.VerifyIL("AllMembers.TestIMapIntStructMembers",
@"
{
  // Code size      790 (0x316)
  .maxstack  5
  .locals init (Windows.Languages.WinRTTest.UserDefinedStruct V_0, //ud
  Windows.Languages.WinRTTest.UserDefinedStruct V_1, //val
  Windows.Languages.WinRTTest.UserDefinedStruct V_2, //outVal
  bool V_3, //success
  bool V_4, //contains
  bool V_5, //remove
  int V_6, //count
  bool V_7, //isReadOnly
  bool V_8, //rez
  Windows.Languages.WinRTTest.UserDefinedStruct V_9)
  IL_0000:  ldstr      ""===  IMapIntStruct  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IMapIntStruct..ctor()""
  IL_000f:  ldloca.s   V_9
  IL_0011:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_0017:  ldloca.s   V_9
  IL_0019:  ldc.i4.s   10
  IL_001b:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0020:  ldloc.s    V_9
  IL_0022:  stloc.0
  IL_0023:  dup
  IL_0024:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldloc.0
  IL_002c:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0031:  dup
  IL_0032:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_0037:  ldc.i4.s   21
  IL_0039:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_003e:  pop
  IL_003f:  dup
  IL_0040:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0045:  box        ""int""
  IL_004a:  ldc.i4.1
  IL_004b:  box        ""int""
  IL_0050:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0055:  pop
  IL_0056:  dup
  IL_0057:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_005c:  dup
  IL_005d:  ldc.i4.1
  IL_005e:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.ContainsKey(int)""
  IL_0063:  pop
  IL_0064:  dup
  IL_0065:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_006a:  ldc.i4.s   19
  IL_006c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0071:  pop
  IL_0072:  dup
  IL_0073:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_0078:  dup
  IL_0079:  ldc.i4.1
  IL_007a:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.this[int].get""
  IL_007f:  stloc.1
  IL_0080:  dup
  IL_0081:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_0086:  ldc.i4.s   17
  IL_0088:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_008d:  pop
  IL_008e:  ldloc.1
  IL_008f:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0094:  box        ""uint""
  IL_0099:  ldc.i4.s   10
  IL_009b:  box        ""int""
  IL_00a0:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00a5:  pop
  IL_00a6:  dup
  IL_00a7:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_00ac:  dup
  IL_00ad:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Keys.get""
  IL_00b2:  pop
  IL_00b3:  dup
  IL_00b4:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_00b9:  ldc.i4.0
  IL_00ba:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00bf:  pop
  IL_00c0:  dup
  IL_00c1:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_00c6:  dup
  IL_00c7:  callvirt   ""System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct> System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Values.get""
  IL_00cc:  pop
  IL_00cd:  dup
  IL_00ce:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_00d3:  ldc.i4.0
  IL_00d4:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d9:  pop
  IL_00da:  dup
  IL_00db:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_00e0:  dup
  IL_00e1:  ldc.i4.1
  IL_00e2:  ldloca.s   V_2
  IL_00e4:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.TryGetValue(int, out Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_00e9:  stloc.3
  IL_00ea:  dup
  IL_00eb:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_00f0:  ldc.i4.s   17
  IL_00f2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00f7:  pop
  IL_00f8:  ldloc.2
  IL_00f9:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_00fe:  box        ""uint""
  IL_0103:  ldloc.0
  IL_0104:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0109:  box        ""uint""
  IL_010e:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0113:  pop
  IL_0114:  ldloc.3
  IL_0115:  box        ""bool""
  IL_011a:  ldc.i4.1
  IL_011b:  box        ""bool""
  IL_0120:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0125:  pop
  IL_0126:  dup
  IL_0127:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_012c:  dup
  IL_012d:  ldc.i4.3
  IL_012e:  ldloca.s   V_9
  IL_0130:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_0136:  ldloca.s   V_9
  IL_0138:  ldc.i4.4
  IL_0139:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_013e:  ldloc.s    V_9
  IL_0140:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0145:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Add(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_014a:  dup
  IL_014b:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_0150:  ldc.i4.s   21
  IL_0152:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0157:  pop
  IL_0158:  dup
  IL_0159:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_015e:  box        ""int""
  IL_0163:  ldc.i4.2
  IL_0164:  box        ""int""
  IL_0169:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_016e:  pop
  IL_016f:  dup
  IL_0170:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_0175:  dup
  IL_0176:  ldc.i4.1
  IL_0177:  ldloc.0
  IL_0178:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_017d:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Contains(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_0182:  stloc.s    V_4
  IL_0184:  dup
  IL_0185:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_018a:  ldc.i4.s   17
  IL_018c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0191:  pop
  IL_0192:  ldloc.s    V_4
  IL_0194:  box        ""bool""
  IL_0199:  ldc.i4.1
  IL_019a:  box        ""bool""
  IL_019f:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01a4:  pop
  IL_01a5:  dup
  IL_01a6:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_01ab:  dup
  IL_01ac:  ldc.i4.8
  IL_01ad:  ldloca.s   V_9
  IL_01af:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_01b5:  ldloc.s    V_9
  IL_01b7:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_01bc:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Contains(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_01c1:  stloc.s    V_4
  IL_01c3:  dup
  IL_01c4:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_01c9:  ldc.i4.s   19
  IL_01cb:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01d0:  pop
  IL_01d1:  ldloc.s    V_4
  IL_01d3:  box        ""bool""
  IL_01d8:  ldc.i4.0
  IL_01d9:  box        ""bool""
  IL_01de:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01e3:  pop
  IL_01e4:  dup
  IL_01e5:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_01ea:  dup
  IL_01eb:  ldc.i4.3
  IL_01ec:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Remove(int)""
  IL_01f1:  stloc.s    V_5
  IL_01f3:  dup
  IL_01f4:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_01f9:  ldc.i4.s   22
  IL_01fb:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0200:  pop
  IL_0201:  dup
  IL_0202:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0207:  box        ""int""
  IL_020c:  ldc.i4.1
  IL_020d:  box        ""int""
  IL_0212:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0217:  pop
  IL_0218:  ldloc.s    V_5
  IL_021a:  box        ""bool""
  IL_021f:  ldc.i4.1
  IL_0220:  box        ""bool""
  IL_0225:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_022a:  pop
  IL_022b:  dup
  IL_022c:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_0231:  dup
  IL_0232:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0237:  stloc.s    V_6
  IL_0239:  dup
  IL_023a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_023f:  ldc.i4.s   18
  IL_0241:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0246:  pop
  IL_0247:  ldloc.s    V_6
  IL_0249:  box        ""int""
  IL_024e:  ldc.i4.1
  IL_024f:  box        ""int""
  IL_0254:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0259:  pop
  IL_025a:  dup
  IL_025b:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_0260:  dup
  IL_0261:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.IsReadOnly.get""
  IL_0266:  stloc.s    V_7
  IL_0268:  dup
  IL_0269:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_026e:  ldc.i4.0
  IL_026f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0274:  pop
  IL_0275:  ldloc.s    V_7
  IL_0277:  box        ""bool""
  IL_027c:  ldc.i4.0
  IL_027d:  box        ""bool""
  IL_0282:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0287:  pop
  IL_0288:  dup
  IL_0289:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_028e:  dup
  IL_028f:  ldc.i4.1
  IL_0290:  ldloc.0
  IL_0291:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0296:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Remove(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_029b:  stloc.s    V_8
  IL_029d:  dup
  IL_029e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_02a3:  ldc.i4.s   22
  IL_02a5:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02aa:  pop
  IL_02ab:  ldloc.s    V_8
  IL_02ad:  box        ""bool""
  IL_02b2:  ldc.i4.1
  IL_02b3:  box        ""bool""
  IL_02b8:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02bd:  pop
  IL_02be:  dup
  IL_02bf:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_02c4:  box        ""int""
  IL_02c9:  ldc.i4.0
  IL_02ca:  box        ""int""
  IL_02cf:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02d4:  pop
  IL_02d5:  dup
  IL_02d6:  ldc.i4.1
  IL_02d7:  ldloc.0
  IL_02d8:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_02dd:  dup
  IL_02de:  ldc.i4.2
  IL_02df:  ldloc.0
  IL_02e0:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_02e5:  dup
  IL_02e6:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntStruct.ClearFlag()""
  IL_02eb:  dup
  IL_02ec:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Clear()""
  IL_02f1:  dup
  IL_02f2:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntStruct.GetFlagState()""
  IL_02f7:  ldc.i4.s   23
  IL_02f9:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02fe:  pop
  IL_02ff:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0304:  box        ""int""
  IL_0309:  ldc.i4.0
  IL_030a:  box        ""int""
  IL_030f:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0314:  pop
  IL_0315:  ret
}
");
            verifier.VerifyIL("AllMembers.TestIMapExplicitAddMembers",
@"{
  // Code size      112 (0x70)
  .maxstack  4
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.IMapExplicitAdd..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""void Windows.Languages.WinRTTest.IMapExplicitAdd.ClearFlag()""
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   ""void Windows.Languages.WinRTTest.IMemberAdd2Args.Add(int, int)""
  IL_0013:  dup
  IL_0014:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapExplicitAdd.GetFlagState()""
  IL_0019:  ldc.i4.s   25
  IL_001b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0020:  pop
  IL_0021:  dup
  IL_0022:  callvirt   ""void Windows.Languages.WinRTTest.IMapExplicitAdd.ClearFlag()""
  IL_0027:  dup
  IL_0028:  ldc.i4.2
  IL_0029:  ldc.i4.2
  IL_002a:  callvirt   ""void Windows.Languages.WinRTTest.IMemberAdd2Args.Add(int, int)""
  IL_002f:  dup
  IL_0030:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapExplicitAdd.GetFlagState()""
  IL_0035:  ldc.i4.s   25
  IL_0037:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_003c:  pop
  IL_003d:  dup
  IL_003e:  callvirt   ""void Windows.Languages.WinRTTest.IMapExplicitAdd.ClearFlag()""
  IL_0043:  dup
  IL_0044:  ldc.i4.3
  IL_0045:  ldc.i4.3
  IL_0046:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_004b:  dup
  IL_004c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapExplicitAdd.GetFlagState()""
  IL_0051:  ldc.i4.s   21
  IL_0053:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0058:  pop
  IL_0059:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_005e:  box        ""int""
  IL_0063:  ldc.i4.3
  IL_0064:  box        ""int""
  IL_0069:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_006e:  pop
  IL_006f:  ret
}");
            verifier.VerifyIL("AllMembers.TestIMapViewMembers",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.IMapViewIntInt..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""void Windows.Languages.WinRTTest.IMapViewIntInt.ClearFlag()""
  IL_000b:  dup
  IL_000c:  callvirt   ""int System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_0011:  pop
  IL_0012:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapViewIntInt.GetFlagState()""
  IL_0017:  ldc.i4.s   18
  IL_0019:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_001e:  pop
  IL_001f:  ret
}");
            verifier.VerifyIL("AllMembers.TestIMapIntIMapViewIntStructMembers",
@"
{
  // Code size      790 (0x316)
  .maxstack  5
  .locals init (Windows.Languages.WinRTTest.UserDefinedStruct V_0, //ud
  Windows.Languages.WinRTTest.UserDefinedStruct V_1, //val
  Windows.Languages.WinRTTest.UserDefinedStruct V_2, //outVal
  bool V_3, //success
  bool V_4, //contains
  bool V_5, //remove
  int V_6, //count
  bool V_7, //isReadOnly
  bool V_8, //rez
  Windows.Languages.WinRTTest.UserDefinedStruct V_9)
  IL_0000:  ldstr      ""===  IMapIMapViewIntStruct  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IMapIMapViewIntStruct..ctor()""
  IL_000f:  ldloca.s   V_9
  IL_0011:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_0017:  ldloca.s   V_9
  IL_0019:  ldc.i4.s   10
  IL_001b:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0020:  ldloc.s    V_9
  IL_0022:  stloc.0
  IL_0023:  dup
  IL_0024:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldloc.0
  IL_002c:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0031:  dup
  IL_0032:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_0037:  ldc.i4.s   21
  IL_0039:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_003e:  pop
  IL_003f:  dup
  IL_0040:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0045:  box        ""int""
  IL_004a:  ldc.i4.1
  IL_004b:  box        ""int""
  IL_0050:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0055:  pop
  IL_0056:  dup
  IL_0057:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_005c:  dup
  IL_005d:  ldc.i4.1
  IL_005e:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.ContainsKey(int)""
  IL_0063:  pop
  IL_0064:  dup
  IL_0065:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_006a:  ldc.i4.s   19
  IL_006c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0071:  pop
  IL_0072:  dup
  IL_0073:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0078:  dup
  IL_0079:  ldc.i4.1
  IL_007a:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.this[int].get""
  IL_007f:  stloc.1
  IL_0080:  dup
  IL_0081:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_0086:  ldc.i4.s   17
  IL_0088:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_008d:  pop
  IL_008e:  ldloc.1
  IL_008f:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0094:  box        ""uint""
  IL_0099:  ldc.i4.s   10
  IL_009b:  box        ""int""
  IL_00a0:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00a5:  pop
  IL_00a6:  dup
  IL_00a7:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_00ac:  dup
  IL_00ad:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Keys.get""
  IL_00b2:  pop
  IL_00b3:  dup
  IL_00b4:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_00b9:  ldc.i4.0
  IL_00ba:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00bf:  pop
  IL_00c0:  dup
  IL_00c1:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_00c6:  dup
  IL_00c7:  callvirt   ""System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct> System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Values.get""
  IL_00cc:  pop
  IL_00cd:  dup
  IL_00ce:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_00d3:  ldc.i4.0
  IL_00d4:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d9:  pop
  IL_00da:  dup
  IL_00db:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_00e0:  dup
  IL_00e1:  ldc.i4.1
  IL_00e2:  ldloca.s   V_2
  IL_00e4:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.TryGetValue(int, out Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_00e9:  stloc.3
  IL_00ea:  dup
  IL_00eb:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_00f0:  ldc.i4.s   17
  IL_00f2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00f7:  pop
  IL_00f8:  ldloc.3
  IL_00f9:  box        ""bool""
  IL_00fe:  ldc.i4.1
  IL_00ff:  box        ""bool""
  IL_0104:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0109:  pop
  IL_010a:  dup
  IL_010b:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0110:  dup
  IL_0111:  ldc.i4.3
  IL_0112:  ldloca.s   V_9
  IL_0114:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_011a:  ldloca.s   V_9
  IL_011c:  ldc.i4.4
  IL_011d:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0122:  ldloc.s    V_9
  IL_0124:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0129:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Add(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_012e:  dup
  IL_012f:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_0134:  ldc.i4.s   21
  IL_0136:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_013b:  pop
  IL_013c:  dup
  IL_013d:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0142:  box        ""int""
  IL_0147:  ldc.i4.2
  IL_0148:  box        ""int""
  IL_014d:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0152:  pop
  IL_0153:  dup
  IL_0154:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0159:  dup
  IL_015a:  ldc.i4.1
  IL_015b:  ldloc.0
  IL_015c:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0161:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Contains(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_0166:  stloc.s    V_4
  IL_0168:  dup
  IL_0169:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_016e:  ldc.i4.s   17
  IL_0170:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0175:  pop
  IL_0176:  ldloc.s    V_4
  IL_0178:  box        ""bool""
  IL_017d:  ldc.i4.1
  IL_017e:  box        ""bool""
  IL_0183:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0188:  pop
  IL_0189:  dup
  IL_018a:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_018f:  dup
  IL_0190:  ldc.i4.8
  IL_0191:  ldloca.s   V_9
  IL_0193:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_0199:  ldloc.s    V_9
  IL_019b:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_01a0:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Contains(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_01a5:  stloc.s    V_4
  IL_01a7:  dup
  IL_01a8:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_01ad:  ldc.i4.s   19
  IL_01af:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01b4:  pop
  IL_01b5:  ldloc.s    V_4
  IL_01b7:  box        ""bool""
  IL_01bc:  ldc.i4.0
  IL_01bd:  box        ""bool""
  IL_01c2:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01c7:  pop
  IL_01c8:  dup
  IL_01c9:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_01ce:  dup
  IL_01cf:  ldc.i4.3
  IL_01d0:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Remove(int)""
  IL_01d5:  stloc.s    V_5
  IL_01d7:  dup
  IL_01d8:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_01dd:  ldc.i4.s   22
  IL_01df:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01e4:  pop
  IL_01e5:  dup
  IL_01e6:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_01eb:  box        ""int""
  IL_01f0:  ldc.i4.1
  IL_01f1:  box        ""int""
  IL_01f6:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01fb:  pop
  IL_01fc:  ldloc.s    V_5
  IL_01fe:  box        ""bool""
  IL_0203:  ldc.i4.1
  IL_0204:  box        ""bool""
  IL_0209:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_020e:  pop
  IL_020f:  dup
  IL_0210:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0215:  dup
  IL_0216:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_021b:  stloc.s    V_6
  IL_021d:  dup
  IL_021e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_0223:  ldc.i4.s   18
  IL_0225:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_022a:  pop
  IL_022b:  ldloc.s    V_6
  IL_022d:  box        ""int""
  IL_0232:  ldc.i4.1
  IL_0233:  box        ""int""
  IL_0238:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_023d:  pop
  IL_023e:  dup
  IL_023f:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0244:  dup
  IL_0245:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.IsReadOnly.get""
  IL_024a:  stloc.s    V_7
  IL_024c:  dup
  IL_024d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_0252:  ldc.i4.0
  IL_0253:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0258:  pop
  IL_0259:  ldloc.s    V_7
  IL_025b:  box        ""bool""
  IL_0260:  ldc.i4.0
  IL_0261:  box        ""bool""
  IL_0266:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_026b:  pop
  IL_026c:  dup
  IL_026d:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0272:  dup
  IL_0273:  ldc.i4.1
  IL_0274:  ldloc.0
  IL_0275:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_027a:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Remove(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_027f:  stloc.s    V_8
  IL_0281:  dup
  IL_0282:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_0287:  ldc.i4.s   22
  IL_0289:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_028e:  pop
  IL_028f:  ldloc.s    V_8
  IL_0291:  box        ""bool""
  IL_0296:  ldc.i4.1
  IL_0297:  box        ""bool""
  IL_029c:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02a1:  pop
  IL_02a2:  dup
  IL_02a3:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_02a8:  box        ""int""
  IL_02ad:  ldc.i4.0
  IL_02ae:  box        ""int""
  IL_02b3:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02b8:  pop
  IL_02b9:  dup
  IL_02ba:  ldc.i4.1
  IL_02bb:  ldloc.0
  IL_02bc:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_02c1:  dup
  IL_02c2:  ldc.i4.2
  IL_02c3:  ldloc.0
  IL_02c4:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_02c9:  dup
  IL_02ca:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_02cf:  dup
  IL_02d0:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Clear()""
  IL_02d5:  dup
  IL_02d6:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_02db:  ldc.i4.s   23
  IL_02dd:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02e2:  pop
  IL_02e3:  dup
  IL_02e4:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_02e9:  box        ""int""
  IL_02ee:  ldc.i4.0
  IL_02ef:  box        ""int""
  IL_02f4:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02f9:  pop
  IL_02fa:  dup
  IL_02fb:  callvirt   ""void Windows.Languages.WinRTTest.IMapIMapViewIntStruct.ClearFlag()""
  IL_0300:  dup
  IL_0301:  callvirt   ""int System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0306:  stloc.s    V_6
  IL_0308:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIMapViewIntStruct.GetFlagState()""
  IL_030d:  ldc.i4.s   18
  IL_030f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0314:  pop
  IL_0315:  ret
}
");
        }

        [Fact]
        public void LegacyCollectionTest04()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestIVectorIntIVectorViewIntIMapIntIntIMapViewIntIntMembers()
    {
        var v = new IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<int>)[0], 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0], 1);
        ValidateValue(arr[1], 0); //there should be nothing there! :)
        //GetEnumerator
        v.ClearFlag();
        int count = ((IList<int>)v).Count;
        IEnumerator<int> enumerator = ((IList<int>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        int index = 0;
        foreach (var e in ((IList<int>)v))
        {
            index = index + 1;
            ValidateValue(e, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<int>)[1], 2);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = ((IList<int>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        int val = ((int)(v as IList<int>)[0]);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 1);
        v.ClearFlag();
        val = ((IList<int>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 2);
        //Remove
        v.ClearFlag();
        ((IList<int>)v).Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<int>)v).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<int>)v).Count, 0);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ((IList<int>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<int>)v).Count, 0);
        //IVectorView
        v.ClearFlag();
        count = ((IReadOnlyList<int>)v).Count;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size);
        var m = v;
        //Add
        m.ClearFlag();
        m.Add(1, 2);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(((IDictionary<int, int>)m).Count, 1);
        //ContainsKey
        m.ClearFlag();
        bool key = ((IDictionary<int, int>)m).ContainsKey(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        //Lookup
        m.ClearFlag();
        val = ((int)(m as IDictionary<int, int>)[1]);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(val, 2);
        //Keys
        m.ClearFlag();
        var keys = ((IDictionary<int, int>)m).Keys;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Values
        m.ClearFlag();
        var values = ((IDictionary<int, int>)m).Values;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Lookup
        m.ClearFlag();
        int outVal;
        bool success = ((IDictionary<int, int>)m).TryGetValue(1, out outVal);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(outVal, 2);
        ValidateValue(success, true);
        //Add
        m.ClearFlag();
        m.Add(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(((IDictionary<int, int>)m).Count, 2);
        //Contains
        m.ClearFlag();
        bool contains = m.Contains(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(contains, true);
        //non-existent pair
        m.ClearFlag();
        contains = m.Contains(new KeyValuePair<int, int>(8, 9));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(contains, false);
        //Remove
        m.ClearFlag();
        bool remove = ((IDictionary<int, int>)m).Remove(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(((IDictionary<int, int>)m).Count, 1);
        ValidateValue(remove, true);
        //CopyTo
        //m.ClearFlag()
        //Dim arr As KeyValuePair(Of Integer, Integer)()
        //ReDim arr(10)
        //m.CopyTo(arr, 1)
        //ValidateMethod(m.GetFlagState(), TestMethodCalled.IVector_GetAt)
        //ValidateValue(arr[0].Value, 2)
        //Count
        m.ClearFlag();
        count = ((IDictionary<int, int>)m).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
        ValidateValue(count, 1);
        //isReadOnly
        m.ClearFlag();
        isReadOnly = ((IDictionary<int, int>)m).IsReadOnly;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Remove
        m.ClearFlag();
        var rez2 = m.Remove(new KeyValuePair<int, int>(3, 4));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(rez2, true);
        m.ClearFlag();
        rez2 = m.Remove(new KeyValuePair<int, int>(2, 3));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(rez2, false);
        m.Add(1, 2);
        m.Add(2, 3);
        m.ClearFlag();
        ((IDictionary<int, int>)m).Clear();
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear);
        ValidateValue(((IDictionary<int, int>)m).Count, 0);
        //IMapView
        m.ClearFlag();
        count = ((IReadOnlyDictionary<int, int>)m).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IVector_get_Size);
    }

    static void TestIVectorStructIVectorViewStructIMapIntStructIMapViewIntStructMembers()
    {
        var v = new IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct();
        var ud = new UserDefinedStruct()
        {
            Id = 1
        }

        ;
        //Add
        v.ClearFlag();
        v.Add(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        UserDefinedStruct[] arr = new UserDefinedStruct[10];

        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0].Id, ud.Id);
        //GetEnumerator
        v.ClearFlag();
        int count = ((IList<UserDefinedStruct>)v).Count;
        IEnumerator<UserDefinedStruct> enumerator = ((IList<UserDefinedStruct>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        enumerator.MoveNext();
        ValidateValue((enumerator.Current).Id, 1);
        int index = 0;
        foreach (var e in ((IList<UserDefinedStruct>)v))
        {
            index = index + 1;
            ValidateValue(e.Id, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, new UserDefinedStruct()
        {
            Id = 4
        }

        );
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = ((IList<UserDefinedStruct>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        var val = (v as IList<UserDefinedStruct>)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        v.ClearFlag();
        val = ((IList<UserDefinedStruct>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        //Remove
        v.ClearFlag();
        v.Remove(ud);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<UserDefinedStruct>)v).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue(((IList<UserDefinedStruct>)v).Count, 0);
        //Clear
        v.Add(ud);
        v.Add(new UserDefinedStruct()
        {
            Id = 4
        }

        );
        v.ClearFlag();
        ((IList<UserDefinedStruct>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue(((IList<UserDefinedStruct>)v).Count, 0);
        //IVectorView
        v.ClearFlag();
        count = ((IReadOnlyList<UserDefinedStruct>)v).Count;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_get_Size);
        var m = v;
        ud = new UserDefinedStruct()
        {
            Id = 10
        }

        ;
        //Add
        m.ClearFlag();
        m.Add(1, ud);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(((IDictionary<int, UserDefinedStruct>)m).Count, 1);
        //ContainsKey
        m.ClearFlag();
        bool key = ((IDictionary<int, UserDefinedStruct>)m).ContainsKey(1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        //Lookup
        m.ClearFlag();
        val = ((IDictionary<int, UserDefinedStruct>)m)[1];
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        //Keys
        m.ClearFlag();
        var keys = ((IDictionary<int, UserDefinedStruct>)m).Keys;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Values
        m.ClearFlag();
        var values = ((IDictionary<int, UserDefinedStruct>)m).Values;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        //Lookup
        m.ClearFlag();
        UserDefinedStruct outVal;
        bool success = ((IDictionary<int, UserDefinedStruct>)m).TryGetValue(1, out outVal);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(success, true);
        //Add
        m.ClearFlag();
        m.Add(new KeyValuePair<int, UserDefinedStruct>(3, new UserDefinedStruct()
        {
            Id = 4
        }

        ));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        ValidateValue(((IDictionary<int, UserDefinedStruct>)m).Count, 2);
        //Contains
        m.ClearFlag();
        bool contains = m.Contains(new KeyValuePair<int, UserDefinedStruct>(1, ud));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Lookup);
        ValidateValue(contains, true);
        //non-existent pair
        m.ClearFlag();
        contains = m.Contains(new KeyValuePair<int, UserDefinedStruct>(8, new UserDefinedStruct()));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey);
        ValidateValue(contains, false);
        //Remove
        m.ClearFlag();
        //bool remove = m.Remove(3);
        //ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        //ValidateValue(((IDictionary<int, UserDefinedStruct>)m).Count, 1);
        //ValidateValue(remove, true);
        //'CopyTo
        //m.ClearFlag()
        //Dim arr As KeyValuePair(Of Integer, UserDefinedStruct)()
        //ReDim arr(10)
        //m.CopyTo(arr, 1)
        //ValidateMethod(m.GetFlagState(), TestMethodCalled.IVector_GetAt)
        //Count
        m.ClearFlag();
        count = ((IDictionary<int, UserDefinedStruct>)m).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
        ValidateValue(count, 1);
        //isReadOnly
        m.ClearFlag();
        isReadOnly = ((IDictionary<int, UserDefinedStruct>)m).IsReadOnly;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Remove
        m.ClearFlag();
        m.Remove(new KeyValuePair<int, UserDefinedStruct>(1, ud));
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Remove);
        ValidateValue(((IDictionary<int, UserDefinedStruct>)m).Count, 0);
        //m.ClearFlag()
        //rez = m.Remove(New KeyValuePair(Of Integer, UserDefinedStruct)(3, ud))
        //ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_HasKey)
        //ValidateValue(rez, False)
        //Clear
        m.Add(1, ud);
        m.Add(2, ud);
        m.ClearFlag();
        ((IDictionary<int, UserDefinedStruct>)m).Clear();
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Clear);
        ValidateValue(((IDictionary<int, UserDefinedStruct>)m).Count, 0);
        //ImapView
        m.ClearFlag();
        count = ((IDictionary<int, UserDefinedStruct>)m).Count;
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_get_Size);
    }

    static int Main()
    {
        TestIVectorIntIVectorViewIntIMapIntIntIMapViewIntIntMembers();
        TestIVectorStructIVectorViewStructIMapIntStructIMapViewIntStructMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestIVectorIntIVectorViewIntIMapIntIntIMapViewIntIntMembers",
@"{
  // Code size     1497 (0x5d9)
  .maxstack  4
  .locals init (Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt V_0, //v
  bool V_1, //b
  int[] V_2, //arr
  int V_3, //count
  int V_4, //index
  int V_5, //rez
  bool V_6, //isReadOnly
  int V_7, //val
  int V_8, //outVal
  bool V_9, //success
  bool V_10, //contains
  bool V_11, //remove
  bool V_12, //rez2
  System.Collections.Generic.IEnumerator<int> V_13)
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0019:  ldc.i4.s   9
  IL_001b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.0
  IL_0023:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_0028:  box        ""int""
  IL_002d:  ldc.i4.1
  IL_002e:  box        ""int""
  IL_0033:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.1
  IL_0041:  callvirt   ""bool System.Collections.Generic.ICollection<int>.Contains(int)""
  IL_0046:  stloc.1
  IL_0047:  ldloc.0
  IL_0048:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_004d:  ldc.i4.5
  IL_004e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0053:  pop
  IL_0054:  ldloc.1
  IL_0055:  box        ""bool""
  IL_005a:  ldc.i4.1
  IL_005b:  box        ""bool""
  IL_0060:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0065:  pop
  IL_0066:  ldloc.0
  IL_0067:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_006c:  ldc.i4.s   10
  IL_006e:  newarr     ""int""
  IL_0073:  stloc.2
  IL_0074:  ldloc.0
  IL_0075:  ldloc.2
  IL_0076:  ldc.i4.0
  IL_0077:  callvirt   ""void System.Collections.Generic.ICollection<int>.CopyTo(int[], int)""
  IL_007c:  ldloc.0
  IL_007d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0082:  ldc.i4.2
  IL_0083:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0088:  pop
  IL_0089:  ldloc.2
  IL_008a:  ldc.i4.0
  IL_008b:  ldelem.i4
  IL_008c:  box        ""int""
  IL_0091:  ldc.i4.1
  IL_0092:  box        ""int""
  IL_0097:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_009c:  pop
  IL_009d:  ldloc.2
  IL_009e:  ldc.i4.1
  IL_009f:  ldelem.i4
  IL_00a0:  box        ""int""
  IL_00a5:  ldc.i4.0
  IL_00a6:  box        ""int""
  IL_00ab:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00b0:  pop
  IL_00b1:  ldloc.0
  IL_00b2:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_00b7:  ldloc.0
  IL_00b8:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_00bd:  stloc.3
  IL_00be:  ldloc.0
  IL_00bf:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_00c4:  pop
  IL_00c5:  ldloc.0
  IL_00c6:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_00cb:  ldc.i4.1
  IL_00cc:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d1:  pop
  IL_00d2:  ldc.i4.0
  IL_00d3:  stloc.s    V_4
  IL_00d5:  ldloc.0
  IL_00d6:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_00db:  stloc.s    V_13
  .try
{
  IL_00dd:  br.s       IL_00fe
  IL_00df:  ldloc.s    V_13
  IL_00e1:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
  IL_00e6:  ldloc.s    V_4
  IL_00e8:  ldc.i4.1
  IL_00e9:  add
  IL_00ea:  stloc.s    V_4
  IL_00ec:  box        ""int""
  IL_00f1:  ldloc.s    V_4
  IL_00f3:  box        ""int""
  IL_00f8:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00fd:  pop
  IL_00fe:  ldloc.s    V_13
  IL_0100:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0105:  brtrue.s   IL_00df
  IL_0107:  leave.s    IL_0115
}
  finally
{
  IL_0109:  ldloc.s    V_13
  IL_010b:  brfalse.s  IL_0114
  IL_010d:  ldloc.s    V_13
  IL_010f:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0114:  endfinally
}
  IL_0115:  ldloc.s    V_4
  IL_0117:  box        ""int""
  IL_011c:  ldc.i4.1
  IL_011d:  box        ""int""
  IL_0122:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0127:  pop
  IL_0128:  ldloc.0
  IL_0129:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_012e:  ldloc.0
  IL_012f:  ldc.i4.1
  IL_0130:  callvirt   ""int System.Collections.Generic.IList<int>.IndexOf(int)""
  IL_0135:  stloc.s    V_5
  IL_0137:  ldloc.0
  IL_0138:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_013d:  ldc.i4.5
  IL_013e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0143:  pop
  IL_0144:  ldloc.s    V_5
  IL_0146:  box        ""int""
  IL_014b:  ldc.i4.0
  IL_014c:  box        ""int""
  IL_0151:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0156:  pop
  IL_0157:  ldloc.0
  IL_0158:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_015d:  ldloc.0
  IL_015e:  ldc.i4.1
  IL_015f:  ldc.i4.2
  IL_0160:  callvirt   ""void System.Collections.Generic.IList<int>.Insert(int, int)""
  IL_0165:  ldloc.0
  IL_0166:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_016b:  ldc.i4.7
  IL_016c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0171:  pop
  IL_0172:  ldloc.0
  IL_0173:  ldc.i4.1
  IL_0174:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_0179:  box        ""int""
  IL_017e:  ldc.i4.2
  IL_017f:  box        ""int""
  IL_0184:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0189:  pop
  IL_018a:  ldloc.0
  IL_018b:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0190:  ldloc.0
  IL_0191:  callvirt   ""bool System.Collections.Generic.ICollection<int>.IsReadOnly.get""
  IL_0196:  stloc.s    V_6
  IL_0198:  ldloc.0
  IL_0199:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_019e:  ldc.i4.0
  IL_019f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01a4:  pop
  IL_01a5:  ldloc.s    V_6
  IL_01a7:  box        ""bool""
  IL_01ac:  ldc.i4.0
  IL_01ad:  box        ""bool""
  IL_01b2:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01b7:  pop
  IL_01b8:  ldloc.0
  IL_01b9:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_01be:  ldloc.0
  IL_01bf:  ldc.i4.0
  IL_01c0:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_01c5:  stloc.s    V_7
  IL_01c7:  ldloc.0
  IL_01c8:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_01cd:  ldc.i4.2
  IL_01ce:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01d3:  pop
  IL_01d4:  ldloc.s    V_7
  IL_01d6:  box        ""int""
  IL_01db:  ldc.i4.1
  IL_01dc:  box        ""int""
  IL_01e1:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01e6:  pop
  IL_01e7:  ldloc.0
  IL_01e8:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_01ed:  ldloc.0
  IL_01ee:  ldc.i4.1
  IL_01ef:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_01f4:  stloc.s    V_7
  IL_01f6:  ldloc.0
  IL_01f7:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_01fc:  ldc.i4.2
  IL_01fd:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0202:  pop
  IL_0203:  ldloc.s    V_7
  IL_0205:  box        ""int""
  IL_020a:  ldc.i4.2
  IL_020b:  box        ""int""
  IL_0210:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0215:  pop
  IL_0216:  ldloc.0
  IL_0217:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_021c:  ldloc.0
  IL_021d:  ldc.i4.1
  IL_021e:  callvirt   ""bool System.Collections.Generic.ICollection<int>.Remove(int)""
  IL_0223:  pop
  IL_0224:  ldloc.0
  IL_0225:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_022a:  ldc.i4.8
  IL_022b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0230:  pop
  IL_0231:  ldloc.0
  IL_0232:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_0237:  box        ""int""
  IL_023c:  ldc.i4.1
  IL_023d:  box        ""int""
  IL_0242:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0247:  pop
  IL_0248:  ldloc.0
  IL_0249:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_024e:  ldloc.0
  IL_024f:  ldc.i4.0
  IL_0250:  callvirt   ""void System.Collections.Generic.IList<int>.RemoveAt(int)""
  IL_0255:  ldloc.0
  IL_0256:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_025b:  ldc.i4.8
  IL_025c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0261:  pop
  IL_0262:  ldloc.0
  IL_0263:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_0268:  box        ""int""
  IL_026d:  ldc.i4.0
  IL_026e:  box        ""int""
  IL_0273:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0278:  pop
  IL_0279:  ldloc.0
  IL_027a:  ldc.i4.1
  IL_027b:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0280:  ldloc.0
  IL_0281:  ldc.i4.2
  IL_0282:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0287:  ldloc.0
  IL_0288:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_028d:  ldloc.0
  IL_028e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Clear()""
  IL_0293:  ldloc.0
  IL_0294:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0299:  ldc.i4.s   11
  IL_029b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02a0:  pop
  IL_02a1:  ldloc.0
  IL_02a2:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_02a7:  box        ""int""
  IL_02ac:  ldc.i4.0
  IL_02ad:  box        ""int""
  IL_02b2:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02b7:  pop
  IL_02b8:  ldloc.0
  IL_02b9:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_02be:  ldloc.0
  IL_02bf:  callvirt   ""int System.Collections.Generic.IReadOnlyCollection<int>.Count.get""
  IL_02c4:  stloc.3
  IL_02c5:  ldloc.0
  IL_02c6:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_02cb:  ldc.i4.3
  IL_02cc:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02d1:  pop
  IL_02d2:  ldloc.0
  IL_02d3:  dup
  IL_02d4:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_02d9:  dup
  IL_02da:  ldc.i4.1
  IL_02db:  ldc.i4.2
  IL_02dc:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_02e1:  dup
  IL_02e2:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_02e7:  ldc.i4.s   21
  IL_02e9:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02ee:  pop
  IL_02ef:  dup
  IL_02f0:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_02f5:  box        ""int""
  IL_02fa:  ldc.i4.1
  IL_02fb:  box        ""int""
  IL_0300:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0305:  pop
  IL_0306:  dup
  IL_0307:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_030c:  dup
  IL_030d:  ldc.i4.1
  IL_030e:  callvirt   ""bool System.Collections.Generic.IDictionary<int, int>.ContainsKey(int)""
  IL_0313:  pop
  IL_0314:  dup
  IL_0315:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_031a:  ldc.i4.s   19
  IL_031c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0321:  pop
  IL_0322:  dup
  IL_0323:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0328:  dup
  IL_0329:  ldc.i4.1
  IL_032a:  callvirt   ""int System.Collections.Generic.IDictionary<int, int>.this[int].get""
  IL_032f:  stloc.s    V_7
  IL_0331:  dup
  IL_0332:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0337:  ldc.i4.s   17
  IL_0339:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_033e:  pop
  IL_033f:  ldloc.s    V_7
  IL_0341:  box        ""int""
  IL_0346:  ldc.i4.2
  IL_0347:  box        ""int""
  IL_034c:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0351:  pop
  IL_0352:  dup
  IL_0353:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0358:  dup
  IL_0359:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, int>.Keys.get""
  IL_035e:  pop
  IL_035f:  dup
  IL_0360:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0365:  ldc.i4.0
  IL_0366:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_036b:  pop
  IL_036c:  dup
  IL_036d:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0372:  dup
  IL_0373:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, int>.Values.get""
  IL_0378:  pop
  IL_0379:  dup
  IL_037a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_037f:  ldc.i4.0
  IL_0380:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0385:  pop
  IL_0386:  dup
  IL_0387:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_038c:  dup
  IL_038d:  ldc.i4.1
  IL_038e:  ldloca.s   V_8
  IL_0390:  callvirt   ""bool System.Collections.Generic.IDictionary<int, int>.TryGetValue(int, out int)""
  IL_0395:  stloc.s    V_9
  IL_0397:  dup
  IL_0398:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_039d:  ldc.i4.s   17
  IL_039f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_03a4:  pop
  IL_03a5:  ldloc.s    V_8
  IL_03a7:  box        ""int""
  IL_03ac:  ldc.i4.2
  IL_03ad:  box        ""int""
  IL_03b2:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_03b7:  pop
  IL_03b8:  ldloc.s    V_9
  IL_03ba:  box        ""bool""
  IL_03bf:  ldc.i4.1
  IL_03c0:  box        ""bool""
  IL_03c5:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_03ca:  pop
  IL_03cb:  dup
  IL_03cc:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_03d1:  dup
  IL_03d2:  ldc.i4.3
  IL_03d3:  ldc.i4.4
  IL_03d4:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_03d9:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Add(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_03de:  dup
  IL_03df:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_03e4:  ldc.i4.s   21
  IL_03e6:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_03eb:  pop
  IL_03ec:  dup
  IL_03ed:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_03f2:  box        ""int""
  IL_03f7:  ldc.i4.2
  IL_03f8:  box        ""int""
  IL_03fd:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0402:  pop
  IL_0403:  dup
  IL_0404:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0409:  dup
  IL_040a:  ldc.i4.3
  IL_040b:  ldc.i4.4
  IL_040c:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0411:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Contains(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_0416:  stloc.s    V_10
  IL_0418:  dup
  IL_0419:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_041e:  ldc.i4.s   17
  IL_0420:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0425:  pop
  IL_0426:  ldloc.s    V_10
  IL_0428:  box        ""bool""
  IL_042d:  ldc.i4.1
  IL_042e:  box        ""bool""
  IL_0433:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0438:  pop
  IL_0439:  dup
  IL_043a:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_043f:  dup
  IL_0440:  ldc.i4.8
  IL_0441:  ldc.i4.s   9
  IL_0443:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0448:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Contains(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_044d:  stloc.s    V_10
  IL_044f:  dup
  IL_0450:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0455:  ldc.i4.s   19
  IL_0457:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_045c:  pop
  IL_045d:  ldloc.s    V_10
  IL_045f:  box        ""bool""
  IL_0464:  ldc.i4.0
  IL_0465:  box        ""bool""
  IL_046a:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_046f:  pop
  IL_0470:  dup
  IL_0471:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0476:  dup
  IL_0477:  ldc.i4.1
  IL_0478:  callvirt   ""bool System.Collections.Generic.IDictionary<int, int>.Remove(int)""
  IL_047d:  stloc.s    V_11
  IL_047f:  dup
  IL_0480:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0485:  ldc.i4.s   22
  IL_0487:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_048c:  pop
  IL_048d:  dup
  IL_048e:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_0493:  box        ""int""
  IL_0498:  ldc.i4.1
  IL_0499:  box        ""int""
  IL_049e:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_04a3:  pop
  IL_04a4:  ldloc.s    V_11
  IL_04a6:  box        ""bool""
  IL_04ab:  ldc.i4.1
  IL_04ac:  box        ""bool""
  IL_04b1:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_04b6:  pop
  IL_04b7:  dup
  IL_04b8:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_04bd:  dup
  IL_04be:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_04c3:  stloc.3
  IL_04c4:  dup
  IL_04c5:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_04ca:  ldc.i4.s   18
  IL_04cc:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_04d1:  pop
  IL_04d2:  ldloc.3
  IL_04d3:  box        ""int""
  IL_04d8:  ldc.i4.1
  IL_04d9:  box        ""int""
  IL_04de:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_04e3:  pop
  IL_04e4:  dup
  IL_04e5:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_04ea:  dup
  IL_04eb:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.IsReadOnly.get""
  IL_04f0:  stloc.s    V_6
  IL_04f2:  dup
  IL_04f3:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_04f8:  ldc.i4.0
  IL_04f9:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_04fe:  pop
  IL_04ff:  ldloc.s    V_6
  IL_0501:  box        ""bool""
  IL_0506:  ldc.i4.0
  IL_0507:  box        ""bool""
  IL_050c:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0511:  pop
  IL_0512:  dup
  IL_0513:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0518:  dup
  IL_0519:  ldc.i4.3
  IL_051a:  ldc.i4.4
  IL_051b:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0520:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Remove(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_0525:  stloc.s    V_12
  IL_0527:  dup
  IL_0528:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_052d:  ldc.i4.s   22
  IL_052f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0534:  pop
  IL_0535:  ldloc.s    V_12
  IL_0537:  box        ""bool""
  IL_053c:  ldc.i4.1
  IL_053d:  box        ""bool""
  IL_0542:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0547:  pop
  IL_0548:  dup
  IL_0549:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_054e:  dup
  IL_054f:  ldc.i4.2
  IL_0550:  ldc.i4.3
  IL_0551:  newobj     ""System.Collections.Generic.KeyValuePair<int, int>..ctor(int, int)""
  IL_0556:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Remove(System.Collections.Generic.KeyValuePair<int, int>)""
  IL_055b:  stloc.s    V_12
  IL_055d:  dup
  IL_055e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_0563:  ldc.i4.s   19
  IL_0565:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_056a:  pop
  IL_056b:  ldloc.s    V_12
  IL_056d:  box        ""bool""
  IL_0572:  ldc.i4.0
  IL_0573:  box        ""bool""
  IL_0578:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_057d:  pop
  IL_057e:  dup
  IL_057f:  ldc.i4.1
  IL_0580:  ldc.i4.2
  IL_0581:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_0586:  dup
  IL_0587:  ldc.i4.2
  IL_0588:  ldc.i4.3
  IL_0589:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_058e:  dup
  IL_058f:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_0594:  dup
  IL_0595:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Clear()""
  IL_059a:  dup
  IL_059b:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_05a0:  ldc.i4.s   23
  IL_05a2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_05a7:  pop
  IL_05a8:  dup
  IL_05a9:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_05ae:  box        ""int""
  IL_05b3:  ldc.i4.0
  IL_05b4:  box        ""int""
  IL_05b9:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_05be:  pop
  IL_05bf:  dup
  IL_05c0:  callvirt   ""void Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.ClearFlag()""
  IL_05c5:  dup
  IL_05c6:  callvirt   ""int System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_05cb:  stloc.3
  IL_05cc:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorIntIVectorViewIntIMapIntIntIMapViewIntInt.GetFlagState()""
  IL_05d1:  ldc.i4.3
  IL_05d2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_05d7:  pop
  IL_05d8:  ret
}");
            verifier.VerifyIL("AllMembers.TestIVectorStructIVectorViewStructIMapIntStructIMapViewIntStructMembers",
@"{
  // Code size     1395 (0x573)
  .maxstack  5
  .locals init (Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct V_0, //v
  Windows.Languages.WinRTTest.UserDefinedStruct V_1, //ud
  bool V_2, //b
  Windows.Languages.WinRTTest.UserDefinedStruct[] V_3, //arr
  int V_4, //count
  System.Collections.Generic.IEnumerator<Windows.Languages.WinRTTest.UserDefinedStruct> V_5, //enumerator
  int V_6, //index
  int V_7, //rez
  bool V_8, //isReadOnly
  Windows.Languages.WinRTTest.UserDefinedStruct V_9, //outVal
  bool V_10, //success
  bool V_11, //contains
  Windows.Languages.WinRTTest.UserDefinedStruct V_12,
  System.Collections.Generic.IEnumerator<Windows.Languages.WinRTTest.UserDefinedStruct> V_13)
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_12
  IL_0008:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_000e:  ldloca.s   V_12
  IL_0010:  ldc.i4.1
  IL_0011:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0016:  ldloc.s    V_12
  IL_0018:  stloc.1
  IL_0019:  ldloc.0
  IL_001a:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_001f:  ldloc.0
  IL_0020:  ldloc.1
  IL_0021:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Add(Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0026:  ldloc.0
  IL_0027:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_002c:  ldc.i4.s   9
  IL_002e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0033:  pop
  IL_0034:  ldloc.0
  IL_0035:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_003a:  ldloc.0
  IL_003b:  ldloc.1
  IL_003c:  callvirt   ""bool System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Contains(Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0041:  stloc.2
  IL_0042:  ldloc.0
  IL_0043:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0048:  ldc.i4.5
  IL_0049:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_004e:  pop
  IL_004f:  ldloc.2
  IL_0050:  box        ""bool""
  IL_0055:  ldc.i4.1
  IL_0056:  box        ""bool""
  IL_005b:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0060:  pop
  IL_0061:  ldloc.0
  IL_0062:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0067:  ldc.i4.s   10
  IL_0069:  newarr     ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_006e:  stloc.3
  IL_006f:  ldloc.0
  IL_0070:  ldloc.3
  IL_0071:  ldc.i4.0
  IL_0072:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.CopyTo(Windows.Languages.WinRTTest.UserDefinedStruct[], int)""
  IL_0077:  ldloc.0
  IL_0078:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_007d:  ldc.i4.2
  IL_007e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0083:  pop
  IL_0084:  ldloc.3
  IL_0085:  ldc.i4.0
  IL_0086:  ldelema    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_008b:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0090:  box        ""uint""
  IL_0095:  ldloc.1
  IL_0096:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_009b:  box        ""uint""
  IL_00a0:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00a5:  pop
  IL_00a6:  ldloc.0
  IL_00a7:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_00ac:  ldloc.0
  IL_00ad:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Count.get""
  IL_00b2:  stloc.s    V_4
  IL_00b4:  ldloc.0
  IL_00b5:  callvirt   ""System.Collections.Generic.IEnumerator<Windows.Languages.WinRTTest.UserDefinedStruct> System.Collections.Generic.IEnumerable<Windows.Languages.WinRTTest.UserDefinedStruct>.GetEnumerator()""
  IL_00ba:  stloc.s    V_5
  IL_00bc:  ldloc.0
  IL_00bd:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_00c2:  ldc.i4.1
  IL_00c3:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00c8:  pop
  IL_00c9:  ldloc.s    V_5
  IL_00cb:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_00d0:  pop
  IL_00d1:  ldloc.s    V_5
  IL_00d3:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IEnumerator<Windows.Languages.WinRTTest.UserDefinedStruct>.Current.get""
  IL_00d8:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_00dd:  box        ""uint""
  IL_00e2:  ldc.i4.1
  IL_00e3:  box        ""int""
  IL_00e8:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00ed:  pop
  IL_00ee:  ldc.i4.0
  IL_00ef:  stloc.s    V_6
  IL_00f1:  ldloc.0
  IL_00f2:  callvirt   ""System.Collections.Generic.IEnumerator<Windows.Languages.WinRTTest.UserDefinedStruct> System.Collections.Generic.IEnumerable<Windows.Languages.WinRTTest.UserDefinedStruct>.GetEnumerator()""
  IL_00f7:  stloc.s    V_13
  .try
{
  IL_00f9:  br.s       IL_011f
  IL_00fb:  ldloc.s    V_13
  IL_00fd:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IEnumerator<Windows.Languages.WinRTTest.UserDefinedStruct>.Current.get""
  IL_0102:  ldloc.s    V_6
  IL_0104:  ldc.i4.1
  IL_0105:  add
  IL_0106:  stloc.s    V_6
  IL_0108:  ldfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_010d:  box        ""uint""
  IL_0112:  ldloc.s    V_6
  IL_0114:  box        ""int""
  IL_0119:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_011e:  pop
  IL_011f:  ldloc.s    V_13
  IL_0121:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0126:  brtrue.s   IL_00fb
  IL_0128:  leave.s    IL_0136
}
  finally
{
  IL_012a:  ldloc.s    V_13
  IL_012c:  brfalse.s  IL_0135
  IL_012e:  ldloc.s    V_13
  IL_0130:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0135:  endfinally
}
  IL_0136:  ldloc.s    V_6
  IL_0138:  box        ""int""
  IL_013d:  ldc.i4.1
  IL_013e:  box        ""int""
  IL_0143:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0148:  pop
  IL_0149:  ldloc.0
  IL_014a:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_014f:  ldloc.0
  IL_0150:  ldloc.1
  IL_0151:  callvirt   ""int System.Collections.Generic.IList<Windows.Languages.WinRTTest.UserDefinedStruct>.IndexOf(Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0156:  stloc.s    V_7
  IL_0158:  ldloc.0
  IL_0159:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_015e:  ldc.i4.5
  IL_015f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0164:  pop
  IL_0165:  ldloc.s    V_7
  IL_0167:  box        ""int""
  IL_016c:  ldc.i4.0
  IL_016d:  box        ""int""
  IL_0172:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0177:  pop
  IL_0178:  ldloc.0
  IL_0179:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_017e:  ldloc.0
  IL_017f:  ldc.i4.1
  IL_0180:  ldloca.s   V_12
  IL_0182:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_0188:  ldloca.s   V_12
  IL_018a:  ldc.i4.4
  IL_018b:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0190:  ldloc.s    V_12
  IL_0192:  callvirt   ""void System.Collections.Generic.IList<Windows.Languages.WinRTTest.UserDefinedStruct>.Insert(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0197:  ldloc.0
  IL_0198:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_019d:  ldc.i4.7
  IL_019e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01a3:  pop
  IL_01a4:  ldloc.0
  IL_01a5:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_01aa:  ldloc.0
  IL_01ab:  callvirt   ""bool System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.IsReadOnly.get""
  IL_01b0:  stloc.s    V_8
  IL_01b2:  ldloc.0
  IL_01b3:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_01b8:  ldc.i4.0
  IL_01b9:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01be:  pop
  IL_01bf:  ldloc.s    V_8
  IL_01c1:  box        ""bool""
  IL_01c6:  ldc.i4.0
  IL_01c7:  box        ""bool""
  IL_01cc:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01d1:  pop
  IL_01d2:  ldloc.0
  IL_01d3:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_01d8:  ldloc.0
  IL_01d9:  ldc.i4.0
  IL_01da:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IList<Windows.Languages.WinRTTest.UserDefinedStruct>.this[int].get""
  IL_01df:  pop
  IL_01e0:  ldloc.0
  IL_01e1:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_01e6:  ldc.i4.2
  IL_01e7:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01ec:  pop
  IL_01ed:  ldloc.0
  IL_01ee:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_01f3:  ldloc.0
  IL_01f4:  ldc.i4.1
  IL_01f5:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IList<Windows.Languages.WinRTTest.UserDefinedStruct>.this[int].get""
  IL_01fa:  pop
  IL_01fb:  ldloc.0
  IL_01fc:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0201:  ldc.i4.2
  IL_0202:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0207:  pop
  IL_0208:  ldloc.0
  IL_0209:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_020e:  ldloc.0
  IL_020f:  ldloc.1
  IL_0210:  callvirt   ""bool System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Remove(Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0215:  pop
  IL_0216:  ldloc.0
  IL_0217:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_021c:  ldc.i4.8
  IL_021d:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0222:  pop
  IL_0223:  ldloc.0
  IL_0224:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Count.get""
  IL_0229:  box        ""int""
  IL_022e:  ldc.i4.1
  IL_022f:  box        ""int""
  IL_0234:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0239:  pop
  IL_023a:  ldloc.0
  IL_023b:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0240:  ldloc.0
  IL_0241:  ldc.i4.0
  IL_0242:  callvirt   ""void System.Collections.Generic.IList<Windows.Languages.WinRTTest.UserDefinedStruct>.RemoveAt(int)""
  IL_0247:  ldloc.0
  IL_0248:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_024d:  ldc.i4.8
  IL_024e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0253:  pop
  IL_0254:  ldloc.0
  IL_0255:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Count.get""
  IL_025a:  box        ""int""
  IL_025f:  ldc.i4.0
  IL_0260:  box        ""int""
  IL_0265:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_026a:  pop
  IL_026b:  ldloc.0
  IL_026c:  ldloc.1
  IL_026d:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Add(Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0272:  ldloc.0
  IL_0273:  ldloca.s   V_12
  IL_0275:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_027b:  ldloca.s   V_12
  IL_027d:  ldc.i4.4
  IL_027e:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_0283:  ldloc.s    V_12
  IL_0285:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Add(Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_028a:  ldloc.0
  IL_028b:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0290:  ldloc.0
  IL_0291:  callvirt   ""void System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Clear()""
  IL_0296:  ldloc.0
  IL_0297:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_029c:  ldc.i4.s   11
  IL_029e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02a3:  pop
  IL_02a4:  ldloc.0
  IL_02a5:  callvirt   ""int System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Count.get""
  IL_02aa:  box        ""int""
  IL_02af:  ldc.i4.0
  IL_02b0:  box        ""int""
  IL_02b5:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02ba:  pop
  IL_02bb:  ldloc.0
  IL_02bc:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_02c1:  ldloc.0
  IL_02c2:  callvirt   ""int System.Collections.Generic.IReadOnlyCollection<Windows.Languages.WinRTTest.UserDefinedStruct>.Count.get""
  IL_02c7:  stloc.s    V_4
  IL_02c9:  ldloc.0
  IL_02ca:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_02cf:  ldc.i4.3
  IL_02d0:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02d5:  pop
  IL_02d6:  ldloc.0
  IL_02d7:  ldloca.s   V_12
  IL_02d9:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_02df:  ldloca.s   V_12
  IL_02e1:  ldc.i4.s   10
  IL_02e3:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_02e8:  ldloc.s    V_12
  IL_02ea:  stloc.1
  IL_02eb:  dup
  IL_02ec:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_02f1:  dup
  IL_02f2:  ldc.i4.1
  IL_02f3:  ldloc.1
  IL_02f4:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_02f9:  dup
  IL_02fa:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_02ff:  ldc.i4.s   21
  IL_0301:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0306:  pop
  IL_0307:  dup
  IL_0308:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_030d:  box        ""int""
  IL_0312:  ldc.i4.1
  IL_0313:  box        ""int""
  IL_0318:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_031d:  pop
  IL_031e:  dup
  IL_031f:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0324:  dup
  IL_0325:  ldc.i4.1
  IL_0326:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.ContainsKey(int)""
  IL_032b:  pop
  IL_032c:  dup
  IL_032d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0332:  ldc.i4.s   19
  IL_0334:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0339:  pop
  IL_033a:  dup
  IL_033b:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0340:  dup
  IL_0341:  ldc.i4.1
  IL_0342:  callvirt   ""Windows.Languages.WinRTTest.UserDefinedStruct System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.this[int].get""
  IL_0347:  pop
  IL_0348:  dup
  IL_0349:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_034e:  ldc.i4.s   17
  IL_0350:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0355:  pop
  IL_0356:  dup
  IL_0357:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_035c:  dup
  IL_035d:  callvirt   ""System.Collections.Generic.ICollection<int> System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Keys.get""
  IL_0362:  pop
  IL_0363:  dup
  IL_0364:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0369:  ldc.i4.0
  IL_036a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_036f:  pop
  IL_0370:  dup
  IL_0371:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0376:  dup
  IL_0377:  callvirt   ""System.Collections.Generic.ICollection<Windows.Languages.WinRTTest.UserDefinedStruct> System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Values.get""
  IL_037c:  pop
  IL_037d:  dup
  IL_037e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0383:  ldc.i4.0
  IL_0384:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0389:  pop
  IL_038a:  dup
  IL_038b:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0390:  dup
  IL_0391:  ldc.i4.1
  IL_0392:  ldloca.s   V_9
  IL_0394:  callvirt   ""bool System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.TryGetValue(int, out Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0399:  stloc.s    V_10
  IL_039b:  dup
  IL_039c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_03a1:  ldc.i4.s   17
  IL_03a3:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_03a8:  pop
  IL_03a9:  ldloc.s    V_10
  IL_03ab:  box        ""bool""
  IL_03b0:  ldc.i4.1
  IL_03b1:  box        ""bool""
  IL_03b6:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_03bb:  pop
  IL_03bc:  dup
  IL_03bd:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_03c2:  dup
  IL_03c3:  ldc.i4.3
  IL_03c4:  ldloca.s   V_12
  IL_03c6:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_03cc:  ldloca.s   V_12
  IL_03ce:  ldc.i4.4
  IL_03cf:  stfld      ""uint Windows.Languages.WinRTTest.UserDefinedStruct.Id""
  IL_03d4:  ldloc.s    V_12
  IL_03d6:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_03db:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Add(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_03e0:  dup
  IL_03e1:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_03e6:  ldc.i4.s   21
  IL_03e8:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_03ed:  pop
  IL_03ee:  dup
  IL_03ef:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_03f4:  box        ""int""
  IL_03f9:  ldc.i4.2
  IL_03fa:  box        ""int""
  IL_03ff:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0404:  pop
  IL_0405:  dup
  IL_0406:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_040b:  dup
  IL_040c:  ldc.i4.1
  IL_040d:  ldloc.1
  IL_040e:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0413:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Contains(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_0418:  stloc.s    V_11
  IL_041a:  dup
  IL_041b:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0420:  ldc.i4.s   17
  IL_0422:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0427:  pop
  IL_0428:  ldloc.s    V_11
  IL_042a:  box        ""bool""
  IL_042f:  ldc.i4.1
  IL_0430:  box        ""bool""
  IL_0435:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_043a:  pop
  IL_043b:  dup
  IL_043c:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0441:  dup
  IL_0442:  ldc.i4.8
  IL_0443:  ldloca.s   V_12
  IL_0445:  initobj    ""Windows.Languages.WinRTTest.UserDefinedStruct""
  IL_044b:  ldloc.s    V_12
  IL_044d:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0452:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Contains(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_0457:  stloc.s    V_11
  IL_0459:  dup
  IL_045a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_045f:  ldc.i4.s   19
  IL_0461:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0466:  pop
  IL_0467:  ldloc.s    V_11
  IL_0469:  box        ""bool""
  IL_046e:  ldc.i4.0
  IL_046f:  box        ""bool""
  IL_0474:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0479:  pop
  IL_047a:  dup
  IL_047b:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0480:  dup
  IL_0481:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_0486:  dup
  IL_0487:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_048c:  stloc.s    V_4
  IL_048e:  dup
  IL_048f:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0494:  ldc.i4.s   18
  IL_0496:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_049b:  pop
  IL_049c:  ldloc.s    V_4
  IL_049e:  box        ""int""
  IL_04a3:  ldc.i4.1
  IL_04a4:  box        ""int""
  IL_04a9:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_04ae:  pop
  IL_04af:  dup
  IL_04b0:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_04b5:  dup
  IL_04b6:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.IsReadOnly.get""
  IL_04bb:  stloc.s    V_8
  IL_04bd:  dup
  IL_04be:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_04c3:  ldc.i4.0
  IL_04c4:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_04c9:  pop
  IL_04ca:  ldloc.s    V_8
  IL_04cc:  box        ""bool""
  IL_04d1:  ldc.i4.0
  IL_04d2:  box        ""bool""
  IL_04d7:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_04dc:  pop
  IL_04dd:  dup
  IL_04de:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_04e3:  dup
  IL_04e4:  ldc.i4.1
  IL_04e5:  ldloc.1
  IL_04e6:  newobj     ""System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>..ctor(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_04eb:  callvirt   ""bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Remove(System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>)""
  IL_04f0:  pop
  IL_04f1:  dup
  IL_04f2:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_04f7:  ldc.i4.s   22
  IL_04f9:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_04fe:  pop
  IL_04ff:  dup
  IL_0500:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0505:  box        ""int""
  IL_050a:  ldc.i4.0
  IL_050b:  box        ""int""
  IL_0510:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0515:  pop
  IL_0516:  dup
  IL_0517:  ldc.i4.1
  IL_0518:  ldloc.1
  IL_0519:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_051e:  dup
  IL_051f:  ldc.i4.2
  IL_0520:  ldloc.1
  IL_0521:  callvirt   ""void System.Collections.Generic.IDictionary<int, Windows.Languages.WinRTTest.UserDefinedStruct>.Add(int, Windows.Languages.WinRTTest.UserDefinedStruct)""
  IL_0526:  dup
  IL_0527:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_052c:  dup
  IL_052d:  callvirt   ""void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Clear()""
  IL_0532:  dup
  IL_0533:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_0538:  ldc.i4.s   23
  IL_053a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_053f:  pop
  IL_0540:  dup
  IL_0541:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0546:  box        ""int""
  IL_054b:  ldc.i4.0
  IL_054c:  box        ""int""
  IL_0551:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0556:  pop
  IL_0557:  dup
  IL_0558:  callvirt   ""void Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.ClearFlag()""
  IL_055d:  dup
  IL_055e:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, Windows.Languages.WinRTTest.UserDefinedStruct>>.Count.get""
  IL_0563:  stloc.s    V_4
  IL_0565:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorStructIVectorViewStructIMapIntStructIMapViewIntStruct.GetFlagState()""
  IL_056a:  ldc.i4.s   18
  IL_056c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0571:  pop
  IL_0572:  ret
}
");
        }

        [Fact]
        public void LegacyCollectionTest05()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestISimpleInterfaceImplMembers()
    {
        ISimpleInterfaceImpl v = new ISimpleInterfaceImpl();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue((v as IList<int>)[0], 1);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(b, true);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(arr[0], 1);
        ValidateValue(arr[1], 0); //there should be nothing there! :)
        //GetEnumerator
        v.ClearFlag();
        int count = (v as IList<int>).Count;
        IEnumerator<int> enumerator = ((IEnumerable<int>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        int index = 0;
        foreach (var e in v)
        {
            index = index + 1;
            ValidateValue(e, index);
        }

        ValidateValue(index, 1); //there should only be 1 element there
        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez, 0); // 1 is on the first line :)
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        ValidateValue((v as IList<int>)[1], 2);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        ValidateValue(isReadOnly, false);
        //Indexing
        v.ClearFlag();
        int val = (v as IList<int>)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 1);
        v.ClearFlag();
        val = ((IList<int>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        ValidateValue(val, 2);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((v as IList<int>).Count, 1);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_RemoveAt);
        ValidateValue((v as IList<int>).Count, 0);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        //v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Clear);
        ValidateValue((v as IList<int>).Count, 0);
    }

    static int Main()
    {
        TestISimpleInterfaceImplMembers();
        
        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestISimpleInterfaceImplMembers",
@"{
  // Code size      686 (0x2ae)
  .maxstack  3
  .locals init (Windows.Languages.WinRTTest.ISimpleInterfaceImpl V_0, //v
  bool V_1, //b
  int[] V_2, //arr
  int V_3, //index
  int V_4, //rez
  bool V_5, //isReadOnly
  int V_6, //val
  System.Collections.Generic.IEnumerator<int> V_7)
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.ISimpleInterfaceImpl..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0019:  ldc.i4.s   9
  IL_001b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.0
  IL_0023:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_0028:  box        ""int""
  IL_002d:  ldc.i4.1
  IL_002e:  box        ""int""
  IL_0033:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.1
  IL_0041:  callvirt   ""bool System.Collections.Generic.ICollection<int>.Contains(int)""
  IL_0046:  stloc.1
  IL_0047:  ldloc.0
  IL_0048:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_004d:  ldc.i4.5
  IL_004e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0053:  pop
  IL_0054:  ldloc.1
  IL_0055:  box        ""bool""
  IL_005a:  ldc.i4.1
  IL_005b:  box        ""bool""
  IL_0060:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0065:  pop
  IL_0066:  ldloc.0
  IL_0067:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_006c:  ldc.i4.s   10
  IL_006e:  newarr     ""int""
  IL_0073:  stloc.2
  IL_0074:  ldloc.0
  IL_0075:  ldloc.2
  IL_0076:  ldc.i4.0
  IL_0077:  callvirt   ""void System.Collections.Generic.ICollection<int>.CopyTo(int[], int)""
  IL_007c:  ldloc.0
  IL_007d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0082:  ldc.i4.2
  IL_0083:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0088:  pop
  IL_0089:  ldloc.2
  IL_008a:  ldc.i4.0
  IL_008b:  ldelem.i4
  IL_008c:  box        ""int""
  IL_0091:  ldc.i4.1
  IL_0092:  box        ""int""
  IL_0097:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_009c:  pop
  IL_009d:  ldloc.2
  IL_009e:  ldc.i4.1
  IL_009f:  ldelem.i4
  IL_00a0:  box        ""int""
  IL_00a5:  ldc.i4.0
  IL_00a6:  box        ""int""
  IL_00ab:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00b0:  pop
  IL_00b1:  ldloc.0
  IL_00b2:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_00b7:  ldloc.0
  IL_00b8:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_00bd:  pop
  IL_00be:  ldloc.0
  IL_00bf:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_00c4:  pop
  IL_00c5:  ldloc.0
  IL_00c6:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_00cb:  ldc.i4.1
  IL_00cc:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d1:  pop
  IL_00d2:  ldc.i4.0
  IL_00d3:  stloc.3
  IL_00d4:  ldloc.0
  IL_00d5:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_00da:  stloc.s    V_7
  .try
{
  IL_00dc:  br.s       IL_00fa
  IL_00de:  ldloc.s    V_7
  IL_00e0:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
  IL_00e5:  ldloc.3
  IL_00e6:  ldc.i4.1
  IL_00e7:  add
  IL_00e8:  stloc.3
  IL_00e9:  box        ""int""
  IL_00ee:  ldloc.3
  IL_00ef:  box        ""int""
  IL_00f4:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00f9:  pop
  IL_00fa:  ldloc.s    V_7
  IL_00fc:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0101:  brtrue.s   IL_00de
  IL_0103:  leave.s    IL_0111
}
  finally
{
  IL_0105:  ldloc.s    V_7
  IL_0107:  brfalse.s  IL_0110
  IL_0109:  ldloc.s    V_7
  IL_010b:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0110:  endfinally
}
  IL_0111:  ldloc.3
  IL_0112:  box        ""int""
  IL_0117:  ldc.i4.1
  IL_0118:  box        ""int""
  IL_011d:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0122:  pop
  IL_0123:  ldloc.0
  IL_0124:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_0129:  ldloc.0
  IL_012a:  ldc.i4.1
  IL_012b:  callvirt   ""int System.Collections.Generic.IList<int>.IndexOf(int)""
  IL_0130:  stloc.s    V_4
  IL_0132:  ldloc.0
  IL_0133:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0138:  ldc.i4.5
  IL_0139:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_013e:  pop
  IL_013f:  ldloc.s    V_4
  IL_0141:  box        ""int""
  IL_0146:  ldc.i4.0
  IL_0147:  box        ""int""
  IL_014c:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0151:  pop
  IL_0152:  ldloc.0
  IL_0153:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_0158:  ldloc.0
  IL_0159:  ldc.i4.1
  IL_015a:  ldc.i4.2
  IL_015b:  callvirt   ""void System.Collections.Generic.IList<int>.Insert(int, int)""
  IL_0160:  ldloc.0
  IL_0161:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0166:  ldc.i4.7
  IL_0167:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_016c:  pop
  IL_016d:  ldloc.0
  IL_016e:  ldc.i4.1
  IL_016f:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_0174:  box        ""int""
  IL_0179:  ldc.i4.2
  IL_017a:  box        ""int""
  IL_017f:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0184:  pop
  IL_0185:  ldloc.0
  IL_0186:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_018b:  ldloc.0
  IL_018c:  callvirt   ""bool System.Collections.Generic.ICollection<int>.IsReadOnly.get""
  IL_0191:  stloc.s    V_5
  IL_0193:  ldloc.0
  IL_0194:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0199:  ldc.i4.0
  IL_019a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_019f:  pop
  IL_01a0:  ldloc.s    V_5
  IL_01a2:  box        ""bool""
  IL_01a7:  ldc.i4.0
  IL_01a8:  box        ""bool""
  IL_01ad:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01b2:  pop
  IL_01b3:  ldloc.0
  IL_01b4:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_01b9:  ldloc.0
  IL_01ba:  ldc.i4.0
  IL_01bb:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_01c0:  stloc.s    V_6
  IL_01c2:  ldloc.0
  IL_01c3:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_01c8:  ldc.i4.2
  IL_01c9:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01ce:  pop
  IL_01cf:  ldloc.s    V_6
  IL_01d1:  box        ""int""
  IL_01d6:  ldc.i4.1
  IL_01d7:  box        ""int""
  IL_01dc:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_01e1:  pop
  IL_01e2:  ldloc.0
  IL_01e3:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_01e8:  ldloc.0
  IL_01e9:  ldc.i4.1
  IL_01ea:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_01ef:  stloc.s    V_6
  IL_01f1:  ldloc.0
  IL_01f2:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_01f7:  ldc.i4.2
  IL_01f8:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01fd:  pop
  IL_01fe:  ldloc.s    V_6
  IL_0200:  box        ""int""
  IL_0205:  ldc.i4.2
  IL_0206:  box        ""int""
  IL_020b:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0210:  pop
  IL_0211:  ldloc.0
  IL_0212:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_0217:  ldloc.0
  IL_0218:  ldc.i4.1
  IL_0219:  callvirt   ""bool System.Collections.Generic.ICollection<int>.Remove(int)""
  IL_021e:  pop
  IL_021f:  ldloc.0
  IL_0220:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0225:  ldc.i4.8
  IL_0226:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_022b:  pop
  IL_022c:  ldloc.0
  IL_022d:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_0232:  box        ""int""
  IL_0237:  ldc.i4.1
  IL_0238:  box        ""int""
  IL_023d:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0242:  pop
  IL_0243:  ldloc.0
  IL_0244:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_0249:  ldloc.0
  IL_024a:  ldc.i4.0
  IL_024b:  callvirt   ""void System.Collections.Generic.IList<int>.RemoveAt(int)""
  IL_0250:  ldloc.0
  IL_0251:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_0256:  ldc.i4.8
  IL_0257:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_025c:  pop
  IL_025d:  ldloc.0
  IL_025e:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_0263:  box        ""int""
  IL_0268:  ldc.i4.0
  IL_0269:  box        ""int""
  IL_026e:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0273:  pop
  IL_0274:  ldloc.0
  IL_0275:  ldc.i4.1
  IL_0276:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_027b:  ldloc.0
  IL_027c:  ldc.i4.2
  IL_027d:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0282:  ldloc.0
  IL_0283:  callvirt   ""void Windows.Languages.WinRTTest.ISimpleInterfaceImpl.ClearFlag()""
  IL_0288:  ldloc.0
  IL_0289:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.ISimpleInterfaceImpl.GetFlagState()""
  IL_028e:  ldc.i4.s   11
  IL_0290:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0295:  pop
  IL_0296:  ldloc.0
  IL_0297:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_029c:  box        ""int""
  IL_02a1:  ldc.i4.0
  IL_02a2:  box        ""int""
  IL_02a7:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_02ac:  pop
  IL_02ad:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest06()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestCollectionInitializers()
    {
        var v = new IVectorInt() { 1, 2, 3, 4, 5 };
        ValidateValue(v.Count, 5);
        var m = new IMapIntInt() { { 1, 2 }, { 2, 3 } };
        ValidateValue(m.Count, 2);
        var t = new Dictionary<int, IVectorInt>()        
        {        
            {1, new IVectorInt(){1, 2, 3}},
            {2, new IVectorInt(){4, 5, 6}}
        };
        ValidateValue(t[1][2], 3);
        ValidateValue(t[2][2], 6);
    }


    static int Main()
    {
        TestCollectionInitializers();
        
        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestCollectionInitializers",
@"{
  // Code size      236 (0xec)
  .maxstack  6
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0013:  dup
  IL_0014:  ldc.i4.3
  IL_0015:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_001a:  dup
  IL_001b:  ldc.i4.4
  IL_001c:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0021:  dup
  IL_0022:  ldc.i4.5
  IL_0023:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0028:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_002d:  box        ""int""
  IL_0032:  ldc.i4.5
  IL_0033:  box        ""int""
  IL_0038:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_003d:  pop
  IL_003e:  newobj     ""Windows.Languages.WinRTTest.IMapIntInt..ctor()""
  IL_0043:  dup
  IL_0044:  ldc.i4.1
  IL_0045:  ldc.i4.2
  IL_0046:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_004b:  dup
  IL_004c:  ldc.i4.2
  IL_004d:  ldc.i4.3
  IL_004e:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_0053:  callvirt   ""int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, int>>.Count.get""
  IL_0058:  box        ""int""
  IL_005d:  ldc.i4.2
  IL_005e:  box        ""int""
  IL_0063:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0068:  pop
  IL_0069:  newobj     ""System.Collections.Generic.Dictionary<int, Windows.Languages.WinRTTest.IVectorInt>..ctor()""
  IL_006e:  dup
  IL_006f:  ldc.i4.1
  IL_0070:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_0075:  dup
  IL_0076:  ldc.i4.1
  IL_0077:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_007c:  dup
  IL_007d:  ldc.i4.2
  IL_007e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0083:  dup
  IL_0084:  ldc.i4.3
  IL_0085:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_008a:  callvirt   ""void System.Collections.Generic.Dictionary<int, Windows.Languages.WinRTTest.IVectorInt>.Add(int, Windows.Languages.WinRTTest.IVectorInt)""
  IL_008f:  dup
  IL_0090:  ldc.i4.2
  IL_0091:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_0096:  dup
  IL_0097:  ldc.i4.4
  IL_0098:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_009d:  dup
  IL_009e:  ldc.i4.5
  IL_009f:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_00a4:  dup
  IL_00a5:  ldc.i4.6
  IL_00a6:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_00ab:  callvirt   ""void System.Collections.Generic.Dictionary<int, Windows.Languages.WinRTTest.IVectorInt>.Add(int, Windows.Languages.WinRTTest.IVectorInt)""
  IL_00b0:  dup
  IL_00b1:  ldc.i4.1
  IL_00b2:  callvirt   ""Windows.Languages.WinRTTest.IVectorInt System.Collections.Generic.Dictionary<int, Windows.Languages.WinRTTest.IVectorInt>.this[int].get""
  IL_00b7:  ldc.i4.2
  IL_00b8:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_00bd:  box        ""int""
  IL_00c2:  ldc.i4.3
  IL_00c3:  box        ""int""
  IL_00c8:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00cd:  pop
  IL_00ce:  ldc.i4.2
  IL_00cf:  callvirt   ""Windows.Languages.WinRTTest.IVectorInt System.Collections.Generic.Dictionary<int, Windows.Languages.WinRTTest.IVectorInt>.this[int].get""
  IL_00d4:  ldc.i4.2
  IL_00d5:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_00da:  box        ""int""
  IL_00df:  ldc.i4.6
  IL_00e0:  box        ""int""
  IL_00e5:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00ea:  pop
  IL_00eb:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest07()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestExpressionTreeCompiler()
    {
        var v = new IVectorInt();
        try
        {
            // Dev11:205875
            Console.WriteLine(""Dev11:205875"");
            ValidateValue(true, true);
            Expression<Action<int>> expr = (val) => v.Add(val);
            v.ClearFlag();
            expr.Compile()(1);
            ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        }
        catch (Exception e)
        {
            Console.WriteLine(""ExprTree compiler"");
            Console.WriteLine(e.Message);
        }
    }

    static int Main()
    {
        TestExpressionTreeCompiler();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);

            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));

            verifier.VerifyIL("AllMembers.TestExpressionTreeCompiler",
@"
{
  // Code size      213 (0xd5)
  .maxstack  6
  .locals init (AllMembers.<>c__DisplayClass3_0 V_0, //CS$<>8__locals0
                System.Linq.Expressions.ParameterExpression V_1)
  IL_0000:  newobj     ""AllMembers.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_000c:  stfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  .try
  {
    IL_0011:  ldstr      ""Dev11:205875""
    IL_0016:  call       ""void System.Console.WriteLine(string)""
    IL_001b:  ldc.i4.1
    IL_001c:  box        ""bool""
    IL_0021:  ldc.i4.1
    IL_0022:  box        ""bool""
    IL_0027:  call       ""bool AllMembers.ValidateValue(object, object)""
    IL_002c:  pop
    IL_002d:  ldtoken    ""int""
    IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0037:  ldstr      ""val""
    IL_003c:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
    IL_0041:  stloc.1
    IL_0042:  ldloc.0
    IL_0043:  ldtoken    ""AllMembers.<>c__DisplayClass3_0""
    IL_0048:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_004d:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
    IL_0052:  ldtoken    ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
    IL_0057:  call       ""System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)""
    IL_005c:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)""
    IL_0061:  ldtoken    ""void System.Collections.Generic.ICollection<int>.Add(int)""
    IL_0066:  ldtoken    ""System.Collections.Generic.ICollection<int>""
    IL_006b:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)""
    IL_0070:  castclass  ""System.Reflection.MethodInfo""
    IL_0075:  ldc.i4.1
    IL_0076:  newarr     ""System.Linq.Expressions.Expression""
    IL_007b:  dup
    IL_007c:  ldc.i4.0
    IL_007d:  ldloc.1
    IL_007e:  stelem.ref
    IL_007f:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
    IL_0084:  ldc.i4.1
    IL_0085:  newarr     ""System.Linq.Expressions.ParameterExpression""
    IL_008a:  dup
    IL_008b:  ldc.i4.0
    IL_008c:  ldloc.1
    IL_008d:  stelem.ref
    IL_008e:  call       ""System.Linq.Expressions.Expression<System.Action<int>> System.Linq.Expressions.Expression.Lambda<System.Action<int>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
    IL_0093:  ldloc.0
    IL_0094:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
    IL_0099:  callvirt   ""void Windows.Languages.WinRTTest.IVectorInt.ClearFlag()""
    IL_009e:  callvirt   ""System.Action<int> System.Linq.Expressions.Expression<System.Action<int>>.Compile()""
    IL_00a3:  ldc.i4.1
    IL_00a4:  callvirt   ""void System.Action<int>.Invoke(int)""
    IL_00a9:  ldloc.0
    IL_00aa:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
    IL_00af:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorInt.GetFlagState()""
    IL_00b4:  ldc.i4.s   9
    IL_00b6:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
    IL_00bb:  pop
    IL_00bc:  leave.s    IL_00d4
  }
  catch System.Exception
  {
    IL_00be:  ldstr      ""ExprTree compiler""
    IL_00c3:  call       ""void System.Console.WriteLine(string)""
    IL_00c8:  callvirt   ""string System.Exception.Message.get""
    IL_00cd:  call       ""void System.Console.WriteLine(string)""
    IL_00d2:  leave.s    IL_00d4
  }
  IL_00d4:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest09()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestLINQ()
    {
        var v = new IVectorInt() { 1, 2, 3, 4, 5 };
        ValidateValue(v.Count, 5);
        // Use methods on v inside query operator
        v.ClearFlag();
        var rez = from e in new int[] { 2, 4, 6, 10, 12 }
                  where v.Contains(e)
                  select e;
        rez = rez.ToList();

        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        ValidateValue(rez.Count(), 2);
        ValidateValue(rez.ToArray()[0], 2);
        ValidateValue(rez.ToArray()[1], 4);

        //Use v as source to linq query
        rez = from e in v
              where e % 2 == 0
              select e;
        rez = rez.ToList();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
        ValidateValue(rez.Count(), 2);

        //over IQueryable
        try
        {
            Console.WriteLine(""Dev11:205875"");
            ValidateValue(false, false);
            //var list = new List<int>() { 1, 2, 3, 4, 5 };
            //var otherRez = from e in list.AsQueryable()
            //               where v.Contains(e)
            //               select e;
            //var vals = otherRez.ToArray();
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(""TestLINQ"");
            Console.WriteLine(e.Message);
        }
    }

    static int Main()
    {
        TestLINQ();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails,
                options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"));

            verifier.VerifyIL("AllMembers.TestLINQ",
@"
{
  // Code size      360 (0x168)
  .maxstack  4
  .locals init (AllMembers.<>c__DisplayClass3_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""AllMembers.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_001a:  dup
  IL_001b:  ldc.i4.3
  IL_001c:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0021:  dup
  IL_0022:  ldc.i4.4
  IL_0023:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0028:  dup
  IL_0029:  ldc.i4.5
  IL_002a:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_002f:  stfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  IL_0034:  ldloc.0
  IL_0035:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  IL_003a:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_003f:  box        ""int""
  IL_0044:  ldc.i4.5
  IL_0045:  box        ""int""
  IL_004a:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_004f:  pop
  IL_0050:  ldloc.0
  IL_0051:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  IL_0056:  callvirt   ""void Windows.Languages.WinRTTest.IVectorInt.ClearFlag()""
  IL_005b:  ldc.i4.5
  IL_005c:  newarr     ""int""
  IL_0061:  dup
  IL_0062:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.864782BF337E3DBC1A27023D5C0C065C80F17087""
  IL_0067:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_006c:  ldloc.0
  IL_006d:  ldftn      ""bool AllMembers.<>c__DisplayClass3_0.<TestLINQ>b__0(int)""
  IL_0073:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0078:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_007d:  call       ""System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0082:  ldloc.0
  IL_0083:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  IL_0088:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorInt.GetFlagState()""
  IL_008d:  ldc.i4.5
  IL_008e:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0093:  pop
  IL_0094:  dup
  IL_0095:  call       ""int System.Linq.Enumerable.Count<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_009a:  box        ""int""
  IL_009f:  ldc.i4.2
  IL_00a0:  box        ""int""
  IL_00a5:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00aa:  pop
  IL_00ab:  dup
  IL_00ac:  call       ""int[] System.Linq.Enumerable.ToArray<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_00b1:  ldc.i4.0
  IL_00b2:  ldelem.i4
  IL_00b3:  box        ""int""
  IL_00b8:  ldc.i4.2
  IL_00b9:  box        ""int""
  IL_00be:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00c3:  pop
  IL_00c4:  call       ""int[] System.Linq.Enumerable.ToArray<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_00c9:  ldc.i4.1
  IL_00ca:  ldelem.i4
  IL_00cb:  box        ""int""
  IL_00d0:  ldc.i4.4
  IL_00d1:  box        ""int""
  IL_00d6:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_00db:  pop
  IL_00dc:  ldloc.0
  IL_00dd:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  IL_00e2:  ldsfld     ""System.Func<int, bool> AllMembers.<>c.<>9__3_1""
  IL_00e7:  dup
  IL_00e8:  brtrue.s   IL_0101
  IL_00ea:  pop
  IL_00eb:  ldsfld     ""AllMembers.<>c AllMembers.<>c.<>9""
  IL_00f0:  ldftn      ""bool AllMembers.<>c.<TestLINQ>b__3_1(int)""
  IL_00f6:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_00fb:  dup
  IL_00fc:  stsfld     ""System.Func<int, bool> AllMembers.<>c.<>9__3_1""
  IL_0101:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_0106:  call       ""System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_010b:  ldloc.0
  IL_010c:  ldfld      ""Windows.Languages.WinRTTest.IVectorInt AllMembers.<>c__DisplayClass3_0.v""
  IL_0111:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorInt.GetFlagState()""
  IL_0116:  ldc.i4.1
  IL_0117:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_011c:  pop
  IL_011d:  call       ""int System.Linq.Enumerable.Count<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0122:  box        ""int""
  IL_0127:  ldc.i4.2
  IL_0128:  box        ""int""
  IL_012d:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0132:  pop
  .try
  {
    IL_0133:  ldstr      ""Dev11:205875""
    IL_0138:  call       ""void System.Console.WriteLine(string)""
    IL_013d:  ldc.i4.0
    IL_013e:  box        ""bool""
    IL_0143:  ldc.i4.0
    IL_0144:  box        ""bool""
    IL_0149:  call       ""bool AllMembers.ValidateValue(object, object)""
    IL_014e:  pop
    IL_014f:  leave.s    IL_0167
  }
  catch System.ArgumentException
  {
    IL_0151:  ldstr      ""TestLINQ""
    IL_0156:  call       ""void System.Console.WriteLine(string)""
    IL_015b:  callvirt   ""string System.Exception.Message.get""
    IL_0160:  call       ""void System.Console.WriteLine(string)""
    IL_0165:  leave.s    IL_0167
  }
  IL_0167:  ret
}
");
        }

        [Fact]
        public void LegacyCollectionTest10()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestNamedArguments()
    {
        var v = new IVectorInt();
        v.ClearFlag();
        v.Add(item: 1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(v.Count, 1);
        var m = new IMapIntInt();
        m.ClearFlag();
        m.Add(key: 1, value: 1);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
        m.ClearFlag();
        m.Add(2, value: 2);
        ValidateMethod(m.GetFlagState(), TestMethodCalled.IMap_Insert);
    }

    static int Main()
    {
        TestNamedArguments();
        
        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestNamedArguments",
@"{
  // Code size      115 (0x73)
  .maxstack  4
  IL_0000:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""void Windows.Languages.WinRTTest.IVectorInt.ClearFlag()""
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0012:  dup
  IL_0013:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorInt.GetFlagState()""
  IL_0018:  ldc.i4.s   9
  IL_001a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_001f:  pop
  IL_0020:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_0025:  box        ""int""
  IL_002a:  ldc.i4.1
  IL_002b:  box        ""int""
  IL_0030:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_0035:  pop
  IL_0036:  newobj     ""Windows.Languages.WinRTTest.IMapIntInt..ctor()""
  IL_003b:  dup
  IL_003c:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_0041:  dup
  IL_0042:  ldc.i4.1
  IL_0043:  ldc.i4.1
  IL_0044:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_0049:  dup
  IL_004a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_004f:  ldc.i4.s   21
  IL_0051:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0056:  pop
  IL_0057:  dup
  IL_0058:  callvirt   ""void Windows.Languages.WinRTTest.IMapIntInt.ClearFlag()""
  IL_005d:  dup
  IL_005e:  ldc.i4.2
  IL_005f:  ldc.i4.2
  IL_0060:  callvirt   ""void System.Collections.Generic.IDictionary<int, int>.Add(int, int)""
  IL_0065:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IMapIntInt.GetFlagState()""
  IL_006a:  ldc.i4.s   21
  IL_006c:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0071:  pop
  IL_0072:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest11()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    private static bool ValidateValue(object actual, object expected)
    {
        var temp = Console.ForegroundColor;
        if (actual.ToString() != expected.ToString())
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual.ToString() == expected.ToString();
    }

    static void TestNullableArgs()
    {
        Console.WriteLine(""===  IVectorInt - nullable ==="");
        var v = new IVectorInt();
        //Add
        v.ClearFlag();
        int? x = 1;
        v.Add((int)x);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        ValidateValue(v[0], 1);
    }

    static int Main()
    {
        TestNullableArgs();
        
        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestNullableArgs",
@"
{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (int? V_0) //x
  IL_0000:  ldstr      ""===  IVectorInt - nullable ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IVectorInt..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IVectorInt.ClearFlag()""
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldc.i4.1
  IL_0018:  call       ""int?..ctor(int)""
  IL_001d:  dup
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""int int?.Value.get""
  IL_0025:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_002a:  dup
  IL_002b:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IVectorInt.GetFlagState()""
  IL_0030:  ldc.i4.s   9
  IL_0032:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0037:  pop
  IL_0038:  ldc.i4.0
  IL_0039:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_003e:  box        ""int""
  IL_0043:  ldc.i4.1
  IL_0044:  box        ""int""
  IL_0049:  call       ""bool AllMembers.ValidateValue(object, object)""
  IL_004e:  pop
  IL_004f:  ret
}
");
        }

        [Fact]
        public void LegacyCollectionTest12()
        {
            var source =
@"using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Test
{
    public class R : IObservableVector<int>
    {
        public event VectorChangedEventHandler<int> VectorChanged;

        public int IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        int IObservableVector<int>.this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        int IList<int>.this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Add(int item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(int item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<int> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}";
            var comp = CreateCompilationWithWinRT(source, references: LegacyRefs, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
    // (30,36): error CS0539: 'R.this[int]' in explicit interface declaration is not a member of interface
    //         int IObservableVector<int>.this[int index]
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Test.R.this[int]").WithLocation(30, 36),
    // (13,53): warning CS0067: The event 'R.VectorChanged' is never used
    //         public event VectorChangedEventHandler<int> VectorChanged;
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "VectorChanged").WithArguments("Test.R.VectorChanged").WithLocation(13, 53),
    // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
    Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
    // (2,1): hidden CS8019: Unnecessary using directive.
    // using System.Reflection;
    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;").WithLocation(2, 1),
    // (4,1): hidden CS8019: Unnecessary using directive.
    // using System.Threading;
    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading;").WithLocation(4, 1),
    // (3,1): hidden CS8019: Unnecessary using directive.
    // using System.Runtime.InteropServices;
    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Runtime.InteropServices;").WithLocation(3, 1));
        }

        [Fact]
        public void LegacyCollectionTest13()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void TestIBindableVectorMembers()
    {
        Console.WriteLine(""===  IBindableVectorSimple  ==="");
        var v = new IBindableVectorSimple();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //GetEnumerator
        v.ClearFlag();
        int count = v.Count;
        IEnumerator enumerator = ((IEnumerable)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableIterable_First);

        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_InsertAt);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        //Indexing
        v.ClearFlag();
        object val = v[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt);
        v.ClearFlag();
        val = v[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear);
    }

    static int Main()
    {
        TestIBindableVectorMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestIBindableVectorMembers",
@"{
  // Code size      410 (0x19a)
  .maxstack  4
  .locals init (int[] V_0) //arr
  IL_0000:  ldstr      ""===  IBindableVectorSimple  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IBindableVectorSimple..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0015:  dup
  IL_0016:  ldc.i4.1
  IL_0017:  box        ""int""
  IL_001c:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_0021:  pop
  IL_0022:  dup
  IL_0023:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_0028:  ldc.i4.s   28
  IL_002a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_002f:  pop
  IL_0030:  dup
  IL_0031:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0036:  dup
  IL_0037:  ldc.i4.1
  IL_0038:  box        ""int""
  IL_003d:  callvirt   ""bool System.Collections.IList.Contains(object)""
  IL_0042:  pop
  IL_0043:  dup
  IL_0044:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_0049:  ldc.i4.s   30
  IL_004b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0050:  pop
  IL_0051:  dup
  IL_0052:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0057:  ldc.i4.s   10
  IL_0059:  newarr     ""int""
  IL_005e:  stloc.0
  IL_005f:  dup
  IL_0060:  ldloc.0
  IL_0061:  ldc.i4.0
  IL_0062:  callvirt   ""void System.Collections.ICollection.CopyTo(System.Array, int)""
  IL_0067:  dup
  IL_0068:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_006d:  ldc.i4.s   28
  IL_006f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0074:  pop
  IL_0075:  dup
  IL_0076:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_007b:  dup
  IL_007c:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_0081:  pop
  IL_0082:  dup
  IL_0083:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0088:  pop
  IL_0089:  dup
  IL_008a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_008f:  ldc.i4.s   26
  IL_0091:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0096:  pop
  IL_0097:  dup
  IL_0098:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_009d:  dup
  IL_009e:  ldc.i4.1
  IL_009f:  box        ""int""
  IL_00a4:  callvirt   ""int System.Collections.IList.IndexOf(object)""
  IL_00a9:  pop
  IL_00aa:  dup
  IL_00ab:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_00b0:  ldc.i4.s   30
  IL_00b2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00b7:  pop
  IL_00b8:  dup
  IL_00b9:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_00be:  dup
  IL_00bf:  ldc.i4.1
  IL_00c0:  ldc.i4.2
  IL_00c1:  box        ""int""
  IL_00c6:  callvirt   ""void System.Collections.IList.Insert(int, object)""
  IL_00cb:  dup
  IL_00cc:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_00d1:  ldc.i4.s   32
  IL_00d3:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d8:  pop
  IL_00d9:  dup
  IL_00da:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_00df:  dup
  IL_00e0:  callvirt   ""bool System.Collections.IList.IsReadOnly.get""
  IL_00e5:  pop
  IL_00e6:  dup
  IL_00e7:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_00ec:  ldc.i4.0
  IL_00ed:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00f2:  pop
  IL_00f3:  dup
  IL_00f4:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_00f9:  dup
  IL_00fa:  ldc.i4.0
  IL_00fb:  callvirt   ""object System.Collections.IList.this[int].get""
  IL_0100:  pop
  IL_0101:  dup
  IL_0102:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_0107:  ldc.i4.s   27
  IL_0109:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_010e:  pop
  IL_010f:  dup
  IL_0110:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0115:  dup
  IL_0116:  ldc.i4.1
  IL_0117:  callvirt   ""object System.Collections.IList.this[int].get""
  IL_011c:  pop
  IL_011d:  dup
  IL_011e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_0123:  ldc.i4.s   27
  IL_0125:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_012a:  pop
  IL_012b:  dup
  IL_012c:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0131:  dup
  IL_0132:  ldc.i4.1
  IL_0133:  box        ""int""
  IL_0138:  callvirt   ""void System.Collections.IList.Remove(object)""
  IL_013d:  dup
  IL_013e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_0143:  ldc.i4.s   30
  IL_0145:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_014a:  pop
  IL_014b:  dup
  IL_014c:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0151:  dup
  IL_0152:  ldc.i4.0
  IL_0153:  callvirt   ""void System.Collections.IList.RemoveAt(int)""
  IL_0158:  dup
  IL_0159:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_015e:  ldc.i4.s   33
  IL_0160:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0165:  pop
  IL_0166:  dup
  IL_0167:  ldc.i4.1
  IL_0168:  box        ""int""
  IL_016d:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_0172:  pop
  IL_0173:  dup
  IL_0174:  ldc.i4.2
  IL_0175:  box        ""int""
  IL_017a:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_017f:  pop
  IL_0180:  dup
  IL_0181:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorSimple.ClearFlag()""
  IL_0186:  dup
  IL_0187:  callvirt   ""void System.Collections.IList.Clear()""
  IL_018c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorSimple.GetFlagState()""
  IL_0191:  ldc.i4.s   36
  IL_0193:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0198:  pop
  IL_0199:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest14()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void TestIBindableIterableMembers()
    {
        Console.WriteLine(""===  IBindableIterableSimple  ==="");
        var v = new IBindableIterableSimple();
        //Add
        v.ClearFlag();
        v.GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableIterable_First);
    }

    static int Main()
    {
        TestIBindableIterableMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"),
                // (7,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"));
            verifier.VerifyIL("AllMembers.TestIBindableIterableMembers",
@"{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldstr      ""===  IBindableIterableSimple  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IBindableIterableSimple..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IBindableIterableSimple.ClearFlag()""
  IL_0015:  dup
  IL_0016:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_001b:  pop
  IL_001c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableIterableSimple.GetFlagState()""
  IL_0021:  ldc.i4.s   26
  IL_0023:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0028:  pop
  IL_0029:  ret
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/386")]
        public void LegacyCollectionTest15()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void TestIBindableVectorIVectorIntMembers()
    {
        Console.WriteLine(""===  IBindableVectorIVectorIntSimple  ==="");
        var v = new IBindableVectorIVectorInt();

        //Validate IBindableVector

        //Add
        v.ClearFlag();
        v.Add((object)1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //Contains
        v.ClearFlag();
        bool b = v.Contains((object)1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //GetEnumerator
        v.ClearFlag();
        int count = ((IList)v).Count;
        IEnumerator enumerator = ((IEnumerable)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);

        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf((object)1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //Insert
        v.ClearFlag();
        v.Insert(1, (object)2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_InsertAt);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = ((IList)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        //Indexing
        v.ClearFlag();
        object val = ((IList)v)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt);
        v.ClearFlag();
        val = ((IList)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt);
        //Remove
        v.ClearFlag();
        v.Remove((object)1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //RemoveAt
        v.ClearFlag();
        ((IList)v).RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ((IList)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear);


        //Validate Vector<int>
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_Append);
        //Contains
        v.ClearFlag();
        b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        //CopyTo
        v.ClearFlag();
        arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //GetEnumerator
        v.ClearFlag();
        count = ((IList<int>)v).Count;
        IEnumerator<int> enumerator2 = ((IList<int>)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);

        //IndexOf
        v.ClearFlag();
        rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_InsertAt);
        //IsReadOnly
        v.ClearFlag();
        isReadOnly = ((IList<int>)v).IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        //Indexing
        v.ClearFlag();
        val = ((IList<int>)v)[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        v.ClearFlag();
        val = ((IList<int>)v)[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_GetAt);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IVector_IndexOf);
        //RemoveAt
        v.ClearFlag();
        ((IList<int>)v).RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        ((IList<int>)v).Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear);

    }

    static int Main()
    {
        TestIBindableVectorIVectorIntMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestIBindableVectorIVectorIntMembers",
@"
{
  // Code size      748 (0x2ec)
  .maxstack  4
  .locals init (int[] V_0) //arr
  IL_0000:  ldstr      ""===  IBindableVectorIVectorIntSimple  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IBindableVectorIVectorInt..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0015:  dup
  IL_0016:  ldc.i4.1
  IL_0017:  box        ""int""
  IL_001c:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_0021:  pop
  IL_0022:  dup
  IL_0023:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0028:  ldc.i4.s   28
  IL_002a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_002f:  pop
  IL_0030:  dup
  IL_0031:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0036:  dup
  IL_0037:  ldc.i4.1
  IL_0038:  box        ""int""
  IL_003d:  callvirt   ""bool System.Collections.IList.Contains(object)""
  IL_0042:  pop
  IL_0043:  dup
  IL_0044:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0049:  ldc.i4.s   30
  IL_004b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0050:  pop
  IL_0051:  dup
  IL_0052:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0057:  ldc.i4.s   10
  IL_0059:  newarr     ""int""
  IL_005e:  stloc.0
  IL_005f:  dup
  IL_0060:  ldloc.0
  IL_0061:  ldc.i4.0
  IL_0062:  callvirt   ""void System.Collections.Generic.ICollection<int>.CopyTo(int[], int)""
  IL_0067:  dup
  IL_0068:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_006d:  ldc.i4.s   28
  IL_006f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0074:  pop
  IL_0075:  dup
  IL_0076:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_007b:  dup
  IL_007c:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_0081:  pop
  IL_0082:  dup
  IL_0083:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0088:  pop
  IL_0089:  dup
  IL_008a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_008f:  ldc.i4.1
  IL_0090:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0095:  pop
  IL_0096:  dup
  IL_0097:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_009c:  dup
  IL_009d:  ldc.i4.1
  IL_009e:  box        ""int""
  IL_00a3:  callvirt   ""int System.Collections.IList.IndexOf(object)""
  IL_00a8:  pop
  IL_00a9:  dup
  IL_00aa:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_00af:  ldc.i4.s   30
  IL_00b1:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00b6:  pop
  IL_00b7:  dup
  IL_00b8:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_00bd:  dup
  IL_00be:  ldc.i4.1
  IL_00bf:  ldc.i4.2
  IL_00c0:  box        ""int""
  IL_00c5:  callvirt   ""void System.Collections.IList.Insert(int, object)""
  IL_00ca:  dup
  IL_00cb:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_00d0:  ldc.i4.s   32
  IL_00d2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d7:  pop
  IL_00d8:  dup
  IL_00d9:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_00de:  dup
  IL_00df:  callvirt   ""bool System.Collections.IList.IsReadOnly.get""
  IL_00e4:  pop
  IL_00e5:  dup
  IL_00e6:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_00eb:  ldc.i4.0
  IL_00ec:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00f1:  pop
  IL_00f2:  dup
  IL_00f3:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_00f8:  dup
  IL_00f9:  ldc.i4.0
  IL_00fa:  callvirt   ""object System.Collections.IList.this[int].get""
  IL_00ff:  pop
  IL_0100:  dup
  IL_0101:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0106:  ldc.i4.s   27
  IL_0108:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_010d:  pop
  IL_010e:  dup
  IL_010f:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0114:  dup
  IL_0115:  ldc.i4.1
  IL_0116:  callvirt   ""object System.Collections.IList.this[int].get""
  IL_011b:  pop
  IL_011c:  dup
  IL_011d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0122:  ldc.i4.s   27
  IL_0124:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0129:  pop
  IL_012a:  dup
  IL_012b:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0130:  dup
  IL_0131:  ldc.i4.1
  IL_0132:  box        ""int""
  IL_0137:  callvirt   ""void System.Collections.IList.Remove(object)""
  IL_013c:  dup
  IL_013d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0142:  ldc.i4.s   30
  IL_0144:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0149:  pop
  IL_014a:  dup
  IL_014b:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0150:  dup
  IL_0151:  ldc.i4.0
  IL_0152:  callvirt   ""void System.Collections.IList.RemoveAt(int)""
  IL_0157:  dup
  IL_0158:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_015d:  ldc.i4.s   33
  IL_015f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0164:  pop
  IL_0165:  dup
  IL_0166:  ldc.i4.1
  IL_0167:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_016c:  dup
  IL_016d:  ldc.i4.2
  IL_016e:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_0173:  dup
  IL_0174:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0179:  dup
  IL_017a:  callvirt   ""void System.Collections.IList.Clear()""
  IL_017f:  dup
  IL_0180:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0185:  ldc.i4.s   36
  IL_0187:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_018c:  pop
  IL_018d:  dup
  IL_018e:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0193:  dup
  IL_0194:  ldc.i4.1
  IL_0195:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_019a:  dup
  IL_019b:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_01a0:  ldc.i4.s   9
  IL_01a2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01a7:  pop
  IL_01a8:  dup
  IL_01a9:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_01ae:  dup
  IL_01af:  ldc.i4.1
  IL_01b0:  callvirt   ""bool System.Collections.Generic.ICollection<int>.Contains(int)""
  IL_01b5:  pop
  IL_01b6:  dup
  IL_01b7:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_01bc:  ldc.i4.5
  IL_01bd:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01c2:  pop
  IL_01c3:  dup
  IL_01c4:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_01c9:  ldc.i4.s   10
  IL_01cb:  newarr     ""int""
  IL_01d0:  stloc.0
  IL_01d1:  dup
  IL_01d2:  ldloc.0
  IL_01d3:  ldc.i4.0
  IL_01d4:  callvirt   ""void System.Collections.Generic.ICollection<int>.CopyTo(int[], int)""
  IL_01d9:  dup
  IL_01da:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_01df:  ldc.i4.s   28
  IL_01e1:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01e6:  pop
  IL_01e7:  dup
  IL_01e8:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_01ed:  dup
  IL_01ee:  callvirt   ""int System.Collections.Generic.ICollection<int>.Count.get""
  IL_01f3:  pop
  IL_01f4:  dup
  IL_01f5:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_01fa:  pop
  IL_01fb:  dup
  IL_01fc:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0201:  ldc.i4.1
  IL_0202:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0207:  pop
  IL_0208:  dup
  IL_0209:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_020e:  dup
  IL_020f:  ldc.i4.1
  IL_0210:  callvirt   ""int System.Collections.Generic.IList<int>.IndexOf(int)""
  IL_0215:  pop
  IL_0216:  dup
  IL_0217:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_021c:  ldc.i4.5
  IL_021d:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0222:  pop
  IL_0223:  dup
  IL_0224:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0229:  dup
  IL_022a:  ldc.i4.1
  IL_022b:  ldc.i4.2
  IL_022c:  callvirt   ""void System.Collections.Generic.IList<int>.Insert(int, int)""
  IL_0231:  dup
  IL_0232:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0237:  ldc.i4.7
  IL_0238:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_023d:  pop
  IL_023e:  dup
  IL_023f:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0244:  dup
  IL_0245:  callvirt   ""bool System.Collections.Generic.ICollection<int>.IsReadOnly.get""
  IL_024a:  pop
  IL_024b:  dup
  IL_024c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0251:  ldc.i4.0
  IL_0252:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0257:  pop
  IL_0258:  dup
  IL_0259:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_025e:  dup
  IL_025f:  ldc.i4.0
  IL_0260:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_0265:  pop
  IL_0266:  dup
  IL_0267:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_026c:  ldc.i4.2
  IL_026d:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0272:  pop
  IL_0273:  dup
  IL_0274:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0279:  dup
  IL_027a:  ldc.i4.1
  IL_027b:  callvirt   ""int System.Collections.Generic.IList<int>.this[int].get""
  IL_0280:  pop
  IL_0281:  dup
  IL_0282:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_0287:  ldc.i4.2
  IL_0288:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_028d:  pop
  IL_028e:  dup
  IL_028f:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_0294:  dup
  IL_0295:  ldc.i4.1
  IL_0296:  callvirt   ""bool System.Collections.Generic.ICollection<int>.Remove(int)""
  IL_029b:  pop
  IL_029c:  dup
  IL_029d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_02a2:  ldc.i4.5
  IL_02a3:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02a8:  pop
  IL_02a9:  dup
  IL_02aa:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_02af:  dup
  IL_02b0:  ldc.i4.0
  IL_02b1:  callvirt   ""void System.Collections.Generic.IList<int>.RemoveAt(int)""
  IL_02b6:  dup
  IL_02b7:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_02bc:  ldc.i4.s   33
  IL_02be:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02c3:  pop
  IL_02c4:  dup
  IL_02c5:  ldc.i4.1
  IL_02c6:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_02cb:  dup
  IL_02cc:  ldc.i4.2
  IL_02cd:  callvirt   ""void System.Collections.Generic.ICollection<int>.Add(int)""
  IL_02d2:  dup
  IL_02d3:  callvirt   ""void Windows.Languages.WinRTTest.IBindableVectorIVectorInt.ClearFlag()""
  IL_02d8:  dup
  IL_02d9:  callvirt   ""void System.Collections.Generic.ICollection<int>.Clear()""
  IL_02de:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableVectorIVectorInt.GetFlagState()""
  IL_02e3:  ldc.i4.s   36
  IL_02e5:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_02ea:  pop
  IL_02eb:  ret
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/386")]
        public void LegacyCollectionTest16()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void TestIBindableIterableIIterableMembers()
    {
        Console.WriteLine(""===  IBindableIterableIIterable  ==="");
        var v = new IBindableIterableIIterable();
        //Add
        v.ClearFlag();
        ((IEnumerable)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IIterable_First);
    }

    static int Main()
    {
        TestIBindableIterableIIterableMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.TestIBindableIterableIIterableMembers",
@"{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldstr      ""===  IBindableIterableIIterable  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.IBindableIterableIIterable..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.IBindableIterableIIterable.ClearFlag()""
  IL_0015:  dup
  IL_0016:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_001b:  pop
  IL_001c:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.IBindableIterableIIterable.GetFlagState()""
  IL_0021:  ldc.i4.1
  IL_0022:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0027:  pop
  IL_0028:  ret
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/386")]
        public void LegacyCollectionTest17()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void INotifyCollectionAndBindableVectorMembers()
    {
        Console.WriteLine(""===  INotifyCollectionAndBindableVectorClass  ==="");
        var v = new INotifyCollectionAndBindableVectorClass();
        //Add
        v.ClearFlag();
        v.Add(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //Contains
        v.ClearFlag();
        bool b = v.Contains(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //CopyTo
        v.ClearFlag();
        int[] arr = new int[10];
        v.CopyTo(arr, 0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_get_Size);
        //GetEnumerator
        v.ClearFlag();
        int count = v.Count;
        IEnumerator enumerator = ((IEnumerable)v).GetEnumerator();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableIterable_First);

        //IndexOf
        v.ClearFlag();
        var rez = v.IndexOf(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //Insert
        v.ClearFlag();
        v.Insert(1, 2);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_InsertAt);
        //IsReadOnly
        v.ClearFlag();
        bool isReadOnly = v.IsReadOnly;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.NotSet);
        //Indexing
        v.ClearFlag();
        object val = v[0];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt);
        v.ClearFlag();
        val = v[1];
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_GetAt);
        //Remove
        v.ClearFlag();
        v.Remove(1);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_IndexOf);
        //RemoveAt
        v.ClearFlag();
        v.RemoveAt(0);
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_RemoveAt);
        //Clear
        v.Add(1);
        v.Add(2);
        v.ClearFlag();
        v.Clear();
        ValidateMethod(v.GetFlagState(), TestMethodCalled.IBindableVector_Clear);


        //Events
        //Add event
        v.ClearFlag();
        var dele = new System.Collections.Specialized.NotifyCollectionChangedEventHandler(v_CollectionChanged);
        v.CollectionChanged += dele;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Add_CollectionChanged);

        //Remove event
        v.ClearFlag();
        v.CollectionChanged -= dele;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Remove_CollectionChanged);
    }

    static void v_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static void v_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static int Main()
    {
        INotifyCollectionAndBindableVectorMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(
                source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));
            verifier.VerifyIL("AllMembers.INotifyCollectionAndBindableVectorMembers",
@"
{
  // Code size      477 (0x1dd)
  .maxstack  4
  .locals init (int[] V_0, //arr
  System.Collections.Specialized.NotifyCollectionChangedEventHandler V_1) //dele
  IL_0000:  ldstr      ""===  INotifyCollectionAndBindableVectorClass  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0015:  dup
  IL_0016:  ldc.i4.1
  IL_0017:  box        ""int""
  IL_001c:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_0021:  pop
  IL_0022:  dup
  IL_0023:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_0028:  ldc.i4.s   28
  IL_002a:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_002f:  pop
  IL_0030:  dup
  IL_0031:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0036:  dup
  IL_0037:  ldc.i4.1
  IL_0038:  box        ""int""
  IL_003d:  callvirt   ""bool System.Collections.IList.Contains(object)""
  IL_0042:  pop
  IL_0043:  dup
  IL_0044:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_0049:  ldc.i4.s   30
  IL_004b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0050:  pop
  IL_0051:  dup
  IL_0052:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0057:  ldc.i4.s   10
  IL_0059:  newarr     ""int""
  IL_005e:  stloc.0
  IL_005f:  dup
  IL_0060:  ldloc.0
  IL_0061:  ldc.i4.0
  IL_0062:  callvirt   ""void System.Collections.ICollection.CopyTo(System.Array, int)""
  IL_0067:  dup
  IL_0068:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_006d:  ldc.i4.s   28
  IL_006f:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0074:  pop
  IL_0075:  dup
  IL_0076:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_007b:  dup
  IL_007c:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_0081:  pop
  IL_0082:  dup
  IL_0083:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0088:  pop
  IL_0089:  dup
  IL_008a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_008f:  ldc.i4.s   26
  IL_0091:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0096:  pop
  IL_0097:  dup
  IL_0098:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_009d:  dup
  IL_009e:  ldc.i4.1
  IL_009f:  box        ""int""
  IL_00a4:  callvirt   ""int System.Collections.IList.IndexOf(object)""
  IL_00a9:  pop
  IL_00aa:  dup
  IL_00ab:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_00b0:  ldc.i4.s   30
  IL_00b2:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00b7:  pop
  IL_00b8:  dup
  IL_00b9:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_00be:  dup
  IL_00bf:  ldc.i4.1
  IL_00c0:  ldc.i4.2
  IL_00c1:  box        ""int""
  IL_00c6:  callvirt   ""void System.Collections.IList.Insert(int, object)""
  IL_00cb:  dup
  IL_00cc:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_00d1:  ldc.i4.s   32
  IL_00d3:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00d8:  pop
  IL_00d9:  dup
  IL_00da:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_00df:  dup
  IL_00e0:  callvirt   ""bool System.Collections.IList.IsReadOnly.get""
  IL_00e5:  pop
  IL_00e6:  dup
  IL_00e7:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_00ec:  ldc.i4.0
  IL_00ed:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_00f2:  pop
  IL_00f3:  dup
  IL_00f4:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_00f9:  dup
  IL_00fa:  ldc.i4.0
  IL_00fb:  callvirt   ""object System.Collections.IList.this[int].get""
  IL_0100:  pop
  IL_0101:  dup
  IL_0102:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_0107:  ldc.i4.s   27
  IL_0109:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_010e:  pop
  IL_010f:  dup
  IL_0110:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0115:  dup
  IL_0116:  ldc.i4.1
  IL_0117:  callvirt   ""object System.Collections.IList.this[int].get""
  IL_011c:  pop
  IL_011d:  dup
  IL_011e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_0123:  ldc.i4.s   27
  IL_0125:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_012a:  pop
  IL_012b:  dup
  IL_012c:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0131:  dup
  IL_0132:  ldc.i4.1
  IL_0133:  box        ""int""
  IL_0138:  callvirt   ""void System.Collections.IList.Remove(object)""
  IL_013d:  dup
  IL_013e:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_0143:  ldc.i4.s   30
  IL_0145:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_014a:  pop
  IL_014b:  dup
  IL_014c:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0151:  dup
  IL_0152:  ldc.i4.0
  IL_0153:  callvirt   ""void System.Collections.IList.RemoveAt(int)""
  IL_0158:  dup
  IL_0159:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_015e:  ldc.i4.s   33
  IL_0160:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0165:  pop
  IL_0166:  dup
  IL_0167:  ldc.i4.1
  IL_0168:  box        ""int""
  IL_016d:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_0172:  pop
  IL_0173:  dup
  IL_0174:  ldc.i4.2
  IL_0175:  box        ""int""
  IL_017a:  callvirt   ""int System.Collections.IList.Add(object)""
  IL_017f:  pop
  IL_0180:  dup
  IL_0181:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_0186:  dup
  IL_0187:  callvirt   ""void System.Collections.IList.Clear()""
  IL_018c:  dup
  IL_018d:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_0192:  ldc.i4.s   36
  IL_0194:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0199:  pop
  IL_019a:  dup
  IL_019b:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_01a0:  ldnull
  IL_01a1:  ldftn      ""void AllMembers.v_CollectionChanged(object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)""
  IL_01a7:  newobj     ""System.Collections.Specialized.NotifyCollectionChangedEventHandler..ctor(object, System.IntPtr)""
  IL_01ac:  stloc.1
  IL_01ad:  dup
  IL_01ae:  ldloc.1
  IL_01af:  callvirt   ""void System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged.add""
  IL_01b4:  dup
  IL_01b5:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_01ba:  ldc.i4.s   37
  IL_01bc:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01c1:  pop
  IL_01c2:  dup
  IL_01c3:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.ClearFlag()""
  IL_01c8:  dup
  IL_01c9:  ldloc.1
  IL_01ca:  callvirt   ""void System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged.remove""
  IL_01cf:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionAndBindableVectorClass.GetFlagState()""
  IL_01d4:  ldc.i4.s   38
  IL_01d6:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_01db:  pop
  IL_01dc:  ret
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/386")]
        public void LegacyCollectionTest18()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void INotifyCollectionChangedMembers()
    {
        Console.WriteLine(""===  INotifyCollectionChangedClass  ==="");
        var v = new INotifyCollectionChangedClass();

        //Events
        //Add event
        v.ClearFlag();
        var dele = new System.Collections.Specialized.NotifyCollectionChangedEventHandler(v_CollectionChanged);
        v.CollectionChanged += dele;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Add_CollectionChanged);

        //Remove event
        v.ClearFlag();
        v.CollectionChanged -= dele;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Remove_CollectionChanged);
    }

    static void v_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static void v_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static int Main()
    {
        INotifyCollectionChangedMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(
                source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"),
                // (7,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"));
            verifier.VerifyIL("AllMembers.INotifyCollectionChangedMembers",
@"
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (System.Collections.Specialized.NotifyCollectionChangedEventHandler V_0) //dele
  IL_0000:  ldstr      ""===  INotifyCollectionChangedClass  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.INotifyCollectionChangedClass..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionChangedClass.ClearFlag()""
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void AllMembers.v_CollectionChanged(object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)""
  IL_001c:  newobj     ""System.Collections.Specialized.NotifyCollectionChangedEventHandler..ctor(object, System.IntPtr)""
  IL_0021:  stloc.0
  IL_0022:  dup
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged.add""
  IL_0029:  dup
  IL_002a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionChangedClass.GetFlagState()""
  IL_002f:  ldc.i4.s   37
  IL_0031:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0036:  pop
  IL_0037:  dup
  IL_0038:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionChangedClass.ClearFlag()""
  IL_003d:  dup
  IL_003e:  ldloc.0
  IL_003f:  callvirt   ""void System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged.remove""
  IL_0044:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionChangedClass.GetFlagState()""
  IL_0049:  ldc.i4.s   38
  IL_004b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0050:  pop
  IL_0051:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest19()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void INotifyCollectionChangedMembers()
    {
        Console.WriteLine(""===  INotifyCollectionChangedClass  ==="");
        var v = new INotifyCollectionChangedClass();

        //Events
        //Add event
        v.ClearFlag();
        var dele = new System.Collections.Specialized.NotifyCollectionChangedEventHandler(v_CollectionChanged);
        v.CollectionChanged += dele;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Add_CollectionChanged);

        //Remove event
        v.ClearFlag();
        v.CollectionChanged -= dele;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyCollectionChanged_Remove_CollectionChanged);
    }

    static void v_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static void v_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static int Main()
    {
        INotifyCollectionChangedMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(
                source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"),
                // (7,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"));
            verifier.VerifyIL("AllMembers.INotifyCollectionChangedMembers",
@"
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (System.Collections.Specialized.NotifyCollectionChangedEventHandler V_0) //dele
  IL_0000:  ldstr      ""===  INotifyCollectionChangedClass  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.INotifyCollectionChangedClass..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionChangedClass.ClearFlag()""
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void AllMembers.v_CollectionChanged(object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)""
  IL_001c:  newobj     ""System.Collections.Specialized.NotifyCollectionChangedEventHandler..ctor(object, System.IntPtr)""
  IL_0021:  stloc.0
  IL_0022:  dup
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged.add""
  IL_0029:  dup
  IL_002a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionChangedClass.GetFlagState()""
  IL_002f:  ldc.i4.s   37
  IL_0031:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0036:  pop
  IL_0037:  dup
  IL_0038:  callvirt   ""void Windows.Languages.WinRTTest.INotifyCollectionChangedClass.ClearFlag()""
  IL_003d:  dup
  IL_003e:  ldloc.0
  IL_003f:  callvirt   ""void System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged.remove""
  IL_0044:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyCollectionChangedClass.GetFlagState()""
  IL_0049:  ldc.i4.s   38
  IL_004b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0050:  pop
  IL_0051:  ret
}");
        }

        [Fact]
        public void LegacyCollectionTest20()
        {
            var source =
@"using Windows.Languages.WinRTTest;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections;

class AllMembers
{
    private static int FailedCount = 0;
    private static bool ValidateMethod(TestMethodCalled actual, TestMethodCalled expected)
    {
        var temp = Console.ForegroundColor;
        if (actual != expected)
        {
            FailedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(""FAIL:  "");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(""PASS:  "");
        }

        Console.ForegroundColor = temp;
        Console.WriteLine(""Expected: {0}, Actual: {1}"", expected, actual);
        return actual == expected;
    }

    static void IPropertyChangedMembers()
    {
        Console.WriteLine(""===  INotifyCollectionChangedClass  ==="");
        var v = new INotifyPropertyChangedClass();

        // PropertyChanged
        //Add
        v.ClearFlag();
        var pdeleg = new System.ComponentModel.PropertyChangedEventHandler(v_PropertyChanged);
        v.PropertyChanged += pdeleg;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyPropertyChanged_Add_PropertyChanged);
        //Remove
        v.ClearFlag();
        v.PropertyChanged -= pdeleg;
        ValidateMethod(v.GetFlagState(), TestMethodCalled.INotifyPropertyChanged_Remove_PropertyChanged);
    }

    static void v_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    static int Main()
    {
        IPropertyChangedMembers();

        Console.WriteLine(FailedCount);
        return FailedCount;
    }
}";
            var verifier = CompileAndVerifyWithWinRt(
                source,
                references: LegacyRefs,
                verify: Verification.Fails);
            verifier.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Reflection;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Reflection;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq.Expressions;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq.Expressions;"),
                // (6,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"),
                // (7,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"));
            verifier.VerifyIL("AllMembers.IPropertyChangedMembers",
@"
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (System.ComponentModel.PropertyChangedEventHandler V_0) //pdeleg
  IL_0000:  ldstr      ""===  INotifyCollectionChangedClass  ===""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""Windows.Languages.WinRTTest.INotifyPropertyChangedClass..ctor()""
  IL_000f:  dup
  IL_0010:  callvirt   ""void Windows.Languages.WinRTTest.INotifyPropertyChangedClass.ClearFlag()""
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void AllMembers.v_PropertyChanged(object, System.ComponentModel.PropertyChangedEventArgs)""
  IL_001c:  newobj     ""System.ComponentModel.PropertyChangedEventHandler..ctor(object, System.IntPtr)""
  IL_0021:  stloc.0
  IL_0022:  dup
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.ComponentModel.INotifyPropertyChanged.PropertyChanged.add""
  IL_0029:  dup
  IL_002a:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyPropertyChangedClass.GetFlagState()""
  IL_002f:  ldc.i4.s   39
  IL_0031:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0036:  pop
  IL_0037:  dup
  IL_0038:  callvirt   ""void Windows.Languages.WinRTTest.INotifyPropertyChangedClass.ClearFlag()""
  IL_003d:  dup
  IL_003e:  ldloc.0
  IL_003f:  callvirt   ""void System.ComponentModel.INotifyPropertyChanged.PropertyChanged.remove""
  IL_0044:  callvirt   ""Windows.Languages.WinRTTest.TestMethodCalled Windows.Languages.WinRTTest.INotifyPropertyChangedClass.GetFlagState()""
  IL_0049:  ldc.i4.s   40
  IL_004b:  call       ""bool AllMembers.ValidateMethod(Windows.Languages.WinRTTest.TestMethodCalled, Windows.Languages.WinRTTest.TestMethodCalled)""
  IL_0050:  pop
  IL_0051:  ret
}
");
        }

        [Fact]
        public void WinRTCompilationReference()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;

namespace Test
{
    public class C : IEnumerable<int>
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return null;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return null;
        }   
    }
}";
            var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.C.GetEnumerator()",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");

            var compRef = verifier.Compilation.ToMetadataReference();
            source =
@"using System;
using Test;

namespace Test2
{
    public class D
    {
        public static void Main(string[] args)
        {
            var c = new C();
            var e = c.GetEnumerator();
        }
    }
}";
            verifier = CompileAndVerifyWithWinRt(source,
                references: new[] { compRef });
            verifier.VerifyDiagnostics(
                // (1,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));
            verifier.VerifyIL("Test2.D.Main",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  newobj     ""Test.C..ctor()""
  IL_0005:  callvirt   ""System.Collections.Generic.IEnumerator<int> Test.C.GetEnumerator()""
  IL_000a:  pop
  IL_000b:  ret
}");
        }

        [Fact, WorkItem(1034461, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034461")]
        public void Bug1034461()
        {
            var source = @"
using Windows.Data.Json;

public class Class1
{
    void Test()
    {
        var jsonObj = new JsonObject();
        jsonObj.Add(""firstEntry"", null);
    }
}
";
            var comp = CreateEmptyCompilation(source, references: WinRtRefs);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var add = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "Add").Single();
            var addMethod = model.GetSymbolInfo(add).Symbol;
            Assert.Equal("void System.Collections.Generic.IDictionary<System.String, Windows.Data.Json.IJsonValue>.Add(System.String key, Windows.Data.Json.IJsonValue value)", addMethod.ToTestDisplayString());

            var jsonObj = ((MemberAccessExpressionSyntax)add.Parent).Expression;

            var jsonObjType = model.GetTypeInfo(jsonObj).Type;
            Assert.Equal("Windows.Data.Json.JsonObject", jsonObjType.ToTestDisplayString());

            Assert.True(model.LookupNames(add.SpanStart, jsonObjType).Contains("Add"));
            Assert.True(model.LookupSymbols(add.SpanStart, jsonObjType, "Add").Contains(addMethod));
            Assert.True(model.LookupSymbols(add.SpanStart, jsonObjType).Contains(addMethod));
        }
    }
}
