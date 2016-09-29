// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests
{
    public class DynamicInstrumentationTests : CSharpTestBase
    {
        const string InstrumentationHelperSource = @"
namespace Microsoft.CodeAnalysis.Runtime
{
    public static class Instrumentation
    {
        private static bool[][] _payloads;
        private static int[] _fileIndices;
        private static System.Guid _mvid;

        public static bool[] CreatePayload(System.Guid mvid, int methodIndex, int fileIndex, ref bool[] payload, int payloadLength)
        {
            if (_mvid != mvid)
            {
                _payloads = new bool[100][];
                _fileIndices = new int[100];
                _mvid = mvid;
            }

            if (System.Threading.Interlocked.CompareExchange(ref payload, new bool[payloadLength], null) == null)
            {
                _payloads[methodIndex] = payload;
                _fileIndices[methodIndex] = fileIndex;
                return payload;
            }

            return _payloads[methodIndex];
        }

        public static void FlushPayload()
        {
            Console.WriteLine(""Flushing"");
            if (_payloads == null)
            {
                return;
            }
            for (int i = 0; i < _payloads.Length; i++)
            {
                bool[] payload = _payloads[i];
                if (payload != null)
                {
                    Console.WriteLine(""Method "" + i.ToString());
                    Console.WriteLine(""File "" + _fileIndices[i].ToString());
                    for (int j = 0; j < payload.Length; j++)
                    {
                        Console.WriteLine(payload[j]);
                        payload[j] = false;
                    }
                }
            }
        }
    }
}
";

        [Fact]
        public void HelpersInstrumentation()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
}
";
            string expectedOutput = @"Flushing
Method 1
File 1
True
True
Method 4
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";
            string expectedCreatePayloadIL = @"{
  // Code size       87 (0x57)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Guid Microsoft.CodeAnalysis.Runtime.Instrumentation._mvid""
  IL_0005:  ldarg.0
  IL_0006:  call       ""bool System.Guid.op_Inequality(System.Guid, System.Guid)""
  IL_000b:  brfalse.s  IL_002b
  IL_000d:  ldc.i4.s   100
  IL_000f:  newarr     ""bool[]""
  IL_0014:  stsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0019:  ldc.i4.s   100
  IL_001b:  newarr     ""int""
  IL_0020:  stsfld     ""int[] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
  IL_0025:  ldarg.0
  IL_0026:  stsfld     ""System.Guid Microsoft.CodeAnalysis.Runtime.Instrumentation._mvid""
  IL_002b:  ldarg.3
  IL_002c:  ldarg.s    V_4
  IL_002e:  newarr     ""bool""
  IL_0033:  ldnull
  IL_0034:  call       ""bool[] System.Threading.Interlocked.CompareExchange<bool[]>(ref bool[], bool[], bool[])""
  IL_0039:  brtrue.s   IL_004f
  IL_003b:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0040:  ldarg.1
  IL_0041:  ldarg.3
  IL_0042:  ldind.ref
  IL_0043:  stelem.ref
  IL_0044:  ldsfld     ""int[] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
  IL_0049:  ldarg.1
  IL_004a:  ldarg.2
  IL_004b:  stelem.i4
  IL_004c:  ldarg.3
  IL_004d:  ldind.ref
  IL_004e:  ret
  IL_004f:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0054:  ldarg.1
  IL_0055:  ldelem.ref
  IL_0056:  ret
}";

            string expectedFlushPayloadIL = @"{
  // Code size      247 (0xf7)
  .maxstack  5
  .locals init (bool[] V_0,
                int V_1, //i
                bool[] V_2, //payload
                int V_3) //j
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0035
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0023:  ldtoken    ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
  IL_0028:  ldelema    ""bool[]""
  IL_002d:  ldc.i4.s   14
  IL_002f:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
  IL_0034:  stloc.0
  IL_0035:  ldloc.0
  IL_0036:  ldc.i4.0
  IL_0037:  ldc.i4.1
  IL_0038:  stelem.i1
  IL_0039:  ldloc.0
  IL_003a:  ldc.i4.1
  IL_003b:  ldc.i4.1
  IL_003c:  stelem.i1
  IL_003d:  ldstr      ""Flushing""
  IL_0042:  call       ""void System.Console.WriteLine(string)""
  IL_0047:  ldloc.0
  IL_0048:  ldc.i4.3
  IL_0049:  ldc.i4.1
  IL_004a:  stelem.i1
  IL_004b:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0050:  brtrue.s   IL_0057
  IL_0052:  ldloc.0
  IL_0053:  ldc.i4.2
  IL_0054:  ldc.i4.1
  IL_0055:  stelem.i1
  IL_0056:  ret
  IL_0057:  ldloc.0
  IL_0058:  ldc.i4.4
  IL_0059:  ldc.i4.1
  IL_005a:  stelem.i1
  IL_005b:  ldc.i4.0
  IL_005c:  stloc.1
  IL_005d:  br         IL_00e9
  IL_0062:  ldloc.0
  IL_0063:  ldc.i4.6
  IL_0064:  ldc.i4.1
  IL_0065:  stelem.i1
  IL_0066:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_006b:  ldloc.1
  IL_006c:  ldelem.ref
  IL_006d:  stloc.2
  IL_006e:  ldloc.0
  IL_006f:  ldc.i4.s   13
  IL_0071:  ldc.i4.1
  IL_0072:  stelem.i1
  IL_0073:  ldloc.2
  IL_0074:  brfalse.s  IL_00e1
  IL_0076:  ldloc.0
  IL_0077:  ldc.i4.7
  IL_0078:  ldc.i4.1
  IL_0079:  stelem.i1
  IL_007a:  ldstr      ""Method ""
  IL_007f:  ldloca.s   V_1
  IL_0081:  call       ""string int.ToString()""
  IL_0086:  call       ""string string.Concat(string, string)""
  IL_008b:  call       ""void System.Console.WriteLine(string)""
  IL_0090:  ldloc.0
  IL_0091:  ldc.i4.8
  IL_0092:  ldc.i4.1
  IL_0093:  stelem.i1
  IL_0094:  ldstr      ""File ""
  IL_0099:  ldsfld     ""int[] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
  IL_009e:  ldloc.1
  IL_009f:  ldelema    ""int""
  IL_00a4:  call       ""string int.ToString()""
  IL_00a9:  call       ""string string.Concat(string, string)""
  IL_00ae:  call       ""void System.Console.WriteLine(string)""
  IL_00b3:  ldloc.0
  IL_00b4:  ldc.i4.s   9
  IL_00b6:  ldc.i4.1
  IL_00b7:  stelem.i1
  IL_00b8:  ldc.i4.0
  IL_00b9:  stloc.3
  IL_00ba:  br.s       IL_00db
  IL_00bc:  ldloc.0
  IL_00bd:  ldc.i4.s   11
  IL_00bf:  ldc.i4.1
  IL_00c0:  stelem.i1
  IL_00c1:  ldloc.2
  IL_00c2:  ldloc.3
  IL_00c3:  ldelem.u1
  IL_00c4:  call       ""void System.Console.WriteLine(bool)""
  IL_00c9:  ldloc.0
  IL_00ca:  ldc.i4.s   12
  IL_00cc:  ldc.i4.1
  IL_00cd:  stelem.i1
  IL_00ce:  ldloc.2
  IL_00cf:  ldloc.3
  IL_00d0:  ldc.i4.0
  IL_00d1:  stelem.i1
  IL_00d2:  ldloc.0
  IL_00d3:  ldc.i4.s   10
  IL_00d5:  ldc.i4.1
  IL_00d6:  stelem.i1
  IL_00d7:  ldloc.3
  IL_00d8:  ldc.i4.1
  IL_00d9:  add
  IL_00da:  stloc.3
  IL_00db:  ldloc.3
  IL_00dc:  ldloc.2
  IL_00dd:  ldlen
  IL_00de:  conv.i4
  IL_00df:  blt.s      IL_00bc
  IL_00e1:  ldloc.0
  IL_00e2:  ldc.i4.5
  IL_00e3:  ldc.i4.1
  IL_00e4:  stelem.i1
  IL_00e5:  ldloc.1
  IL_00e6:  ldc.i4.1
  IL_00e7:  add
  IL_00e8:  stloc.1
  IL_00e9:  ldloc.1
  IL_00ea:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_00ef:  ldlen
  IL_00f0:  conv.i4
  IL_00f1:  blt        IL_0062
  IL_00f6:  ret
}";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyIL("Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload", expectedCreatePayloadIL);
            verifier.VerifyIL("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload", expectedFlushPayloadIL);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void GotoCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()
    {
        Console.WriteLine(""foo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
        Fred();
        return;
    }

    static void Wilma()
    {
        Betty(true);
        Barney(true);
        Barney(false);
        Betty(true);
    }

    static int Barney(bool b)
    {
        if (b)
            return 10;
        if (b)
            return 100;
        return 20;
    }

    static int Betty(bool b)
    {
        if (b)
            return 30;
        if (b)
            return 100;
        return 40;
    }

    static void Fred()
    {
        Wilma();
    }
}
";
            string expectedOutput = @"foo
bar
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
False
True
True
True
Method 3
File 1
True
True
True
True
True
Method 4
File 1
True
True
True
False
True
True
Method 5
File 1
True
True
True
False
False
False
Method 6
File 1
True
True
Method 9
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            string expectedBarneyIL = @"{
  // Code size       91 (0x5b)
  .maxstack  5
  .locals init (bool[] V_0)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""int Program.Barney(bool)""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0034
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""int Program.Barney(bool)""
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0023:  ldtoken    ""int Program.Barney(bool)""
  IL_0028:  ldelema    ""bool[]""
  IL_002d:  ldc.i4.6
  IL_002e:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.2
  IL_003a:  ldc.i4.1
  IL_003b:  stelem.i1
  IL_003c:  ldarg.0
  IL_003d:  brfalse.s  IL_0046
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.1
  IL_0041:  ldc.i4.1
  IL_0042:  stelem.i1
  IL_0043:  ldc.i4.s   10
  IL_0045:  ret
  IL_0046:  ldloc.0
  IL_0047:  ldc.i4.4
  IL_0048:  ldc.i4.1
  IL_0049:  stelem.i1
  IL_004a:  ldarg.0
  IL_004b:  brfalse.s  IL_0054
  IL_004d:  ldloc.0
  IL_004e:  ldc.i4.3
  IL_004f:  ldc.i4.1
  IL_0050:  stelem.i1
  IL_0051:  ldc.i4.s   100
  IL_0053:  ret
  IL_0054:  ldloc.0
  IL_0055:  ldc.i4.5
  IL_0056:  ldc.i4.1
  IL_0057:  stelem.i1
  IL_0058:  ldc.i4.s   20
  IL_005a:  ret
}";

            string expectedPIDStaticConstructorIL = @"{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldtoken    Max Method Token Index
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  newarr     ""bool[]""
  IL_000c:  stsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0011:  ldstr      ##MVID##
  IL_0016:  call       ""System.Guid System.Guid.Parse(string)""
  IL_001b:  stsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0020:  ret
}";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.Barney", expectedBarneyIL);
            verifier.VerifyIL(".cctor", expectedPIDStaticConstructorIL);
            verifier.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(16, 9));
        }

        [Fact]
        public void MethodsOfGenericTypesCoverage()
        {
            string source = @"
using System;

class MyBox<T> where T : class
{
    readonly T _value;

    public MyBox(T value)
    {
        _value = value;
    }

    public T GetValue()
    {
        if (_value == null)
        {
            return null;
        }

        return _value;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()
    {
        MyBox<object> x = new MyBox<object>(null);
        Console.WriteLine(x.GetValue() == null ? ""null"" : x.GetValue().ToString());
        MyBox<string> s = new MyBox<string>(""Hello"");
        Console.WriteLine(s.GetValue() == null ? ""null"" : s.GetValue());
    }
}
";
            // All instrumentation points in method 2 are True because they are covered by at least one specialization.
            //
            // This test verifies that the payloads of methods of generic types are in terms of method definitions and
            // not method references -- the indices for the methods would be different for references.
            string expectedOutput = @"null
Hello
Flushing
Method 1
File 1
True
True
Method 2
File 1
True
True
True
True
Method 3
File 1
True
True
True
Method 4
File 1
True
True
True
True
True
Method 7
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            string expectedReleaseGetValueIL = @"{
  // Code size       98 (0x62)
  .maxstack  5
  .locals init (bool[] V_0,
                T V_1)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0034
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0023:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_0028:  ldelema    ""bool[]""
  IL_002d:  ldc.i4.4
  IL_002e:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.2
  IL_003a:  ldc.i4.1
  IL_003b:  stelem.i1
  IL_003c:  ldarg.0
  IL_003d:  ldfld      ""T MyBox<T>._value""
  IL_0042:  box        ""T""
  IL_0047:  brtrue.s   IL_0057
  IL_0049:  ldloc.0
  IL_004a:  ldc.i4.1
  IL_004b:  ldc.i4.1
  IL_004c:  stelem.i1
  IL_004d:  ldloca.s   V_1
  IL_004f:  initobj    ""T""
  IL_0055:  ldloc.1
  IL_0056:  ret
  IL_0057:  ldloc.0
  IL_0058:  ldc.i4.3
  IL_0059:  ldc.i4.1
  IL_005a:  stelem.i1
  IL_005b:  ldarg.0
  IL_005c:  ldfld      ""T MyBox<T>._value""
  IL_0061:  ret
}";

            string expectedDebugGetValueIL = @"{
  // Code size      110 (0x6e)
  .maxstack  5
  .locals init (bool[] V_0,
                bool V_1,
                T V_2,
                T V_3)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0034
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0023:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_0028:  ldelema    ""bool[]""
  IL_002d:  ldc.i4.4
  IL_002e:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.2
  IL_003a:  ldc.i4.1
  IL_003b:  stelem.i1
  IL_003c:  ldarg.0
  IL_003d:  ldfld      ""T MyBox<T>._value""
  IL_0042:  box        ""T""
  IL_0047:  ldnull
  IL_0048:  ceq
  IL_004a:  stloc.1
  IL_004b:  ldloc.1
  IL_004c:  brfalse.s  IL_005f
  IL_004e:  nop
  IL_004f:  ldloc.0
  IL_0050:  ldc.i4.1
  IL_0051:  ldc.i4.1
  IL_0052:  stelem.i1
  IL_0053:  ldloca.s   V_2
  IL_0055:  initobj    ""T""
  IL_005b:  ldloc.2
  IL_005c:  stloc.3
  IL_005d:  br.s       IL_006c
  IL_005f:  ldloc.0
  IL_0060:  ldc.i4.3
  IL_0061:  ldc.i4.1
  IL_0062:  stelem.i1
  IL_0063:  ldarg.0
  IL_0064:  ldfld      ""T MyBox<T>._value""
  IL_0069:  stloc.3
  IL_006a:  br.s       IL_006c
  IL_006c:  ldloc.3
  IL_006d:  ret
}";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyIL("MyBox<T>.GetValue", expectedReleaseGetValueIL);
            verifier.VerifyDiagnostics();

            verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyIL("MyBox<T>.GetValue", expectedDebugGetValueIL);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void NonStaticImplicitBlockMethodsCoverage()
        {
            string source = @"
using System;

public class Program
{
    public int Prop { get; }

    public int Prop2 { get; } = 25;

    public int Prop3 { get; set; }                                              // Methods 3 and 4

    public Program()                                                            // Method 5
    {
        Prop = 12;
        Prop3 = 12;
        Prop2 = Prop3;
    }

    public static void Main(string[] args)                                      // Method 6
    {
        new Program();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
}
";
            string expectedOutput = @"Flushing
Method 3
File 1
True
True
Method 4
File 1
True
True
Method 5
File 1
True
True
True
True
True
Method 6
File 1
True
True
True
Method 8
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitBlockMethodsCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
    
    static void TestMain()
    {
        int x = Count;
        x += Prop;
        Prop = x;
        x += Prop2;
        Lambda(x, (y) => y + 1);
    }

    static int Function(int x) => x;

    static int Count => Function(44);

    static int Prop { get; set; }

    static int Prop2 { get; set; } = 12;

    static int Lambda(int x, Func<int, int> l)
    {
        return l(x);
    }

    // Method 11 is a synthesized static constructor.
}
";
            // There is no entry for method '8' since it's a Prop2_set which is never called.
            string expectedOutput = @"Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
True
True
Method 3
File 1
True
True
Method 4
File 1
True
True
Method 5
File 1
True
True
Method 6
File 1
True
True
Method 7
File 1
True
True
Method 9
File 1
True
True
Method 11
File 1
True
Method 13
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void MultipleFilesCoverage()
        {
            string source = @"
using System;

public class Program
{
#line 10 ""File1.cs""
    public static void Main(string[] args)                                  // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
    
#line 20 ""File2.cs""
    static void TestMain()                                                  // Method 2
    {
        Fred();
        Program p = new Program();
        return;
    }

#line 30 ""File3.cs""
    static void Fred()                                                      // Method 3
    {
        return;
    }

#line 40 ""File5.cs""

    // The synthesized instance constructor is method 4 and
    // appears in the original source file, which gets file index 4.
}
";

            string expectedOutput = @"Flushing
Method 1
File 1
True
True
True
Method 2
File 2
True
True
True
True
Method 3
File 3
True
True
Method 4
File 4
Method 6
File 5
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void MultipleDeclarationsCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)                                      // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                                      // Method 2
    {
        int x;
        int a, b;
        DoubleDeclaration(5);
        DoubleForDeclaration(5);
    }

    static int DoubleDeclaration(int x)                                         // Method 3
    {
        int c = x;
        int a, b;
        int f;

        a = b = f = c;
        int d = a, e = b;
        return d + e + f;
    }

    static int DoubleForDeclaration(int x)                                      // Method 4
    {
        for(int a = x, b = x; a + b < 10; a++)
        {
            Console.WriteLine(""Cannot get here."");
            x++;
        }

        return x;
    }
}
";
            string expectedOutput = @"Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
True
True
True
True
True
Method 4
File 1
True
True
True
False
False
False
True
Method 7
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(14, 13),
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(15, 13),
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "b").WithArguments("b").WithLocation(15, 16));
        }

        [Fact]
        public void UsingAndFixedCoverage()
        {
            string source = @"
using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)                                          // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                                          // Method 2
    {
        using (var memoryStream = new MemoryStream())
        {
            ;
        }

        using (MemoryStream s1 = new MemoryStream(), s2 = new MemoryStream())
        {
            ;
        }

        var otherStream = new MemoryStream();
        using (otherStream)
        {
            ;
        }

        unsafe
        {
            double[] a = { 1, 2, 3 };
            fixed(double* p = a)
            {
                ;
            }
            fixed(double* q = a, r = a)
            {
                ;
            }
        }
    }
}
";
            string expectedOutput = @"Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
True
True
True
True
True
True
True
True
True
True
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";
           
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.UnsafeDebugExe, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ManyStatementsCoverage()                                    // Method 3
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()
    {
        VariousStatements(2);
        Empty();
    }

    static void VariousStatements(int z)
    {
        int x = z + 10;
        switch (z)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            default:
                break;
        }

        if (x > 10)
        {
            x++;
        }
        else
        {
            x--;
        }

        for (int y = 0; y < 50; y++)
        {
            if (y < 30)
            {
                x++;
                continue;
            }
            else
                break;
        }

        int[] a = new int[] { 1, 2, 3, 4 };
        foreach (int i in a)
        {
            x++;
        }

        while (x < 100)
        {
            x++;
        }

        try
        {
            x++;
            if (x > 10)
            {
                throw new System.Exception();
            }
            x++;
        }
        catch (System.Exception)
        {
            x++;
        }
        finally
        {
            x++;
        }

        lock (new object())
        {
            ;
        }

        Console.WriteLine(x);

        try
        {
            using ((System.IDisposable)new object())
            {
                ;
            }
        }
        catch (System.Exception)
        {
        }

        // Include an infinite loop to make sure that a compiler optimization doesn't eliminate the instrumentation.
        while (true)
        {
            return;
        }
    }

    static void Empty()                                 // Method 4
    {
    }
}
";
            string expectedOutput = @"103
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
True
False
True
False
False
True
True
False
True
True
True
True
True
True
True
True
True
True
True
True
True
True
True
False
True
True
True
True
True
False
True
True
True
Method 4
File 1
True
Method 7
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void PatternsCoverage()
        {
            string source = @"
using System;

public class C
{
    public static void Main()
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                 // Method 2
    {
        Student s = new Student();
        s.Name = ""Bozo"";
        s.GPA = 2.3;
        Operate(s);
    }
     
    static string Operate(Person p)                         // Method 3
    {
        switch (p)
        {
            case Student s when s.GPA > 3.5:
                return $""Student {s.Name} ({s.GPA:N1})"";
            case Student s:
                return $""Student {s.Name} ({s.GPA:N1})"";
            case Teacher t:
                return $""Teacher {t.Name} of {t.Subject}"";
            default:
                return $""Person {p.Name}"";
        }
    }
}

class Person { public string Name; }
class Teacher : Person { public string Subject; }
class Student : Person { public double GPA; }

    // Methods 5 and 7 are implicit constructors.
";
            string expectedOutput = @"Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
True
Method 3
File 1
True
False
True
False
False
True
Method 5
File 1
Method 7
File 1
Method 9
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Subject").WithArguments("Teacher.Subject", "null").WithLocation(37, 40));
        }

        [Fact]
        public void LambdaCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)                                  // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();      // Method 2
    }

    static void TestMain()
    {
        int y = 5;
        Func<int, int> tester = (x) =>
        {
            while (x > 10)
            {
                return y;
            }

            return x;
        };

        y = 75;
        if (tester(20) > 50)
            Console.WriteLine(""OK"");
        else
            Console.WriteLine(""Bad"");
    }
}
";
            string expectedOutput = @"OK
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
False
True
True
True
False
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void AsyncCoverage()
        {
            string source = @"
using System;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()
    {
        Console.WriteLine(Outer(""Goo"").Result);
    }

    async static Task<string> Outer(string s)
    {
        string s1 = await First(s);
        string s2 = await Second(s);

        return s1 + s2;
    }

    async static Task<string> First(string s)
    {
        string result = await Second(s) + ""Glue"";
        if (result.Length > 2)
            return result;
        else
            return ""Too short"";
    }

    async static Task<string> Second(string s)
    {
        string doubled = """";
        if (s.Length > 2)
            doubled = s + s;
        else
            doubled = ""HuhHuh"";
        return await Task.Factory.StartNew(() => doubled);
    }
}
";
            string expectedOutput = @"GooGooGlueGooGoo
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
Method 3
File 1
True
True
True
True
Method 4
File 1
True
True
True
False
True
Method 5
File 1
True
True
True
False
True
True
Method 8
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestFieldInitializerCoverage()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                      // Method 2
    {
        C local = new C(); local = new C(1, s_z);
    }

    static int Init() => 33;                                    // Method 3

    C()                                                         // Method 4
    {
        _z = 12;
    }

    static C()                                                  // Method 5
    {
        s_z = 123;
    }

    int _x = Init();
    int _y = Init() + 12;
    int _z;
    static int s_x = Init();
    static int s_y = Init() + 153;
    static int s_z;

    C(int x)                                                    // Method 6
    {
        _z = x;
    }

    C(int a, int b)                                             // Method 7
    {
        _z = a + b;
    }

    int Prop1 { get; } = 15;
    static int Prop2 { get; } = 255;
}
";
            string expectedOutput = @"
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
True
Method 4
File 1
True
True
True
True
True
Method 5
File 1
True
True
True
True
True
Method 7
File 1
True
True
True
True
True
Method 11
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestImplicitConstructorCoverage()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                      // Method 2
    {
        C local = new C();
        int x = local._x + local._y + C.s_x + C.s_y + C.s_z;
    }

    static int Init() => 33;                                    // Method 3

    // Method 6 is the implicit instance constructor.
    // Method 7 is the implicit shared constructor.

    int _x = Init();
    int _y = Init() + 12;
    static int s_x = Init();
    static int s_y = Init() + 153;
    static int s_z = 144;

    int Prop1 { get; } = 15;
    static int Prop2 { get; } = 255;
}
";
            string expectedOutput = @"
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
True
Method 6
File 1
True
True
True
Method 7
File 1
True
True
True
True
Method 9
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestImplicitConstructorsWithLambdasCoverage()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                               // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                                  // Method 2
    {
        int y = s_c._function();
        D d = new D();
        int z = d._c._function();
        int zz = D.s_c._function();
        int zzz = d._c1._function();
    }

    public C(Func<int> f)                                                   // Method 3
    {
        _function = f;
    }

    static C s_c = new C(() => 115);
    Func<int> _function;
}

partial class D
{
}

partial class D
{
}

partial class D
{
    public C _c = new C(() => 120);
    public static C s_c = new C(() => 144);
    public C _c1 = new C(() => 130);
    public static C s_c1 = new C(() => 156);
}

partial class D
{
}

partial struct E
{
}

partial struct E
{
    public static C s_c = new C(() => 1444);
    public static C s_c1 = new C(() => { return 1567; });
}

// Method 4 is the synthesized static constructor for C.
// Method 5 is the synthesized instance constructor for D.
// Method 6 is the synthesized static constructor for D.
";
            string expectedOutput = @"
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
True
True
Method 3
File 1
True
True
Method 4
File 1
True
True
Method 5
File 1
True
True
True
True
Method 6
File 1
True
False
True
True
Method 9
File 1
True
True
False
True
True
True
True
True
True
True
True
True
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void MissingMethodNeededForAnalysis()
        {
            string source = @"
namespace System
{
    public class Object { }  
    public struct Int32 { }  
    public struct Boolean { }  
    public class String { }  
    public class Exception { }  
    public class ValueType { }  
    public class Enum { }  
    public struct Void { }  
    public class Guid { }
}

public class Console
{
    public static void WriteLine(string s) { }
    public static void WriteLine(int i) { }
    public static void WriteLine(bool b) { }
}

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static int TestMain()
    {
        return 3;
    }
}
";

            ImmutableArray<Diagnostic> diagnostics = CreateCompilation(source + InstrumentationHelperSource).GetEmitDiagnostics(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Code == (int)ErrorCode.ERR_MissingPredefinedMember &&
                    diagnostic.Arguments[0].Equals("System.Guid") && diagnostic.Arguments[1].Equals("Parse"))
                {
                    return;
                }
            }

            Assert.True(false);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, CompilationOptions options = null)
        {
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, additionalRefs: s_refs, options: (options ?? TestOptions.ReleaseExe).WithDeterministic(true), emitOptions: EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
        }

        private static readonly MetadataReference[] s_refs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };
    }
}
