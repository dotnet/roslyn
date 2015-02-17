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
  // Code size      611 (0x263)
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
  IL_001c:  constrained. ""char""
  IL_0022:  callvirt   ""string object.ToString()""
  IL_0027:  ldstr      ""3""
  IL_002c:  call       ""string string.Concat(string, string)""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ldstr      ""4""
  IL_003b:  ldloca.s   V_1
  IL_003d:  constrained. ""char""
  IL_0043:  callvirt   ""string object.ToString()""
  IL_0048:  call       ""string string.Concat(string, string)""
  IL_004d:  call       ""void System.Console.WriteLine(string)""
  IL_0052:  ldc.i4.1
  IL_0053:  stloc.2
  IL_0054:  ldloca.s   V_2
  IL_0056:  constrained. ""bool""
  IL_005c:  callvirt   ""string object.ToString()""
  IL_0061:  ldstr      ""5""
  IL_0066:  ldloca.s   V_0
  IL_0068:  constrained. ""char""
  IL_006e:  callvirt   ""string object.ToString()""
  IL_0073:  call       ""string string.Concat(string, string, string)""
  IL_0078:  call       ""void System.Console.WriteLine(string)""
  IL_007d:  ldstr      ""6""
  IL_0082:  ldloca.s   V_1
  IL_0084:  constrained. ""char""
  IL_008a:  callvirt   ""string object.ToString()""
  IL_008f:  ldc.i4.7
  IL_0090:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0095:  stloc.3
  IL_0096:  ldloca.s   V_3
  IL_0098:  constrained. ""System.IntPtr""
  IL_009e:  callvirt   ""string object.ToString()""
  IL_00a3:  call       ""string string.Concat(string, string, string)""
  IL_00a8:  call       ""void System.Console.WriteLine(string)""
  IL_00ad:  ldstr      ""8""
  IL_00b2:  ldc.i4.s   9
  IL_00b4:  conv.i8
  IL_00b5:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_00ba:  stloc.s    V_4
  IL_00bc:  ldloca.s   V_4
  IL_00be:  constrained. ""System.UIntPtr""
  IL_00c4:  callvirt   ""string object.ToString()""
  IL_00c9:  ldc.i4.0
  IL_00ca:  stloc.2
  IL_00cb:  ldloca.s   V_2
  IL_00cd:  constrained. ""bool""
  IL_00d3:  callvirt   ""string object.ToString()""
  IL_00d8:  call       ""string string.Concat(string, string, string)""
  IL_00dd:  call       ""void System.Console.WriteLine(string)""
  IL_00e2:  ldloca.s   V_0
  IL_00e4:  constrained. ""char""
  IL_00ea:  callvirt   ""string object.ToString()""
  IL_00ef:  ldstr      ""10""
  IL_00f4:  ldloca.s   V_1
  IL_00f6:  constrained. ""char""
  IL_00fc:  callvirt   ""string object.ToString()""
  IL_0101:  ldstr      ""11""
  IL_0106:  call       ""string string.Concat(string, string, string, string)""
  IL_010b:  call       ""void System.Console.WriteLine(string)""
  IL_0110:  ldstr      ""12""
  IL_0115:  ldloca.s   V_0
  IL_0117:  constrained. ""char""
  IL_011d:  callvirt   ""string object.ToString()""
  IL_0122:  ldstr      ""13""
  IL_0127:  ldloca.s   V_1
  IL_0129:  constrained. ""char""
  IL_012f:  callvirt   ""string object.ToString()""
  IL_0134:  call       ""string string.Concat(string, string, string, string)""
  IL_0139:  call       ""void System.Console.WriteLine(string)""
  IL_013e:  ldstr      ""a14b15a16""
  IL_0143:  call       ""void System.Console.WriteLine(string)""
  IL_0148:  ldc.i4.6
  IL_0149:  newarr     ""string""
  IL_014e:  dup
  IL_014f:  ldc.i4.0
  IL_0150:  ldloca.s   V_0
  IL_0152:  constrained. ""char""
  IL_0158:  callvirt   ""string object.ToString()""
  IL_015d:  stelem.ref
  IL_015e:  dup
  IL_015f:  ldc.i4.1
  IL_0160:  ldstr      ""17""
  IL_0165:  stelem.ref
  IL_0166:  dup
  IL_0167:  ldc.i4.2
  IL_0168:  ldloca.s   V_1
  IL_016a:  constrained. ""char""
  IL_0170:  callvirt   ""string object.ToString()""
  IL_0175:  stelem.ref
  IL_0176:  dup
  IL_0177:  ldc.i4.3
  IL_0178:  ldstr      ""18""
  IL_017d:  stelem.ref
  IL_017e:  dup
  IL_017f:  ldc.i4.4
  IL_0180:  ldloca.s   V_0
  IL_0182:  constrained. ""char""
  IL_0188:  callvirt   ""string object.ToString()""
  IL_018d:  stelem.ref
  IL_018e:  dup
  IL_018f:  ldc.i4.5
  IL_0190:  ldstr      ""19""
  IL_0195:  stelem.ref
  IL_0196:  call       ""string string.Concat(params string[])""
  IL_019b:  call       ""void System.Console.WriteLine(string)""
  IL_01a0:  ldc.i4.6
  IL_01a1:  newarr     ""object""
  IL_01a6:  dup
  IL_01a7:  ldc.i4.0
  IL_01a8:  ldstr      ""20""
  IL_01ad:  stelem.ref
  IL_01ae:  dup
  IL_01af:  ldc.i4.1
  IL_01b0:  ldc.i4.s   21
  IL_01b2:  box        ""int""
  IL_01b7:  stelem.ref
  IL_01b8:  dup
  IL_01b9:  ldc.i4.2
  IL_01ba:  ldloca.s   V_0
  IL_01bc:  constrained. ""char""
  IL_01c2:  callvirt   ""string object.ToString()""
  IL_01c7:  stelem.ref
  IL_01c8:  dup
  IL_01c9:  ldc.i4.3
  IL_01ca:  ldloca.s   V_1
  IL_01cc:  constrained. ""char""
  IL_01d2:  callvirt   ""string object.ToString()""
  IL_01d7:  stelem.ref
  IL_01d8:  dup
  IL_01d9:  ldc.i4.4
  IL_01da:  ldloca.s   V_0
  IL_01dc:  constrained. ""char""
  IL_01e2:  callvirt   ""string object.ToString()""
  IL_01e7:  stelem.ref
  IL_01e8:  dup
  IL_01e9:  ldc.i4.5
  IL_01ea:  ldloca.s   V_1
  IL_01ec:  constrained. ""char""
  IL_01f2:  callvirt   ""string object.ToString()""
  IL_01f7:  stelem.ref
  IL_01f8:  call       ""string string.Concat(params object[])""
  IL_01fd:  call       ""void System.Console.WriteLine(string)""
  IL_0202:  ldc.i4.6
  IL_0203:  newarr     ""string""
  IL_0208:  dup
  IL_0209:  ldc.i4.0
  IL_020a:  ldstr      ""22""
  IL_020f:  stelem.ref
  IL_0210:  dup
  IL_0211:  ldc.i4.1
  IL_0212:  ldloca.s   V_0
  IL_0214:  constrained. ""char""
  IL_021a:  callvirt   ""string object.ToString()""
  IL_021f:  stelem.ref
  IL_0220:  dup
  IL_0221:  ldc.i4.2
  IL_0222:  ldstr      ""23""
  IL_0227:  stelem.ref
  IL_0228:  dup
  IL_0229:  ldc.i4.3
  IL_022a:  ldloca.s   V_1
  IL_022c:  constrained. ""char""
  IL_0232:  callvirt   ""string object.ToString()""
  IL_0237:  stelem.ref
  IL_0238:  dup
  IL_0239:  ldc.i4.4
  IL_023a:  ldloca.s   V_0
  IL_023c:  constrained. ""char""
  IL_0242:  callvirt   ""string object.ToString()""
  IL_0247:  stelem.ref
  IL_0248:  dup
  IL_0249:  ldc.i4.5
  IL_024a:  ldloca.s   V_1
  IL_024c:  constrained. ""char""
  IL_0252:  callvirt   ""string object.ToString()""
  IL_0257:  stelem.ref
  IL_0258:  call       ""string string.Concat(params string[])""
  IL_025d:  call       ""void System.Console.WriteLine(string)""
  IL_0262:  ret
}
");
        }
    }
}
