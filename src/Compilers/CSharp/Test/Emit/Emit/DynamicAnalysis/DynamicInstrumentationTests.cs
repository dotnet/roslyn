// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
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
        private static System.Guid _mvid;

        public static bool[] CreatePayload(System.Guid mvid, int methodIndex, ref bool[] payload, int payloadLength)
        {
            if (_mvid != mvid)
            {
                _payloads = new bool[100][];
                _mvid = mvid;
            }

            if (System.Threading.Interlocked.CompareExchange(ref payload, new bool[payloadLength], null) == null)
            {
                _payloads[methodIndex] = payload;
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
                    Console.WriteLine(i);
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
1
True
4
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
";
            string expectedCreatePayloadIL = @"{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Guid Microsoft.CodeAnalysis.Runtime.Instrumentation._mvid""
  IL_0005:  ldarg.0
  IL_0006:  call       ""bool System.Guid.op_Inequality(System.Guid, System.Guid)""
  IL_000b:  brfalse.s  IL_001f
  IL_000d:  ldc.i4.s   100
  IL_000f:  newarr     ""bool[]""
  IL_0014:  stsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0019:  ldarg.0
  IL_001a:  stsfld     ""System.Guid Microsoft.CodeAnalysis.Runtime.Instrumentation._mvid""
  IL_001f:  ldarg.2
  IL_0020:  ldarg.3
  IL_0021:  newarr     ""bool""
  IL_0026:  ldnull
  IL_0027:  call       ""bool[] System.Threading.Interlocked.CompareExchange<bool[]>(ref bool[], bool[], bool[])""
  IL_002c:  brtrue.s   IL_003a
  IL_002e:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0033:  ldarg.1
  IL_0034:  ldarg.2
  IL_0035:  ldind.ref
  IL_0036:  stelem.ref
  IL_0037:  ldarg.2
  IL_0038:  ldind.ref
  IL_0039:  ret
  IL_003a:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_003f:  ldarg.1
  IL_0040:  ldelem.ref
  IL_0041:  ret
}";

            string expectedFlushPayloadIL = @"{
  // Code size      179 (0xb3)
  .maxstack  4
  .locals init (bool[] V_0,
                int V_1, //i
                bool[] V_2, //payload
                int V_3) //j
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0030
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
  IL_0019:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_001e:  ldtoken    ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
  IL_0023:  ldelema    ""bool[]""
  IL_0028:  ldc.i4.s   12
  IL_002a:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, ref bool[], int)""
  IL_002f:  stloc.0
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.0
  IL_0032:  ldc.i4.1
  IL_0033:  stelem.i1
  IL_0034:  ldstr      ""Flushing""
  IL_0039:  call       ""void System.Console.WriteLine(string)""
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4.2
  IL_0040:  ldc.i4.1
  IL_0041:  stelem.i1
  IL_0042:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_0047:  brtrue.s   IL_004e
  IL_0049:  ldloc.0
  IL_004a:  ldc.i4.1
  IL_004b:  ldc.i4.1
  IL_004c:  stelem.i1
  IL_004d:  ret
  IL_004e:  ldloc.0
  IL_004f:  ldc.i4.3
  IL_0050:  ldc.i4.1
  IL_0051:  stelem.i1
  IL_0052:  ldc.i4.0
  IL_0053:  stloc.1
  IL_0054:  br.s       IL_00a8
  IL_0056:  ldloc.0
  IL_0057:  ldc.i4.5
  IL_0058:  ldc.i4.1
  IL_0059:  stelem.i1
  IL_005a:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_005f:  ldloc.1
  IL_0060:  ldelem.ref
  IL_0061:  stloc.2
  IL_0062:  ldloc.0
  IL_0063:  ldc.i4.s   11
  IL_0065:  ldc.i4.1
  IL_0066:  stelem.i1
  IL_0067:  ldloc.2
  IL_0068:  brfalse.s  IL_00a0
  IL_006a:  ldloc.0
  IL_006b:  ldc.i4.6
  IL_006c:  ldc.i4.1
  IL_006d:  stelem.i1
  IL_006e:  ldloc.1
  IL_006f:  call       ""void System.Console.WriteLine(int)""
  IL_0074:  ldloc.0
  IL_0075:  ldc.i4.7
  IL_0076:  ldc.i4.1
  IL_0077:  stelem.i1
  IL_0078:  ldc.i4.0
  IL_0079:  stloc.3
  IL_007a:  br.s       IL_009a
  IL_007c:  ldloc.0
  IL_007d:  ldc.i4.s   9
  IL_007f:  ldc.i4.1
  IL_0080:  stelem.i1
  IL_0081:  ldloc.2
  IL_0082:  ldloc.3
  IL_0083:  ldelem.u1
  IL_0084:  call       ""void System.Console.WriteLine(bool)""
  IL_0089:  ldloc.0
  IL_008a:  ldc.i4.s   10
  IL_008c:  ldc.i4.1
  IL_008d:  stelem.i1
  IL_008e:  ldloc.2
  IL_008f:  ldloc.3
  IL_0090:  ldc.i4.0
  IL_0091:  stelem.i1
  IL_0092:  ldloc.0
  IL_0093:  ldc.i4.8
  IL_0094:  ldc.i4.1
  IL_0095:  stelem.i1
  IL_0096:  ldloc.3
  IL_0097:  ldc.i4.1
  IL_0098:  add
  IL_0099:  stloc.3
  IL_009a:  ldloc.3
  IL_009b:  ldloc.2
  IL_009c:  ldlen
  IL_009d:  conv.i4
  IL_009e:  blt.s      IL_007c
  IL_00a0:  ldloc.0
  IL_00a1:  ldc.i4.4
  IL_00a2:  ldc.i4.1
  IL_00a3:  stelem.i1
  IL_00a4:  ldloc.1
  IL_00a5:  ldc.i4.1
  IL_00a6:  add
  IL_00a7:  stloc.1
  IL_00a8:  ldloc.1
  IL_00a9:  ldsfld     ""bool[][] Microsoft.CodeAnalysis.Runtime.Instrumentation._payloads""
  IL_00ae:  ldlen
  IL_00af:  conv.i4
  IL_00b0:  blt.s      IL_0056
  IL_00b2:  ret
}";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
            verifier.VerifyIL("Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload", expectedCreatePayloadIL);
            verifier.VerifyIL("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload", expectedFlushPayloadIL);
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
1
True
True
2
True
True
False
True
True
True
3
True
True
True
True
4
True
True
False
True
True
5
True
True
False
False
False
6
True
9
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
";

            string expectedBarneyIL = @"{
  // Code size       82 (0x52)
  .maxstack  4
  .locals init (bool[] V_0)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""int Program.Barney(bool)""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_002f
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""int Program.Barney(bool)""
  IL_0019:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_001e:  ldtoken    ""int Program.Barney(bool)""
  IL_0023:  ldelema    ""bool[]""
  IL_0028:  ldc.i4.5
  IL_0029:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, ref bool[], int)""
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i1
  IL_0033:  ldarg.0
  IL_0034:  brfalse.s  IL_003d
  IL_0036:  ldloc.0
  IL_0037:  ldc.i4.0
  IL_0038:  ldc.i4.1
  IL_0039:  stelem.i1
  IL_003a:  ldc.i4.s   10
  IL_003c:  ret
  IL_003d:  ldloc.0
  IL_003e:  ldc.i4.3
  IL_003f:  ldc.i4.1
  IL_0040:  stelem.i1
  IL_0041:  ldarg.0
  IL_0042:  brfalse.s  IL_004b
  IL_0044:  ldloc.0
  IL_0045:  ldc.i4.2
  IL_0046:  ldc.i4.1
  IL_0047:  stelem.i1
  IL_0048:  ldc.i4.s   100
  IL_004a:  ret
  IL_004b:  ldloc.0
  IL_004c:  ldc.i4.4
  IL_004d:  ldc.i4.1
  IL_004e:  stelem.i1
  IL_004f:  ldc.i4.s   20
  IL_0051:  ret
}
";

            string expectedPIDStaticConstructorIL = @"{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldtoken    Max Method Token Index
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  newarr     ""bool[]""
  IL_000c:  stsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0011:  ldtoken    ""<PrivateImplementationDetails>""
  IL_0016:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001b:  callvirt   ""System.Reflection.Module System.Type.Module.get""
  IL_0020:  callvirt   ""System.Guid System.Reflection.Module.ModuleVersionId.get""
  IL_0025:  stsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_002a:  ret
}";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.Barney", expectedBarneyIL);
            verifier.VerifyIL(".cctor", expectedPIDStaticConstructorIL);
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
1
True
2
True
True
True
3
True
True
4
True
True
True
True
7
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
";

            string expectedGetValueIL = @"{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (bool[] V_0,
                T V_1)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_002f
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_0019:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_001e:  ldtoken    ""T MyBox<T>.GetValue()""
  IL_0023:  ldelema    ""bool[]""
  IL_0028:  ldc.i4.3
  IL_0029:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, ref bool[], int)""
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i1
  IL_0033:  ldarg.0
  IL_0034:  ldfld      ""T MyBox<T>._value""
  IL_0039:  box        ""T""
  IL_003e:  brtrue.s   IL_004e
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.0
  IL_0042:  ldc.i4.1
  IL_0043:  stelem.i1
  IL_0044:  ldloca.s   V_1
  IL_0046:  initobj    ""T""
  IL_004c:  ldloc.1
  IL_004d:  ret
  IL_004e:  ldloc.0
  IL_004f:  ldc.i4.2
  IL_0050:  ldc.i4.1
  IL_0051:  stelem.i1
  IL_0052:  ldarg.0
  IL_0053:  ldfld      ""T MyBox<T>._value""
  IL_0058:  ret
}";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
            verifier.VerifyIL("MyBox<T>.GetValue", expectedGetValueIL);
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
        Lambda(x, (y) => y + 1);
    }

    static int Function(int x) => x;

    static int Count => Function(44);

    static int Prop { get; set; }

    static int Lambda(int x, Func<int, int> l)
    {
        return l(x);
    }
}
";
            string expectedOutput = @"Flushing
1
True
True
2
True
True
True
True
3
True
4
True
5
True
6
True
7
True
10
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
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
        }

        [Fact]
        public void MultipleDeclarationsCoverage()
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
        int x;
        int a, b;
        DoubleDeclaration(5);
        DoubleForDeclaration(5);
    }

    static int DoubleDeclaration(int x)
    {
        int c = x;
        int a, b;
        int f;

        a = b = f = c;
        int d = a, e = b;
        return d + e + f;
    }

    static int DoubleForDeclaration(int x)
    {
        for(int a = x, b = x; a + b < 10; a++)
        {
            x++;
        }

        return x;
    }
}
";
            string expectedOutput = @"Flushing
1
True
True
2
True
True
3
True
True
True
True
4
True
False
False
True
7
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
";

            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
        }

        [Fact]
        public void UsingAndFixedCoverage()
        {
            string source = @"
using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }

    static void TestMain()
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
1
True
True
2
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
5
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
";
           
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, options: TestOptions.UnsafeDebugExe,  emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
        }

        [Fact]
        public void ManyStatementsCoverage()
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
        catch (System.Exception e)
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
        catch (System.Exception e)
        {
        }

        return;
    }
}
";
            string expectedOutput = @"103
Flushing
1
True
True
2
True
3
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
6
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
";

            CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
        }

        [Fact]
        public void LambdaCoverage()
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
1
True
True
2
True
True
True
False
True
True
True
False
True
5
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
";

            CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
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
1
True
True
2
True
3
True
True
True
4
True
True
False
True
5
True
True
False
True
True
8
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
";

            CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"), expectedOutput: expectedOutput);
        }

        [Fact]
        public void PortableAnalysis()
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
    public struct RuntimeTypeHandle { }

    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) { return new Type(); }
    }
}

public class Console
{
    public static void WriteLine(string s) { }
    public static void WriteLine(int i) { }
    public static void WriteLine(bool b) { }
}

namespace System.Reflection
{
    public class MemberInfo
    {
        public virtual Module Module => new Module();
    }

    public class TypeInfo : MemberInfo
    {
    }

    public class Module
    {
        public virtual Guid ModuleVersionId => new Guid();
    }

    public static class IntrospectionExtensions
    {
        public static TypeInfo GetTypeInfo(System.Type t) { return new TypeInfo(); }
    }
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

            string expectedTestMainIL = @"{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (bool[] V_0)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""int Program.TestMain()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_002f
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""int Program.TestMain()""
  IL_0019:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_001e:  ldtoken    ""int Program.TestMain()""
  IL_0023:  ldelema    ""bool[]""
  IL_0028:  ldc.i4.1
  IL_0029:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, ref bool[], int)""
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i1
  IL_0033:  ldc.i4.3
  IL_0034:  ret
}";

            string expectedPIDStaticConstructorIL = @"{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldtoken    Max Method Token Index
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  newarr     ""bool[]""
  IL_000c:  stsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0011:  ldtoken    ""<PrivateImplementationDetails>""
  IL_0016:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001b:  call       ""System.Reflection.TypeInfo System.Reflection.IntrospectionExtensions.GetTypeInfo(System.Type)""
  IL_0020:  callvirt   ""System.Reflection.Module System.Reflection.MemberInfo.Module.get""
  IL_0025:  callvirt   ""System.Guid System.Reflection.Module.ModuleVersionId.get""
  IL_002a:  stsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_002f:  ret
}";

            CompilationVerifier verifier = CompileWithNoFramework(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"));
            verifier.VerifyIL("Program.TestMain", expectedTestMainIL);
            verifier.VerifyIL(".cctor", expectedPIDStaticConstructorIL);
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
    public struct RuntimeTypeHandle { }

    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) { return new Type(); }
    }
}

public class Console
{
    public static void WriteLine(string s) { }
    public static void WriteLine(int i) { }
    public static void WriteLine(bool b) { }
}

namespace System.Reflection
{
    public class MemberInfo
    {
        public virtual Module Module => new Module();
    }

    public class TypeInfo : MemberInfo
    {
    }

    public class Module
    {
        public virtual Guid ModuleVersionId => new Guid();
    }

    public static class IntrospectionExtensions
    {
        // public static TypeInfo GetTypeInfo(System.Type t) { return new TypeInfo(); }
    }
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

            try
            {
                CompilationVerifier verifier = CompileWithNoFramework(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"));
            }
            catch (EmitException exception)
            {
                foreach (Diagnostic diagnostic in exception.Diagnostics)
                {
                    if (diagnostic.ToString().Contains("Missing compiler required member 'System.Reflection.IntrospectionExtensions.GetTypeInfo'"))
                    {
                        return;
                    }
                }           
            }

            Assert.True(false);
        }

        public void MissingPropertyNeededForAnalysis()
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
    public struct RuntimeTypeHandle { }

    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) { return new Type(); }
    }
}

public class Console
{
    public static void WriteLine(string s) { }
    public static void WriteLine(int i) { }
    public static void WriteLine(bool b) { }
}

namespace System.Reflection
{
    public class MemberInfo
    {
        public virtual Module Module => new Module();
    }

    public class TypeInfo : MemberInfo
    {
    }

    public class Module
    {
        // public virtual Guid ModuleVersionId => new Guid();
    }

    public static class IntrospectionExtensions
    {
        public static TypeInfo GetTypeInfo(System.Type t) { return new TypeInfo(); }
    }
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

            try
            {
                CompilationVerifier verifier = CompileWithNoFramework(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithInstrument("Test.Flag"));
            }
            catch (EmitException exception)
            {
                foreach (Diagnostic diagnostic in exception.Diagnostics)
                {
                    if (diagnostic.ToString().Contains("Missing compiler required member 'System.Reflection.Module.ModuleVersionId'"))
                    {
                        return;
                    }
                }
            }

            Assert.True(false);
        }

        private CompilationVerifier CompileWithNoFramework(string source, EmitOptions emitOptions, CompilationOptions options = null)
        {
            return base.CompileAndVerify(source, verify: false, options: options, emitOptions: emitOptions);
        }

        private CompilationVerifier CompileAndVerify(string source, EmitOptions emitOptions, string expectedOutput = null, CompilationOptions options = null)
        {
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, additionalRefs: s_refs, options: options, emitOptions: emitOptions);
        }

        private static readonly MetadataReference[] s_refs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };
    }
}
