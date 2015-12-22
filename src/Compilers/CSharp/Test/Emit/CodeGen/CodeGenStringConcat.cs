// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
  // Code size      187 (0xbb)
  .maxstack  4
  IL_0000:  ldsfld     ""string Test.S""
  IL_0005:  ldstr      ""AB""
  IL_000a:  ldsfld     ""string Test.S""
  IL_000f:  call       ""string string.Concat(string, string, string)""
  IL_0014:  call       ""void System.Console.WriteLine(string)""
  IL_0019:  ldsfld     ""object Test.O""
  IL_001e:  ldstr      ""AB""
  IL_0023:  ldsfld     ""object Test.O""
  IL_0028:  call       ""string string.Concat(object, object, object)""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ldc.i4.6
  IL_0033:  newarr     ""object""
  IL_0038:  dup
  IL_0039:  ldc.i4.0
  IL_003a:  ldsfld     ""string Test.S""
  IL_003f:  stelem.ref
  IL_0040:  dup
  IL_0041:  ldc.i4.1
  IL_0042:  ldstr      ""AB""
  IL_0047:  stelem.ref
  IL_0048:  dup
  IL_0049:  ldc.i4.2
  IL_004a:  ldsfld     ""string Test.S""
  IL_004f:  stelem.ref
  IL_0050:  dup
  IL_0051:  ldc.i4.3
  IL_0052:  ldsfld     ""object Test.O""
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.4
  IL_005a:  ldstr      ""AB""
  IL_005f:  stelem.ref
  IL_0060:  dup
  IL_0061:  ldc.i4.5
  IL_0062:  ldsfld     ""object Test.O""
  IL_0067:  stelem.ref
  IL_0068:  call       ""string string.Concat(params object[])""
  IL_006d:  call       ""void System.Console.WriteLine(string)""
  IL_0072:  ldc.i4.7
  IL_0073:  newarr     ""object""
  IL_0078:  dup
  IL_0079:  ldc.i4.0
  IL_007a:  ldsfld     ""object Test.O""
  IL_007f:  stelem.ref
  IL_0080:  dup
  IL_0081:  ldc.i4.1
  IL_0082:  ldstr      ""A""
  IL_0087:  stelem.ref
  IL_0088:  dup
  IL_0089:  ldc.i4.2
  IL_008a:  ldsfld     ""string Test.S""
  IL_008f:  stelem.ref
  IL_0090:  dup
  IL_0091:  ldc.i4.3
  IL_0092:  ldstr      ""AB""
  IL_0097:  stelem.ref
  IL_0098:  dup
  IL_0099:  ldc.i4.4
  IL_009a:  ldsfld     ""object Test.O""
  IL_009f:  stelem.ref
  IL_00a0:  dup
  IL_00a1:  ldc.i4.5
  IL_00a2:  ldsfld     ""string Test.S""
  IL_00a7:  stelem.ref
  IL_00a8:  dup
  IL_00a9:  ldc.i4.6
  IL_00aa:  ldstr      ""A""
  IL_00af:  stelem.ref
  IL_00b0:  call       ""string string.Concat(params object[])""
  IL_00b5:  call       ""void System.Console.WriteLine(string)""
  IL_00ba:  ret
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
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"O
F");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""object Test.O""
  IL_0005:  call       ""string string.Concat(object)""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  ldsfld     ""string Test.S""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_001d
  IL_0017:  pop
  IL_0018:  ldstr      """"
  IL_001d:  call       ""void System.Console.WriteLine(string)""
  IL_0022:  ret
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
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""object Test.O""
  IL_0005:  call       ""string string.Concat(object)""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  ldsfld     ""string Test.S""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_001d
  IL_0017:  pop
  IL_0018:  ldstr      """"
  IL_001d:  call       ""void System.Console.WriteLine(string)""
  IL_0022:  ret
}
");
        }

        [Fact]
        [WorkItem(679120, "DevDiv")]
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

        [WorkItem(529064, "DevDiv")]
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
  // Code size      193 (0xc1)
  .maxstack  4
  .locals init (T V_0)
  IL_0000:  ldstr      ""A""
  IL_0005:  ldloca.s   V_0
  IL_0007:  initobj    ""T""
  IL_000d:  ldloc.0
  IL_000e:  box        ""T""
  IL_0013:  call       ""string string.Concat(object, object)""
  IL_0018:  call       ""void System.Console.WriteLine(string)""
  IL_001d:  ldc.i4.4
  IL_001e:  newarr     ""object""
  IL_0023:  dup
  IL_0024:  ldc.i4.0
  IL_0025:  ldstr      ""A""
  IL_002a:  stelem.ref
  IL_002b:  dup
  IL_002c:  ldc.i4.1
  IL_002d:  ldloca.s   V_0
  IL_002f:  initobj    ""T""
  IL_0035:  ldloc.0
  IL_0036:  box        ""T""
  IL_003b:  stelem.ref
  IL_003c:  dup
  IL_003d:  ldc.i4.2
  IL_003e:  ldstr      ""A""
  IL_0043:  stelem.ref
  IL_0044:  dup
  IL_0045:  ldc.i4.3
  IL_0046:  ldloca.s   V_0
  IL_0048:  initobj    ""T""
  IL_004e:  ldloc.0
  IL_004f:  box        ""T""
  IL_0054:  stelem.ref
  IL_0055:  call       ""string string.Concat(params object[])""
  IL_005a:  call       ""void System.Console.WriteLine(string)""
  IL_005f:  ldloca.s   V_0
  IL_0061:  initobj    ""T""
  IL_0067:  ldloc.0
  IL_0068:  box        ""T""
  IL_006d:  ldstr      ""B""
  IL_0072:  call       ""string string.Concat(object, object)""
  IL_0077:  call       ""void System.Console.WriteLine(string)""
  IL_007c:  ldloca.s   V_0
  IL_007e:  initobj    ""T""
  IL_0084:  ldloc.0
  IL_0085:  box        ""T""
  IL_008a:  call       ""string string.Concat(object)""
  IL_008f:  call       ""void System.Console.WriteLine(string)""
  IL_0094:  ldstr      ""#""
  IL_0099:  call       ""void System.Console.WriteLine(string)""
  IL_009e:  ldloca.s   V_0
  IL_00a0:  initobj    ""T""
  IL_00a6:  ldloc.0
  IL_00a7:  box        ""T""
  IL_00ac:  call       ""string string.Concat(object)""
  IL_00b1:  call       ""void System.Console.WriteLine(string)""
  IL_00b6:  ldstr      ""#""
  IL_00bb:  call       ""void System.Console.WriteLine(string)""
  IL_00c0:  ret
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

        [Fact, WorkItem(1092853, "DevDiv")]
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

        [Fact, WorkItem(1092853, "DevDiv")]
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
        public void ConcatMutableStructs()
        {
            const string source = @"
using System;
using static System.Console;

struct Mutable
{
    int x;

    public override string ToString() => (x++).ToString();

    static void Main()
    {
        Mutable m = new Mutable();
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
  // Code size      473 (0x1d9)
  .maxstack  4
  .locals init (char V_0, //c
                char V_1, //d
                bool V_2,
                System.IntPtr V_3,
                System.UIntPtr V_4)
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
  IL_0147:  newarr     ""object""
  IL_014c:  dup
  IL_014d:  ldc.i4.0
  IL_014e:  ldstr      ""20""
  IL_0153:  stelem.ref
  IL_0154:  dup
  IL_0155:  ldc.i4.1
  IL_0156:  ldc.i4.s   21
  IL_0158:  box        ""int""
  IL_015d:  stelem.ref
  IL_015e:  dup
  IL_015f:  ldc.i4.2
  IL_0160:  ldloca.s   V_0
  IL_0162:  call       ""string char.ToString()""
  IL_0167:  stelem.ref
  IL_0168:  dup
  IL_0169:  ldc.i4.3
  IL_016a:  ldloca.s   V_1
  IL_016c:  call       ""string char.ToString()""
  IL_0171:  stelem.ref
  IL_0172:  dup
  IL_0173:  ldc.i4.4
  IL_0174:  ldloca.s   V_0
  IL_0176:  call       ""string char.ToString()""
  IL_017b:  stelem.ref
  IL_017c:  dup
  IL_017d:  ldc.i4.5
  IL_017e:  ldloca.s   V_1
  IL_0180:  call       ""string char.ToString()""
  IL_0185:  stelem.ref
  IL_0186:  call       ""string string.Concat(params object[])""
  IL_018b:  call       ""void System.Console.WriteLine(string)""
  IL_0190:  ldc.i4.6
  IL_0191:  newarr     ""string""
  IL_0196:  dup
  IL_0197:  ldc.i4.0
  IL_0198:  ldstr      ""22""
  IL_019d:  stelem.ref
  IL_019e:  dup
  IL_019f:  ldc.i4.1
  IL_01a0:  ldloca.s   V_0
  IL_01a2:  call       ""string char.ToString()""
  IL_01a7:  stelem.ref
  IL_01a8:  dup
  IL_01a9:  ldc.i4.2
  IL_01aa:  ldstr      ""23""
  IL_01af:  stelem.ref
  IL_01b0:  dup
  IL_01b1:  ldc.i4.3
  IL_01b2:  ldloca.s   V_1
  IL_01b4:  call       ""string char.ToString()""
  IL_01b9:  stelem.ref
  IL_01ba:  dup
  IL_01bb:  ldc.i4.4
  IL_01bc:  ldloca.s   V_0
  IL_01be:  call       ""string char.ToString()""
  IL_01c3:  stelem.ref
  IL_01c4:  dup
  IL_01c5:  ldc.i4.5
  IL_01c6:  ldloca.s   V_1
  IL_01c8:  call       ""string char.ToString()""
  IL_01cd:  stelem.ref
  IL_01ce:  call       ""string string.Concat(params string[])""
  IL_01d3:  call       ""void System.Console.WriteLine(string)""
  IL_01d8:  ret
}
");
        }
    }
}
