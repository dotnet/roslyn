// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class StringConcatTests : CSharpTestBase
    {
        [Fact]
        public void ConcatConsts()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        Console.WriteLine(""A"" + ""B"");

        Console.WriteLine(""A"" + (string)null);
        Console.WriteLine(""A"" + (object)null);
        Console.WriteLine(""A"" + (object)null + ""A"" + (object)null);

        Console.WriteLine((string)null + ""B"");
        Console.WriteLine((object)null + ""B"");

        Console.WriteLine((string)null + (object)null);
        Console.WriteLine(""#"");
        Console.WriteLine((object)null + (string)null);
        Console.WriteLine(""#"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"AB
A
A
AA
B
B

#

#");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      101 (0x65)
  .maxstack  1
  IL_0000:  ldstr      ""AB""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""A""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""A""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ldstr      ""AA""
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  ldstr      ""B""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ldstr      ""B""
  IL_0037:  call       ""void System.Console.WriteLine(string)""
  IL_003c:  ldstr      """"
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldstr      ""#""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ldstr      """"
  IL_0055:  call       ""void System.Console.WriteLine(string)""
  IL_005a:  ldstr      ""#""
  IL_005f:  call       ""void System.Console.WriteLine(string)""
  IL_0064:  ret
}
");
        }

        [Fact, WorkItem(38858, "https://github.com/dotnet/roslyn/issues/38858")]
        public void ConcatEnumWithToString()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write(M(Enum.A));
    }
    public static string M(Enum e)
    {
        return e + """";
    }
}
public enum Enum { A = 0, ToString = 1 }
";
            var comp = CompileAndVerify(source, expectedOutput: "A");
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(38858, "https://github.com/dotnet/roslyn/issues/38858")]
        public void ConcatStructWithToString()
        {
            var source = @"
public struct Bad
{
    public new int ToString;

    string Crash()
    {
        return """" + this;
    }
}
";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConcatDefaults()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        Console.WriteLine(""A"" + ""B"");

        Console.WriteLine(""A"" + default(string));
        Console.WriteLine(""A"" + default(object));
        Console.WriteLine(""A"" + default(object) + ""A"" + default(object));

        Console.WriteLine(default(string) + ""B"");
        Console.WriteLine(default(object) + ""B"");

        Console.WriteLine(default(string) + default(object));
        Console.WriteLine(""#"");
        Console.WriteLine(default(object) + default(string));
        Console.WriteLine(""#"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"AB
A
A
AA
B
B

#

#");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      101 (0x65)
  .maxstack  1
  IL_0000:  ldstr      ""AB""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""A""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""A""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ldstr      ""AA""
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  ldstr      ""B""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ldstr      ""B""
  IL_0037:  call       ""void System.Console.WriteLine(string)""
  IL_003c:  ldstr      """"
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldstr      ""#""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ldstr      """"
  IL_0055:  call       ""void System.Console.WriteLine(string)""
  IL_005a:  ldstr      ""#""
  IL_005f:  call       ""void System.Console.WriteLine(string)""
  IL_0064:  ret
}
");
        }

        [Fact]
        public void ConcatFour()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {        
            var s = ""qq"";
            var ss = s + s + s + s;
            Console.WriteLine(ss);    
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"qqqqqqqq"
);

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       19 (0x13)
  .maxstack  4
  IL_0000:  ldstr      ""qq""
  IL_0005:  dup
  IL_0006:  dup
  IL_0007:  dup
  IL_0008:  call       ""string string.Concat(string, string, string, string)""
  IL_000d:  call       ""void System.Console.WriteLine(string)""
  IL_0012:  ret
}
");
        }

        [Fact]
        public void ConcatMerge()
        {
            var source = @"
using System;

public class Test
{
    private static string S = ""F"";
    private static object O = ""O"";

    static void Main()
    {        
        Console.WriteLine( (S + ""A"") + (""B"" + S));
        Console.WriteLine( (O + ""A"") + (""B"" + O));
        Console.WriteLine( ((S + ""A"") + (""B"" + S)) + ((O + ""A"") + (""B"" + O)));
        Console.WriteLine( ((O + ""A"") + (S + ""A"")) + ((""B"" + O) + (S + ""A"")));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"FABF
OABO
FABFOABO
OAFABOFA");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      259 (0x103)
  .maxstack  5
  IL_0000:  ldsfld     ""string Test.S""
  IL_0005:  ldstr      ""AB""
  IL_000a:  ldsfld     ""string Test.S""
  IL_000f:  call       ""string string.Concat(string, string, string)""
  IL_0014:  call       ""void System.Console.WriteLine(string)""
  IL_0019:  ldsfld     ""object Test.O""
  IL_001e:  dup
  IL_001f:  brtrue.s   IL_0025
  IL_0021:  pop
  IL_0022:  ldnull
  IL_0023:  br.s       IL_002a
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  ldstr      ""AB""
  IL_002f:  ldsfld     ""object Test.O""
  IL_0034:  dup
  IL_0035:  brtrue.s   IL_003b
  IL_0037:  pop
  IL_0038:  ldnull
  IL_0039:  br.s       IL_0040
  IL_003b:  callvirt   ""string object.ToString()""
  IL_0040:  call       ""string string.Concat(string, string, string)""
  IL_0045:  call       ""void System.Console.WriteLine(string)""
  IL_004a:  ldc.i4.6
  IL_004b:  newarr     ""string""
  IL_0050:  dup
  IL_0051:  ldc.i4.0
  IL_0052:  ldsfld     ""string Test.S""
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.1
  IL_005a:  ldstr      ""AB""
  IL_005f:  stelem.ref
  IL_0060:  dup
  IL_0061:  ldc.i4.2
  IL_0062:  ldsfld     ""string Test.S""
  IL_0067:  stelem.ref
  IL_0068:  dup
  IL_0069:  ldc.i4.3
  IL_006a:  ldsfld     ""object Test.O""
  IL_006f:  dup
  IL_0070:  brtrue.s   IL_0076
  IL_0072:  pop
  IL_0073:  ldnull
  IL_0074:  br.s       IL_007b
  IL_0076:  callvirt   ""string object.ToString()""
  IL_007b:  stelem.ref
  IL_007c:  dup
  IL_007d:  ldc.i4.4
  IL_007e:  ldstr      ""AB""
  IL_0083:  stelem.ref
  IL_0084:  dup
  IL_0085:  ldc.i4.5
  IL_0086:  ldsfld     ""object Test.O""
  IL_008b:  dup
  IL_008c:  brtrue.s   IL_0092
  IL_008e:  pop
  IL_008f:  ldnull
  IL_0090:  br.s       IL_0097
  IL_0092:  callvirt   ""string object.ToString()""
  IL_0097:  stelem.ref
  IL_0098:  call       ""string string.Concat(params string[])""
  IL_009d:  call       ""void System.Console.WriteLine(string)""
  IL_00a2:  ldc.i4.7
  IL_00a3:  newarr     ""string""
  IL_00a8:  dup
  IL_00a9:  ldc.i4.0
  IL_00aa:  ldsfld     ""object Test.O""
  IL_00af:  dup
  IL_00b0:  brtrue.s   IL_00b6
  IL_00b2:  pop
  IL_00b3:  ldnull
  IL_00b4:  br.s       IL_00bb
  IL_00b6:  callvirt   ""string object.ToString()""
  IL_00bb:  stelem.ref
  IL_00bc:  dup
  IL_00bd:  ldc.i4.1
  IL_00be:  ldstr      ""A""
  IL_00c3:  stelem.ref
  IL_00c4:  dup
  IL_00c5:  ldc.i4.2
  IL_00c6:  ldsfld     ""string Test.S""
  IL_00cb:  stelem.ref
  IL_00cc:  dup
  IL_00cd:  ldc.i4.3
  IL_00ce:  ldstr      ""AB""
  IL_00d3:  stelem.ref
  IL_00d4:  dup
  IL_00d5:  ldc.i4.4
  IL_00d6:  ldsfld     ""object Test.O""
  IL_00db:  dup
  IL_00dc:  brtrue.s   IL_00e2
  IL_00de:  pop
  IL_00df:  ldnull
  IL_00e0:  br.s       IL_00e7
  IL_00e2:  callvirt   ""string object.ToString()""
  IL_00e7:  stelem.ref
  IL_00e8:  dup
  IL_00e9:  ldc.i4.5
  IL_00ea:  ldsfld     ""string Test.S""
  IL_00ef:  stelem.ref
  IL_00f0:  dup
  IL_00f1:  ldc.i4.6
  IL_00f2:  ldstr      ""A""
  IL_00f7:  stelem.ref
  IL_00f8:  call       ""string string.Concat(params string[])""
  IL_00fd:  call       ""void System.Console.WriteLine(string)""
  IL_0102:  ret
}
");
        }

        [Fact]
        [WorkItem(37830, "https://github.com/dotnet/roslyn/issues/37830")]
        public void ConcatMerge_MarshalByRefObject()
        {
            var source = @"
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

class MyProxy : RealProxy
{
    readonly MarshalByRefObject target;

    public MyProxy(MarshalByRefObject target) : base(target.GetType())
    {
        this.target = target;
    }

    public override IMessage Invoke(IMessage request)
    {
        IMethodCallMessage call = (IMethodCallMessage)request;
        IMethodReturnMessage res = RemotingServices.ExecuteMessage(target, call);
        return res;
    }
}

class R1 : MarshalByRefObject
{
    public int test_field = 5;
}

class Test
{
    static void Main()
    {
        R1 myobj = new R1();
        MyProxy real_proxy = new MyProxy(myobj);
        R1 o = (R1)real_proxy.GetTransparentProxy();
        o.test_field = 2;
        Console.WriteLine(""test_field: "" + o.test_field);
    }
}
";
            var comp = CompileAndVerify(source, targetFramework: TargetFramework.NetFramework, expectedOutput: ExecutionConditionUtil.IsWindowsDesktop ? @"test_field: 2" : null);
            comp.VerifyDiagnostics();
            // Note: we use ldfld on the field, but not ldflda, because the type is MarshalByRefObject
            comp.VerifyIL("Test.Main", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (R1 V_0, //o
                int V_1)
  IL_0000:  newobj     ""R1..ctor()""
  IL_0005:  newobj     ""MyProxy..ctor(System.MarshalByRefObject)""
  IL_000a:  callvirt   ""object System.Runtime.Remoting.Proxies.RealProxy.GetTransparentProxy()""
  IL_000f:  castclass  ""R1""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.2
  IL_0017:  stfld      ""int R1.test_field""
  IL_001c:  ldstr      ""test_field: ""
  IL_0021:  ldloc.0
  IL_0022:  ldfld      ""int R1.test_field""
  IL_0027:  stloc.1
  IL_0028:  ldloca.s   V_1
  IL_002a:  constrained. ""int""
  IL_0030:  callvirt   ""string object.ToString()""
  IL_0035:  call       ""string string.Concat(string, string)""
  IL_003a:  call       ""void System.Console.WriteLine(string)""
  IL_003f:  ret
}
");
        }

        [Fact]
        public void ConcatMergeFromOne()
        {
            var source = @"
using System;

public class Test
{
    private static string S = ""F"";

    static void Main()
    {        
        Console.WriteLine( (S + null) + (S + ""A"") + (""B"" + S) + (S + null));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"FFABFF");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldsfld     ""string Test.S""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldsfld     ""string Test.S""
  IL_0015:  stelem.ref
  IL_0016:  dup
  IL_0017:  ldc.i4.2
  IL_0018:  ldstr      ""AB""
  IL_001d:  stelem.ref
  IL_001e:  dup
  IL_001f:  ldc.i4.3
  IL_0020:  ldsfld     ""string Test.S""
  IL_0025:  stelem.ref
  IL_0026:  dup
  IL_0027:  ldc.i4.4
  IL_0028:  ldsfld     ""string Test.S""
  IL_002d:  stelem.ref
  IL_002e:  call       ""string string.Concat(params string[])""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ret
}
");
        }

        [Fact]
        public void ConcatOneArg()
        {
            var source = @"
using System;

public class Test
{
    private static string S = ""F"";
    private static object O = ""O"";

    static void Main()
    {
        Console.WriteLine(O + null);
        Console.WriteLine(S + null);
        Console.WriteLine(O?.ToString() + null);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"O
F
O");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       82 (0x52)
  .maxstack  2
  IL_0000:  ldsfld     ""object Test.O""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  br.s       IL_0011
  IL_000c:  callvirt   ""string object.ToString()""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_001a
  IL_0014:  pop
  IL_0015:  ldstr      """"
  IL_001a:  call       ""void System.Console.WriteLine(string)""
  IL_001f:  ldsfld     ""string Test.S""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_002d
  IL_0027:  pop
  IL_0028:  ldstr      """"
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ldsfld     ""object Test.O""
  IL_0037:  dup
  IL_0038:  brtrue.s   IL_003e
  IL_003a:  pop
  IL_003b:  ldnull
  IL_003c:  br.s       IL_0043
  IL_003e:  callvirt   ""string object.ToString()""
  IL_0043:  dup
  IL_0044:  brtrue.s   IL_004c
  IL_0046:  pop
  IL_0047:  ldstr      """"
  IL_004c:  call       ""void System.Console.WriteLine(string)""
  IL_0051:  ret
}
");
        }

        [Fact]
        public void ConcatOneArgWithNullToString()
        {
            var source = @"
using System;

public class Test
{
    private static object C = new C();

    static void Main()
    {
        Console.WriteLine((C + null) == """" ? ""Y"" : ""N"");
        Console.WriteLine((C + null + null) == """" ? ""Y"" : ""N"");
    }
}

public class C
{
    public override string ToString() => null;
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"Y
Y");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      111 (0x6f)
  .maxstack  2
  IL_0000:  ldsfld     ""object Test.C""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  br.s       IL_0011
  IL_000c:  callvirt   ""string object.ToString()""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_001a
  IL_0014:  pop
  IL_0015:  ldstr      """"
  IL_001a:  ldstr      """"
  IL_001f:  call       ""bool string.op_Equality(string, string)""
  IL_0024:  brtrue.s   IL_002d
  IL_0026:  ldstr      ""N""
  IL_002b:  br.s       IL_0032
  IL_002d:  ldstr      ""Y""
  IL_0032:  call       ""void System.Console.WriteLine(string)""
  IL_0037:  ldsfld     ""object Test.C""
  IL_003c:  dup
  IL_003d:  brtrue.s   IL_0043
  IL_003f:  pop
  IL_0040:  ldnull
  IL_0041:  br.s       IL_0048
  IL_0043:  callvirt   ""string object.ToString()""
  IL_0048:  dup
  IL_0049:  brtrue.s   IL_0051
  IL_004b:  pop
  IL_004c:  ldstr      """"
  IL_0051:  ldstr      """"
  IL_0056:  call       ""bool string.op_Equality(string, string)""
  IL_005b:  brtrue.s   IL_0064
  IL_005d:  ldstr      ""N""
  IL_0062:  br.s       IL_0069
  IL_0064:  ldstr      ""Y""
  IL_0069:  call       ""void System.Console.WriteLine(string)""
  IL_006e:  ret
}
");
        }

        [Fact]
        public void ConcatOneArgWithExplicitConcatCall()
        {
            var source = @"
using System;

public class Test
{
    private static object O = ""O"";

    static void Main()
    {
        Console.WriteLine(string.Concat(O) + null);
        Console.WriteLine(string.Concat(O) + null + null);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"O
O");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""object Test.O""
  IL_0005:  call       ""string string.Concat(object)""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0013
  IL_000d:  pop
  IL_000e:  ldstr      """"
  IL_0013:  call       ""void System.Console.WriteLine(string)""
  IL_0018:  ldsfld     ""object Test.O""
  IL_001d:  call       ""string string.Concat(object)""
  IL_0022:  dup
  IL_0023:  brtrue.s   IL_002b
  IL_0025:  pop
  IL_0026:  ldstr      """"
  IL_002b:  call       ""void System.Console.WriteLine(string)""
  IL_0030:  ret
}
");
        }

        [Fact]
        public void ConcatEmptyString()
        {
            var source = @"
using System;

public class Test
{
    private static string S = ""F"";
    private static object O = ""O"";

    static void Main()
    {
        Console.WriteLine(O + """");
        Console.WriteLine(S + """");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"O
F");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  2
  IL_0000:  ldsfld     ""object Test.O""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  br.s       IL_0011
  IL_000c:  callvirt   ""string object.ToString()""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_001a
  IL_0014:  pop
  IL_0015:  ldstr      """"
  IL_001a:  call       ""void System.Console.WriteLine(string)""
  IL_001f:  ldsfld     ""string Test.S""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_002d
  IL_0027:  pop
  IL_0028:  ldstr      """"
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
}
");
        }

        [Fact]
        [WorkItem(679120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679120")]
        public void ConcatEmptyArray()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        Console.WriteLine(""Start"");
        Console.WriteLine(string.Concat(new string[] {}));
        Console.WriteLine(string.Concat(new string[] {}) + string.Concat(new string[] {}));
        Console.WriteLine(""A"" + string.Concat(new string[] {}));
        Console.WriteLine(string.Concat(new string[] {}) + ""B"");
        Console.WriteLine(""End"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"Start


A
B
End");

            comp.VerifyDiagnostics();
            // NOTE: Dev11 doesn't optimize away string.Concat(new string[0]) either.
            // We could add an optimization, but it's unlikely to occur in real code.
            comp.VerifyIL("Test.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  1
  IL_0000:  ldstr      ""Start""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldc.i4.0
  IL_000b:  newarr     ""string""
  IL_0010:  call       ""string string.Concat(params string[])""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldstr      """"
  IL_001f:  call       ""void System.Console.WriteLine(string)""
  IL_0024:  ldstr      ""A""
  IL_0029:  call       ""void System.Console.WriteLine(string)""
  IL_002e:  ldstr      ""B""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ldstr      ""End""
  IL_003d:  call       ""void System.Console.WriteLine(string)""
  IL_0042:  ret
}
");
        }

        [WorkItem(529064, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529064")]
        [Fact]
        public void TestStringConcatOnLiteralAndCompound()
        {
            var source = @"
public class Test
{
    static string field01 = ""A"";
    static string field02 = ""B"";
    static void Main()
    {
        field01 += field02 + ""C"" + ""D"";
    }
}
";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  3
  IL_0000:  ldsfld     ""string Test.field01""
  IL_0005:  ldsfld     ""string Test.field02""
  IL_000a:  ldstr      ""CD""
  IL_000f:  call       ""string string.Concat(string, string, string)""
  IL_0014:  stsfld     ""string Test.field01""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void ConcatGeneric()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        TestMethod<int>();
    }

    private static void TestMethod<T>()
    {
        Console.WriteLine(""A"" + default(T));
        Console.WriteLine(""A"" + default(T) + ""A"" + default(T));

        Console.WriteLine(default(T) + ""B"");

        Console.WriteLine(default(string) + default(T));
        Console.WriteLine(""#"");
        Console.WriteLine(default(T) + default(string));
        Console.WriteLine(""#"");
    }
}

";
            var comp = CompileAndVerify(source, expectedOutput: @"A0
A0A0
0B
0
#
0
#");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.TestMethod<T>()", @"
{
  // Code size      291 (0x123)
  .maxstack  4
  .locals init (T V_0)
  IL_0000:  ldstr      ""A""
  IL_0005:  ldloca.s   V_0
  IL_0007:  initobj    ""T""
  IL_000d:  ldloc.0
  IL_000e:  box        ""T""
  IL_0013:  brtrue.s   IL_0018
  IL_0015:  ldnull
  IL_0016:  br.s       IL_0025
  IL_0018:  ldloca.s   V_0
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""string object.ToString()""
  IL_0025:  call       ""string string.Concat(string, string)""
  IL_002a:  call       ""void System.Console.WriteLine(string)""
  IL_002f:  ldstr      ""A""
  IL_0034:  ldloca.s   V_0
  IL_0036:  initobj    ""T""
  IL_003c:  ldloc.0
  IL_003d:  box        ""T""
  IL_0042:  brtrue.s   IL_0047
  IL_0044:  ldnull
  IL_0045:  br.s       IL_0054
  IL_0047:  ldloca.s   V_0
  IL_0049:  constrained. ""T""
  IL_004f:  callvirt   ""string object.ToString()""
  IL_0054:  ldstr      ""A""
  IL_0059:  ldloca.s   V_0
  IL_005b:  initobj    ""T""
  IL_0061:  ldloc.0
  IL_0062:  box        ""T""
  IL_0067:  brtrue.s   IL_006c
  IL_0069:  ldnull
  IL_006a:  br.s       IL_0079
  IL_006c:  ldloca.s   V_0
  IL_006e:  constrained. ""T""
  IL_0074:  callvirt   ""string object.ToString()""
  IL_0079:  call       ""string string.Concat(string, string, string, string)""
  IL_007e:  call       ""void System.Console.WriteLine(string)""
  IL_0083:  ldloca.s   V_0
  IL_0085:  initobj    ""T""
  IL_008b:  ldloc.0
  IL_008c:  box        ""T""
  IL_0091:  brtrue.s   IL_0096
  IL_0093:  ldnull
  IL_0094:  br.s       IL_00a3
  IL_0096:  ldloca.s   V_0
  IL_0098:  constrained. ""T""
  IL_009e:  callvirt   ""string object.ToString()""
  IL_00a3:  ldstr      ""B""
  IL_00a8:  call       ""string string.Concat(string, string)""
  IL_00ad:  call       ""void System.Console.WriteLine(string)""
  IL_00b2:  ldloca.s   V_0
  IL_00b4:  initobj    ""T""
  IL_00ba:  ldloc.0
  IL_00bb:  box        ""T""
  IL_00c0:  brtrue.s   IL_00c5
  IL_00c2:  ldnull
  IL_00c3:  br.s       IL_00d2
  IL_00c5:  ldloca.s   V_0
  IL_00c7:  constrained. ""T""
  IL_00cd:  callvirt   ""string object.ToString()""
  IL_00d2:  dup
  IL_00d3:  brtrue.s   IL_00db
  IL_00d5:  pop
  IL_00d6:  ldstr      """"
  IL_00db:  call       ""void System.Console.WriteLine(string)""
  IL_00e0:  ldstr      ""#""
  IL_00e5:  call       ""void System.Console.WriteLine(string)""
  IL_00ea:  ldloca.s   V_0
  IL_00ec:  initobj    ""T""
  IL_00f2:  ldloc.0
  IL_00f3:  box        ""T""
  IL_00f8:  brtrue.s   IL_00fd
  IL_00fa:  ldnull
  IL_00fb:  br.s       IL_010a
  IL_00fd:  ldloca.s   V_0
  IL_00ff:  constrained. ""T""
  IL_0105:  callvirt   ""string object.ToString()""
  IL_010a:  dup
  IL_010b:  brtrue.s   IL_0113
  IL_010d:  pop
  IL_010e:  ldstr      """"
  IL_0113:  call       ""void System.Console.WriteLine(string)""
  IL_0118:  ldstr      ""#""
  IL_011d:  call       ""void System.Console.WriteLine(string)""
  IL_0122:  ret
}
");
        }

        [Fact]
        public void ConcatGenericConstrained()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        TestMethod<Exception, Exception>();
    }

    private static void TestMethod<T, U>() where T:class where U: class
    {
        Console.WriteLine(""A"" + default(T));
        Console.WriteLine(""A"" + default(T) + ""A"" + default(T));

        Console.WriteLine(default(T) + ""B"");

        Console.WriteLine(default(string) + default(T));
        Console.WriteLine(""#"");
        Console.WriteLine(default(T) + default(string));
        Console.WriteLine(""#"");

        Console.WriteLine(""A"" + (U)null);
        Console.WriteLine(""A"" + (U)null + ""A"" + (U)null);

        Console.WriteLine((U)null + ""B"");

        Console.WriteLine(default(string) + (U)null);
        Console.WriteLine(""#"");
        Console.WriteLine((U)null + default(string));
        Console.WriteLine(""#"");
    }
}

";
            var comp = CompileAndVerify(source, expectedOutput: @"A
AA
B

#

#
A
AA
B

#

#");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.TestMethod<T, U>()", @"
{
  // Code size      141 (0x8d)
  .maxstack  1
  IL_0000:  ldstr      ""A""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""AA""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""B""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ldstr      """"
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  ldstr      ""#""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ldstr      """"
  IL_0037:  call       ""void System.Console.WriteLine(string)""
  IL_003c:  ldstr      ""#""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldstr      ""A""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ldstr      ""AA""
  IL_0055:  call       ""void System.Console.WriteLine(string)""
  IL_005a:  ldstr      ""B""
  IL_005f:  call       ""void System.Console.WriteLine(string)""
  IL_0064:  ldstr      """"
  IL_0069:  call       ""void System.Console.WriteLine(string)""
  IL_006e:  ldstr      ""#""
  IL_0073:  call       ""void System.Console.WriteLine(string)""
  IL_0078:  ldstr      """"
  IL_007d:  call       ""void System.Console.WriteLine(string)""
  IL_0082:  ldstr      ""#""
  IL_0087:  call       ""void System.Console.WriteLine(string)""
  IL_008c:  ret
}
");
        }

        [Fact]
        public void ConcatGenericUnconstrained()
        {
            var source = @"
using System;
class Test
{
    static void Main()
    {
        var p1 = new Printer<string>(""F"");
        p1.Print(""P"");
        p1.Print(null);
        var p2 = new Printer<string>(null);
        p2.Print(""P"");
        p2.Print(null);
        var p3 = new Printer<MutableStruct>(new MutableStruct());
        MutableStruct m = new MutableStruct();
        p3.Print(m);
        p3.Print(m);
    }
}

class Printer<T>
{
    private T field;
    public Printer(T field) => this.field = field;
    public void Print(T p)
    {
        Console.WriteLine("""" + p + p + field + field);
    }
}

struct MutableStruct
{
    private int i;
    public override string ToString() => (++i).ToString();
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"PPFF
FF
PP

1111
1111");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Printer<T>.Print", @"
{
  // Code size      125 (0x7d)
  .maxstack  4
  .locals init (T V_0)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        ""T""
  IL_0008:  brtrue.s   IL_000d
  IL_000a:  ldnull
  IL_000b:  br.s       IL_001a
  IL_000d:  ldloca.s   V_0
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""string object.ToString()""
  IL_001a:  ldarg.1
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  box        ""T""
  IL_0022:  brtrue.s   IL_0027
  IL_0024:  ldnull
  IL_0025:  br.s       IL_0034
  IL_0027:  ldloca.s   V_0
  IL_0029:  constrained. ""T""
  IL_002f:  callvirt   ""string object.ToString()""
  IL_0034:  ldarg.0
  IL_0035:  ldfld      ""T Printer<T>.field""
  IL_003a:  stloc.0
  IL_003b:  ldloc.0
  IL_003c:  box        ""T""
  IL_0041:  brtrue.s   IL_0046
  IL_0043:  ldnull
  IL_0044:  br.s       IL_0053
  IL_0046:  ldloca.s   V_0
  IL_0048:  constrained. ""T""
  IL_004e:  callvirt   ""string object.ToString()""
  IL_0053:  ldarg.0
  IL_0054:  ldfld      ""T Printer<T>.field""
  IL_0059:  stloc.0
  IL_005a:  ldloc.0
  IL_005b:  box        ""T""
  IL_0060:  brtrue.s   IL_0065
  IL_0062:  ldnull
  IL_0063:  br.s       IL_0072
  IL_0065:  ldloca.s   V_0
  IL_0067:  constrained. ""T""
  IL_006d:  callvirt   ""string object.ToString()""
  IL_0072:  call       ""string string.Concat(string, string, string, string)""
  IL_0077:  call       ""void System.Console.WriteLine(string)""
  IL_007c:  ret
}
");
        }

        [Fact]
        public void ConcatGenericConstrainedClass()
        {
            var source = @"
using System;
class Test
{
    static void Main()
    {
        var p1 = new Printer<string>(""F"");
        p1.Print(""P"");
        p1.Print(null);
        var p2 = new Printer<string>(null);
        p2.Print(""P"");
        p2.Print(null);
    }
}

class Printer<T> where T : class
{
    private T field;
    public Printer(T field) => this.field = field;
    public void Print(T p)
    {
        Console.WriteLine("""" + p + p + field + field);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"PPFF
FF
PP
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Printer<T>.Print", @"
{
  // Code size       93 (0x5d)
  .maxstack  5
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000d
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  br.s       IL_0012
  IL_000d:  callvirt   ""string object.ToString()""
  IL_0012:  ldarg.1
  IL_0013:  box        ""T""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_001f
  IL_001b:  pop
  IL_001c:  ldnull
  IL_001d:  br.s       IL_0024
  IL_001f:  callvirt   ""string object.ToString()""
  IL_0024:  ldarg.0
  IL_0025:  ldfld      ""T Printer<T>.field""
  IL_002a:  box        ""T""
  IL_002f:  dup
  IL_0030:  brtrue.s   IL_0036
  IL_0032:  pop
  IL_0033:  ldnull
  IL_0034:  br.s       IL_003b
  IL_0036:  callvirt   ""string object.ToString()""
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""T Printer<T>.field""
  IL_0041:  box        ""T""
  IL_0046:  dup
  IL_0047:  brtrue.s   IL_004d
  IL_0049:  pop
  IL_004a:  ldnull
  IL_004b:  br.s       IL_0052
  IL_004d:  callvirt   ""string object.ToString()""
  IL_0052:  call       ""string string.Concat(string, string, string, string)""
  IL_0057:  call       ""void System.Console.WriteLine(string)""
  IL_005c:  ret
}
");

        }

        [Fact]
        public void ConcatGenericConstrainedStruct()
        {
            var source = @"
using System;
class Test
{
    static void Main()
    {
        MutableStruct m = new MutableStruct();
        var p1 = new Printer<MutableStruct>(new MutableStruct());
        p1.Print(m);
        p1.Print(m);
    }
}

class Printer<T> where T : struct
{
    private T field;
    public Printer(T field) => this.field = field;
    public void Print(T p)
    {
        Console.WriteLine("""" + p + p + field + field);
    }
}

struct MutableStruct
{
    private int i;
    public override string ToString() => (++i).ToString();
}";

            var comp = CompileAndVerify(source, expectedOutput: @"1111
1111");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Printer<T>.Print", @"
{
  // Code size       81 (0x51)
  .maxstack  4
  .locals init (T V_0)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""string object.ToString()""
  IL_000f:  ldarg.1
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  constrained. ""T""
  IL_0019:  callvirt   ""string object.ToString()""
  IL_001e:  ldarg.0
  IL_001f:  ldfld      ""T Printer<T>.field""
  IL_0024:  stloc.0
  IL_0025:  ldloca.s   V_0
  IL_0027:  constrained. ""T""
  IL_002d:  callvirt   ""string object.ToString()""
  IL_0032:  ldarg.0
  IL_0033:  ldfld      ""T Printer<T>.field""
  IL_0038:  stloc.0
  IL_0039:  ldloca.s   V_0
  IL_003b:  constrained. ""T""
  IL_0041:  callvirt   ""string object.ToString()""
  IL_0046:  call       ""string string.Concat(string, string, string, string)""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ret
}
");

        }

        [Fact]
        public void ConcatWithOtherOptimizations()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {        
        var expr1 = ""hi"";
        var expr2 = ""bye"";

        // expr1 is optimized away 
        // only expr2 should be lifted!! 
        Func<string> f = () => (""abc"" + ""def"" + null ?? expr1 + ""moo"" + ""baz"") + expr2;

        System.Console.WriteLine(f());
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"abcdefbye");

            comp.VerifyDiagnostics();

            // IMPORTANT!!  only  c__DisplayClass0.expr2  should be initialized,
            //              there should not be such thing as c__DisplayClass0.expr1
            comp.VerifyIL("Test.Main", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""Test.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldstr      ""bye""
  IL_000b:  stfld      ""string Test.<>c__DisplayClass0_0.expr2""
  IL_0010:  ldftn      ""string Test.<>c__DisplayClass0_0.<Main>b__0()""
  IL_0016:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_001b:  callvirt   ""string System.Func<string>.Invoke()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ret
}
");
        }

        [Fact, WorkItem(1092853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092853")]
        public void ConcatWithNullCoalescedNullLiteral()
        {
            const string source = @"
class Repro
{
    static string Bug(string s)
    {
        string x = """";
        x += s ?? null;
        return x;
    }

    static void Main()
    {
        System.Console.Write(""\""{0}\"""", Bug(null));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "\"\"");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Repro.Bug", @"
{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldstr      """"
  IL_0005:  ldarg.0
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  call       ""string string.Concat(string, string)""
  IL_0010:  ret
}
");
        }

        [Fact, WorkItem(1092853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092853")]
        public void ConcatWithNullCoalescedNullLiteral_2()
        {
            const string source = @"
class Repro
{
    static string Bug(string s)
    {
        string x = """";
        x += s ?? ((string)null ?? null);
        return x;
    }

    static void Main()
    {
        System.Console.Write(""\""{0}\"""", Bug(null));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "\"\"");

            comp.VerifyIL("Repro.Bug", @"
{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldstr      """"
  IL_0005:  ldarg.0
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  call       ""string string.Concat(string, string)""
  IL_0010:  ret
}
");
        }

        [Fact]
        public void ConcatMutableStruct()
        {
            var source = @"
using System;
class Test
{
    static MutableStruct f = new MutableStruct();

    static void Main()
    {
        MutableStruct l = new MutableStruct();

        Console.WriteLine("""" + l + l + f + f);
    }
}

struct MutableStruct
{
    private int i;
    public override string ToString() => (++i).ToString();
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"1111");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       87 (0x57)
  .maxstack  4
  .locals init (MutableStruct V_0, //l
                MutableStruct V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""MutableStruct""
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  constrained. ""MutableStruct""
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  ldloc.0
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_1
  IL_001b:  constrained. ""MutableStruct""
  IL_0021:  callvirt   ""string object.ToString()""
  IL_0026:  ldsfld     ""MutableStruct Test.f""
  IL_002b:  stloc.1
  IL_002c:  ldloca.s   V_1
  IL_002e:  constrained. ""MutableStruct""
  IL_0034:  callvirt   ""string object.ToString()""
  IL_0039:  ldsfld     ""MutableStruct Test.f""
  IL_003e:  stloc.1
  IL_003f:  ldloca.s   V_1
  IL_0041:  constrained. ""MutableStruct""
  IL_0047:  callvirt   ""string object.ToString()""
  IL_004c:  call       ""string string.Concat(string, string, string, string)""
  IL_0051:  call       ""void System.Console.WriteLine(string)""
  IL_0056:  ret
}");
        }

        [Fact]
        public void ConcatMutableStructsSideEffects()
        {
            const string source = @"
using System;
using static System.Console;

struct Mutable
{
    int x;
    public override string ToString() => (x++).ToString();
}

class Test
{
    static Mutable m = new Mutable();

    static void Main()
    {
        Write(""("" + m + "")"");               // (0)
        Write(""("" + m + "")"");               // (0)

        Write(""("" + m.ToString() + "")"");    // (0)
        Write(""("" + m.ToString() + "")"");    // (1)
        Write(""("" + m.ToString() + "")"");    // (2)

        Nullable<Mutable> n = new Mutable();
        Write(""("" + n + "")"");               // (0)
        Write(""("" + n + "")"");               // (0)

        Write(""("" + n.ToString() + "")"");    // (0)
        Write(""("" + n.ToString() + "")"");    // (1)
        Write(""("" + n.ToString() + "")"");    // (2)
    }
}";

            CompileAndVerify(source, expectedOutput: "(0)(0)(0)(1)(2)(0)(0)(0)(1)(2)");
        }

        [Fact]
        public void ConcatReadonlyStruct()
        {
            var source = @"
using System;
class Test
{
    static ReadonlyStruct f = new ReadonlyStruct();

    static void Main()
    {
        ReadonlyStruct l = new ReadonlyStruct();

        Console.WriteLine("""" + l + l + f + f);
    }
}

readonly struct ReadonlyStruct
{
    public override string ToString() => ""R"";
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"RRRR");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       77 (0x4d)
  .maxstack  4
  .locals init (ReadonlyStruct V_0) //l
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""ReadonlyStruct""
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. ""ReadonlyStruct""
  IL_0010:  callvirt   ""string object.ToString()""
  IL_0015:  ldloca.s   V_0
  IL_0017:  constrained. ""ReadonlyStruct""
  IL_001d:  callvirt   ""string object.ToString()""
  IL_0022:  ldsflda    ""ReadonlyStruct Test.f""
  IL_0027:  constrained. ""ReadonlyStruct""
  IL_002d:  callvirt   ""string object.ToString()""
  IL_0032:  ldsflda    ""ReadonlyStruct Test.f""
  IL_0037:  constrained. ""ReadonlyStruct""
  IL_003d:  callvirt   ""string object.ToString()""
  IL_0042:  call       ""string string.Concat(string, string, string, string)""
  IL_0047:  call       ""void System.Console.WriteLine(string)""
  IL_004c:  ret
}
");
        }

        [Fact]
        public void ConcatStructWithReadonlyToString()
        {
            var source = @"
using System;
class Test
{
    static StructWithReadonlyToString f = new StructWithReadonlyToString();

    static void Main()
    {
        StructWithReadonlyToString l = new StructWithReadonlyToString();

        Console.WriteLine("""" + l + l + f + f);
    }
}

struct StructWithReadonlyToString
{
    public readonly override string ToString() => ""R"";
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"RRRR");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       77 (0x4d)
  .maxstack  4
  .locals init (StructWithReadonlyToString V_0) //l
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""StructWithReadonlyToString""
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. ""StructWithReadonlyToString""
  IL_0010:  callvirt   ""string object.ToString()""
  IL_0015:  ldloca.s   V_0
  IL_0017:  constrained. ""StructWithReadonlyToString""
  IL_001d:  callvirt   ""string object.ToString()""
  IL_0022:  ldsflda    ""StructWithReadonlyToString Test.f""
  IL_0027:  constrained. ""StructWithReadonlyToString""
  IL_002d:  callvirt   ""string object.ToString()""
  IL_0032:  ldsflda    ""StructWithReadonlyToString Test.f""
  IL_0037:  constrained. ""StructWithReadonlyToString""
  IL_003d:  callvirt   ""string object.ToString()""
  IL_0042:  call       ""string string.Concat(string, string, string, string)""
  IL_0047:  call       ""void System.Console.WriteLine(string)""
  IL_004c:  ret
}
");
        }

        [Fact]
        public void ConcatStructWithNoToString()
        {
            var source = @"
using System;
class Test
{
    static S f = new S();

    static void Main()
    {
        S l = new S();

        Console.WriteLine("""" + l + l + f + f);
    }
}

struct S { }
";

            var comp = CompileAndVerify(source, expectedOutput: @"SSSS");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       77 (0x4d)
  .maxstack  4
  .locals init (S V_0) //l
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. ""S""
  IL_0010:  callvirt   ""string object.ToString()""
  IL_0015:  ldloca.s   V_0
  IL_0017:  constrained. ""S""
  IL_001d:  callvirt   ""string object.ToString()""
  IL_0022:  ldsflda    ""S Test.f""
  IL_0027:  constrained. ""S""
  IL_002d:  callvirt   ""string object.ToString()""
  IL_0032:  ldsflda    ""S Test.f""
  IL_0037:  constrained. ""S""
  IL_003d:  callvirt   ""string object.ToString()""
  IL_0042:  call       ""string string.Concat(string, string, string, string)""
  IL_0047:  call       ""void System.Console.WriteLine(string)""
  IL_004c:  ret
}
");
        }

        [Fact]
        public void ConcatWithImplicitOperator()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        Console.WriteLine(""S"" + new Test());
    }

    public static implicit operator string(Test test) => ""T"";
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"ST");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldstr      ""S""
  IL_0005:  newobj     ""Test..ctor()""
  IL_000a:  call       ""string Test.op_Implicit(Test)""
  IL_000f:  call       ""string string.Concat(string, string)""
  IL_0014:  call       ""void System.Console.WriteLine(string)""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void ConcatWithNull()
        {
            var source = @"
using System;

public class Test
{
    public static Test T = null;

    static void Main()
    {
        Console.WriteLine(""S"" + T);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"S");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldstr      ""S""
  IL_0005:  ldsfld     ""Test Test.T""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0011
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  br.s       IL_0016
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  call       ""string string.Concat(string, string)""
  IL_001b:  call       ""void System.Console.WriteLine(string)""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void ConcatWithSpecialValueTypes()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        const char a = 'a', b = 'b';
        char c = 'c', d = 'd';

        Console.WriteLine(a + ""1"");
        Console.WriteLine(""2"" + b);

        Console.WriteLine(c + ""3"");
        Console.WriteLine(""4"" + d);
        
        Console.WriteLine(true + ""5"" + c);
        Console.WriteLine(""6"" + d + (IntPtr)7);
        Console.WriteLine(""8"" + (UIntPtr)9 + false);

        Console.WriteLine(c + ""10"" + d + ""11"");
        Console.WriteLine(""12"" + c + ""13"" + d);

        Console.WriteLine(a + ""14"" + b + ""15"" + a + ""16"");
        Console.WriteLine(c + ""17"" + d + ""18"" + c + ""19"");

        Console.WriteLine(""20"" + 21 + c + d + c + d);
        Console.WriteLine(""22"" + c + ""23"" + d + c + d);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"a1
2b
c3
4d
True5c
6d7
89False
c10d11
12c13d
a14b15a16
c17d18c19
2021cdcd
22c23dcd");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      477 (0x1dd)
  .maxstack  4
  .locals init (char V_0, //c
                char V_1, //d
                bool V_2,
                System.IntPtr V_3,
                System.UIntPtr V_4,
                int V_5)
  IL_0000:  ldc.i4.s   99
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.s   100
  IL_0005:  stloc.1
  IL_0006:  ldstr      ""a1""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  ldstr      ""2b""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  call       ""string char.ToString()""
  IL_0021:  ldstr      ""3""
  IL_0026:  call       ""string string.Concat(string, string)""
  IL_002b:  call       ""void System.Console.WriteLine(string)""
  IL_0030:  ldstr      ""4""
  IL_0035:  ldloca.s   V_1
  IL_0037:  call       ""string char.ToString()""
  IL_003c:  call       ""string string.Concat(string, string)""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldc.i4.1
  IL_0047:  stloc.2
  IL_0048:  ldloca.s   V_2
  IL_004a:  call       ""string bool.ToString()""
  IL_004f:  ldstr      ""5""
  IL_0054:  ldloca.s   V_0
  IL_0056:  call       ""string char.ToString()""
  IL_005b:  call       ""string string.Concat(string, string, string)""
  IL_0060:  call       ""void System.Console.WriteLine(string)""
  IL_0065:  ldstr      ""6""
  IL_006a:  ldloca.s   V_1
  IL_006c:  call       ""string char.ToString()""
  IL_0071:  ldc.i4.7
  IL_0072:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0077:  stloc.3
  IL_0078:  ldloca.s   V_3
  IL_007a:  call       ""string System.IntPtr.ToString()""
  IL_007f:  call       ""string string.Concat(string, string, string)""
  IL_0084:  call       ""void System.Console.WriteLine(string)""
  IL_0089:  ldstr      ""8""
  IL_008e:  ldc.i4.s   9
  IL_0090:  conv.i8
  IL_0091:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_0096:  stloc.s    V_4
  IL_0098:  ldloca.s   V_4
  IL_009a:  call       ""string System.UIntPtr.ToString()""
  IL_009f:  ldc.i4.0
  IL_00a0:  stloc.2
  IL_00a1:  ldloca.s   V_2
  IL_00a3:  call       ""string bool.ToString()""
  IL_00a8:  call       ""string string.Concat(string, string, string)""
  IL_00ad:  call       ""void System.Console.WriteLine(string)""
  IL_00b2:  ldloca.s   V_0
  IL_00b4:  call       ""string char.ToString()""
  IL_00b9:  ldstr      ""10""
  IL_00be:  ldloca.s   V_1
  IL_00c0:  call       ""string char.ToString()""
  IL_00c5:  ldstr      ""11""
  IL_00ca:  call       ""string string.Concat(string, string, string, string)""
  IL_00cf:  call       ""void System.Console.WriteLine(string)""
  IL_00d4:  ldstr      ""12""
  IL_00d9:  ldloca.s   V_0
  IL_00db:  call       ""string char.ToString()""
  IL_00e0:  ldstr      ""13""
  IL_00e5:  ldloca.s   V_1
  IL_00e7:  call       ""string char.ToString()""
  IL_00ec:  call       ""string string.Concat(string, string, string, string)""
  IL_00f1:  call       ""void System.Console.WriteLine(string)""
  IL_00f6:  ldstr      ""a14b15a16""
  IL_00fb:  call       ""void System.Console.WriteLine(string)""
  IL_0100:  ldc.i4.6
  IL_0101:  newarr     ""string""
  IL_0106:  dup
  IL_0107:  ldc.i4.0
  IL_0108:  ldloca.s   V_0
  IL_010a:  call       ""string char.ToString()""
  IL_010f:  stelem.ref
  IL_0110:  dup
  IL_0111:  ldc.i4.1
  IL_0112:  ldstr      ""17""
  IL_0117:  stelem.ref
  IL_0118:  dup
  IL_0119:  ldc.i4.2
  IL_011a:  ldloca.s   V_1
  IL_011c:  call       ""string char.ToString()""
  IL_0121:  stelem.ref
  IL_0122:  dup
  IL_0123:  ldc.i4.3
  IL_0124:  ldstr      ""18""
  IL_0129:  stelem.ref
  IL_012a:  dup
  IL_012b:  ldc.i4.4
  IL_012c:  ldloca.s   V_0
  IL_012e:  call       ""string char.ToString()""
  IL_0133:  stelem.ref
  IL_0134:  dup
  IL_0135:  ldc.i4.5
  IL_0136:  ldstr      ""19""
  IL_013b:  stelem.ref
  IL_013c:  call       ""string string.Concat(params string[])""
  IL_0141:  call       ""void System.Console.WriteLine(string)""
  IL_0146:  ldc.i4.6
  IL_0147:  newarr     ""string""
  IL_014c:  dup
  IL_014d:  ldc.i4.0
  IL_014e:  ldstr      ""20""
  IL_0153:  stelem.ref
  IL_0154:  dup
  IL_0155:  ldc.i4.1
  IL_0156:  ldc.i4.s   21
  IL_0158:  stloc.s    V_5
  IL_015a:  ldloca.s   V_5
  IL_015c:  call       ""string int.ToString()""
  IL_0161:  stelem.ref
  IL_0162:  dup
  IL_0163:  ldc.i4.2
  IL_0164:  ldloca.s   V_0
  IL_0166:  call       ""string char.ToString()""
  IL_016b:  stelem.ref
  IL_016c:  dup
  IL_016d:  ldc.i4.3
  IL_016e:  ldloca.s   V_1
  IL_0170:  call       ""string char.ToString()""
  IL_0175:  stelem.ref
  IL_0176:  dup
  IL_0177:  ldc.i4.4
  IL_0178:  ldloca.s   V_0
  IL_017a:  call       ""string char.ToString()""
  IL_017f:  stelem.ref
  IL_0180:  dup
  IL_0181:  ldc.i4.5
  IL_0182:  ldloca.s   V_1
  IL_0184:  call       ""string char.ToString()""
  IL_0189:  stelem.ref
  IL_018a:  call       ""string string.Concat(params string[])""
  IL_018f:  call       ""void System.Console.WriteLine(string)""
  IL_0194:  ldc.i4.6
  IL_0195:  newarr     ""string""
  IL_019a:  dup
  IL_019b:  ldc.i4.0
  IL_019c:  ldstr      ""22""
  IL_01a1:  stelem.ref
  IL_01a2:  dup
  IL_01a3:  ldc.i4.1
  IL_01a4:  ldloca.s   V_0
  IL_01a6:  call       ""string char.ToString()""
  IL_01ab:  stelem.ref
  IL_01ac:  dup
  IL_01ad:  ldc.i4.2
  IL_01ae:  ldstr      ""23""
  IL_01b3:  stelem.ref
  IL_01b4:  dup
  IL_01b5:  ldc.i4.3
  IL_01b6:  ldloca.s   V_1
  IL_01b8:  call       ""string char.ToString()""
  IL_01bd:  stelem.ref
  IL_01be:  dup
  IL_01bf:  ldc.i4.4
  IL_01c0:  ldloca.s   V_0
  IL_01c2:  call       ""string char.ToString()""
  IL_01c7:  stelem.ref
  IL_01c8:  dup
  IL_01c9:  ldc.i4.5
  IL_01ca:  ldloca.s   V_1
  IL_01cc:  call       ""string char.ToString()""
  IL_01d1:  stelem.ref
  IL_01d2:  call       ""string string.Concat(params string[])""
  IL_01d7:  call       ""void System.Console.WriteLine(string)""
  IL_01dc:  ret
}
");
        }

        [Fact]
        public void ConcatExpressions()
        {
            var source = @"
using System;

class Test
{
    static int X = 3;
    static int Y = 4;

    static void Main()
    {
        Console.WriteLine(X + ""+"" + Y + ""="" + (X + Y));
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "3+4=7");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       81 (0x51)
  .maxstack  5
  .locals init (int V_0)
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldsflda    ""int Test.X""
  IL_000d:  call       ""string int.ToString()""
  IL_0012:  stelem.ref
  IL_0013:  dup
  IL_0014:  ldc.i4.1
  IL_0015:  ldstr      ""+""
  IL_001a:  stelem.ref
  IL_001b:  dup
  IL_001c:  ldc.i4.2
  IL_001d:  ldsflda    ""int Test.Y""
  IL_0022:  call       ""string int.ToString()""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.3
  IL_002a:  ldstr      ""=""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.4
  IL_0032:  ldsfld     ""int Test.X""
  IL_0037:  ldsfld     ""int Test.Y""
  IL_003c:  add
  IL_003d:  stloc.0
  IL_003e:  ldloca.s   V_0
  IL_0040:  call       ""string int.ToString()""
  IL_0045:  stelem.ref
  IL_0046:  call       ""string string.Concat(params string[])""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ret
}");
        }

        [Fact]
        public void ConcatRefs()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        string s1 = ""S1"";
        string s2 = ""S2"";
        int i1 = 3;
        int i2 = 4;
        object o1 = ""O1"";
        object o2 = ""O2"";
        Print(ref s1, ref i1, ref o1, ref s2, ref i2, ref o2);
    }

    static void Print<T1, T2, T3>(ref string s, ref int i, ref object o, ref T1 t1, ref T2 t2, ref T3 t3)
        where T1 : class
        where T2 : struct
    {
        Console.WriteLine(s + i + o + t1 + t2 + t3);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "S13O1S24O2");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Print<T1, T2, T3>", @"
{
  // Code size      133 (0x85)
  .maxstack  5
  .locals init (T2 V_0,
                T3 V_1)
  IL_0000:  ldc.i4.6
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldarg.0
  IL_0009:  ldind.ref
  IL_000a:  stelem.ref
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldarg.1
  IL_000e:  call       ""string int.ToString()""
  IL_0013:  stelem.ref
  IL_0014:  dup
  IL_0015:  ldc.i4.2
  IL_0016:  ldarg.2
  IL_0017:  ldind.ref
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_001f
  IL_001b:  pop
  IL_001c:  ldnull
  IL_001d:  br.s       IL_0024
  IL_001f:  callvirt   ""string object.ToString()""
  IL_0024:  stelem.ref
  IL_0025:  dup
  IL_0026:  ldc.i4.3
  IL_0027:  ldarg.3
  IL_0028:  ldobj      ""T1""
  IL_002d:  box        ""T1""
  IL_0032:  dup
  IL_0033:  brtrue.s   IL_0039
  IL_0035:  pop
  IL_0036:  ldnull
  IL_0037:  br.s       IL_003e
  IL_0039:  callvirt   ""string object.ToString()""
  IL_003e:  stelem.ref
  IL_003f:  dup
  IL_0040:  ldc.i4.4
  IL_0041:  ldarg.s    V_4
  IL_0043:  ldobj      ""T2""
  IL_0048:  stloc.0
  IL_0049:  ldloca.s   V_0
  IL_004b:  constrained. ""T2""
  IL_0051:  callvirt   ""string object.ToString()""
  IL_0056:  stelem.ref
  IL_0057:  dup
  IL_0058:  ldc.i4.5
  IL_0059:  ldarg.s    V_5
  IL_005b:  ldobj      ""T3""
  IL_0060:  stloc.1
  IL_0061:  ldloc.1
  IL_0062:  box        ""T3""
  IL_0067:  brtrue.s   IL_006c
  IL_0069:  ldnull
  IL_006a:  br.s       IL_0079
  IL_006c:  ldloca.s   V_1
  IL_006e:  constrained. ""T3""
  IL_0074:  callvirt   ""string object.ToString()""
  IL_0079:  stelem.ref
  IL_007a:  call       ""string string.Concat(params string[])""
  IL_007f:  call       ""void System.Console.WriteLine(string)""
  IL_0084:  ret
}");
        }

        [Fact]
        public void ConcatNullConditionalAccesses()
        {
            var source = """
                C c = null;

                System.Console.WriteLine(string.Concat(c?.Prop, "a") + "b");
                
                class C
                {
                    public string Prop { get; }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "ab");
            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       29 (0x1d)
                  .maxstack  2
                  IL_0000:  ldnull
                  IL_0001:  dup
                  IL_0002:  brtrue.s   IL_0008
                  IL_0004:  pop
                  IL_0005:  ldnull
                  IL_0006:  br.s       IL_000d
                  IL_0008:  call       "string C.Prop.get"
                  IL_000d:  ldstr      "ab"
                  IL_0012:  call       "string string.Concat(string, string)"
                  IL_0017:  call       "void System.Console.WriteLine(string)"
                  IL_001c:  ret
                }
                """);
        }

        [Fact]
        public void CompoundAdditionDirectConcatOptimization()
        {
            var source = """
                string s1 = "a";
                string s2 = "b";
                string s3 = "c";
                string s4 = "d";

                s1 += $"{s2}{s3}{s4}";

                System.Console.WriteLine(s1);
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "abcd");
            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       37 (0x25)
                  .maxstack  4
                  .locals init (string V_0, //s2
                                string V_1, //s3
                                string V_2) //s4
                  IL_0000:  ldstr      "a"
                  IL_0005:  ldstr      "b"
                  IL_000a:  stloc.0
                  IL_000b:  ldstr      "c"
                  IL_0010:  stloc.1
                  IL_0011:  ldstr      "d"
                  IL_0016:  stloc.2
                  IL_0017:  ldloc.0
                  IL_0018:  ldloc.1
                  IL_0019:  ldloc.2
                  IL_001a:  call       "string string.Concat(string, string, string, string)"
                  IL_001f:  call       "void System.Console.WriteLine(string)"
                  IL_0024:  ret
                }
                """);
        }

        [Fact]
        public void ConstantCharPlusNull()
        {
            var source = """
                const char c = 'a';
                System.Console.WriteLine(c + (string)null);
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "a");
            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  ldstr      "a"
                  IL_0005:  call       "void System.Console.WriteLine(string)"
                  IL_000a:  ret
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80254")]
        public void CompoundAssignment_Property()
        {
            var source = """
                var c = new C();
                c.P += "a" + c.P;
                System.Console.WriteLine(c.P);

                class C
                {
                    public string P { get; set; } = "x";
                }
                """;
            CompileAndVerify(source, expectedOutput: "xax").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80254")]
        public void CompoundAssignment_RefReturningProperty()
        {
            var source = """
                var c = new C();
                c.P += "a" + c.P;
                System.Console.WriteLine(c.P);

                class C
                {
                    private string p = "x";
                    public ref string P => ref p;
                }
                """;
            CompileAndVerify(source, expectedOutput: "xax").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80254")]
        public void CompoundAssignment_ExtensionProperty()
        {
            var source = """
                var c = new C();
                c.P += "a" + c.P;
                System.Console.WriteLine(c.P);

                class C
                {
                }

                static class Ext
                {
                    private static string p = "x";
                    extension(C c)
                    {
                        public string P
                        {
                            get => p;
                            set => p = value;
                        }
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: "xax").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80254")]
        public void CompoundAssignment_ExtensionRefReturningProperty()
        {
            var source = """
                var c = new C();
                c.P += "a" + c.P;
                System.Console.WriteLine(c.P);

                class C
                {
                }

                static class Ext
                {
                    private static string p = "x";
                    extension(C c)
                    {
                        public ref string P => ref p;
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: "xax").VerifyDiagnostics();
        }
    }
}
