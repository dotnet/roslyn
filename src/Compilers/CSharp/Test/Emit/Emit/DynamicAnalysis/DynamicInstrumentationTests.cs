// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Roslyn.Test.Utilities;
using static Microsoft.CodeAnalysis.Test.Utilities.CSharpInstrumentationChecker;

namespace Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests
{
    public class DynamicInstrumentationTests : CSharpTestBase
    {
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
True
True
";

            string expectedCreatePayloadForMethodsSpanningSingleFileIL = @"{
  // Code size       21 (0x15)
  .maxstack  6
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.1
  IL_0003:  newarr     ""int""
  IL_0008:  dup
  IL_0009:  ldc.i4.0
  IL_000a:  ldarg.2
  IL_000b:  stelem.i4
  IL_000c:  ldarg.3
  IL_000d:  ldarg.s    V_4
  IL_000f:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int[], ref bool[], int)""
  IL_0014:  ret
}";

            string expectedCreatePayloadForMethodsSpanningMultipleFilesIL = @"{
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
  IL_001b:  newarr     ""int[]""
  IL_0020:  stsfld     ""int[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
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
  IL_0044:  ldsfld     ""int[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
  IL_0049:  ldarg.1
  IL_004a:  ldarg.2
  IL_004b:  stelem.ref
  IL_004c:  ldarg.3
  IL_004d:  ldind.ref
  IL_004e:  ret
  IL_004f:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0054:  ldarg.1
  IL_0055:  ldelem.ref
  IL_0056:  ret
}";

            string expectedFlushPayloadIL = @"{
  // Code size      288 (0x120)
  .maxstack  5
  .locals init (bool[] V_0,
                int V_1, //i
                bool[] V_2, //payload
                int V_3, //j
                int V_4) //j
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
  IL_002d:  ldc.i4.s   16
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
  IL_005d:  br         IL_0112
  IL_0062:  ldloc.0
  IL_0063:  ldc.i4.6
  IL_0064:  ldc.i4.1
  IL_0065:  stelem.i1
  IL_0066:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_006b:  ldloc.1
  IL_006c:  ldelem.ref
  IL_006d:  stloc.2
  IL_006e:  ldloc.0
  IL_006f:  ldc.i4.s   15
  IL_0071:  ldc.i4.1
  IL_0072:  stelem.i1
  IL_0073:  ldloc.2
  IL_0074:  brfalse    IL_010a
  IL_0079:  ldloc.0
  IL_007a:  ldc.i4.7
  IL_007b:  ldc.i4.1
  IL_007c:  stelem.i1
  IL_007d:  ldstr      ""Method ""
  IL_0082:  ldloca.s   V_1
  IL_0084:  call       ""string int.ToString()""
  IL_0089:  call       ""string string.Concat(string, string)""
  IL_008e:  call       ""void System.Console.WriteLine(string)""
  IL_0093:  ldloc.0
  IL_0094:  ldc.i4.8
  IL_0095:  ldc.i4.1
  IL_0096:  stelem.i1
  IL_0097:  ldc.i4.0
  IL_0098:  stloc.3
  IL_0099:  br.s       IL_00ca
  IL_009b:  ldloc.0
  IL_009c:  ldc.i4.s   10
  IL_009e:  ldc.i4.1
  IL_009f:  stelem.i1
  IL_00a0:  ldstr      ""File ""
  IL_00a5:  ldsfld     ""int[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
  IL_00aa:  ldloc.1
  IL_00ab:  ldelem.ref
  IL_00ac:  ldloc.3
  IL_00ad:  ldelema    ""int""
  IL_00b2:  call       ""string int.ToString()""
  IL_00b7:  call       ""string string.Concat(string, string)""
  IL_00bc:  call       ""void System.Console.WriteLine(string)""
  IL_00c1:  ldloc.0
  IL_00c2:  ldc.i4.s   9
  IL_00c4:  ldc.i4.1
  IL_00c5:  stelem.i1
  IL_00c6:  ldloc.3
  IL_00c7:  ldc.i4.1
  IL_00c8:  add
  IL_00c9:  stloc.3
  IL_00ca:  ldloc.3
  IL_00cb:  ldsfld     ""int[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._fileIndices""
  IL_00d0:  ldloc.1
  IL_00d1:  ldelem.ref
  IL_00d2:  ldlen
  IL_00d3:  conv.i4
  IL_00d4:  blt.s      IL_009b
  IL_00d6:  ldloc.0
  IL_00d7:  ldc.i4.s   11
  IL_00d9:  ldc.i4.1
  IL_00da:  stelem.i1
  IL_00db:  ldc.i4.0
  IL_00dc:  stloc.s    V_4
  IL_00de:  br.s       IL_0103
  IL_00e0:  ldloc.0
  IL_00e1:  ldc.i4.s   13
  IL_00e3:  ldc.i4.1
  IL_00e4:  stelem.i1
  IL_00e5:  ldloc.2
  IL_00e6:  ldloc.s    V_4
  IL_00e8:  ldelem.u1
  IL_00e9:  call       ""void System.Console.WriteLine(bool)""
  IL_00ee:  ldloc.0
  IL_00ef:  ldc.i4.s   14
  IL_00f1:  ldc.i4.1
  IL_00f2:  stelem.i1
  IL_00f3:  ldloc.2
  IL_00f4:  ldloc.s    V_4
  IL_00f6:  ldc.i4.0
  IL_00f7:  stelem.i1
  IL_00f8:  ldloc.0
  IL_00f9:  ldc.i4.s   12
  IL_00fb:  ldc.i4.1
  IL_00fc:  stelem.i1
  IL_00fd:  ldloc.s    V_4
  IL_00ff:  ldc.i4.1
  IL_0100:  add
  IL_0101:  stloc.s    V_4
  IL_0103:  ldloc.s    V_4
  IL_0105:  ldloc.2
  IL_0106:  ldlen
  IL_0107:  conv.i4
  IL_0108:  blt.s      IL_00e0
  IL_010a:  ldloc.0
  IL_010b:  ldc.i4.5
  IL_010c:  ldc.i4.1
  IL_010d:  stelem.i1
  IL_010e:  ldloc.1
  IL_010f:  ldc.i4.1
  IL_0110:  add
  IL_0111:  stloc.1
  IL_0112:  ldloc.1
  IL_0113:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0118:  ldlen
  IL_0119:  conv.i4
  IL_011a:  blt        IL_0062
  IL_011f:  ret
}";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyIL("Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)", expectedCreatePayloadForMethodsSpanningSingleFileIL);
            verifier.VerifyIL("Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int[], ref bool[], int)", expectedCreatePayloadForMethodsSpanningMultipleFilesIL);
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
        Console.WriteLine(""goo"");
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
            string expectedOutput = @"goo
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
  IL_0016:  newobj     ""System.Guid..ctor(string)""
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
" + InstrumentationHelperSource;

            var checker = new CSharpInstrumentationChecker();
            checker.Method(3, 1, "public int Prop3")
                .True("get");
            checker.Method(4, 1, "public int Prop3")
                .True("set");
            checker.Method(5, 1, "public Program()")
                .True("25")
                .True("Prop = 12;")
                .True("Prop3 = 12;")
                .True("Prop2 = Prop3;");
            checker.Method(6, 1, "public static void Main")
                .True("new Program();")
                .True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();");
            checker.Method(8, 1)
                .True()
                .False()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True();

            CompilationVerifier verifier = CompileAndVerify(source, expectedOutput: checker.ExpectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            checker.CompleteCheck(verifier.Compilation, source);

            verifier = CompileAndVerify(source, expectedOutput: checker.ExpectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
            checker.CompleteCheck(verifier.Compilation, source);
        }

        [Fact]
        public void ImplicitBlockMethodsCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)                                  // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
    
    static void TestMain()                                                  // Method 2
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
True
True
";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LocalFunctionWithLambdaCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)                                  // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
    
    static void TestMain()                                                  // Method 2
    {
        new D().M1();
    } 
}

public class D
{
    public void M1()                                                        // Method 4
    {
        L1();
        void L1()
        {
            var f = new Func<int>(
                () => 1
            );

            var f1 = new Func<int>(
                () => 2
            );

            var f2 = new Func<int, int>(
                (x) => x + 3
            );

            var f3 = new Func<int, int>(
                x => x + 4
            );

            f();
            f3(2);
        }
    }

    // Method 5 is the synthesized instance constructor for D.
}
" + InstrumentationHelperSource;

            var checker = new CSharpInstrumentationChecker();
            checker.Method(1, 1, "public static void Main")
                .True("TestMain();")
                .True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();");
            checker.Method(2, 1, "static void TestMain")
                .True("new D().M1();");
            checker.Method(4, 1, "public void M1()")
                .True("L1();")
                .True("1")
                .True("var f = new Func<int>")
                .False("2")
                .True("var f1 = new Func<int>")
                .False("x + 3")
                .True("var f2 = new Func<int, int>")
                .True("x + 4")
                .True("var f3 = new Func<int, int>")
                .True("f();")
                .True("f3(2);");
            checker.Method(5, 1, snippet: null, expectBodySpan: false);
            checker.Method(7, 1)
                .True()
                .False()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True();

            CompilationVerifier verifier = CompileAndVerify(source, expectedOutput: checker.ExpectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            checker.CompleteCheck(verifier.Compilation, source);

            verifier = CompileAndVerify(source, expectedOutput: checker.ExpectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
            checker.CompleteCheck(verifier.Compilation, source);
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
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.UnsafeDebugExe, expectedOutput: expectedOutput, verify: Verification.Fails);
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
        s.GPA = s.Name switch { _ => 2.3 }; // switch expression is not instrumented
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
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Subject").WithArguments("Teacher.Subject", "null").WithLocation(37, 40));
        }

        [Fact]
        public void DeconstructionStatementCoverage()
        {
            string source = @"
using System;

public class C
{
    public static void Main() // Method 1
    {
        TestMain2();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain2() // Method 2
    {
        var (x, y) = new C();
    }

    static void TestMain3() // Method 3
    {
        var (x, y) = new C();
    }

    public C() // Method 4
    {
    }

    public void Deconstruct(out int x, out int y) // Method 5
    {
        x = 1;
        y = 1 switch { 1 => 2, 3 => 4, _ => 5 }; // switch expression is not instrumented
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
Method 4
File 1
True
Method 5
File 1
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
True
True
";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DeconstructionForeachStatementCoverage()
        {
            string source = @"
using System;

public class C
{
    public static void Main() // Method 1
    {
        TestMain2(new C[] { new C() });
        TestMain3(new C[] { });
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain2(C[] a) // Method 2
    {
        foreach (
            var (x, y)
            in a)
            ;
    }

    static void TestMain3(C[] a) // Method 3
    {
        foreach (
            var (x, y)
            in a)
            ;
    }

    public C() // Method 4
    {
    }

    public void Deconstruct(out int x, out int y) // Method 5
    {
        x = 1;
        y = 2;
    }
}
";
            string expectedOutput = @"Flushing
Method 1
File 1
True
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
False
False
Method 4
File 1
True
Method 5
File 1
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
True
True
";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
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
    public static void Main(string[] args)                                  // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                                  // Method 2
    {
        Console.WriteLine(Outer(""Goo"").Result);
    }

    async static Task<string> Outer(string s)                               // Method 3
    {
        string s1 = await First(s);
        string s2 = await Second(s);

        return s1 + s2;
    }

    async static Task<string> First(string s)                               // Method 4
    {
        string result = await Second(s) + ""Glue"";
        if (result.Length > 2)
            return result;
        else
            return ""Too short"";
    }

    async static Task<string> Second(string s)                              // Method 5
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
True
True
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void IteratorCoverage()
        {
            string source = @"
using System;                 

public class Program
{
    public static void Main(string[] args)                                  // Method 1
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()                                                  // Method 2
    {
        foreach (var i in Goo())
        {    
            Console.WriteLine(i);
        }  
        foreach (var i in Goo())
        {    
            Console.WriteLine(i);
        }
    }

    public static System.Collections.Generic.IEnumerable<int> Goo()
    {
        for (int i = 0; i < 5; ++i)
        {
            yield return i;
        }
    }
}
";
            string expectedOutput = @"0
1
2
3
4
0
1
2
3
4
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
Method 3
File 1
True
True
True
True
Method 6
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

            ImmutableArray<Diagnostic> diagnostics = CreateEmptyCompilation(source + InstrumentationHelperSource).GetEmitDiagnostics(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Code == (int)ErrorCode.ERR_MissingPredefinedMember &&
                    diagnostic.Arguments[0].Equals("System.Guid") && diagnostic.Arguments[1].Equals(".ctor"))
                {
                    return;
                }
            }

            Assert.True(false);
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_Method()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    [ExcludeFromCodeCoverage]
    void M1() { Console.WriteLine(1); }

    void M2() { Console.WriteLine(1); }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C.M1");
            AssertInstrumented(verifier, "C.M2");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_Ctor()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    int a = 1;

    [ExcludeFromCodeCoverage]
    public C() { Console.WriteLine(3); }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C..ctor");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_Cctor()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    static int a = 1;

    [ExcludeFromCodeCoverage]
    static C() { Console.WriteLine(3); }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);
            AssertNotInstrumented(verifier, "C..cctor");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_LocalFunctionsAndLambdas_InMethod()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    [ExcludeFromCodeCoverage]
    static void M1() { L1(); void L1() { new Action(() => { Console.WriteLine(1); }).Invoke(); } }
                                                      
    static void M2() { L2(); void L2() { new Action(() => { Console.WriteLine(2); }).Invoke(); } }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C.M1");
            AssertNotInstrumented(verifier, "C.<M1>g__L1|0_0");
            AssertNotInstrumented(verifier, "C.<>c.<M1>b__0_1");

            AssertInstrumented(verifier, "C.M2");
            AssertInstrumented(verifier, "C.<>c__DisplayClass1_0.<M2>g__L2|0"); // M2:L2
            AssertInstrumented(verifier, "C.<>c__DisplayClass1_0.<M2>b__1"); // M2:L2 lambda
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_LocalFunctionsAndLambdas_InInitializers()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    Action IF = new Action(() => { Console.WriteLine(1); });
    Action IP { get; } = new Action(() => { Console.WriteLine(2); });

    static Action SF = new Action(() => { Console.WriteLine(3); });
    static Action SP { get; } = new Action(() => { Console.WriteLine(4); });

    [ExcludeFromCodeCoverage]
    C() {}

    static C() {}
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C..ctor");
            AssertNotInstrumented(verifier, "C.<>c.<.ctor>b__8_0");
            AssertNotInstrumented(verifier, "C.<>c.<.ctor>b__8_1");

            AssertInstrumented(verifier, "C..cctor");
            AssertInstrumented(verifier, "C.<>c__DisplayClass9_0.<.cctor>b__0");
            AssertInstrumented(verifier, "C.<>c__DisplayClass9_0.<.cctor>b__1");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_LocalFunctionsAndLambdas_InAccessors()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    [ExcludeFromCodeCoverage]
    int P1 
    { 
        get { L1(); void L1() { Console.WriteLine(1); } return 1; } 
        set { L2(); void L2() { Console.WriteLine(2); } } 
    }

    int P2
    { 
        get { L3(); void L3() { Console.WriteLine(3); } return 3; } 
        set { L4(); void L4() { Console.WriteLine(4); } } 
    }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C.P1.get");
            AssertNotInstrumented(verifier, "C.P1.set");
            AssertNotInstrumented(verifier, "C.<get_P1>g__L1|1_0");
            AssertNotInstrumented(verifier, "C.<set_P1>g__L2|2_0");

            AssertInstrumented(verifier, "C.P2.get");
            AssertInstrumented(verifier, "C.P2.set");
            AssertInstrumented(verifier, "C.<get_P2>g__L3|4_0");
            AssertInstrumented(verifier, "C.<set_P2>g__L4|5_0");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_Type()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
class C
{
    int x = 1;

    static C() { }

    void M1() { Console.WriteLine(1); }

    int P { get => 1; set { } }

    event Action E { add { } remove { } }
}

class D
{
    int x = 1;

    static D() { }

    void M1() { Console.WriteLine(1); }

    int P { get => 1; set { } }

    event Action E { add { } remove { } }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C..ctor");
            AssertNotInstrumented(verifier, "C..cctor");
            AssertNotInstrumented(verifier, "C.M1");
            AssertNotInstrumented(verifier, "C.P.get");
            AssertNotInstrumented(verifier, "C.P.set");
            AssertNotInstrumented(verifier, "C.E.add");
            AssertNotInstrumented(verifier, "C.E.remove");

            AssertInstrumented(verifier, "D..ctor");
            AssertInstrumented(verifier, "D..cctor");
            AssertInstrumented(verifier, "D.M1");
            AssertInstrumented(verifier, "D.P.get");
            AssertInstrumented(verifier, "D.P.set");
            AssertInstrumented(verifier, "D.E.add");
            AssertInstrumented(verifier, "D.E.remove");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_NestedType()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class A
{
    class B1
    {
        [ExcludeFromCodeCoverage]
        class C
        {
            void M1() { Console.WriteLine(1); }
        }

        void M2() { Console.WriteLine(2); }
    }

    [ExcludeFromCodeCoverage]
    partial class B2
    {
        partial class C1
        {
            void M3() { Console.WriteLine(3); }
        }

        class C2
        {
            void M4() { Console.WriteLine(4); }
        }

        void M5() { Console.WriteLine(5); }
    }

    partial class B2
    {
        [ExcludeFromCodeCoverage]
        partial class C1
        {
            void M6() { Console.WriteLine(6); }
        }

        void M7() { Console.WriteLine(7); }
    }

    void M8() { Console.WriteLine(8); }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "A.B1.C.M1");
            AssertInstrumented(verifier, "A.B1.M2");
            AssertNotInstrumented(verifier, "A.B2.C1.M3");
            AssertNotInstrumented(verifier, "A.B2.C2.M4");
            AssertNotInstrumented(verifier, "A.B2.C1.M6");
            AssertNotInstrumented(verifier, "A.B2.M7");
            AssertInstrumented(verifier, "A.M8");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_Accessors()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    [ExcludeFromCodeCoverage]
    int P1 { get => 1; set {} }
          
    [ExcludeFromCodeCoverage]
    event Action E1 { add { } remove { } }
                                            
    int P2 { get => 1; set {} }
    event Action E2 { add { } remove { } }
}
";
            var verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);

            AssertNotInstrumented(verifier, "C.P1.get");
            AssertNotInstrumented(verifier, "C.P1.set");
            AssertNotInstrumented(verifier, "C.E1.add");
            AssertNotInstrumented(verifier, "C.E1.remove");

            AssertInstrumented(verifier, "C.P2.get");
            AssertInstrumented(verifier, "C.P2.set");
            AssertInstrumented(verifier, "C.E2.add");
            AssertInstrumented(verifier, "C.E2.remove");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_CustomDefinition_Good()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public ExcludeFromCodeCoverageAttribute() {}
    }
}

[ExcludeFromCodeCoverage]
class C
{
    void M() {}
}

class D
{
    void M() {}
}
";
            var c = CreateCompilationWithMscorlib40(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);
            c.VerifyDiagnostics();

            var verifier = CompileAndVerify(c, emitOptions: EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
            c.VerifyEmitDiagnostics();

            AssertNotInstrumented(verifier, "C.M");
            AssertInstrumented(verifier, "D.M");
        }

        [Fact]
        public void ExcludeFromCodeCoverageAttribute_CustomDefinition_Bad()
        {
            string source = @"
using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public ExcludeFromCodeCoverageAttribute(int x) {}
    }
}

[ExcludeFromCodeCoverage(1)]
class C
{
    void M() {}
}

class D
{
    void M() {}
}
";
            var c = CreateCompilationWithMscorlib40(source + InstrumentationHelperSource, options: TestOptions.ReleaseDll);
            c.VerifyDiagnostics();

            var verifier = CompileAndVerify(c, emitOptions: EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
            c.VerifyEmitDiagnostics();

            AssertInstrumented(verifier, "C.M");
            AssertInstrumented(verifier, "D.M");
        }

        [Fact]
        public void TestPartialMethodsWithImplementation()
        {
            var source = @"
using System;

public partial class Class1<T>
{
    partial void Method1<U>(int x);
    public void Method2(int x) 
    {
        Console.WriteLine($""Method2: x = {x}"");
        Method1<T>(x);
    }
}

public partial class Class1<T>
{
    partial void Method1<U>(int x)
    {
        Console.WriteLine($""Method1: x = {x}"");
        if (x > 0)
        {
             Console.WriteLine(""Method1: x > 0"");
             Method1<U>(0);
        }
        else if (x < 0)
        {
            Console.WriteLine(""Method1: x < 0"");
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Test();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void Test()
    {
        Console.WriteLine(""Test"");
        var c = new Class1<int>();
        c.Method2(1);
    }
}
" + InstrumentationHelperSource;

            var checker = new CSharpInstrumentationChecker();
            checker.Method(1, 1, "partial void Method1<U>(int x)")
                .True(@"Console.WriteLine($""Method1: x = {x}"");")
                .True(@"Console.WriteLine(""Method1: x > 0"");")
                .True("Method1<U>(0);")
                .False(@"Console.WriteLine(""Method1: x < 0"");")
                .True("x < 0)")
                .True("x > 0)");
            checker.Method(2, 1, "public void Method2(int x)")
                .True(@"Console.WriteLine($""Method2: x = {x}"");")
                .True("Method1<T>(x);");
            checker.Method(3, 1, ".ctor()", expectBodySpan: false);
            checker.Method(4, 1, "public static void Main(string[] args)")
                .True("Test();")
                .True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();");
            checker.Method(5, 1, "static void Test()")
                .True(@"Console.WriteLine(""Test"");")
                .True("var c = new Class1<int>();")
                .True("c.Method2(1);");
            checker.Method(8, 1)
                .True()
                .False()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True();

            var expectedOutput = @"Test
Method2: x = 1
Method1: x = 1
Method1: x > 0
Method1: x = 0
" + checker.ExpectedOutput;

            var verifier = CompileAndVerify(source, expectedOutput, options: TestOptions.ReleaseExe);
            checker.CompleteCheck(verifier.Compilation, source);
            verifier.VerifyDiagnostics();

            verifier = CompileAndVerify(source, expectedOutput, options: TestOptions.DebugExe);
            checker.CompleteCheck(verifier.Compilation, source);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestPartialMethodsWithoutImplementation()
        {
            var source = @"
using System;

public partial class Class1<T>
{
    partial void Method1<U>(int x);
    public void Method2(int x) 
    {
        Console.WriteLine($""Method2: x = {x}"");
        Method1<T>(x);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Test();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void Test()
    {
        Console.WriteLine(""Test"");
        var c = new Class1<int>();
        c.Method2(1);
    }
}
" + InstrumentationHelperSource;

            var checker = new CSharpInstrumentationChecker();
            checker.Method(1, 1, "public void Method2(int x)")
                .True(@"Console.WriteLine($""Method2: x = {x}"");");
            checker.Method(2, 1, ".ctor()", expectBodySpan: false);
            checker.Method(3, 1, "public static void Main(string[] args)")
                .True("Test();")
                .True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();");
            checker.Method(4, 1, "static void Test()")
                .True(@"Console.WriteLine(""Test"");")
                .True("var c = new Class1<int>();")
                .True("c.Method2(1);");
            checker.Method(7, 1)
                .True()
                .False()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True()
                .True();

            var expectedOutput = @"Test
Method2: x = 1
" + checker.ExpectedOutput;

            var verifier = CompileAndVerify(source, expectedOutput, options: TestOptions.ReleaseExe);
            checker.CompleteCheck(verifier.Compilation, source);
            verifier.VerifyDiagnostics();

            verifier = CompileAndVerify(source, expectedOutput, options: TestOptions.DebugExe);
            checker.CompleteCheck(verifier.Compilation, source);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestSynthesizedConstructorWithSpansInMultipleFilesCoverage()
        {
            var source1 = @"
using System;

public partial class Class1<T>
{
    private int x = 1;
}

public class Program
{
    public static void Main(string[] args)
    {
        Test();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void Test()
    {
        Console.WriteLine(""Test"");
        var c = new Class1<int>();
        c.Method1(1);
    }
}
" + InstrumentationHelperSource;

            var source2 = @"
public partial class Class1<T>
{
    private int y = 2;
}

public partial class Class1<T>
{
    private int z = 3;
}";

            var source3 = @"
using System;

public partial class Class1<T>
{
    private Action<int> a = i =>
        {
            Console.WriteLine(i);
        };

    public void Method1(int i)
    {
        a(i);
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
    }
}";

            var sources = new[] {
                (Name: "b.cs", Content: source1),
                (Name: "c.cs", Content: source2),
                (Name: "a.cs", Content: source3)
            };

            var expectedOutput = @"Test
1
1
2
3
Flushing
Method 1
File 1
True
True
True
True
True
Method 2
File 1
File 2
File 3
True
True
True
True
True
Method 3
File 2
True
True
True
Method 4
File 2
True
True
True
True
Method 7
File 2
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
";

            var verifier = CompileAndVerify(sources, expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();

            verifier = CompileAndVerify(sources, expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestSynthesizedStaticConstructorWithSpansInMultipleFilesCoverage()
        {
            var source1 = @"
using System;

public partial class Class1<T>
{
    private static int x = 1;
}

public class Program
{
    public static void Main(string[] args)
    {
        Test();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void Test()
    {
        Console.WriteLine(""Test"");
        var c = new Class1<int>();
        Class1<int>.Method1(1);
    }
}
" + InstrumentationHelperSource;

            var source2 = @"
public partial class Class1<T>
{
    private static int y = 2;
}

public partial class Class1<T>
{
    private static int z = 3;
}";

            var source3 = @"
using System;

public partial class Class1<T>
{
    private static Action<int> a = i =>
        {
            Console.WriteLine(i);
        };

    public static void Method1(int i)
    {
        a(i);
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
    }
}";

            var sources = new[] {
                (Name: "b.cs", Content: source1),
                (Name: "c.cs", Content: source2),
                (Name: "a.cs", Content: source3)
            };

            var expectedOutput = @"Test
1
1
2
3
Flushing
Method 1
File 1
True
True
True
True
True
Method 2
File 2
Method 3
File 1
File 2
File 3
True
True
True
True
True
Method 4
File 2
True
True
True
Method 5
File 2
True
True
True
True
Method 8
File 2
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
";

            var verifier = CompileAndVerify(sources, expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();

            verifier = CompileAndVerify(sources, expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestLineDirectiveCoverage()
        {
            var source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Test();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void Test()
    {
#line 300 ""File2.cs""
        Console.WriteLine(""Start"");
#line hidden
        Console.WriteLine(""Hidden"");
#line default
        Console.WriteLine(""Visible"");
#line 400 ""File3.cs""
        Console.WriteLine(""End"");
    }
}
" + InstrumentationHelperSource;

            var expectedOutput = @"Start
Hidden
Visible
End
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
File 2
File 3
True
True
True
True
True
Method 5
File 3
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
";

            var verifier = CompileAndVerify(source, expectedOutput, options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();

            verifier = CompileAndVerify(source, expectedOutput, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
        }

        private static void AssertNotInstrumented(CompilationVerifier verifier, string qualifiedMethodName)
            => AssertInstrumented(verifier, qualifiedMethodName, expected: false);

        private static void AssertInstrumented(CompilationVerifier verifier, string qualifiedMethodName, bool expected = true)
        {
            string il = verifier.VisualizeIL(qualifiedMethodName);

            // Tests using this helper are constructed such that instrumented methods contain a call to CreatePayload, 
            // lambdas a reference to payload bool array.
            bool instrumented = il.Contains("CreatePayload") || il.Contains("bool[]");

            Assert.True(expected == instrumented, $"Method '{qualifiedMethodName}' should {(expected ? "be" : "not be")} instrumented. Actual IL:{Environment.NewLine}{il}");
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, CSharpCompilationOptions options = null, Verification verify = Verification.Passes)
        {
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, options: (options ?? TestOptions.ReleaseExe).WithDeterministic(true), emitOptions: EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)), verify: verify);
        }

        private CompilationVerifier CompileAndVerify((string Path, string Content)[] sources, string expectedOutput = null, CSharpCompilationOptions options = null)
        {
            var trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var source in sources)
            {
                // The trees must be assigned unique file names in order for instrumentation to work correctly.
                trees.Add(Parse(source.Content, filename: source.Path));
            }

            var compilation = CreateCompilation(trees.ToArray(), options: (options ?? TestOptions.ReleaseExe).WithDeterministic(true));
            trees.Free();
            return base.CompileAndVerify(compilation, expectedOutput: expectedOutput, emitOptions: EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
        }

    }
}
