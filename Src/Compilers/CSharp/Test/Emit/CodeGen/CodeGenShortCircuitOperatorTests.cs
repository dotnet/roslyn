// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenShortCircuitOperatorTests : CSharpTestBase
    {
        [Fact]
        public void TestShortCircuitAnd()
        {
            var source = @"
class C 
{ 
    public static bool Test(char ch, bool result)
    {
        System.Console.WriteLine(ch);
        return result;
    }

    public static void Main() 
    { 
        const bool c1 = true;
        const bool c2 = false;
        bool v1 = true;
        bool v2 = false;

        System.Console.WriteLine(true && true);
        System.Console.WriteLine(true && false);
        System.Console.WriteLine(false && true);
        System.Console.WriteLine(false && false);

        System.Console.WriteLine(c1 && c1);
        System.Console.WriteLine(c1 && c2);
        System.Console.WriteLine(c2 && c1);
        System.Console.WriteLine(c2 && c2);

        System.Console.WriteLine(v1 && v1);
        System.Console.WriteLine(v1 && v2);
        System.Console.WriteLine(v2 && v1);
        System.Console.WriteLine(v2 && v2);

        System.Console.WriteLine(Test('L', true) && Test('R', true));
        System.Console.WriteLine(Test('L', true) && Test('R', false));
        System.Console.WriteLine(Test('L', false) && Test('R', true));
        System.Console.WriteLine(Test('L', false) && Test('R', false));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
True
False
False
False
True
False
False
False
True
False
False
False
L
R
True
L
R
False
L
False
L
False
");

            compilation.VerifyIL("C.Main", @"
{
  // Code size      189 (0xbd)
  .maxstack  2
  .locals init (bool V_0, //v1
  bool V_1) //v2
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  call       ""void System.Console.WriteLine(bool)""
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""void System.Console.WriteLine(bool)""
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""void System.Console.WriteLine(bool)""
  IL_0016:  ldc.i4.0
  IL_0017:  call       ""void System.Console.WriteLine(bool)""
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""void System.Console.WriteLine(bool)""
  IL_0022:  ldc.i4.0
  IL_0023:  call       ""void System.Console.WriteLine(bool)""
  IL_0028:  ldc.i4.0
  IL_0029:  call       ""void System.Console.WriteLine(bool)""
  IL_002e:  ldc.i4.0
  IL_002f:  call       ""void System.Console.WriteLine(bool)""
  IL_0034:  ldloc.0
  IL_0035:  dup
  IL_0036:  and
  IL_0037:  call       ""void System.Console.WriteLine(bool)""
  IL_003c:  ldloc.0
  IL_003d:  ldloc.1
  IL_003e:  and
  IL_003f:  call       ""void System.Console.WriteLine(bool)""
  IL_0044:  ldloc.1
  IL_0045:  ldloc.0
  IL_0046:  and
  IL_0047:  call       ""void System.Console.WriteLine(bool)""
  IL_004c:  ldloc.1
  IL_004d:  dup
  IL_004e:  and
  IL_004f:  call       ""void System.Console.WriteLine(bool)""
  IL_0054:  ldc.i4.s   76
  IL_0056:  ldc.i4.1
  IL_0057:  call       ""bool C.Test(char, bool)""
  IL_005c:  brfalse.s  IL_0068
  IL_005e:  ldc.i4.s   82
  IL_0060:  ldc.i4.1
  IL_0061:  call       ""bool C.Test(char, bool)""
  IL_0066:  br.s       IL_0069
  IL_0068:  ldc.i4.0
  IL_0069:  call       ""void System.Console.WriteLine(bool)""
  IL_006e:  ldc.i4.s   76
  IL_0070:  ldc.i4.1
  IL_0071:  call       ""bool C.Test(char, bool)""
  IL_0076:  brfalse.s  IL_0082
  IL_0078:  ldc.i4.s   82
  IL_007a:  ldc.i4.0
  IL_007b:  call       ""bool C.Test(char, bool)""
  IL_0080:  br.s       IL_0083
  IL_0082:  ldc.i4.0
  IL_0083:  call       ""void System.Console.WriteLine(bool)""
  IL_0088:  ldc.i4.s   76
  IL_008a:  ldc.i4.0
  IL_008b:  call       ""bool C.Test(char, bool)""
  IL_0090:  brfalse.s  IL_009c
  IL_0092:  ldc.i4.s   82
  IL_0094:  ldc.i4.1
  IL_0095:  call       ""bool C.Test(char, bool)""
  IL_009a:  br.s       IL_009d
  IL_009c:  ldc.i4.0
  IL_009d:  call       ""void System.Console.WriteLine(bool)""
  IL_00a2:  ldc.i4.s   76
  IL_00a4:  ldc.i4.0
  IL_00a5:  call       ""bool C.Test(char, bool)""
  IL_00aa:  brfalse.s  IL_00b6
  IL_00ac:  ldc.i4.s   82
  IL_00ae:  ldc.i4.0
  IL_00af:  call       ""bool C.Test(char, bool)""
  IL_00b4:  br.s       IL_00b7
  IL_00b6:  ldc.i4.0
  IL_00b7:  call       ""void System.Console.WriteLine(bool)""
  IL_00bc:  ret
}");
        }

        [Fact]
        public void TestShortCircuitOr()
        {
            var source = @"
class C 
{ 
    public static bool Test(char ch, bool result)
    {
        System.Console.WriteLine(ch);
        return result;
    }

    public static void Main() 
    { 
        const bool c1 = true;
        const bool c2 = false;
        bool v1 = true;
        bool v2 = false;

        System.Console.WriteLine(true || true);
        System.Console.WriteLine(true || false);
        System.Console.WriteLine(false || true);
        System.Console.WriteLine(false || false);

        System.Console.WriteLine(c1 || c1);
        System.Console.WriteLine(c1 || c2);
        System.Console.WriteLine(c2 || c1);
        System.Console.WriteLine(c2 || c2);

        System.Console.WriteLine(v1 || v1);
        System.Console.WriteLine(v1 || v2);
        System.Console.WriteLine(v2 || v1);
        System.Console.WriteLine(v2 || v2);

        System.Console.WriteLine(Test('L', true) || Test('R', true));
        System.Console.WriteLine(Test('L', true) || Test('R', false));
        System.Console.WriteLine(Test('L', false) || Test('R', true));
        System.Console.WriteLine(Test('L', false) || Test('R', false));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
True
True
True
False
True
True
True
False
True
True
True
False
L
True
L
True
L
R
True
L
R
False
");

            compilation.VerifyIL("C.Main", @"
{
  // Code size      189 (0xbd)
  .maxstack  2
  .locals init (bool V_0, //v1
  bool V_1) //v2
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  call       ""void System.Console.WriteLine(bool)""
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void System.Console.WriteLine(bool)""
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""void System.Console.WriteLine(bool)""
  IL_0016:  ldc.i4.0
  IL_0017:  call       ""void System.Console.WriteLine(bool)""
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""void System.Console.WriteLine(bool)""
  IL_0022:  ldc.i4.1
  IL_0023:  call       ""void System.Console.WriteLine(bool)""
  IL_0028:  ldc.i4.1
  IL_0029:  call       ""void System.Console.WriteLine(bool)""
  IL_002e:  ldc.i4.0
  IL_002f:  call       ""void System.Console.WriteLine(bool)""
  IL_0034:  ldloc.0
  IL_0035:  dup
  IL_0036:  or
  IL_0037:  call       ""void System.Console.WriteLine(bool)""
  IL_003c:  ldloc.0
  IL_003d:  ldloc.1
  IL_003e:  or
  IL_003f:  call       ""void System.Console.WriteLine(bool)""
  IL_0044:  ldloc.1
  IL_0045:  ldloc.0
  IL_0046:  or
  IL_0047:  call       ""void System.Console.WriteLine(bool)""
  IL_004c:  ldloc.1
  IL_004d:  dup
  IL_004e:  or
  IL_004f:  call       ""void System.Console.WriteLine(bool)""
  IL_0054:  ldc.i4.s   76
  IL_0056:  ldc.i4.1
  IL_0057:  call       ""bool C.Test(char, bool)""
  IL_005c:  brtrue.s   IL_0068
  IL_005e:  ldc.i4.s   82
  IL_0060:  ldc.i4.1
  IL_0061:  call       ""bool C.Test(char, bool)""
  IL_0066:  br.s       IL_0069
  IL_0068:  ldc.i4.1
  IL_0069:  call       ""void System.Console.WriteLine(bool)""
  IL_006e:  ldc.i4.s   76
  IL_0070:  ldc.i4.1
  IL_0071:  call       ""bool C.Test(char, bool)""
  IL_0076:  brtrue.s   IL_0082
  IL_0078:  ldc.i4.s   82
  IL_007a:  ldc.i4.0
  IL_007b:  call       ""bool C.Test(char, bool)""
  IL_0080:  br.s       IL_0083
  IL_0082:  ldc.i4.1
  IL_0083:  call       ""void System.Console.WriteLine(bool)""
  IL_0088:  ldc.i4.s   76
  IL_008a:  ldc.i4.0
  IL_008b:  call       ""bool C.Test(char, bool)""
  IL_0090:  brtrue.s   IL_009c
  IL_0092:  ldc.i4.s   82
  IL_0094:  ldc.i4.1
  IL_0095:  call       ""bool C.Test(char, bool)""
  IL_009a:  br.s       IL_009d
  IL_009c:  ldc.i4.1
  IL_009d:  call       ""void System.Console.WriteLine(bool)""
  IL_00a2:  ldc.i4.s   76
  IL_00a4:  ldc.i4.0
  IL_00a5:  call       ""bool C.Test(char, bool)""
  IL_00aa:  brtrue.s   IL_00b6
  IL_00ac:  ldc.i4.s   82
  IL_00ae:  ldc.i4.0
  IL_00af:  call       ""bool C.Test(char, bool)""
  IL_00b4:  br.s       IL_00b7
  IL_00b6:  ldc.i4.1
  IL_00b7:  call       ""void System.Console.WriteLine(bool)""
  IL_00bc:  ret
}");
        }

        [Fact]
        public void TestChainedShortCircuitOperators()
        {
            var source = @"
class C 
{ 
    public static bool Test(char ch, bool result)
    {
        System.Console.WriteLine(ch);
        return result;
    }

    public static void Main() 
    { 
        // AND AND
        System.Console.WriteLine(Test('A', true) && Test('B', true) && Test('C' , true));
        System.Console.WriteLine(Test('A', true) && Test('B', true) && Test('C' , false));
        System.Console.WriteLine(Test('A', true) && Test('B', false) && Test('C' , true));
        System.Console.WriteLine(Test('A', true) && Test('B', false) && Test('C' , false));
        System.Console.WriteLine(Test('A', false) && Test('B', true) && Test('C' , true));
        System.Console.WriteLine(Test('A', false) && Test('B', true) && Test('C' , false));
        System.Console.WriteLine(Test('A', false) && Test('B', false) && Test('C' , true));
        System.Console.WriteLine(Test('A', false) && Test('B', false) && Test('C' , false));

        // AND OR
        System.Console.WriteLine(Test('A', true) && Test('B', true) || Test('C' , true));
        System.Console.WriteLine(Test('A', true) && Test('B', true) || Test('C' , false));
        System.Console.WriteLine(Test('A', true) && Test('B', false) || Test('C' , true));
        System.Console.WriteLine(Test('A', true) && Test('B', false) || Test('C' , false));
        System.Console.WriteLine(Test('A', false) && Test('B', true) || Test('C' , true));
        System.Console.WriteLine(Test('A', false) && Test('B', true) || Test('C' , false));
        System.Console.WriteLine(Test('A', false) && Test('B', false) || Test('C' , true));
        System.Console.WriteLine(Test('A', false) && Test('B', false) || Test('C' , false));

        // OR AND
        System.Console.WriteLine(Test('A', true) || Test('B', true) && Test('C' , true));
        System.Console.WriteLine(Test('A', true) || Test('B', true) && Test('C' , false));
        System.Console.WriteLine(Test('A', true) || Test('B', false) && Test('C' , true));
        System.Console.WriteLine(Test('A', true) || Test('B', false) && Test('C' , false));
        System.Console.WriteLine(Test('A', false) || Test('B', true) && Test('C' , true));
        System.Console.WriteLine(Test('A', false) || Test('B', true) && Test('C' , false));
        System.Console.WriteLine(Test('A', false) || Test('B', false) && Test('C' , true));
        System.Console.WriteLine(Test('A', false) || Test('B', false) && Test('C' , false));

        // OR OR
        System.Console.WriteLine(Test('A', true) || Test('B', true) || Test('C' , true));
        System.Console.WriteLine(Test('A', true) || Test('B', true) || Test('C' , false));
        System.Console.WriteLine(Test('A', true) || Test('B', false) || Test('C' , true));
        System.Console.WriteLine(Test('A', true) || Test('B', false) || Test('C' , false));
        System.Console.WriteLine(Test('A', false) || Test('B', true) || Test('C' , true));
        System.Console.WriteLine(Test('A', false) || Test('B', true) || Test('C' , false));
        System.Console.WriteLine(Test('A', false) || Test('B', false) || Test('C' , true));
        System.Console.WriteLine(Test('A', false) || Test('B', false) || Test('C' , false));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
A
B
C
True
A
B
C
False
A
B
False
A
B
False
A
False
A
False
A
False
A
False
A
B
True
A
B
True
A
B
C
True
A
B
C
False
A
C
True
A
C
False
A
C
True
A
C
False
A
True
A
True
A
True
A
True
A
B
C
True
A
B
C
False
A
B
False
A
B
False
A
True
A
True
A
True
A
True
A
B
True
A
B
True
A
B
C
True
A
B
C
False
");

            compilation.VerifyIL("C.Main", @"{
  // Code size     1177 (0x499)
  .maxstack  2
  IL_0000:  ldc.i4.s   65
  IL_0002:  ldc.i4.1  
  IL_0003:  call       ""bool C.Test(char, bool)""
  IL_0008:  brfalse.s  IL_001e
  IL_000a:  ldc.i4.s   66
  IL_000c:  ldc.i4.1  
  IL_000d:  call       ""bool C.Test(char, bool)""
  IL_0012:  brfalse.s  IL_001e
  IL_0014:  ldc.i4.s   67
  IL_0016:  ldc.i4.1  
  IL_0017:  call       ""bool C.Test(char, bool)""
  IL_001c:  br.s       IL_001f
  IL_001e:  ldc.i4.0  
  IL_001f:  call       ""void System.Console.WriteLine(bool)""
  IL_0024:  ldc.i4.s   65
  IL_0026:  ldc.i4.1  
  IL_0027:  call       ""bool C.Test(char, bool)""
  IL_002c:  brfalse.s  IL_0042
  IL_002e:  ldc.i4.s   66
  IL_0030:  ldc.i4.1  
  IL_0031:  call       ""bool C.Test(char, bool)""
  IL_0036:  brfalse.s  IL_0042
  IL_0038:  ldc.i4.s   67
  IL_003a:  ldc.i4.0  
  IL_003b:  call       ""bool C.Test(char, bool)""
  IL_0040:  br.s       IL_0043
  IL_0042:  ldc.i4.0  
  IL_0043:  call       ""void System.Console.WriteLine(bool)""
  IL_0048:  ldc.i4.s   65
  IL_004a:  ldc.i4.1  
  IL_004b:  call       ""bool C.Test(char, bool)""
  IL_0050:  brfalse.s  IL_0066
  IL_0052:  ldc.i4.s   66
  IL_0054:  ldc.i4.0  
  IL_0055:  call       ""bool C.Test(char, bool)""
  IL_005a:  brfalse.s  IL_0066
  IL_005c:  ldc.i4.s   67
  IL_005e:  ldc.i4.1  
  IL_005f:  call       ""bool C.Test(char, bool)""
  IL_0064:  br.s       IL_0067
  IL_0066:  ldc.i4.0  
  IL_0067:  call       ""void System.Console.WriteLine(bool)""
  IL_006c:  ldc.i4.s   65
  IL_006e:  ldc.i4.1  
  IL_006f:  call       ""bool C.Test(char, bool)""
  IL_0074:  brfalse.s  IL_008a
  IL_0076:  ldc.i4.s   66
  IL_0078:  ldc.i4.0  
  IL_0079:  call       ""bool C.Test(char, bool)""
  IL_007e:  brfalse.s  IL_008a
  IL_0080:  ldc.i4.s   67
  IL_0082:  ldc.i4.0  
  IL_0083:  call       ""bool C.Test(char, bool)""
  IL_0088:  br.s       IL_008b
  IL_008a:  ldc.i4.0  
  IL_008b:  call       ""void System.Console.WriteLine(bool)""
  IL_0090:  ldc.i4.s   65
  IL_0092:  ldc.i4.0  
  IL_0093:  call       ""bool C.Test(char, bool)""
  IL_0098:  brfalse.s  IL_00ae
  IL_009a:  ldc.i4.s   66
  IL_009c:  ldc.i4.1  
  IL_009d:  call       ""bool C.Test(char, bool)""
  IL_00a2:  brfalse.s  IL_00ae
  IL_00a4:  ldc.i4.s   67
  IL_00a6:  ldc.i4.1  
  IL_00a7:  call       ""bool C.Test(char, bool)""
  IL_00ac:  br.s       IL_00af
  IL_00ae:  ldc.i4.0  
  IL_00af:  call       ""void System.Console.WriteLine(bool)""
  IL_00b4:  ldc.i4.s   65
  IL_00b6:  ldc.i4.0  
  IL_00b7:  call       ""bool C.Test(char, bool)""
  IL_00bc:  brfalse.s  IL_00d2
  IL_00be:  ldc.i4.s   66
  IL_00c0:  ldc.i4.1  
  IL_00c1:  call       ""bool C.Test(char, bool)""
  IL_00c6:  brfalse.s  IL_00d2
  IL_00c8:  ldc.i4.s   67
  IL_00ca:  ldc.i4.0  
  IL_00cb:  call       ""bool C.Test(char, bool)""
  IL_00d0:  br.s       IL_00d3
  IL_00d2:  ldc.i4.0  
  IL_00d3:  call       ""void System.Console.WriteLine(bool)""
  IL_00d8:  ldc.i4.s   65
  IL_00da:  ldc.i4.0  
  IL_00db:  call       ""bool C.Test(char, bool)""
  IL_00e0:  brfalse.s  IL_00f6
  IL_00e2:  ldc.i4.s   66
  IL_00e4:  ldc.i4.0  
  IL_00e5:  call       ""bool C.Test(char, bool)""
  IL_00ea:  brfalse.s  IL_00f6
  IL_00ec:  ldc.i4.s   67
  IL_00ee:  ldc.i4.1  
  IL_00ef:  call       ""bool C.Test(char, bool)""
  IL_00f4:  br.s       IL_00f7
  IL_00f6:  ldc.i4.0  
  IL_00f7:  call       ""void System.Console.WriteLine(bool)""
  IL_00fc:  ldc.i4.s   65
  IL_00fe:  ldc.i4.0  
  IL_00ff:  call       ""bool C.Test(char, bool)""
  IL_0104:  brfalse.s  IL_011a
  IL_0106:  ldc.i4.s   66
  IL_0108:  ldc.i4.0  
  IL_0109:  call       ""bool C.Test(char, bool)""
  IL_010e:  brfalse.s  IL_011a
  IL_0110:  ldc.i4.s   67
  IL_0112:  ldc.i4.0  
  IL_0113:  call       ""bool C.Test(char, bool)""
  IL_0118:  br.s       IL_011b
  IL_011a:  ldc.i4.0  
  IL_011b:  call       ""void System.Console.WriteLine(bool)""
  IL_0120:  ldc.i4.s   65
  IL_0122:  ldc.i4.1  
  IL_0123:  call       ""bool C.Test(char, bool)""
  IL_0128:  brfalse.s  IL_0134
  IL_012a:  ldc.i4.s   66
  IL_012c:  ldc.i4.1  
  IL_012d:  call       ""bool C.Test(char, bool)""
  IL_0132:  brtrue.s   IL_013e
  IL_0134:  ldc.i4.s   67
  IL_0136:  ldc.i4.1  
  IL_0137:  call       ""bool C.Test(char, bool)""
  IL_013c:  br.s       IL_013f
  IL_013e:  ldc.i4.1  
  IL_013f:  call       ""void System.Console.WriteLine(bool)""
  IL_0144:  ldc.i4.s   65
  IL_0146:  ldc.i4.1  
  IL_0147:  call       ""bool C.Test(char, bool)""
  IL_014c:  brfalse.s  IL_0158
  IL_014e:  ldc.i4.s   66
  IL_0150:  ldc.i4.1  
  IL_0151:  call       ""bool C.Test(char, bool)""
  IL_0156:  brtrue.s   IL_0162
  IL_0158:  ldc.i4.s   67
  IL_015a:  ldc.i4.0  
  IL_015b:  call       ""bool C.Test(char, bool)""
  IL_0160:  br.s       IL_0163
  IL_0162:  ldc.i4.1  
  IL_0163:  call       ""void System.Console.WriteLine(bool)""
  IL_0168:  ldc.i4.s   65
  IL_016a:  ldc.i4.1  
  IL_016b:  call       ""bool C.Test(char, bool)""
  IL_0170:  brfalse.s  IL_017c
  IL_0172:  ldc.i4.s   66
  IL_0174:  ldc.i4.0  
  IL_0175:  call       ""bool C.Test(char, bool)""
  IL_017a:  brtrue.s   IL_0186
  IL_017c:  ldc.i4.s   67
  IL_017e:  ldc.i4.1  
  IL_017f:  call       ""bool C.Test(char, bool)""
  IL_0184:  br.s       IL_0187
  IL_0186:  ldc.i4.1  
  IL_0187:  call       ""void System.Console.WriteLine(bool)""
  IL_018c:  ldc.i4.s   65
  IL_018e:  ldc.i4.1  
  IL_018f:  call       ""bool C.Test(char, bool)""
  IL_0194:  brfalse.s  IL_01a0
  IL_0196:  ldc.i4.s   66
  IL_0198:  ldc.i4.0  
  IL_0199:  call       ""bool C.Test(char, bool)""
  IL_019e:  brtrue.s   IL_01aa
  IL_01a0:  ldc.i4.s   67
  IL_01a2:  ldc.i4.0  
  IL_01a3:  call       ""bool C.Test(char, bool)""
  IL_01a8:  br.s       IL_01ab
  IL_01aa:  ldc.i4.1  
  IL_01ab:  call       ""void System.Console.WriteLine(bool)""
  IL_01b0:  ldc.i4.s   65
  IL_01b2:  ldc.i4.0  
  IL_01b3:  call       ""bool C.Test(char, bool)""
  IL_01b8:  brfalse.s  IL_01c4
  IL_01ba:  ldc.i4.s   66
  IL_01bc:  ldc.i4.1  
  IL_01bd:  call       ""bool C.Test(char, bool)""
  IL_01c2:  brtrue.s   IL_01ce
  IL_01c4:  ldc.i4.s   67
  IL_01c6:  ldc.i4.1  
  IL_01c7:  call       ""bool C.Test(char, bool)""
  IL_01cc:  br.s       IL_01cf
  IL_01ce:  ldc.i4.1  
  IL_01cf:  call       ""void System.Console.WriteLine(bool)""
  IL_01d4:  ldc.i4.s   65
  IL_01d6:  ldc.i4.0  
  IL_01d7:  call       ""bool C.Test(char, bool)""
  IL_01dc:  brfalse.s  IL_01e8
  IL_01de:  ldc.i4.s   66
  IL_01e0:  ldc.i4.1  
  IL_01e1:  call       ""bool C.Test(char, bool)""
  IL_01e6:  brtrue.s   IL_01f2
  IL_01e8:  ldc.i4.s   67
  IL_01ea:  ldc.i4.0  
  IL_01eb:  call       ""bool C.Test(char, bool)""
  IL_01f0:  br.s       IL_01f3
  IL_01f2:  ldc.i4.1  
  IL_01f3:  call       ""void System.Console.WriteLine(bool)""
  IL_01f8:  ldc.i4.s   65
  IL_01fa:  ldc.i4.0  
  IL_01fb:  call       ""bool C.Test(char, bool)""
  IL_0200:  brfalse.s  IL_020c
  IL_0202:  ldc.i4.s   66
  IL_0204:  ldc.i4.0  
  IL_0205:  call       ""bool C.Test(char, bool)""
  IL_020a:  brtrue.s   IL_0216
  IL_020c:  ldc.i4.s   67
  IL_020e:  ldc.i4.1  
  IL_020f:  call       ""bool C.Test(char, bool)""
  IL_0214:  br.s       IL_0217
  IL_0216:  ldc.i4.1  
  IL_0217:  call       ""void System.Console.WriteLine(bool)""
  IL_021c:  ldc.i4.s   65
  IL_021e:  ldc.i4.0  
  IL_021f:  call       ""bool C.Test(char, bool)""
  IL_0224:  brfalse.s  IL_0230
  IL_0226:  ldc.i4.s   66
  IL_0228:  ldc.i4.0  
  IL_0229:  call       ""bool C.Test(char, bool)""
  IL_022e:  brtrue.s   IL_023a
  IL_0230:  ldc.i4.s   67
  IL_0232:  ldc.i4.0  
  IL_0233:  call       ""bool C.Test(char, bool)""
  IL_0238:  br.s       IL_023b
  IL_023a:  ldc.i4.1  
  IL_023b:  call       ""void System.Console.WriteLine(bool)""
  IL_0240:  ldc.i4.s   65
  IL_0242:  ldc.i4.1  
  IL_0243:  call       ""bool C.Test(char, bool)""
  IL_0248:  brtrue.s   IL_0261
  IL_024a:  ldc.i4.s   66
  IL_024c:  ldc.i4.1  
  IL_024d:  call       ""bool C.Test(char, bool)""
  IL_0252:  brfalse.s  IL_025e
  IL_0254:  ldc.i4.s   67
  IL_0256:  ldc.i4.1  
  IL_0257:  call       ""bool C.Test(char, bool)""
  IL_025c:  br.s       IL_0262
  IL_025e:  ldc.i4.0  
  IL_025f:  br.s       IL_0262
  IL_0261:  ldc.i4.1  
  IL_0262:  call       ""void System.Console.WriteLine(bool)""
  IL_0267:  ldc.i4.s   65
  IL_0269:  ldc.i4.1  
  IL_026a:  call       ""bool C.Test(char, bool)""
  IL_026f:  brtrue.s   IL_0288
  IL_0271:  ldc.i4.s   66
  IL_0273:  ldc.i4.1  
  IL_0274:  call       ""bool C.Test(char, bool)""
  IL_0279:  brfalse.s  IL_0285
  IL_027b:  ldc.i4.s   67
  IL_027d:  ldc.i4.0  
  IL_027e:  call       ""bool C.Test(char, bool)""
  IL_0283:  br.s       IL_0289
  IL_0285:  ldc.i4.0  
  IL_0286:  br.s       IL_0289
  IL_0288:  ldc.i4.1  
  IL_0289:  call       ""void System.Console.WriteLine(bool)""
  IL_028e:  ldc.i4.s   65
  IL_0290:  ldc.i4.1  
  IL_0291:  call       ""bool C.Test(char, bool)""
  IL_0296:  brtrue.s   IL_02af
  IL_0298:  ldc.i4.s   66
  IL_029a:  ldc.i4.0  
  IL_029b:  call       ""bool C.Test(char, bool)""
  IL_02a0:  brfalse.s  IL_02ac
  IL_02a2:  ldc.i4.s   67
  IL_02a4:  ldc.i4.1  
  IL_02a5:  call       ""bool C.Test(char, bool)""
  IL_02aa:  br.s       IL_02b0
  IL_02ac:  ldc.i4.0  
  IL_02ad:  br.s       IL_02b0
  IL_02af:  ldc.i4.1  
  IL_02b0:  call       ""void System.Console.WriteLine(bool)""
  IL_02b5:  ldc.i4.s   65
  IL_02b7:  ldc.i4.1  
  IL_02b8:  call       ""bool C.Test(char, bool)""
  IL_02bd:  brtrue.s   IL_02d6
  IL_02bf:  ldc.i4.s   66
  IL_02c1:  ldc.i4.0  
  IL_02c2:  call       ""bool C.Test(char, bool)""
  IL_02c7:  brfalse.s  IL_02d3
  IL_02c9:  ldc.i4.s   67
  IL_02cb:  ldc.i4.0  
  IL_02cc:  call       ""bool C.Test(char, bool)""
  IL_02d1:  br.s       IL_02d7
  IL_02d3:  ldc.i4.0  
  IL_02d4:  br.s       IL_02d7
  IL_02d6:  ldc.i4.1  
  IL_02d7:  call       ""void System.Console.WriteLine(bool)""
  IL_02dc:  ldc.i4.s   65
  IL_02de:  ldc.i4.0  
  IL_02df:  call       ""bool C.Test(char, bool)""
  IL_02e4:  brtrue.s   IL_02fd
  IL_02e6:  ldc.i4.s   66
  IL_02e8:  ldc.i4.1  
  IL_02e9:  call       ""bool C.Test(char, bool)""
  IL_02ee:  brfalse.s  IL_02fa
  IL_02f0:  ldc.i4.s   67
  IL_02f2:  ldc.i4.1  
  IL_02f3:  call       ""bool C.Test(char, bool)""
  IL_02f8:  br.s       IL_02fe
  IL_02fa:  ldc.i4.0  
  IL_02fb:  br.s       IL_02fe
  IL_02fd:  ldc.i4.1  
  IL_02fe:  call       ""void System.Console.WriteLine(bool)""
  IL_0303:  ldc.i4.s   65
  IL_0305:  ldc.i4.0  
  IL_0306:  call       ""bool C.Test(char, bool)""
  IL_030b:  brtrue.s   IL_0324
  IL_030d:  ldc.i4.s   66
  IL_030f:  ldc.i4.1  
  IL_0310:  call       ""bool C.Test(char, bool)""
  IL_0315:  brfalse.s  IL_0321
  IL_0317:  ldc.i4.s   67
  IL_0319:  ldc.i4.0  
  IL_031a:  call       ""bool C.Test(char, bool)""
  IL_031f:  br.s       IL_0325
  IL_0321:  ldc.i4.0  
  IL_0322:  br.s       IL_0325
  IL_0324:  ldc.i4.1  
  IL_0325:  call       ""void System.Console.WriteLine(bool)""
  IL_032a:  ldc.i4.s   65
  IL_032c:  ldc.i4.0  
  IL_032d:  call       ""bool C.Test(char, bool)""
  IL_0332:  brtrue.s   IL_034b
  IL_0334:  ldc.i4.s   66
  IL_0336:  ldc.i4.0  
  IL_0337:  call       ""bool C.Test(char, bool)""
  IL_033c:  brfalse.s  IL_0348
  IL_033e:  ldc.i4.s   67
  IL_0340:  ldc.i4.1  
  IL_0341:  call       ""bool C.Test(char, bool)""
  IL_0346:  br.s       IL_034c
  IL_0348:  ldc.i4.0  
  IL_0349:  br.s       IL_034c
  IL_034b:  ldc.i4.1  
  IL_034c:  call       ""void System.Console.WriteLine(bool)""
  IL_0351:  ldc.i4.s   65
  IL_0353:  ldc.i4.0  
  IL_0354:  call       ""bool C.Test(char, bool)""
  IL_0359:  brtrue.s   IL_0372
  IL_035b:  ldc.i4.s   66
  IL_035d:  ldc.i4.0  
  IL_035e:  call       ""bool C.Test(char, bool)""
  IL_0363:  brfalse.s  IL_036f
  IL_0365:  ldc.i4.s   67
  IL_0367:  ldc.i4.0  
  IL_0368:  call       ""bool C.Test(char, bool)""
  IL_036d:  br.s       IL_0373
  IL_036f:  ldc.i4.0  
  IL_0370:  br.s       IL_0373
  IL_0372:  ldc.i4.1  
  IL_0373:  call       ""void System.Console.WriteLine(bool)""
  IL_0378:  ldc.i4.s   65
  IL_037a:  ldc.i4.1  
  IL_037b:  call       ""bool C.Test(char, bool)""
  IL_0380:  brtrue.s   IL_0396
  IL_0382:  ldc.i4.s   66
  IL_0384:  ldc.i4.1  
  IL_0385:  call       ""bool C.Test(char, bool)""
  IL_038a:  brtrue.s   IL_0396
  IL_038c:  ldc.i4.s   67
  IL_038e:  ldc.i4.1  
  IL_038f:  call       ""bool C.Test(char, bool)""
  IL_0394:  br.s       IL_0397
  IL_0396:  ldc.i4.1  
  IL_0397:  call       ""void System.Console.WriteLine(bool)""
  IL_039c:  ldc.i4.s   65
  IL_039e:  ldc.i4.1  
  IL_039f:  call       ""bool C.Test(char, bool)""
  IL_03a4:  brtrue.s   IL_03ba
  IL_03a6:  ldc.i4.s   66
  IL_03a8:  ldc.i4.1  
  IL_03a9:  call       ""bool C.Test(char, bool)""
  IL_03ae:  brtrue.s   IL_03ba
  IL_03b0:  ldc.i4.s   67
  IL_03b2:  ldc.i4.0  
  IL_03b3:  call       ""bool C.Test(char, bool)""
  IL_03b8:  br.s       IL_03bb
  IL_03ba:  ldc.i4.1  
  IL_03bb:  call       ""void System.Console.WriteLine(bool)""
  IL_03c0:  ldc.i4.s   65
  IL_03c2:  ldc.i4.1  
  IL_03c3:  call       ""bool C.Test(char, bool)""
  IL_03c8:  brtrue.s   IL_03de
  IL_03ca:  ldc.i4.s   66
  IL_03cc:  ldc.i4.0  
  IL_03cd:  call       ""bool C.Test(char, bool)""
  IL_03d2:  brtrue.s   IL_03de
  IL_03d4:  ldc.i4.s   67
  IL_03d6:  ldc.i4.1  
  IL_03d7:  call       ""bool C.Test(char, bool)""
  IL_03dc:  br.s       IL_03df
  IL_03de:  ldc.i4.1  
  IL_03df:  call       ""void System.Console.WriteLine(bool)""
  IL_03e4:  ldc.i4.s   65
  IL_03e6:  ldc.i4.1  
  IL_03e7:  call       ""bool C.Test(char, bool)""
  IL_03ec:  brtrue.s   IL_0402
  IL_03ee:  ldc.i4.s   66
  IL_03f0:  ldc.i4.0  
  IL_03f1:  call       ""bool C.Test(char, bool)""
  IL_03f6:  brtrue.s   IL_0402
  IL_03f8:  ldc.i4.s   67
  IL_03fa:  ldc.i4.0  
  IL_03fb:  call       ""bool C.Test(char, bool)""
  IL_0400:  br.s       IL_0403
  IL_0402:  ldc.i4.1  
  IL_0403:  call       ""void System.Console.WriteLine(bool)""
  IL_0408:  ldc.i4.s   65
  IL_040a:  ldc.i4.0  
  IL_040b:  call       ""bool C.Test(char, bool)""
  IL_0410:  brtrue.s   IL_0426
  IL_0412:  ldc.i4.s   66
  IL_0414:  ldc.i4.1  
  IL_0415:  call       ""bool C.Test(char, bool)""
  IL_041a:  brtrue.s   IL_0426
  IL_041c:  ldc.i4.s   67
  IL_041e:  ldc.i4.1  
  IL_041f:  call       ""bool C.Test(char, bool)""
  IL_0424:  br.s       IL_0427
  IL_0426:  ldc.i4.1  
  IL_0427:  call       ""void System.Console.WriteLine(bool)""
  IL_042c:  ldc.i4.s   65
  IL_042e:  ldc.i4.0  
  IL_042f:  call       ""bool C.Test(char, bool)""
  IL_0434:  brtrue.s   IL_044a
  IL_0436:  ldc.i4.s   66
  IL_0438:  ldc.i4.1  
  IL_0439:  call       ""bool C.Test(char, bool)""
  IL_043e:  brtrue.s   IL_044a
  IL_0440:  ldc.i4.s   67
  IL_0442:  ldc.i4.0  
  IL_0443:  call       ""bool C.Test(char, bool)""
  IL_0448:  br.s       IL_044b
  IL_044a:  ldc.i4.1  
  IL_044b:  call       ""void System.Console.WriteLine(bool)""
  IL_0450:  ldc.i4.s   65
  IL_0452:  ldc.i4.0  
  IL_0453:  call       ""bool C.Test(char, bool)""
  IL_0458:  brtrue.s   IL_046e
  IL_045a:  ldc.i4.s   66
  IL_045c:  ldc.i4.0  
  IL_045d:  call       ""bool C.Test(char, bool)""
  IL_0462:  brtrue.s   IL_046e
  IL_0464:  ldc.i4.s   67
  IL_0466:  ldc.i4.1  
  IL_0467:  call       ""bool C.Test(char, bool)""
  IL_046c:  br.s       IL_046f
  IL_046e:  ldc.i4.1  
  IL_046f:  call       ""void System.Console.WriteLine(bool)""
  IL_0474:  ldc.i4.s   65
  IL_0476:  ldc.i4.0  
  IL_0477:  call       ""bool C.Test(char, bool)""
  IL_047c:  brtrue.s   IL_0492
  IL_047e:  ldc.i4.s   66
  IL_0480:  ldc.i4.0  
  IL_0481:  call       ""bool C.Test(char, bool)""
  IL_0486:  brtrue.s   IL_0492
  IL_0488:  ldc.i4.s   67
  IL_048a:  ldc.i4.0  
  IL_048b:  call       ""bool C.Test(char, bool)""
  IL_0490:  br.s       IL_0493
  IL_0492:  ldc.i4.1  
  IL_0493:  call       ""void System.Console.WriteLine(bool)""
  IL_0498:  ret       
}
");
        }

        [Fact]
        public void TestConditionalMemberAccess001()
        {
            var source = @"

public class C
{
    static void Main()
    {
        Test(null);
        System.Console.Write('#');
        int[] a = new int[] { };
        Test(a);
    }

    static void Test(int[] x)
    {
        System.Console.Write(x?.ToString().ToString().ToString() ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "NULL#System.Int32[]");
            comp.VerifyIL("C.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0008
  IL_0004:  pop
  IL_0005:  ldnull
  IL_0006:  br.s       IL_0017
  IL_0008:  callvirt   ""string object.ToString()""
  IL_000d:  callvirt   ""string object.ToString()""
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  dup
  IL_0018:  brtrue.s   IL_0020
  IL_001a:  pop
  IL_001b:  ldstr      ""NULL""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccess001ext()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null);
        System.Console.Write('#');
        int[] a = new int[] { };
        Test(a);
    }

    static void Test(int[] x)
    {
        System.Console.Write(x?.ToStr().ToStr().ToStr() ?? ""NULL"");
    }

    static string ToStr(this object arg)
    {
        return arg.ToString();
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "NULL#System.Int32[]");
            comp.VerifyIL("C.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0008
  IL_0004:  pop
  IL_0005:  ldnull
  IL_0006:  br.s       IL_0017
  IL_0008:  call       ""string C.ToStr(object)""
  IL_000d:  call       ""string C.ToStr(object)""
  IL_0012:  call       ""string C.ToStr(object)""
  IL_0017:  dup
  IL_0018:  brtrue.s   IL_0020
  IL_001a:  pop
  IL_001b:  ldstr      ""NULL""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccess001dyn()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null);
        System.Console.Write('#');
        int[] a = new int[] { };
        Test(a);
    }

    static void Test(dynamic x)
    {
        System.Console.Write(x?.ToString().ToString()?.ToString() ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: "NULL#System.Int32[]");
            comp.VerifyIL("C.Test", @"
{
  // Code size      353 (0x161)
  .maxstack  14
  .locals init (object V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<Test>o__SiteContainer0.<>p__Site4""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Write""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<Test>o__SiteContainer0.<>p__Site4""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<Test>o__SiteContainer0.<>p__Site4""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<Test>o__SiteContainer0.<>p__Site4""
  IL_0055:  ldtoken    ""System.Console""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldarg.0
  IL_0060:  brtrue.s   IL_0068
  IL_0062:  ldnull
  IL_0063:  br         IL_00ff
  IL_0068:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site2""
  IL_006d:  brtrue.s   IL_009f
  IL_006f:  ldc.i4.0
  IL_0070:  ldstr      ""ToString""
  IL_0075:  ldnull
  IL_0076:  ldtoken    ""C""
  IL_007b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0080:  ldc.i4.1
  IL_0081:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0086:  dup
  IL_0087:  ldc.i4.0
  IL_0088:  ldc.i4.0
  IL_0089:  ldnull
  IL_008a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008f:  stelem.ref
  IL_0090:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0095:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site2""
  IL_009f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site2""
  IL_00a4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_00a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site2""
  IL_00ae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site1""
  IL_00b3:  brtrue.s   IL_00e5
  IL_00b5:  ldc.i4.0
  IL_00b6:  ldstr      ""ToString""
  IL_00bb:  ldnull
  IL_00bc:  ldtoken    ""C""
  IL_00c1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00c6:  ldc.i4.1
  IL_00c7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00cc:  dup
  IL_00cd:  ldc.i4.0
  IL_00ce:  ldc.i4.0
  IL_00cf:  ldnull
  IL_00d0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00d5:  stelem.ref
  IL_00d6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00db:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00e0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site1""
  IL_00e5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site1""
  IL_00ea:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_00ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site1""
  IL_00f4:  ldarg.0
  IL_00f5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00fa:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00ff:  dup
  IL_0100:  stloc.0
  IL_0101:  brtrue.s   IL_0106
  IL_0103:  ldnull
  IL_0104:  br.s       IL_0152
  IL_0106:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site3""
  IL_010b:  brtrue.s   IL_013d
  IL_010d:  ldc.i4.0
  IL_010e:  ldstr      ""ToString""
  IL_0113:  ldnull
  IL_0114:  ldtoken    ""C""
  IL_0119:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_011e:  ldc.i4.1
  IL_011f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0124:  dup
  IL_0125:  ldc.i4.0
  IL_0126:  ldc.i4.0
  IL_0127:  ldnull
  IL_0128:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_012d:  stelem.ref
  IL_012e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0133:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0138:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site3""
  IL_013d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site3""
  IL_0142:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0147:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Test>o__SiteContainer0.<>p__Site3""
  IL_014c:  ldloc.0
  IL_014d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0152:  dup
  IL_0153:  brtrue.s   IL_015b
  IL_0155:  pop
  IL_0156:  ldstr      ""NULL""
  IL_015b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0160:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccess001dyn1()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null);
        System.Console.Write('#');
        int[] a = new int[] { };
        Test(a);
    }

    static void Test(dynamic x)
    {
        System.Console.Write(x?.ToString()?[1].ToString() ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: "NULL#y");
        }

        [Fact]
        public void TestConditionalMemberAccess001dyn2()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null, ""aa"");
        System.Console.Write('#');
        Test(""aa"", ""bb"");
    }

    static void Test(string s, dynamic ds)
    {
        System.Console.Write(s?.CompareTo(ds) ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: "NULL#-1");
        }

        [Fact]
        public void TestConditionalMemberAccess001dyn3()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null, 1);
        System.Console.Write('#');
        int[] a = new int[] { };
        Test(a, 1);
    }

    static void Test(int[] x, dynamic i)
    {
        System.Console.Write(x?.ToString()?[i].ToString() ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: "NULL#y");
        }

        [Fact]
        public void TestConditionalMemberAccess001dyn4()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null);
        System.Console.Write('#');
        int[] a = new int[] {1,2,3};
        Test(a);
    }

    static void Test(dynamic x)
    {
        System.Console.Write(x?.Length.ToString() ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: "NULL#3");
        }

        [Fact]
        public void TestConditionalMemberAccess001dyn5()
        {
            var source = @"

public static class C
{
    static void Main()
    {
        Test(null);
        System.Console.Write('#');
        int[] a = new int[] {1,2,3};
        Test(a);
    }

    static void Test(dynamic x)
    {
        System.Console.Write(x?.Length?.ToString() ?? ""NULL"");
    }
}";

            var comp = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: "NULL#3");
        }

        [Fact]
        public void TestConditionalMemberAccessUnused()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString();
        var dummy2 = ""qqq""?.ToString();
        var dummy3 = 1.ToString()?.ToString();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "");
            comp.VerifyIL("C.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldstr      ""qqq""
  IL_0005:  callvirt   ""string object.ToString()""
  IL_000a:  pop
  IL_000b:  ldc.i4.1
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""string int.ToString()""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_0019
  IL_0017:  pop
  IL_0018:  ret
  IL_0019:  callvirt   ""string object.ToString()""
  IL_001e:  pop
  IL_001f:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUsed()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString();
        var dummy2 = ""qqq""?.ToString();
        var dummy3 = 1.ToString()?.ToString();
        dummy1 += dummy2 += dummy3;
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "");
            comp.VerifyIL("C.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  3
  .locals init (string V_0, //dummy2
  string V_1, //dummy3
  int V_2)
  IL_0000:  ldnull
  IL_0001:  ldstr      ""qqq""
  IL_0006:  callvirt   ""string object.ToString()""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  stloc.2
  IL_000e:  ldloca.s   V_2
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  dup
  IL_0016:  brtrue.s   IL_001c
  IL_0018:  pop
  IL_0019:  ldnull
  IL_001a:  br.s       IL_0021
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  stloc.1
  IL_0022:  ldloc.0
  IL_0023:  ldloc.1
  IL_0024:  call       ""string string.Concat(string, string)""
  IL_0029:  dup
  IL_002a:  stloc.0
  IL_002b:  call       ""string string.Concat(string, string)""
  IL_0030:  pop
  IL_0031:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUnused1()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString().Length;
        var dummy2 = ""qqq""?.ToString().Length;
        var dummy3 = 1.ToString()?.ToString().Length;
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "");
            comp.VerifyIL("C.Main", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldstr      ""qqq""
  IL_0005:  callvirt   ""string object.ToString()""
  IL_000a:  callvirt   ""int string.Length.get""
  IL_000f:  pop
  IL_0010:  ldc.i4.1
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""string int.ToString()""
  IL_0019:  dup
  IL_001a:  brtrue.s   IL_001e
  IL_001c:  pop
  IL_001d:  ret
  IL_001e:  callvirt   ""string object.ToString()""
  IL_0023:  callvirt   ""int string.Length.get""
  IL_0028:  pop
  IL_0029:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUsed1()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString().Length;
        System.Console.WriteLine(dummy1);

        var dummy2 = ""qqq""?.ToString().Length;
        System.Console.WriteLine(dummy2);

        var dummy3 = 1.ToString()?.ToString().Length;
        System.Console.WriteLine(dummy3);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"3
1");
            comp.VerifyIL("C.Main", @"
{
  // Code size       99 (0x63)
  .maxstack  2
  .locals init (int? V_0,
  int V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloc.0
  IL_0009:  box        ""int?""
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ldstr      ""qqq""
  IL_0018:  callvirt   ""string object.ToString()""
  IL_001d:  callvirt   ""int string.Length.get""
  IL_0022:  newobj     ""int?..ctor(int)""
  IL_0027:  box        ""int?""
  IL_002c:  call       ""void System.Console.WriteLine(object)""
  IL_0031:  ldc.i4.1
  IL_0032:  stloc.1
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       ""string int.ToString()""
  IL_003a:  dup
  IL_003b:  brtrue.s   IL_0049
  IL_003d:  pop
  IL_003e:  ldloca.s   V_0
  IL_0040:  initobj    ""int?""
  IL_0046:  ldloc.0
  IL_0047:  br.s       IL_0058
  IL_0049:  callvirt   ""string object.ToString()""
  IL_004e:  callvirt   ""int string.Length.get""
  IL_0053:  newobj     ""int?..ctor(int)""
  IL_0058:  box        ""int?""
  IL_005d:  call       ""void System.Console.WriteLine(object)""
  IL_0062:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUnused2()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString().Length?.ToString();
        var dummy2 = ""qqq""?.ToString().Length.ToString();
        var dummy3 = 1.ToString()?.ToString().Length.ToString();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "");
            comp.VerifyIL("C.Main", @"
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (int? V_0,
  int? V_1,
  int V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloc.1
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  brfalse.s  IL_0025
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int int?.GetValueOrDefault()""
  IL_001c:  stloc.2
  IL_001d:  ldloca.s   V_2
  IL_001f:  call       ""string int.ToString()""
  IL_0024:  pop
  IL_0025:  ldstr      ""qqq""
  IL_002a:  callvirt   ""string object.ToString()""
  IL_002f:  callvirt   ""int string.Length.get""
  IL_0034:  stloc.2
  IL_0035:  ldloca.s   V_2
  IL_0037:  call       ""string int.ToString()""
  IL_003c:  pop
  IL_003d:  ldc.i4.1
  IL_003e:  stloc.2
  IL_003f:  ldloca.s   V_2
  IL_0041:  call       ""string int.ToString()""
  IL_0046:  dup
  IL_0047:  brtrue.s   IL_004b
  IL_0049:  pop
  IL_004a:  ret
  IL_004b:  callvirt   ""string object.ToString()""
  IL_0050:  callvirt   ""int string.Length.get""
  IL_0055:  stloc.2
  IL_0056:  ldloca.s   V_2
  IL_0058:  call       ""string int.ToString()""
  IL_005d:  pop
  IL_005e:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUsed2()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString().Length?.ToString();
        System.Console.WriteLine(dummy1);

        var dummy2 = ""qqq""?.ToString().Length?.ToString();
        System.Console.WriteLine(dummy2);

        var dummy3 = 1.ToString()?.ToString().Length?.ToString();
        System.Console.WriteLine(dummy3);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"3
1");
            comp.VerifyIL("C.Main", @"
{
  // Code size      174 (0xae)
  .maxstack  2
  .locals init (int? V_0,
  int? V_1,
  int V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloc.1
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  brtrue.s   IL_0018
  IL_0015:  ldnull
  IL_0016:  br.s       IL_0027
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""int int?.GetValueOrDefault()""
  IL_001f:  stloc.2
  IL_0020:  ldloca.s   V_2
  IL_0022:  call       ""string int.ToString()""
  IL_0027:  call       ""void System.Console.WriteLine(string)""
  IL_002c:  ldstr      ""qqq""
  IL_0031:  callvirt   ""string object.ToString()""
  IL_0036:  callvirt   ""int string.Length.get""
  IL_003b:  newobj     ""int?..ctor(int)""
  IL_0040:  dup
  IL_0041:  stloc.0
  IL_0042:  stloc.1
  IL_0043:  ldloca.s   V_1
  IL_0045:  call       ""bool int?.HasValue.get""
  IL_004a:  brtrue.s   IL_004f
  IL_004c:  ldnull
  IL_004d:  br.s       IL_005e
  IL_004f:  ldloca.s   V_0
  IL_0051:  call       ""int int?.GetValueOrDefault()""
  IL_0056:  stloc.2
  IL_0057:  ldloca.s   V_2
  IL_0059:  call       ""string int.ToString()""
  IL_005e:  call       ""void System.Console.WriteLine(string)""
  IL_0063:  ldc.i4.1
  IL_0064:  stloc.2
  IL_0065:  ldloca.s   V_2
  IL_0067:  call       ""string int.ToString()""
  IL_006c:  dup
  IL_006d:  brtrue.s   IL_007b
  IL_006f:  pop
  IL_0070:  ldloca.s   V_1
  IL_0072:  initobj    ""int?""
  IL_0078:  ldloc.1
  IL_0079:  br.s       IL_008a
  IL_007b:  callvirt   ""string object.ToString()""
  IL_0080:  callvirt   ""int string.Length.get""
  IL_0085:  newobj     ""int?..ctor(int)""
  IL_008a:  dup
  IL_008b:  stloc.0
  IL_008c:  stloc.1
  IL_008d:  ldloca.s   V_1
  IL_008f:  call       ""bool int?.HasValue.get""
  IL_0094:  brtrue.s   IL_0099
  IL_0096:  ldnull
  IL_0097:  br.s       IL_00a8
  IL_0099:  ldloca.s   V_0
  IL_009b:  call       ""int int?.GetValueOrDefault()""
  IL_00a0:  stloc.2
  IL_00a1:  ldloca.s   V_2
  IL_00a3:  call       ""string int.ToString()""
  IL_00a8:  call       ""void System.Console.WriteLine(string)""
  IL_00ad:  ret
}
");
        }

        [Fact]
        [WorkItem(976765, "DevDiv")]
        public void ConditionalMemberAccessConstrained()
        {
            var source = @"
class Program
{
    static void M<T>(T x) where T: System.Exception
    {
        object s = x?.ToString();
        System.Console.WriteLine(s);

        s = x?.GetType();
        System.Console.WriteLine(s);
    }
 
    static void Main()
    {
        M(new System.Exception(""a""));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"System.Exception: a
System.Exception");
            comp.VerifyIL("Program.M<T>", @"
{
  // Code size       57 (0x39)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  box        ""T""
  IL_0007:  brtrue.s   IL_000d
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  br.s       IL_0017
  IL_000d:  box        ""T""
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  call       ""void System.Console.WriteLine(object)""
  IL_001c:  ldarg.0
  IL_001d:  dup
  IL_001e:  box        ""T""
  IL_0023:  brtrue.s   IL_0029
  IL_0025:  pop
  IL_0026:  ldnull
  IL_0027:  br.s       IL_0033
  IL_0029:  box        ""T""
  IL_002e:  callvirt   ""System.Type System.Exception.GetType()""
  IL_0033:  call       ""void System.Console.WriteLine(object)""
  IL_0038:  ret
}
");

        }

        [Fact]
        [WorkItem(991400, "DevDiv")]
        public void ConditionalMemberAccessStatement()
        {
            var source = @"
class Program
{
    class C1
    {
        public void Print0()
        {
            System.Console.WriteLine(""print0"");
        }

        public int Print1()
        {
            System.Console.WriteLine(""print1"");
            return 1;
        }

        public object Print2()
        {
            System.Console.WriteLine(""print2"");
            return 1;
        }
    }

    static void M(C1 x)
    {
        x?.Print0();
        x?.Print1();
        x?.Print2();
    }
 
    static void Main()
    {
        M(null);
        M(new C1());
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"print0
print1
print2");
            comp.VerifyIL("Program.M(Program.C1)", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0007
  IL_0004:  pop
  IL_0005:  br.s       IL_000c
  IL_0007:  callvirt   ""void Program.C1.Print0()""
  IL_000c:  ldarg.0
  IL_000d:  dup
  IL_000e:  brtrue.s   IL_0013
  IL_0010:  pop
  IL_0011:  br.s       IL_0019
  IL_0013:  callvirt   ""int Program.C1.Print1()""
  IL_0018:  pop
  IL_0019:  ldarg.0
  IL_001a:  dup
  IL_001b:  brtrue.s   IL_001f
  IL_001d:  pop
  IL_001e:  ret
  IL_001f:  callvirt   ""object Program.C1.Print2()""
  IL_0024:  pop
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(991400, "DevDiv")]
        public void ConditionalMemberAccessStatement01()
        {
            var source = @"
class Program
{
    struct S1
    {
        public void Print0()
        {
            System.Console.WriteLine(""print0"");
        }

        public int Print1()
        {
            System.Console.WriteLine(""print1"");
            return 1;
        }

        public object Print2()
        {
            System.Console.WriteLine(""print2"");
            return 1;
        }
    }

    static void M(S1? x)
    {
        x?.Print0();
        x?.Print1();
        x?.Print2()?.ToString().ToString()?.ToString();
    }
 
    static void Main()
    {
        M(null);
        M(new S1());
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"print0
print1
print2");
            comp.VerifyIL("Program.M(Program.S1?)", @"
{
  // Code size      105 (0x69)
  .maxstack  2
  .locals init (Program.S1 V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool Program.S1?.HasValue.get""
  IL_0007:  brfalse.s  IL_0018
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""Program.S1 Program.S1?.GetValueOrDefault()""
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""void Program.S1.Print0()""
  IL_0018:  ldarga.s   V_0
  IL_001a:  call       ""bool Program.S1?.HasValue.get""
  IL_001f:  brfalse.s  IL_0031
  IL_0021:  ldarga.s   V_0
  IL_0023:  call       ""Program.S1 Program.S1?.GetValueOrDefault()""
  IL_0028:  stloc.0
  IL_0029:  ldloca.s   V_0
  IL_002b:  call       ""int Program.S1.Print1()""
  IL_0030:  pop
  IL_0031:  ldarga.s   V_0
  IL_0033:  call       ""bool Program.S1?.HasValue.get""
  IL_0038:  brtrue.s   IL_003d
  IL_003a:  ldnull
  IL_003b:  br.s       IL_004c
  IL_003d:  ldarga.s   V_0
  IL_003f:  call       ""Program.S1 Program.S1?.GetValueOrDefault()""
  IL_0044:  stloc.0
  IL_0045:  ldloca.s   V_0
  IL_0047:  call       ""object Program.S1.Print2()""
  IL_004c:  dup
  IL_004d:  brtrue.s   IL_0053
  IL_004f:  pop
  IL_0050:  ldnull
  IL_0051:  br.s       IL_005d
  IL_0053:  callvirt   ""string object.ToString()""
  IL_0058:  callvirt   ""string object.ToString()""
  IL_005d:  dup
  IL_005e:  brtrue.s   IL_0062
  IL_0060:  pop
  IL_0061:  ret
  IL_0062:  callvirt   ""string object.ToString()""
  IL_0067:  pop
  IL_0068:  ret
}
");
        }

        [Fact]
        [WorkItem(991400, "DevDiv")]
        public void ConditionalMemberAccessStatement02()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    class C1
    {
        public void Print0(int i)
        {
            System.Console.WriteLine(""print0"");
        }

        public int Print1(int i)
        {
            System.Console.WriteLine(""print1"");
            return 1;
        }

        public object Print2(int i)
        {
            System.Console.WriteLine(""print2"");
            return 1;
        }
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 1;
    }

    static async Task<int> M(C1 x)
    {
        x?.Print0(await Val());
        x?.Print1(await Val());
        x?.Print2(await Val());
        return 1;
    }

    static void Main()
    {
        M(null).Wait();
        M(new C1()).Wait();
    }
}
";
            var comp = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: @"print0
print1
print2");
        }

        [Fact]
        [WorkItem(991400, "DevDiv")]
        public void ConditionalMemberAccessStatement03()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    struct C1
    {
        public void Print0(int i)
        {
            System.Console.WriteLine(""print0"");
        }

        public int Print1(int i)
        {
            System.Console.WriteLine(""print1"");
            return 1;
        }

        public object Print2(int i)
        {
            System.Console.WriteLine(""print2"");
            return 1;
        }
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 1;
    }

    static async Task<int> M(C1? x)
    {
        x?.Print0(await Val());
        x?.Print1(await Val());
        x?.Print2(await Val());
        return 1;
    }

    static void Main()
    {
        M(null).Wait();
        M(new C1()).Wait();
    }
}
";
            var comp = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: @"print0
print1
print2");
        }

    }
}