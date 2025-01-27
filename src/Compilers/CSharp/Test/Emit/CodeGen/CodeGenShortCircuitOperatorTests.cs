// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
  IL_0035:  ldloc.0
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
  IL_004d:  ldloc.1
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
  IL_0035:  ldloc.0
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
  IL_004d:  ldloc.1
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
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldnull
  IL_0004:  br.s       IL_0016
  IL_0006:  ldarg.0
  IL_0007:  callvirt   ""string object.ToString()""
  IL_000c:  callvirt   ""string object.ToString()""
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_001f
  IL_0019:  pop
  IL_001a:  ldstr      ""NULL""
  IL_001f:  call       ""void System.Console.Write(string)""
  IL_0024:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "NULL#System.Int32[]");
            comp.VerifyIL("C.Test", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldnull
  IL_0004:  br.s       IL_0016
  IL_0006:  ldarg.0
  IL_0007:  call       ""string C.ToStr(object)""
  IL_000c:  call       ""string C.ToStr(object)""
  IL_0011:  call       ""string C.ToStr(object)""
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_001f
  IL_0019:  pop
  IL_001a:  ldstr      ""NULL""
  IL_001f:  call       ""void System.Console.Write(string)""
  IL_0024:  ret
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

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "NULL#System.Int32[]");
            comp.VerifyIL("C.Test", @"
{
  // Code size      355 (0x163)
  .maxstack  14
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__3""
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
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__3""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__3""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__3""
  IL_0055:  ldtoken    ""System.Console""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldarg.0
  IL_0060:  stloc.0
  IL_0061:  ldloc.0
  IL_0062:  brtrue.s   IL_006a
  IL_0064:  ldnull
  IL_0065:  br         IL_0154
  IL_006a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_006f:  brtrue.s   IL_00a1
  IL_0071:  ldc.i4.0
  IL_0072:  ldstr      ""ToString""
  IL_0077:  ldnull
  IL_0078:  ldtoken    ""C""
  IL_007d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0082:  ldc.i4.1
  IL_0083:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0088:  dup
  IL_0089:  ldc.i4.0
  IL_008a:  ldc.i4.0
  IL_008b:  ldnull
  IL_008c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0091:  stelem.ref
  IL_0092:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0097:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00a1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00a6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_00ab:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00b0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_00b5:  brtrue.s   IL_00e7
  IL_00b7:  ldc.i4.0
  IL_00b8:  ldstr      ""ToString""
  IL_00bd:  ldnull
  IL_00be:  ldtoken    ""C""
  IL_00c3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00c8:  ldc.i4.1
  IL_00c9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00ce:  dup
  IL_00cf:  ldc.i4.0
  IL_00d0:  ldc.i4.0
  IL_00d1:  ldnull
  IL_00d2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00d7:  stelem.ref
  IL_00d8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00dd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00e2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_00e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_00ec:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_00f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_00f6:  ldloc.0
  IL_00f7:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00fc:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0101:  stloc.1
  IL_0102:  ldloc.1
  IL_0103:  brtrue.s   IL_0108
  IL_0105:  ldnull
  IL_0106:  br.s       IL_0154
  IL_0108:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__2""
  IL_010d:  brtrue.s   IL_013f
  IL_010f:  ldc.i4.0
  IL_0110:  ldstr      ""ToString""
  IL_0115:  ldnull
  IL_0116:  ldtoken    ""C""
  IL_011b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0120:  ldc.i4.1
  IL_0121:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0126:  dup
  IL_0127:  ldc.i4.0
  IL_0128:  ldc.i4.0
  IL_0129:  ldnull
  IL_012a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_012f:  stelem.ref
  IL_0130:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0135:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_013a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__2""
  IL_013f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__2""
  IL_0144:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0149:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__2""
  IL_014e:  ldloc.1
  IL_014f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0154:  dup
  IL_0155:  brtrue.s   IL_015d
  IL_0157:  pop
  IL_0158:  ldstr      ""NULL""
  IL_015d:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0162:  ret
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

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "NULL#y");
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

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "NULL#-1");
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

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "NULL#y");
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

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "NULL#3");
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

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "NULL#3");
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
        var dummy1 = ((string)null)?.ToString().NullableLength()?.ToString();
        var dummy2 = ""qqq""?.ToString().NullableLength().ToString();
        var dummy3 = 1.ToString()?.ToString().NullableLength()?.ToString();
        }
    }

    public static class C1
    {
        public static int? NullableLength(this string self)
        {
            return self.Length;
        }
    }
";

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: "");
            comp.VerifyIL("C.Main", @"
{
  // Code size       82 (0x52)
  .maxstack  2
  .locals init (int? V_0,
                int V_1)
  IL_0000:  ldstr      ""qqq""
  IL_0005:  callvirt   ""string object.ToString()""
  IL_000a:  call       ""int? C1.NullableLength(string)""
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  constrained. ""int?""
  IL_0018:  callvirt   ""string object.ToString()""
  IL_001d:  pop
  IL_001e:  ldc.i4.1
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  call       ""string int.ToString()""
  IL_0027:  dup
  IL_0028:  brtrue.s   IL_002c
  IL_002a:  pop
  IL_002b:  ret
  IL_002c:  callvirt   ""string object.ToString()""
  IL_0031:  call       ""int? C1.NullableLength(string)""
  IL_0036:  stloc.0
  IL_0037:  ldloca.s   V_0
  IL_0039:  dup
  IL_003a:  call       ""bool int?.HasValue.get""
  IL_003f:  brtrue.s   IL_0043
  IL_0041:  pop
  IL_0042:  ret
  IL_0043:  call       ""int int?.GetValueOrDefault()""
  IL_0048:  stloc.1
  IL_0049:  ldloca.s   V_1
  IL_004b:  call       ""string int.ToString()""
  IL_0050:  pop
  IL_0051:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUnused2a()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString()?.Length.ToString();
        var dummy2 = ""qqq""?.ToString().Length.ToString();
        var dummy3 = 1.ToString()?.ToString().Length.ToString();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "");
            comp.VerifyIL("C.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldstr      ""qqq""
  IL_0005:  callvirt   ""string object.ToString()""
  IL_000a:  callvirt   ""int string.Length.get""
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""string int.ToString()""
  IL_0017:  pop
  IL_0018:  ldc.i4.1
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  call       ""string int.ToString()""
  IL_0021:  dup
  IL_0022:  brtrue.s   IL_0026
  IL_0024:  pop
  IL_0025:  ret
  IL_0026:  callvirt   ""string object.ToString()""
  IL_002b:  callvirt   ""int string.Length.get""
  IL_0030:  stloc.0
  IL_0031:  ldloca.s   V_0
  IL_0033:  call       ""string int.ToString()""
  IL_0038:  pop
  IL_0039:  ret
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
        var dummy1 = ((string)null)?.ToString().NullableLength()?.ToString();
        System.Console.WriteLine(dummy1);

        var dummy2 = ""qqq""?.ToString().NullableLength()?.ToString();
        System.Console.WriteLine(dummy2);

        var dummy3 = 1.ToString()?.ToString().NullableLength()?.ToString();
        System.Console.WriteLine(dummy3);
    }
}

public static class C1
{
    public static int? NullableLength(this string self)
    {
        return self.Length;
    }
}";

            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: @"3
1");
            comp.VerifyIL("C.Main", @"
{
  // Code size      114 (0x72)
  .maxstack  2
  .locals init (int? V_0,
                int V_1)
  IL_0000:  ldnull
  IL_0001:  call       ""void System.Console.WriteLine(string)""
  IL_0006:  ldstr      ""qqq""
  IL_000b:  callvirt   ""string object.ToString()""
  IL_0010:  call       ""int? C1.NullableLength(string)""
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  dup
  IL_0019:  call       ""bool int?.HasValue.get""
  IL_001e:  brtrue.s   IL_0024
  IL_0020:  pop
  IL_0021:  ldnull
  IL_0022:  br.s       IL_0031
  IL_0024:  call       ""int int?.GetValueOrDefault()""
  IL_0029:  stloc.1
  IL_002a:  ldloca.s   V_1
  IL_002c:  call       ""string int.ToString()""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ldc.i4.1
  IL_0037:  stloc.1
  IL_0038:  ldloca.s   V_1
  IL_003a:  call       ""string int.ToString()""
  IL_003f:  dup
  IL_0040:  brtrue.s   IL_0046
  IL_0042:  pop
  IL_0043:  ldnull
  IL_0044:  br.s       IL_006c
  IL_0046:  callvirt   ""string object.ToString()""
  IL_004b:  call       ""int? C1.NullableLength(string)""
  IL_0050:  stloc.0
  IL_0051:  ldloca.s   V_0
  IL_0053:  dup
  IL_0054:  call       ""bool int?.HasValue.get""
  IL_0059:  brtrue.s   IL_005f
  IL_005b:  pop
  IL_005c:  ldnull
  IL_005d:  br.s       IL_006c
  IL_005f:  call       ""int int?.GetValueOrDefault()""
  IL_0064:  stloc.1
  IL_0065:  ldloca.s   V_1
  IL_0067:  call       ""string int.ToString()""
  IL_006c:  call       ""void System.Console.WriteLine(string)""
  IL_0071:  ret
}
");
        }

        [Fact]
        public void TestConditionalMemberAccessUsed2a()
        {
            var source = @"

public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString()?.Length.ToString();
        System.Console.WriteLine(dummy1);

        var dummy2 = ""qqq""?.ToString()?.Length.ToString();
        System.Console.WriteLine(dummy2);

        var dummy3 = 1.ToString()?.ToString()?.Length.ToString();
        System.Console.WriteLine(dummy3);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"3
1");
            comp.VerifyIL("C.Main", @"
{
  // Code size       88 (0x58)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldnull
  IL_0001:  call       ""void System.Console.WriteLine(string)""
  IL_0006:  ldstr      ""qqq""
  IL_000b:  callvirt   ""string object.ToString()""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_0017
  IL_0013:  pop
  IL_0014:  ldnull
  IL_0015:  br.s       IL_0024
  IL_0017:  call       ""int string.Length.get""
  IL_001c:  stloc.0
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""string int.ToString()""
  IL_0024:  call       ""void System.Console.WriteLine(string)""
  IL_0029:  ldc.i4.1
  IL_002a:  stloc.0
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       ""string int.ToString()""
  IL_0032:  dup
  IL_0033:  brtrue.s   IL_0039
  IL_0035:  pop
  IL_0036:  ldnull
  IL_0037:  br.s       IL_0052
  IL_0039:  callvirt   ""string object.ToString()""
  IL_003e:  dup
  IL_003f:  brtrue.s   IL_0045
  IL_0041:  pop
  IL_0042:  ldnull
  IL_0043:  br.s       IL_0052
  IL_0045:  call       ""int string.Length.get""
  IL_004a:  stloc.0
  IL_004b:  ldloca.s   V_0
  IL_004d:  call       ""string int.ToString()""
  IL_0052:  call       ""void System.Console.WriteLine(string)""
  IL_0057:  ret
}
");
        }

        [Fact]
        [WorkItem(976765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/976765")]
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
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000d
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  br.s       IL_0012
  IL_000d:  callvirt   ""string object.ToString()""
  IL_0012:  call       ""void System.Console.WriteLine(object)""
  IL_0017:  ldarg.0
  IL_0018:  box        ""T""
  IL_001d:  dup
  IL_001e:  brtrue.s   IL_0024
  IL_0020:  pop
  IL_0021:  ldnull
  IL_0022:  br.s       IL_0029
  IL_0024:  callvirt   ""System.Type System.Exception.GetType()""
  IL_0029:  call       ""void System.Console.WriteLine(object)""
  IL_002e:  ret
}
");
        }

        [Fact]
        [WorkItem(991400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991400")]
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
  // Code size       30 (0x1e)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0009
  IL_0003:  ldarg.0
  IL_0004:  call       ""void Program.C1.Print0()""
  IL_0009:  ldarg.0
  IL_000a:  brfalse.s  IL_0013
  IL_000c:  ldarg.0
  IL_000d:  call       ""int Program.C1.Print1()""
  IL_0012:  pop
  IL_0013:  ldarg.0
  IL_0014:  brfalse.s  IL_001d
  IL_0016:  ldarg.0
  IL_0017:  call       ""object Program.C1.Print2()""
  IL_001c:  pop
  IL_001d:  ret
}
");
        }

        [Fact]
        [WorkItem(991400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991400")]
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
  // Code size      100 (0x64)
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
  IL_0038:  brfalse.s  IL_0063
  IL_003a:  ldarga.s   V_0
  IL_003c:  call       ""Program.S1 Program.S1?.GetValueOrDefault()""
  IL_0041:  stloc.0
  IL_0042:  ldloca.s   V_0
  IL_0044:  call       ""object Program.S1.Print2()""
  IL_0049:  dup
  IL_004a:  brtrue.s   IL_004e
  IL_004c:  pop
  IL_004d:  ret
  IL_004e:  callvirt   ""string object.ToString()""
  IL_0053:  callvirt   ""string object.ToString()""
  IL_0058:  dup
  IL_0059:  brtrue.s   IL_005d
  IL_005b:  pop
  IL_005c:  ret
  IL_005d:  callvirt   ""string object.ToString()""
  IL_0062:  pop
  IL_0063:  ret
}
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(991400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991400")]
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
            var comp = CompileAndVerify(source, targetFramework: TargetFramework.Empty, references: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: @"print0
print1
print2");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(991400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991400")]
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
            var comp = CompileAndVerify(source, targetFramework: TargetFramework.Empty, references: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: @"print0
print1
print2");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrained()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    class C1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(ref c, ref c);

        S1 s = new S1();
        Test(ref s, ref s);
    }

    static void Test<T>(ref T x, ref T y) where T : IDisposable
    {
        x?.Dispose();
        y?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
False
True");
            comp.VerifyIL("Program.Test<T>(ref T, ref T)", @"
{
  // Code size       94 (0x5e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0024
  IL_0011:  ldobj      ""T""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        ""T""
  IL_001f:  brtrue.s   IL_0024
  IL_0021:  pop
  IL_0022:  br.s       IL_002f
  IL_0024:  constrained. ""T""
  IL_002a:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002f:  ldarg.1
  IL_0030:  ldloca.s   V_0
  IL_0032:  initobj    ""T""
  IL_0038:  ldloc.0
  IL_0039:  box        ""T""
  IL_003e:  brtrue.s   IL_0052
  IL_0040:  ldobj      ""T""
  IL_0045:  stloc.0
  IL_0046:  ldloca.s   V_0
  IL_0048:  ldloc.0
  IL_0049:  box        ""T""
  IL_004e:  brtrue.s   IL_0052
  IL_0050:  pop
  IL_0051:  ret
  IL_0052:  constrained. ""T""
  IL_0058:  callvirt   ""void System.IDisposable.Dispose()""
  IL_005d:  ret
}");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrained1()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    class C1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        var c = new C1[] {new C1()};
        Test(c, c);

        var s = new S1[] {new S1()};
        Test(s, s);
    }

    static void Test<T>(T[] x, T[] y) where T : IDisposable
    {
        x[0]?.Dispose();
        y[0]?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
False
True");
            comp.VerifyIL("Program.Test<T>(T[], T[])", @"
{
  // Code size      110 (0x6e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  readonly.
  IL_0004:  ldelema    ""T""
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""T""
  IL_0011:  ldloc.0
  IL_0012:  box        ""T""
  IL_0017:  brtrue.s   IL_002c
  IL_0019:  ldobj      ""T""
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  box        ""T""
  IL_0027:  brtrue.s   IL_002c
  IL_0029:  pop
  IL_002a:  br.s       IL_0037
  IL_002c:  constrained. ""T""
  IL_0032:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0037:  ldarg.1
  IL_0038:  ldc.i4.0
  IL_0039:  readonly.
  IL_003b:  ldelema    ""T""
  IL_0040:  ldloca.s   V_0
  IL_0042:  initobj    ""T""
  IL_0048:  ldloc.0
  IL_0049:  box        ""T""
  IL_004e:  brtrue.s   IL_0062
  IL_0050:  ldobj      ""T""
  IL_0055:  stloc.0
  IL_0056:  ldloca.s   V_0
  IL_0058:  ldloc.0
  IL_0059:  box        ""T""
  IL_005e:  brtrue.s   IL_0062
  IL_0060:  pop
  IL_0061:  ret
  IL_0062:  constrained. ""T""
  IL_0068:  callvirt   ""void System.IDisposable.Dispose()""
  IL_006d:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessConstrained1()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    class C1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        var c = new C1[] {new C1()};
        Test(c, c);
    }

    static void Test<T>(T[] x, T[] y) where T : class, IDisposable
    {
        x[0]?.Dispose();
        y[0]?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
");
            comp.VerifyIL("Program.Test<T>(T[], T[])", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem     ""T""
  IL_0007:  box        ""T""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0012
  IL_000f:  pop
  IL_0010:  br.s       IL_0017
  IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0017:  ldarg.1
  IL_0018:  ldc.i4.0
  IL_0019:  ldelem     ""T""
  IL_001e:  box        ""T""
  IL_0023:  dup
  IL_0024:  brtrue.s   IL_0028
  IL_0026:  pop
  IL_0027:  ret
  IL_0028:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002d:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedVal()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    class C1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(c);

        S1 s = new S1();
        Test(s);
    }

    static void Test<T>(T x) where T : IDisposable
    {
        x?.Dispose();
        x?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
False
True");
            comp.VerifyIL("Program.Test<T>(T)", @"
{
  // Code size       43 (0x2b)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarga.s   V_0
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0015:  ldarg.0
  IL_0016:  box        ""T""
  IL_001b:  brfalse.s  IL_002a
  IL_001d:  ldarga.s   V_0
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002a:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedVal001()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    class C1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(() => c);

        S1 s = new S1();
        Test(() => s);
    }

    static void Test<T>(Func<T> x) where T : IDisposable
    {
        x()?.Dispose();
        x()?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
False
False");
            comp.VerifyIL("Program.Test<T>(System.Func<T>)", @"
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  dup
  IL_000a:  ldobj      ""T""
  IL_000f:  box        ""T""
  IL_0014:  brtrue.s   IL_0019
  IL_0016:  pop
  IL_0017:  br.s       IL_0024
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0024:  ldarg.0
  IL_0025:  callvirt   ""T System.Func<T>.Invoke()""
  IL_002a:  stloc.0
  IL_002b:  ldloca.s   V_0
  IL_002d:  dup
  IL_002e:  ldobj      ""T""
  IL_0033:  box        ""T""
  IL_0038:  brtrue.s   IL_003c
  IL_003a:  pop
  IL_003b:  ret
  IL_003c:  constrained. ""T""
  IL_0042:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0047:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessConstrainedVal001()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    class C1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(() => c);
    }

    static void Test<T>(Func<T> x) where T : class, IDisposable
    {
        x()?.Dispose();
        x()?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True");
            comp.VerifyIL("Program.Test<T>(System.Func<T>)", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0006:  box        ""T""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0011
  IL_000e:  pop
  IL_000f:  br.s       IL_0016
  IL_0011:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0016:  ldarg.0
  IL_0017:  callvirt   ""T System.Func<T>.Invoke()""
  IL_001c:  box        ""T""
  IL_0021:  dup
  IL_0022:  brtrue.s   IL_0026
  IL_0024:  pop
  IL_0025:  ret
  IL_0026:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002b:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedDyn()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    interface IDisposable1
    {
        void Dispose(int i);
        void Dispose(long i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public void Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
        public void Dispose(long i)
        {
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public void Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
        public void Dispose(long i)
        {
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(ref c, ref c);

        S1 s = new S1();
        Test(ref s, ref s);
    }

    static void Test<T>(ref T x, ref T y) where T : IDisposable1
    {
        dynamic d = 1;
        x?.Dispose(d);
        y?.Dispose(d);
    }
}
";
            var comp = CompileAndVerify(source, references: new MetadataReference[] { CSharpRef }, expectedOutput: @"False
True
False
False");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedDynVal()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    interface IDisposable1
    {
        void Dispose(int i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public void Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public void Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(c, c);

        S1 s = new S1();
        Test(s, s);
    }

    static void Test<T>(T x, T y) where T : IDisposable1
    {
        dynamic d = 1;
        x?.Dispose(d);
        y?.Dispose(d);
    }
}
";
            var comp = CompileAndVerify(source, references: new MetadataReference[] { CSharpRef }, expectedOutput: @"False
True
False
False");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedAsync()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    interface IDisposable1
    {
        void Dispose(int i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public void Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public void Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1[] c = new C1[] { new C1() };
        Test(c, c).Wait();

        S1[] s = new S1[] { new S1() };
        Test(s, s).Wait();
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 0;
    }

    static async Task<int> Test<T>(T[] x, T[] y) where T : IDisposable1
    {
        x[0]?.Dispose(await Val());
        y[0]?.Dispose(await Val());
        return 1;
    }
}";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True
False
True");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedAsyncVal()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    interface IDisposable1
    {
        int Dispose(int i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public int Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
            return 1;
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public int Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
            return 1;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(c, c).Wait();

        S1 s = new S1();
        Test(s, s).Wait();
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 0;
    }

    static async Task<int> Test<T>(T x, T y) where T : IDisposable1
    {
        x?.Dispose(await Val());
        y?.Dispose(await Val());
        return 1;
    }
}
";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True
False
False");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedAsyncValExt()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DE;

namespace DE
{
    public static class IDispExt
    {
        public static void DisposeExt(this Program.IDisposable1 d, int i)
        {
            d.Dispose(i);
        }
    }
}

public class Program
{
    public interface IDisposable1
    {
        int Dispose(int i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public int Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
            return 1;
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public int Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed = true;
            return 1;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(c, c).Wait();

        S1 s = new S1();
        Test(s, s).Wait();
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 0;
    }

    static async Task<int> Test<T>(T x, T y) where T : IDisposable1
    {
        x?.DisposeExt(await Val());
        y?.DisposeExt(await Val());
        return 1;
    }
}
";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True
False
False");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedAsyncNested()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    interface IDisposable1
    {
        IDisposable1 Dispose(int i);
    }

    class C1 : IDisposable1
    { 
        private bool disposed;

        public IDisposable1 Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed ^= true;
            return this;
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public IDisposable1 Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed ^= true;
            return this;
        }
    }

    static void Main(string[] args)
    {
        C1[] c = new C1[] { new C1() };
        Test(c, c).Wait();

        System.Console.WriteLine();

        S1[] s = new S1[] { new S1() };
        Test(s, s).Wait();
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 0;
    }

    static async Task<int> Test<T>(T[] x, T[] y) where T : IDisposable1
    {
        x[0]?.Dispose(await Val())?.Dispose(await Val())?.Dispose(await Val())?.Dispose(await Val());
        y[0]?.Dispose(await Val())?.Dispose(await Val())?.Dispose(await Val())?.Dispose(await Val());
        return 1;
    }
}";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True
False
True
False
True
False
True

False
True
False
True
True
False
True
False");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedAsyncNestedArr()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    interface IDisposable1
    {
        IDisposable1[] Dispose(int i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public IDisposable1[] Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed ^= true;
            return new IDisposable1[] { this };
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public IDisposable1[] Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed ^= true;
            return new IDisposable1[]{this};
        }
    }

    static void Main(string[] args)
    {
        C1[] c = new C1[] { new C1() };
        Test(c, c).Wait();

        System.Console.WriteLine();

        S1[] s = new S1[] { new S1() };
        Test(s, s).Wait();
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 0;
    }

    static async Task<int> Test<T>(T[] x, T[] y) where T : IDisposable1
    {
        x[0]?.Dispose(await Val())[0]?.Dispose(await Val())[0]?.Dispose(await Val())[0]?.Dispose(await Val());
        y[0]?.Dispose(await Val())[0]?.Dispose(await Val())[0]?.Dispose(await Val())[0]?.Dispose(await Val());
        return 1;
    }
}";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True
False
True
False
True
False
True

False
True
False
True
True
False
True
False");
        }

        [Fact]
        public void ConditionalMemberAccessUnConstrainedAsyncSuperNested()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    interface IDisposable1
    {
        Task<int> Dispose(int i);
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public Task<int> Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed ^= true;
            return Task.FromResult(i);
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public Task<int> Dispose(int i)
        {
            System.Console.WriteLine(disposed);
            disposed ^= true;
            return Task.FromResult(i);
        }
    }

    static void Main(string[] args)
    {
        C1[] c = new C1[] { new C1() };
        Test(c, c).Wait();

        System.Console.WriteLine();

        S1[] s = new S1[] { new S1() };
        Test(s, s).Wait();
    }

    static async Task<int> Val()
    {
        await Task.Yield();
        return 0;
    }

    static async Task<int> Test<T>(T[] x, T[] y) where T : IDisposable1
    {
        x[0]?.Dispose(await x[0]?.Dispose(await x[0]?.Dispose(await x[0]?.Dispose(await Val()))));
        y[0]?.Dispose(await y[0]?.Dispose(await y[0]?.Dispose(await y[0]?.Dispose(await Val()))));
        return 1;
    }
}";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True
False
True
False
True
False
True

False
True
False
True
False
True
False
True");
        }

        [Fact]
        public void ConditionalExtensionAccessGeneric001()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        long? x = 1;
        Test0(x);
        return;
    }
    static void Test0<T>(T x) 
    {
        x?.CheckT();
    }
}
static class Ext
{
    public static void CheckT<T>(this T x)
    {
        System.Console.WriteLine(typeof(T));
        return;
    }
}

";
            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: @"System.Nullable`1[System.Int64]");
            comp.VerifyIL("Test.Test0<T>(T)", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldloca.s   V_0
  IL_0004:  initobj    ""T""
  IL_000a:  ldloc.0
  IL_000b:  box        ""T""
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldobj      ""T""
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_0024
  IL_0022:  pop
  IL_0023:  ret
  IL_0024:  ldobj      ""T""
  IL_0029:  call       ""void Ext.CheckT<T>(T)""
  IL_002e:  ret
}
");
        }

        [Fact]
        public void ConditionalExtensionAccessGeneric002()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        long? x = 1;
        Test0(ref x);
        return;
    }
    static void Test0<T>(ref T x) 
    {
        x?.CheckT();
    }
}
static class Ext
{
    public static void CheckT<T>(this T x)
    {
        System.Console.WriteLine(typeof(T));
        return;
    }
}

";
            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: @"System.Nullable`1[System.Int64]");
            comp.VerifyIL("Test.Test0<T>(ref T)", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      ""T""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        ""T""
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      ""T""
  IL_0028:  call       ""void Ext.CheckT<T>(T)""
  IL_002d:  ret
}
");
        }

        [Fact]
        public void ConditionalExtensionAccessGeneric003()
        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        Test0(""qqq"");
    }

    static void Test0<T>(T x) where T:IEnumerable<char>
    {
        x?.Count();
    }

    static void Test1<T>(ref T x) where T:IEnumerable<char>
    {
        x?.Count();
    }
}

";
            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: @"");
            comp.VerifyIL("Test.Test0<T>(T)", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  brfalse.s  IL_001a
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      ""T""
  IL_000f:  box        ""T""
  IL_0014:  call       ""int System.Linq.Enumerable.Count<char>(System.Collections.Generic.IEnumerable<char>)""
  IL_0019:  pop
  IL_001a:  ret
}
").VerifyIL("Test.Test1<T>(ref T)", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      ""T""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        ""T""
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      ""T""
  IL_0028:  box        ""T""
  IL_002d:  call       ""int System.Linq.Enumerable.Count<char>(System.Collections.Generic.IEnumerable<char>)""
  IL_0032:  pop
  IL_0033:  ret
}
");
        }

        [Fact]
        public void ConditionalExtensionAccessGenericAsync001()
        {
            var source = @"
using System.Threading.Tasks;
class Test
{
    static void Main()
    {

    }
    async Task<int?> TestAsync<T>(T[] x) where T : I1
    {
        return x[0]?.CallAsync(await PassAsync());
    }
    static async Task<int> PassAsync()
    {
        await Task.Yield();
        return 1;
    }
}
interface I1
{
    int CallAsync(int x);
}


";
            var comp = CreateCompilationWithMscorlib461(source, references: new[] { CSharpRef });
            base.CompileAndVerify(comp);
        }

        [Fact]
        public void ConditionalExtensionAccessGenericAsyncNullable001()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class Test
{
    static void Main()
    {
        var arr = new S1?[] { new S1(), new S1()};
        TestAsync(arr).Wait();

        System.Console.WriteLine(arr[1].Value.called);
    }
    static async Task<int?> TestAsync<T>(T?[] x)
            where T : struct, I1
    {
        return x[await PassAsync()]?.CallAsync(await PassAsync());
    }

    static async Task<int> PassAsync()
    {
        await Task.Yield();
        return 1;
    }
}

struct S1 : I1
{
    public int called;

    public int CallAsync(int x)
    {
        called++;
        System.Console.Write(called + 41);
        return called;
    }
}

interface I1
{
    int CallAsync(int x);
}
";
            var comp = CreateCompilationWithMscorlib461(source, references: new[] { CSharpRef }, options: TestOptions.ReleaseExe);
            base.CompileAndVerify(comp, expectedOutput: "420");
        }

        [Fact]
        public void ConditionalMemberAccessCoalesce001()
        {
            var source = @"
class Program
{
    class C1
    {
        public int x{get; set;}
        public int? y{get; set;}
    }

    static void Main()
    {
        var c = new C1();
        System.Console.WriteLine(Test1(c));
        System.Console.WriteLine(Test1(null));

        System.Console.WriteLine(Test2(c));
        System.Console.WriteLine(Test2(null));
    }

    static int Test1(C1 c)
    {
        return c?.x ?? 42;
    }

    static int Test2(C1 c)
    {
        return c?.y ?? 42;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"0
42
42
42");
            comp.VerifyIL("Program.Test1(Program.C1)", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.s   42
  IL_0005:  ret
  IL_0006:  ldarg.0
  IL_0007:  call       ""int Program.C1.x.get""
  IL_000c:  ret
}
").VerifyIL("Program.Test2(Program.C1)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (int? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       ""int? Program.C1.y.get""
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldc.i4.s   42
  IL_0019:  call       ""int int?.GetValueOrDefault(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessCoalesce001n()
        {
            var source = @"
class Program
{
    class C1
    {
        public int x{get; set;}
        public int? y{get; set;}
    }

    static void Main()
    {
        var c = new C1();
        System.Console.WriteLine(Test1(c));
        System.Console.WriteLine(Test1(null));

        System.Console.WriteLine(Test2(c));
        System.Console.WriteLine(Test2(null));
    }

    static int? Test1(C1 c)
    {
        return c?.x ?? (int?)42;
    }

    static int? Test2(C1 c)
    {
        return c?.y ?? (int?)42;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"0
42
42
42");
            comp.VerifyIL("Program.Test1(Program.C1)", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000b
  IL_0003:  ldc.i4.s   42
  IL_0005:  newobj     ""int?..ctor(int)""
  IL_000a:  ret
  IL_000b:  ldarg.0
  IL_000c:  call       ""int Program.C1.x.get""
  IL_0011:  newobj     ""int?..ctor(int)""
  IL_0016:  ret
}
").VerifyIL("Program.Test2(Program.C1)", @"
{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (int? V_0,
                int? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       ""int? Program.C1.y.get""
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""bool int?.HasValue.get""
  IL_001c:  brtrue.s   IL_0026
  IL_001e:  ldc.i4.s   42
  IL_0020:  newobj     ""int?..ctor(int)""
  IL_0025:  ret
  IL_0026:  ldloc.0
  IL_0027:  ret
}");
        }

        [Fact]
        public void ConditionalMemberAccessCoalesce001r()
        {
            var source = @"
class Program
{
    class C1
    {
        public int x {get; set;}
        public int? y {get; set;}
    }

    static void Main()
    {
        var c = new C1();
        C1 n = null;

        System.Console.WriteLine(Test1(ref c));
        System.Console.WriteLine(Test1(ref n));

        System.Console.WriteLine(Test2(ref c));
        System.Console.WriteLine(Test2(ref n));
    }

    static int Test1(ref C1 c)
    {
        return c?.x ?? 42;
    }

    static int Test2(ref C1 c)
    {
        return c?.y ?? 42;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"0
42
42
42");
            comp.VerifyIL("Program.Test1(ref Program.C1)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0009
  IL_0005:  pop
  IL_0006:  ldc.i4.s   42
  IL_0008:  ret
  IL_0009:  call       ""int Program.C1.x.get""
  IL_000e:  ret
}
").VerifyIL("Program.Test2(ref Program.C1)", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int? V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0011
  IL_0005:  pop
  IL_0006:  ldloca.s   V_0
  IL_0008:  initobj    ""int?""
  IL_000e:  ldloc.0
  IL_000f:  br.s       IL_0016
  IL_0011:  call       ""int? Program.C1.y.get""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.s   42
  IL_001b:  call       ""int int?.GetValueOrDefault(int)""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessCoalesce002()
        {
            var source = @"
class Program
{
    struct C1
    {
        public int x{get; set;}
        public int? y{get; set;}
    }

    static void Main()
    {
        var c = new C1();
        System.Console.WriteLine(Test1(c));
        System.Console.WriteLine(Test1(null));

        System.Console.WriteLine(Test2(c));
        System.Console.WriteLine(Test2(null));
    }

    static int Test1(C1? c)
    {
        return c?.x ?? 42;
    }

    static int Test2(C1? c)
    {
        return c?.y ?? 42;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"0
42
42
42");
            comp.VerifyIL("Program.Test1(Program.C1?)", @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (Program.C1 V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool Program.C1?.HasValue.get""
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  ldc.i4.s   42
  IL_000b:  ret
  IL_000c:  ldarga.s   V_0
  IL_000e:  call       ""Program.C1 Program.C1?.GetValueOrDefault()""
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       ""readonly int Program.C1.x.get""
  IL_001b:  ret
}
").VerifyIL("Program.Test2(Program.C1?)", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (int? V_0,
                Program.C1 V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool Program.C1?.HasValue.get""
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_0023
  IL_0014:  ldarga.s   V_0
  IL_0016:  call       ""Program.C1 Program.C1?.GetValueOrDefault()""
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_1
  IL_001e:  call       ""readonly int? Program.C1.y.get""
  IL_0023:  stloc.0
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldc.i4.s   42
  IL_0028:  call       ""int int?.GetValueOrDefault(int)""
  IL_002d:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessCoalesce002r()
        {
            var source = @"
class Program
{
    struct C1
    {
        public int x{get; set;}
        public int? y{get; set;}
    }

    static void Main()
    {
        C1? c = new C1();
        C1? n = null;

        System.Console.WriteLine(Test1(ref c));
        System.Console.WriteLine(Test1(ref n));

        System.Console.WriteLine(Test2(ref c));
        System.Console.WriteLine(Test2(ref n));
    }

    static int Test1(ref C1? c)
    {
        return c?.x ?? 42;
    }

    static int Test2(ref C1? c)
    {
        return c?.y ?? 42;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"0
42
42
42");
            comp.VerifyIL("Program.Test1(ref Program.C1?)", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (Program.C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  call       ""bool Program.C1?.HasValue.get""
  IL_0007:  brtrue.s   IL_000d
  IL_0009:  pop
  IL_000a:  ldc.i4.s   42
  IL_000c:  ret
  IL_000d:  call       ""Program.C1 Program.C1?.GetValueOrDefault()""
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       ""readonly int Program.C1.x.get""
  IL_001a:  ret
}

").VerifyIL("Program.Test2(ref Program.C1?)", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (int? V_0,
                Program.C1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  call       ""bool Program.C1?.HasValue.get""
  IL_0007:  brtrue.s   IL_0015
  IL_0009:  pop
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""int?""
  IL_0012:  ldloc.0
  IL_0013:  br.s       IL_0022
  IL_0015:  call       ""Program.C1 Program.C1?.GetValueOrDefault()""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""readonly int? Program.C1.y.get""
  IL_0022:  stloc.0
  IL_0023:  ldloca.s   V_0
  IL_0025:  ldc.i4.s   42
  IL_0027:  call       ""int int?.GetValueOrDefault(int)""
  IL_002c:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessCoalesceDefault()
        {
            var source = @"
class Program
{
    class C1
    {
        public int x { get; set; }
    }

    static void Main()
    {
        var c = new C1() { x = 42 };
        System.Console.WriteLine(Test(c));
        System.Console.WriteLine(Test(null));
    }

    static int Test(C1 c)
    {
        return c?.x ?? 0;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
42
0");
            comp.VerifyIL("Program.Test(Program.C1)", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int Program.C1.x.get""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessNullCheck001()
        {
            var source = @"
class Program
{
    class C1
    {
        public int x{get; set;}
    }

    static void Main()
    {
        var c = new C1();
        System.Console.WriteLine(Test1(c));
        System.Console.WriteLine(Test1(null));

        System.Console.WriteLine(Test2(c));
        System.Console.WriteLine(Test2(null));

        System.Console.WriteLine(Test3(c));
        System.Console.WriteLine(Test3(null));
    }

    static bool Test1(C1 c)
    {
        return c?.x == null;
    }

    static bool Test2(C1 c)
    {
        return c?.x != null;
    }

    static bool Test3(C1 c)
    {
        return c?.x > null;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
True
False
False
False");
            comp.VerifyIL("Program.Test1(Program.C1)", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.1
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int Program.C1.x.get""
  IL_000b:  pop
  IL_000c:  ldc.i4.0
  IL_000d:  ret
}
").VerifyIL("Program.Test2(Program.C1)", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int Program.C1.x.get""
  IL_000b:  pop
  IL_000c:  ldc.i4.1
  IL_000d:  ret
}
").VerifyIL("Program.Test3(Program.C1)", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int Program.C1.x.get""
  IL_000b:  pop
  IL_000c:  ldc.i4.0
  IL_000d:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessBinary001()
        {
            var source = @"
public enum N
{
    zero = 0,
    one = 1,
    mone = -1
}

class Program
{
    class C1
    {
        public N x{get; set;}
    }

    static void Main()
    {
        var c = new C1();
        System.Console.WriteLine(Test1(c));
        System.Console.WriteLine(Test1(null));

        System.Console.WriteLine(Test2(c));
        System.Console.WriteLine(Test2(null));

        System.Console.WriteLine(Test3(c));
        System.Console.WriteLine(Test3(null));
    }

    static bool Test1(C1 c)
    {
        return c?.x == N.zero;
    }

    static bool Test2(C1 c)
    {
        return c?.x != N.one;
    }

    static bool Test3(C1 c)
    {
        return c?.x > N.mone;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"True
False
True
True
True
False");
            comp.VerifyIL("Program.Test1(Program.C1)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""N Program.C1.x.get""
  IL_000b:  ldc.i4.0
  IL_000c:  ceq
  IL_000e:  ret
}
").VerifyIL("Program.Test2(Program.C1)", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.1
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""N Program.C1.x.get""
  IL_000b:  ldc.i4.1
  IL_000c:  ceq
  IL_000e:  ldc.i4.0
  IL_000f:  ceq
  IL_0011:  ret
}
").VerifyIL("Program.Test3(Program.C1)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""N Program.C1.x.get""
  IL_000b:  ldc.i4.m1
  IL_000c:  cgt
  IL_000e:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessBinary002()
        {
            var source = @"

static class ext
{
    public static Program.C1.S1 y(this Program.C1 self)
    {
        return self.x;
    }
}

class Program
{
    public class C1
    {
        public struct S1
        {
            public static bool operator <(S1 s1, int s2)
            {
                System.Console.WriteLine('<');
                return true;
            }
            public static bool operator >(S1 s1, int s2)
            {
                System.Console.WriteLine('>');
                return false;
            }
        }

        public S1 x { get; set; }
    }

    static void Main()
    {
        C1 c = new C1();
        C1 n = null;
        System.Console.WriteLine(Test1(c));
        System.Console.WriteLine(Test1(n));

        System.Console.WriteLine(Test2(ref c));
        System.Console.WriteLine(Test2(ref n));

        System.Console.WriteLine(Test3(c));
        System.Console.WriteLine(Test3(n));

        System.Console.WriteLine(Test4(ref c));
        System.Console.WriteLine(Test4(ref n));
     }

    static bool Test1(C1 c)
    {
        return c?.x > -1;
    }

    static bool Test2(ref C1 c)
    {
        return c?.x < -1;
    }

    static bool Test3(C1 c)
    {
        return c?.y() > -1;
    }

    static bool Test4(ref C1 c)
    {
        return c?.y() < -1;
    }
}
";
            var comp = CompileAndVerify(source, references: new[] { CSharpRef }, expectedOutput: @"   >
False
False
<
True
False
>
False
False
<
True
False");
            comp.VerifyIL("Program.Test1(Program.C1)", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""Program.C1.S1 Program.C1.x.get""
  IL_000b:  ldc.i4.m1
  IL_000c:  call       ""bool Program.C1.S1.op_GreaterThan(Program.C1.S1, int)""
  IL_0011:  ret
}
").VerifyIL("Program.Test2(ref Program.C1)", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0008
  IL_0005:  pop
  IL_0006:  ldc.i4.0
  IL_0007:  ret
  IL_0008:  call       ""Program.C1.S1 Program.C1.x.get""
  IL_000d:  ldc.i4.m1
  IL_000e:  call       ""bool Program.C1.S1.op_LessThan(Program.C1.S1, int)""
  IL_0013:  ret
}
").VerifyIL("Program.Test3(Program.C1)", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""Program.C1.S1 ext.y(Program.C1)""
  IL_000b:  ldc.i4.m1
  IL_000c:  call       ""bool Program.C1.S1.op_GreaterThan(Program.C1.S1, int)""
  IL_0011:  ret
}
").VerifyIL("Program.Test4(ref Program.C1)", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0008
  IL_0005:  pop
  IL_0006:  ldc.i4.0
  IL_0007:  ret
  IL_0008:  call       ""Program.C1.S1 ext.y(Program.C1)""
  IL_000d:  ldc.i4.m1
  IL_000e:  call       ""bool Program.C1.S1.op_LessThan(Program.C1.S1, int)""
  IL_0013:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessOptimizedLocal001()
        {
            var source = @"
using System;

class Program
{
    class C1 : System.IDisposable
    {
        public bool disposed;
        public void Dispose()
        {
            disposed = true;
        }
    }

    static void Main()
    {
        Test1();
        Test2<C1>();
    }

    static void Test1()
    {
        var c = new C1();
        c?.Dispose();
    }

    static void Test2<T>() where T : IDisposable, new()
    {
        var c = new T();
        c?.Dispose();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"");
            comp.VerifyIL("Program.Test1()", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  newobj     ""Program.C1..ctor()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  pop
  IL_0009:  ret
  IL_000a:  call       ""void Program.C1.Dispose()""
  IL_000f:  ret
}
").VerifyIL("Program.Test2<T>()", @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (T V_0) //c
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  box        ""T""
  IL_000c:  brfalse.s  IL_001b
  IL_000e:  ldloca.s   V_0
  IL_0010:  constrained. ""T""
  IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001b:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessOptimizedLocal002()
        {
            var source = @"
using System;

class Program
{
    interface I1
    {
        void Goo(I1 arg);
    }

    class C1 : I1
    {
        public void Goo(I1 arg)
        {
        }
    }

    static void Main()
    {
        Test1();
        Test2<C1>();
    }

    static void Test1()
    {
        var c = new C1();
        c?.Goo(c);
    }

    static void Test2<T>() where T : I1, new()
    {
        var c = new T();
        c?.Goo(c);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"");
            comp.VerifyIL("Program.Test1()", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (Program.C1 V_0) //c
  IL_0000:  newobj     ""Program.C1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0010
  IL_0009:  ldloc.0
  IL_000a:  ldloc.0
  IL_000b:  call       ""void Program.C1.Goo(Program.I1)""
  IL_0010:  ret
}
").VerifyIL("Program.Test2<T>()", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (T V_0) //c
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  box        ""T""
  IL_000c:  brfalse.s  IL_0021
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldloc.0
  IL_0011:  box        ""T""
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""void Program.I1.Goo(Program.I1)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessRace001()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        string s = ""hello"";

        System.Action a = () =>
        {
            for (int i = 0; i < 1000000; i++)
            {
                try
                {
                    s = s?.Length.ToString();
                    s = null;
                    Thread.Yield();
                } 
                catch (System.Exception ex)
                {
                    System.Console.WriteLine(ex);
                }
                finally
                {
                    s = s ?? ""hello"";
                }
            }
        };

        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);
        Task.Factory.StartNew(a);

        a();
        System.Console.WriteLine(""Success"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"Success");
        }

        [Fact(), WorkItem(836, "GitHub")]
        public void ConditionalMemberAccessRace002()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        string s = ""hello"";

        Test(s);
    }

    private static void Test<T>(T s) where T : IEnumerable<char>
    {
        Action a = () =>
        {
            for (int i = 0; i < 1000000; i++)
            {
                var temp = s;
                try
                {
                    s?.GetEnumerator();
                    s = default(T);
                    Thread.Yield();
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine(ex);
                }
                finally
                {
                    s = temp;
                }
            }
        };

        var tasks = new List<Task>();
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));
        tasks.Add(Task.Factory.StartNew(a));

        a();

        // wait for all tasks to exit or we may have
        // test issues when unloading ApDomain while threads still running in it
        Task.WaitAll(tasks.ToArray());

        System.Console.WriteLine(""Success"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"Success");
        }

        [Fact]
        public void ConditionalMemberAccessConditional001()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        Test1<string>(null);
        Test2<string>(null);
    }

    static string Test1<T>(T[] arr)
    {
        if (arr != null && arr.Length > 0)
        {
            return arr[0].ToString();
        }

        return ""none"";
    }

    static string Test2<T>(T[] arr)
    {
        if (arr?.Length > 0)
        {
            return arr[0].ToString();
        }

        return ""none"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"");
            comp.VerifyIL("Program.Test1<T>(T[])", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001c
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  brfalse.s  IL_001c
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.0
  IL_0009:  readonly.
  IL_000b:  ldelema    ""T""
  IL_0010:  constrained. ""T""
  IL_0016:  callvirt   ""string object.ToString()""
  IL_001b:  ret
  IL_001c:  ldstr      ""none""
  IL_0021:  ret
}
").VerifyIL("Program.Test2<T>(T[])", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001c
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  brfalse.s  IL_001c
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.0
  IL_0009:  readonly.
  IL_000b:  ldelema    ""T""
  IL_0010:  constrained. ""T""
  IL_0016:  callvirt   ""string object.ToString()""
  IL_001b:  ret
  IL_001c:  ldstr      ""none""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessConditional002()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        Test1<string>(null);
        Test2<string>(null);
    }

    static string Test1<T>(T[] arr)
    {
        if (!(arr != null && arr.Length > 0))
        {
            return ""none"";
        }

        return arr[0].ToString();
    }

    static string Test2<T>(T[] arr)
    {
        if (!(arr?.Length > 0))
        {
            return ""none"";
        }

        return arr[0].ToString();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"");
            comp.VerifyIL("Program.Test1<T>(T[])", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0007
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  brtrue.s   IL_000d
  IL_0007:  ldstr      ""none""
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.0
  IL_000f:  readonly.
  IL_0011:  ldelema    ""T""
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  ret
}
").VerifyIL("Program.Test2<T>(T[])", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0007
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  brtrue.s   IL_000d
  IL_0007:  ldstr      ""none""
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.0
  IL_000f:  readonly.
  IL_0011:  ldelema    ""T""
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessConditional003()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test1<string>(null));
        System.Console.WriteLine(Test2<string>(null));
        System.Console.WriteLine(Test1<string>(new string[] {}));
        System.Console.WriteLine(Test2<string>(new string[] {}));
        System.Console.WriteLine(Test1<string>(new string[] { System.String.Empty }));
        System.Console.WriteLine(Test2<string>(new string[] { System.String.Empty }));
    }

    static string Test1<T>(T[] arr1)
    {
        var arr = arr1;
        if (arr != null && arr.Length == 0)
        {
            return ""empty"";
        }

        return ""not empty"";
    }

    static string Test2<T>(T[] arr1)
    {
        var arr = arr1;
        if (!(arr?.Length != 0))
        {
            return ""empty"";
        }

        return ""not empty"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"not empty
not empty
empty
empty
not empty
not empty");
            comp.VerifyIL("Program.Test1<T>(T[])", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (T[] V_0) //arr
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_000f
  IL_0005:  ldloc.0
  IL_0006:  ldlen
  IL_0007:  brtrue.s   IL_000f
  IL_0009:  ldstr      ""empty""
  IL_000e:  ret
  IL_000f:  ldstr      ""not empty""
  IL_0014:  ret
}
").VerifyIL("Program.Test2<T>(T[])", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0008
  IL_0004:  pop
  IL_0005:  ldc.i4.1
  IL_0006:  br.s       IL_000c
  IL_0008:  ldlen
  IL_0009:  ldc.i4.0
  IL_000a:  cgt.un
  IL_000c:  brtrue.s   IL_0014
  IL_000e:  ldstr      ""empty""
  IL_0013:  ret
  IL_0014:  ldstr      ""not empty""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void ConditionalMemberAccessConditional004()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        var w = new WeakReference<string>(null);
        Test0(ref w);
        Test1(ref w);
        Test2(ref w);
        Test3(ref w);
    }

    static string Test0(ref WeakReference<string> slot)
    {
        string value = null;
        WeakReference<string> weak = slot;
        if (weak != null && weak.TryGetTarget(out value)) 
        {
            return value;
        }

        return ""hello"";
    }

    static string Test1(ref WeakReference<string> slot)
    {
        string value = null;
        WeakReference<string> weak = slot;
        if (weak?.TryGetTarget(out value) == true) 
        {
            return value;
        }

        return ""hello"";
    }

    static string Test2(ref WeakReference<string> slot)
    {

        string value = null;
        if (slot?.TryGetTarget(out value) == true) 
        {
            return value;
        }

        return ""hello"";
    }

    static string Test3(ref WeakReference<string> slot)
    {

        string value = null;
        if (slot?.TryGetTarget(out value) ?? false) 
        {
            return value;
        }

        return ""hello"";
    }
}
";
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "").
                VerifyIL("Program.Test0(ref System.WeakReference<string>)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (string V_0, //value
                System.WeakReference<string> V_1) //weak
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldind.ref
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  brfalse.s  IL_0014
  IL_0008:  ldloc.1
  IL_0009:  ldloca.s   V_0
  IL_000b:  callvirt   ""bool System.WeakReference<string>.TryGetTarget(out string)""
  IL_0010:  brfalse.s  IL_0014
  IL_0012:  ldloc.0
  IL_0013:  ret
  IL_0014:  ldstr      ""hello""
  IL_0019:  ret
}
").VerifyIL("Program.Test1(ref System.WeakReference<string>)", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //value
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldind.ref
  IL_0004:  dup
  IL_0005:  brtrue.s   IL_000b
  IL_0007:  pop
  IL_0008:  ldc.i4.0
  IL_0009:  br.s       IL_0012
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""bool System.WeakReference<string>.TryGetTarget(out string)""
  IL_0012:  brfalse.s  IL_0016
  IL_0014:  ldloc.0
  IL_0015:  ret
  IL_0016:  ldstr      ""hello""
  IL_001b:  ret
}
").VerifyIL("Program.Test2(ref System.WeakReference<string>)", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //value
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldind.ref
  IL_0004:  dup
  IL_0005:  brtrue.s   IL_000b
  IL_0007:  pop
  IL_0008:  ldc.i4.0
  IL_0009:  br.s       IL_0012
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""bool System.WeakReference<string>.TryGetTarget(out string)""
  IL_0012:  brfalse.s  IL_0016
  IL_0014:  ldloc.0
  IL_0015:  ret
  IL_0016:  ldstr      ""hello""
  IL_001b:  ret
}
").VerifyIL("Program.Test3(ref System.WeakReference<string>)", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //value
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldind.ref
  IL_0004:  dup
  IL_0005:  brtrue.s   IL_000b
  IL_0007:  pop
  IL_0008:  ldc.i4.0
  IL_0009:  br.s       IL_0012
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""bool System.WeakReference<string>.TryGetTarget(out string)""
  IL_0012:  brfalse.s  IL_0016
  IL_0014:  ldloc.0
  IL_0015:  ret
  IL_0016:  ldstr      ""hello""
  IL_001b:  ret
}
");
        }

        [WorkItem(1042288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042288")]
        [Fact]
        public void Bug1042288()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        var c1 = new C1();
        System.Console.WriteLine(c1?.M1() ?? (long)1000);
        return;
    }
}
class C1
{
    public int M1()
    {
        return 1;
    }
}

";
            var comp = CompileAndVerify(source, expectedOutput: @"1");
            comp.VerifyIL("Test.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (int? V_0,
                int? V_1)
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0014
  IL_0008:  pop
  IL_0009:  ldloca.s   V_1
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.1
  IL_0012:  br.s       IL_001e
  IL_0014:  call       ""int C1.M1()""
  IL_0019:  newobj     ""int?..ctor(int)""
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0030
  IL_0028:  ldc.i4     0x3e8
  IL_002d:  conv.i8
  IL_002e:  br.s       IL_0038
  IL_0030:  ldloca.s   V_0
  IL_0032:  call       ""int int?.GetValueOrDefault()""
  IL_0037:  conv.i8
  IL_0038:  call       ""void System.Console.WriteLine(long)""
  IL_003d:  ret
}
");
        }

        [WorkItem(470, "CodPlex")]
        [Fact]
        public void CodPlexBug470_01()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(MyMethod(null));
        System.Console.WriteLine(MyMethod(new MyType()));
    }

    public static decimal MyMethod(MyType myObject)
    {
        return myObject?.MyField ?? 0m;
    }
}

public class MyType
{
    public decimal MyField = 123;
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0
123");

            verifier.VerifyIL("C.MyMethod", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldsfld     ""decimal decimal.Zero""
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  ldfld      ""decimal MyType.MyField""
  IL_000f:  ret
}");
        }

        [WorkItem(470, "CodPlex")]
        [Fact]
        public void CodPlexBug470_02()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(MyMethod(null));
        System.Console.WriteLine(MyMethod(new MyType()));
    }

    public static decimal MyMethod(MyType myObject)
    {
        return myObject?.MyField ?? default(decimal);
    }
}

public class MyType
{
    public decimal MyField = 123;
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0
123");

            verifier.VerifyIL("C.MyMethod", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldsfld     ""decimal decimal.Zero""
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  ldfld      ""decimal MyType.MyField""
  IL_000f:  ret
}");
        }

        [WorkItem(470, "CodPlex")]
        [Fact]
        public void CodPlexBug470_03()
        {
            var source = @"
using System;

class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, ""{0}"", MyMethod(null)));
        System.Console.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, ""{0}"", MyMethod(new MyType())));
    }

    public static DateTime MyMethod(MyType myObject)
    {
        return myObject?.MyField ?? default(DateTime);
    }
}

public class MyType
{
    public DateTime MyField = new DateTime(100000000);
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"01/01/0001 00:00:00
01/01/0001 00:00:10");

            verifier.VerifyIL("C.MyMethod", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (System.DateTime V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""System.DateTime""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldfld      ""System.DateTime MyType.MyField""
  IL_0013:  ret
}");
        }

        [WorkItem(470, "CodPlex")]
        [Fact]
        public void CodPlexBug470_04()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(MyMethod(null).F);
        System.Console.WriteLine(MyMethod(new MyType()).F);
    }

    public static MyStruct MyMethod(MyType myObject)
    {
        return myObject?.MyField ?? default(MyStruct);
    }
}

public class MyType
{
    public MyStruct MyField = new MyStruct() {F = 123};
}

public struct MyStruct
{
    public int F;
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0
123");

            verifier.VerifyIL("C.MyMethod", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (MyStruct V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""MyStruct""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldfld      ""MyStruct MyType.MyField""
  IL_0013:  ret
}");
        }

        [WorkItem(1103294, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1103294")]
        [Fact]
        public void Bug1103294_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(""---"");
        Goo<int>(new C<int>());
        System.Console.WriteLine(""---"");
        Goo<int>(null);
        System.Console.WriteLine(""---"");
    }

    static void Goo<T>(C<T> x)
    {
        x?.M();
    }
}
 
class C<T>
{
    public T M()
    {
        System.Console.WriteLine(""M"");
        return default(T);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: @"---
M
---
---");

            verifier.VerifyIL("C.Goo<T>", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000a
  IL_0003:  ldarg.0
  IL_0004:  call       ""T C<T>.M()""
  IL_0009:  pop
  IL_000a:  ret
}");
        }

        [WorkItem(1103294, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1103294")]
        [Fact]
        public void Bug1103294_02()
        {
            var source = @"
unsafe class C
{
    static void Main()
    {
        System.Console.WriteLine(""---"");
        Goo(new C());
        System.Console.WriteLine(""---"");
        Goo(null);
        System.Console.WriteLine(""---"");
    }

    static void Goo(C x)
    {
        x?.M();
    }

    public int* M()
    {
        System.Console.WriteLine(""M"");
        return null;
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), verify: Verification.Fails, expectedOutput: @"---
M
---
---");

            verifier.VerifyIL("C.Goo", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  brtrue.s   IL_0006
  IL_0004:  br.s       IL_000d
  IL_0006:  ldarg.0
  IL_0007:  call       ""int* C.M()""
  IL_000c:  pop
  IL_000d:  ret
}");
        }

        [WorkItem(23422, "https://github.com/dotnet/roslyn/issues/23422")]
        [Fact]
        public void ConditionalRefLike()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(""---"");
        Goo(new C());
        System.Console.WriteLine(""---"");
        Goo(null);
        System.Console.WriteLine(""---"");
    }

    static void Goo(C x)
    {
        x?.M();
    }

    public RefLike M()
    {
        System.Console.WriteLine(""M"");
        return default;
    }

    public ref struct RefLike{}
}
";
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            var verifier = CompileAndVerify(source, verify: Verification.FailsILVerify, options: TestOptions.DebugExe.WithAllowUnsafe(true), expectedOutput: @"---
M
---
---");

            verifier.VerifyIL("C.Goo", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  brtrue.s   IL_0006
  IL_0004:  br.s       IL_000d
  IL_0006:  ldarg.0
  IL_0007:  call       ""C.RefLike C.M()""
  IL_000c:  pop
  IL_000d:  ret
}");
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_01()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        System.Console.WriteLine(""---"");
        C.F1(null);
        System.Console.WriteLine(""---"");
        C.F1(new C());
        System.Console.WriteLine(""---"");
        C.F2(null);
        System.Console.WriteLine(""---"");
        C.F2(new C());
        System.Console.WriteLine(""---"");
    }
}

class C 
{
    static public void F1(C c) 
    {
        System.Console.WriteLine(""F1"");
        Action a = () => c?.M();
        a();
    }

    static public void F2(C c) => c?.M();

    void M() => System.Console.WriteLine(""M"");
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"---
F1
---
F1
M
---
---
M
---");

            verifier.VerifyIL("C.<>c__DisplayClass0_0.<F1>b__0", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass0_0.c""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  pop
  IL_000a:  br.s       IL_0012
  IL_000c:  call       ""void C.M()""
  IL_0011:  nop
  IL_0012:  ret
}");

            verifier.VerifyIL("C.F2", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  br.s       IL_000c
  IL_0005:  ldarg.0
  IL_0006:  call       ""void C.M()""
  IL_000b:  nop
  IL_000c:  ret
}");
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_02()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
    }
}

class C 
{
    static public void F1(C c) 
    {
        System.Console.WriteLine(""F1"");
        Func<object> a = () => c?.M();
    }

    static public object F2(C c) => c?.M();

    static public object P1 => (new C())?.M();

    void M() => System.Console.WriteLine(""M"");
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
    // (16,32): error CS0029: Cannot implicitly convert type 'void' to 'object'
    //         Func<object> a = () => c?.M();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "c?.M()").WithArguments("void", "object").WithLocation(16, 32),
    // (16,32): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         Func<object> a = () => c?.M();
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "c?.M()").WithArguments("lambda expression").WithLocation(16, 32),
    // (19,37): error CS0029: Cannot implicitly convert type 'void' to 'object'
    //     static public object F2(C c) => c?.M();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "c?.M()").WithArguments("void", "object").WithLocation(19, 37),
    // (21,32): error CS0029: Cannot implicitly convert type 'void' to 'object'
    //     static public object P1 => (new C())?.M();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "(new C())?.M()").WithArguments("void", "object").WithLocation(21, 32)
                );
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_03()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        System.Console.WriteLine(""---"");
        C<int>.F1(null);
        System.Console.WriteLine(""---"");
        C<int>.F1(new C<int>());
        System.Console.WriteLine(""---"");
        C<int>.F2(null);
        System.Console.WriteLine(""---"");
        C<int>.F2(new C<int>());
        System.Console.WriteLine(""---"");
    }
}

class C<T> 
{
    static public void F1(C<T> c) 
    {
        System.Console.WriteLine(""F1"");
        Action a = () => c?.M();
        a();
    }

    static public void F2(C<T> c) => c?.M();

    T M() 
    {
        System.Console.WriteLine(""M"");
        return default(T);
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"---
F1
---
F1
M
---
---
M
---");

            verifier.VerifyIL("C<T>.<>c__DisplayClass0_0.<F1>b__0()", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C<T> C<T>.<>c__DisplayClass0_0.c""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  pop
  IL_000a:  br.s       IL_0012
  IL_000c:  call       ""T C<T>.M()""
  IL_0011:  pop
  IL_0012:  ret
}");

            verifier.VerifyIL("C<T>.F2", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  br.s       IL_000c
  IL_0005:  ldarg.0
  IL_0006:  call       ""T C<T>.M()""
  IL_000b:  pop
  IL_000c:  ret
}");
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_04()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
    }
}

class C<T> 
{
    static public void F1(C<T> c) 
    {
        Func<object> a = () => c?.M();
    }

    static public object F2(C<T> c) => c?.M();

    static public object P1 => (new C<T>())?.M();

    T M() 
    {
        return default(T);
    }
}
";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (15,34): error CS8977: 'T' cannot be made nullable.
                //         Func<object> a = () => c?.M();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("T").WithLocation(15, 34),
                // (18,42): error CS8977: 'T' cannot be made nullable.
                //     static public object F2(C<T> c) => c?.M();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("T").WithLocation(18, 42),
                // (20,45): error CS8977: 'T' cannot be made nullable.
                //     static public object P1 => (new C<T>())?.M();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("T").WithLocation(20, 45)
                );
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_05()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        System.Console.WriteLine(""---"");
        C.F1(null);
        System.Console.WriteLine(""---"");
        C.F1(new C());
        System.Console.WriteLine(""---"");
        C.F2(null);
        System.Console.WriteLine(""---"");
        C.F2(new C());
        System.Console.WriteLine(""---"");
    }
}

unsafe class C 
{
    static public void F1(C c) 
    {
        System.Console.WriteLine(""F1"");
        Action<object> a = o => c?.M();
        a(null);
    }

    static public void F2(C c) => c?.M();

    void* M() 
    {
        System.Console.WriteLine(""M"");
        return null;
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), verify: Verification.Fails, expectedOutput: @"---
F1
---
F1
M
---
---
M
---");

            verifier.VerifyIL("C.<>c__DisplayClass0_0.<F1>b__0", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass0_0.c""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  pop
  IL_000a:  br.s       IL_0012
  IL_000c:  call       ""void* C.M()""
  IL_0011:  pop
  IL_0012:  ret
}");

            verifier.VerifyIL("C.F2", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  br.s       IL_000c
  IL_0005:  ldarg.0
  IL_0006:  call       ""void* C.M()""
  IL_000b:  pop
  IL_000c:  ret
}");
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_06()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
    }
}

unsafe class C 
{
    static public void F1(C c) 
    {
        System.Console.WriteLine(""F1"");
        Func<object, object> a = o => c?.M();
    }

    static public object F2(C c) => c?.M();

    static public object P1 => (new C())?.M();

    void* M() 
    {
        System.Console.WriteLine(""M"");
        return null;
    }
}
";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
                // (16,41): error CS8977: 'void*' cannot be made nullable.
                //         Func<object, object> a = o => c?.M();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("void*").WithLocation(16, 41),
                // (19,39): error CS8977: 'void*' cannot be made nullable.
                //     static public object F2(C c) => c?.M();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("void*").WithLocation(19, 39),
                // (21,42): error CS8977: 'void*' cannot be made nullable.
                //     static public object P1 => (new C())?.M();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("void*").WithLocation(21, 42)
                );
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_07()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        C<int>.Test();
    }
}

class C<T> 
{
    public static void Test()
    {
        var x = new [] {null, new C<T>()};

        for (int i = 0; i < 2; x[i-1]?.M())
        {
            System.Console.WriteLine(""---"");
            System.Console.WriteLine(""Loop"");
            i++;
        }

        System.Console.WriteLine(""---"");
    }

    public T M() 
    {
        System.Console.WriteLine(""M"");
        return default(T);
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @" ---
Loop
---
Loop
M
---");
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_08()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        C<int>.Test();
    }
}

class C<T> 
{
    public static void Test()
    {
        var x = new [] {null, new C<T>()};
        
        System.Console.WriteLine(""---"");
        for (x[0]?.M(); false;)
        {
        }

        System.Console.WriteLine(""---"");
        for (x[1]?.M(); false;)
        {
        }

        System.Console.WriteLine(""---"");
    }

    public T M() 
    {
        System.Console.WriteLine(""M"");
        return default(T);
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"---
---
M
---");
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_09()
        {
            var source = @"
class Test
{
    static void Main()
    {
    }
}

class C<T> 
{
    public static void Test()
    {
        C<T> x = null;
        
        for (; x?.M();)
        {
        }
    }

    public T M() 
    {
        return default(T);
    }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (15,18): error CS8977: 'T' cannot be made nullable.
                //         for (; x?.M();)
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".M()").WithArguments("T").WithLocation(15, 18)
                );
        }

        [WorkItem(1109164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109164")]
        [Fact]
        public void Bug1109164_10()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        C<int>.Test();
    }
}

class C<T> 
{
    public static void Test()
    {
        System.Console.WriteLine(""---"");
        M1(a => a?.M(), null);
        System.Console.WriteLine(""---"");
        M1((a) => a?.M(), new C<T>());
        System.Console.WriteLine(""---"");
    }

    static void M1(Action<C<T>> x, C<T> y)
    {
        System.Console.WriteLine(""M1(Action<C<T>> x)"");
        x(y);
    }

    static void M1(Func<C<T>, object> x, C<T> y)
    {
        System.Console.WriteLine(""M1(Func<C<T>, object> x)"");
        x(y);
    }

    public T M() 
    {
        System.Console.WriteLine(""M"");
        return default(T);
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"---
M1(Action<C<T>> x)
---
M1(Action<C<T>> x)
M
---");
        }

        [WorkItem(74, "https://github.com/dotnet/roslyn/issues/74")]
        [Fact]
        public void ConditionalInAsyncTask()
        {
            var source = @"
#pragma warning disable CS1998 // suppress 'no await in async' warning
using System;
using System.Threading.Tasks;

class Goo<T>
{
    public T Method(int i)
    {
        Console.Write(i);
        return default(T); // returns value of unconstrained type parameter type
    }
    public void M1(Goo<T> x) => x?.Method(4);
    public async void M2(Goo<T> x) => x?.Method(5);
    public async Task M3(Goo<T> x) => x?.Method(6);
    public async Task M4() {
        Goo<T> a = new Goo<T>();
        Goo<T> b = null;

        Action f1 = async () => a?.Method(1);
        f1();
        f1 = async () => b?.Method(0);
        f1();

        Func<Task> f2 = async () => a?.Method(2);
        await f2();
        Func<Task> f3 = async () => b?.Method(3);
        await f3();

        M1(a); M1(b);
        M2(a); M2(b);
        await M3(a);
        await M3(b);
    }
}
class Program
{
    public static void Main()
    {
        // this will complete synchronously as there are no truly async ops.
        new Goo<int>().M4();
    }
}";
            var compilation = CreateCompilationWithMscorlib461(
                source, references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: "12456");
        }

        [WorkItem(825, "https://github.com/dotnet/roslyn/issues/825")]
        [Fact]
        public void ConditionalBoolExpr01()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(HasLength(null, 0));
    }

    static bool HasLength(string s, int len)
    {
        return s?.Length == len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"False");

            verifier.VerifyIL("C.HasLength", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int string.Length.get""
  IL_000b:  ldarg.1
  IL_000c:  ceq
  IL_000e:  ret
}");
        }

        [WorkItem(825, "https://github.com/dotnet/roslyn/issues/825")]
        [Fact]
        public void ConditionalBoolExpr01a()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(HasLength(null, 0));
    }

    static bool HasLength(string s, byte len)
    {
        return s?.Length == len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"False");

            verifier.VerifyIL("C.HasLength", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int string.Length.get""
  IL_000b:  ldarg.1
  IL_000c:  ceq
  IL_000e:  ret
}");
        }

        [WorkItem(825, "https://github.com/dotnet/roslyn/issues/825")]
        [WorkItem(5662, "https://github.com/dotnet/roslyn/issues/5662")]
        [Fact]
        public void ConditionalBoolExpr01b()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.WriteLine(HasLength(null, long.MaxValue));
        try
        {
            System.Console.WriteLine(HasLengthChecked(null, long.MaxValue));
        } 
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex.GetType().Name);
        }        
    }

    static bool HasLength(string s, long len)
    {
        return s?.Length == (int)(byte)len;
    }

    static bool HasLengthChecked(string s, long len)
    {
        checked
        {
            return s?.Length == (int)(byte)len;
        }
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"False
OverflowException");

            verifier.VerifyIL("C.HasLength", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int string.Length.get""
  IL_000b:  ldarg.1
  IL_000c:  conv.u1
  IL_000d:  ceq
  IL_000f:  ret
}").VerifyIL("C.HasLengthChecked", @"
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (int? V_0,
                int V_1,
                int? V_2)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.2
  IL_000c:  br.s       IL_0019
  IL_000e:  ldarg.0
  IL_000f:  call       ""int string.Length.get""
  IL_0014:  newobj     ""int?..ctor(int)""
  IL_0019:  stloc.0
  IL_001a:  ldarg.1
  IL_001b:  conv.ovf.u1
  IL_001c:  stloc.1
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""int int?.GetValueOrDefault()""
  IL_0024:  ldloc.1
  IL_0025:  ceq
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       ""bool int?.HasValue.get""
  IL_002e:  and
  IL_002f:  ret
}");
        }

        [Fact]
        public void ConditionalBoolExpr02()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.Write(HasLength(null, 0));
        System.Console.Write(HasLength(null, 3));
        System.Console.Write(HasLength(""q"", 2));
    }

    static bool HasLength(string s, int len)
    {
        return (s?.Length ?? 2) + 1 == len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"FalseTrueTrue");

            verifier.VerifyIL("C.HasLength", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.2
  IL_0004:  br.s       IL_000c
  IL_0006:  ldarg.0
  IL_0007:  call       ""int string.Length.get""
  IL_000c:  ldc.i4.1
  IL_000d:  add
  IL_000e:  ldarg.1
  IL_000f:  ceq
  IL_0011:  ret
}");
        }

        [Fact]
        public void ConditionalBoolExpr02a()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.Write(NotHasLength(null, 0));
        System.Console.Write(NotHasLength(null, 3));
        System.Console.Write(NotHasLength(""q"", 2));
    }

    static bool NotHasLength(string s, int len)
    {
        return s?.Length + 1 != len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"TrueTrueFalse");

            verifier.VerifyIL("C.NotHasLength", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.1
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""int string.Length.get""
  IL_000b:  ldc.i4.1
  IL_000c:  add
  IL_000d:  ldarg.1
  IL_000e:  ceq
  IL_0010:  ldc.i4.0
  IL_0011:  ceq
  IL_0013:  ret
}");
        }

        [Fact]
        public void ConditionalBoolExpr02b()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        System.Console.Write(NotHasLength(null, 0));
        System.Console.Write(NotHasLength(null, 3));
        System.Console.Write(NotHasLength(""q"", 2));
        System.Console.Write(NotHasLength(null, null));
    }

    static bool NotHasLength(string s, int? len)
    {
        return s?.Length + 1 != len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"TrueTrueFalseFalse");

            verifier.VerifyIL("C.NotHasLength", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000b
  IL_0003:  ldarga.s   V_1
  IL_0005:  call       ""bool int?.HasValue.get""
  IL_000a:  ret
  IL_000b:  ldarg.0
  IL_000c:  call       ""int string.Length.get""
  IL_0011:  ldc.i4.1
  IL_0012:  add
  IL_0013:  ldarg.1
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int int?.GetValueOrDefault()""
  IL_001c:  ceq
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""bool int?.HasValue.get""
  IL_0025:  and
  IL_0026:  ldc.i4.0
  IL_0027:  ceq
  IL_0029:  ret
}");
        }

        [Fact]
        public void ConditionalBoolExpr03()
        {
            var source = @"
    using System.Threading.Tasks;
    static class C
    {
        public static void Main()
        {
            System.Console.Write(HasLength(null, 0).Result);
            System.Console.Write(HasLength(null, 3).Result);
            System.Console.Write(HasLength(""q"", 2).Result);
        }

        static async Task<bool> HasLength(string s, int len)
        {
            return (s?.Goo(await Bar()) ?? await Bar() + await Bar()) + 1 == len;
        }

        static int Goo(this string s, int arg)
        {
            return s.Length;
        }

        static async Task<int> Bar()
        {
            await Task.Yield();
            return 1;
        }
    }


";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"FalseTrueTrue");
        }

        [Fact]
        public void ConditionalBoolExpr04()
        {
            var source = @"
    using System.Threading.Tasks;
    static class C
    {
       public static void Main()
        {
            System.Console.Write(HasLength((string)null, 0).Result);
            System.Console.Write(HasLength((string)null, 3).Result);
            System.Console.Write(HasLength(""q"", 2).Result);
        }

        static async Task<bool> HasLength<T>(T s, int len)
        {
            return (s?.Goo(await Bar()) ?? 2) + 1 == len;
        }

        static int Goo<T>(this T s, int arg)
        {
            return ((string)(object)s).Length;
        }

        static async Task<int> Bar()
        {
            await Task.Yield();
            return 1;
        }
    }


";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"FalseTrueTrue");
        }

        [Fact]
        public void ConditionalBoolExpr05()
        {
            var source = @"
    using System.Threading.Tasks;
    static class C
    {
       public static void Main()
        {
            System.Console.Write(HasLength((string)null, 0).Result);
            System.Console.Write(HasLength((string)null, 3).Result);
            System.Console.Write(HasLength(""q"", 2).Result);
        }

        static async Task<bool> HasLength<T>(T s, int len)
        {
            return (s?.Goo(await Bar(await Bar())) ?? 2) + 1 == len;
        }

        static int Goo<T>(this T s, int arg)
        {
            return ((string)(object)s).Length;
        }

        static async Task<int> Bar()
        {
            await Task.Yield();
            return 1;
        }

        static async Task<int> Bar(int arg)
        {
            await Task.Yield();
            return arg;
        }
    }


";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"FalseTrueTrue");
        }

        [Fact]
        public void ConditionalBoolExpr06()
        {
            var source = @"
    using System.Threading.Tasks;
    static class C
    {
        public static void Main()
        {
            System.Console.Write(HasLength(null, 0).Result);
            System.Console.Write(HasLength(null, 7).Result);
            System.Console.Write(HasLength(""q"", 7).Result);
        }

        static async Task<bool> HasLength(string s, int len)
        {
            System.Console.WriteLine(s?.Goo(await Bar())?.Goo(await Bar()) + ""#"");
            return s?.Goo(await Bar())?.Goo(await Bar()).Length == len;
        }

        static string Goo(this string s, string arg)
        {
            return s + arg;
        }

        static async Task<string> Bar()
        {
            await Task.Yield();
            return ""Bar"";
        }
    }
";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"#
False#
FalseqBarBar#
True");
        }

        [Fact]
        public void ConditionalBoolExpr07()
        {
            var source = @"
    using System.Threading.Tasks;
    static class C
    {
        public static void Main()
        {
            System.Console.WriteLine(Test(null).Result);
            System.Console.WriteLine(Test(""q"").Result);
        }

        static async Task<bool> Test(string s)
        {
            return (await Bar(s))?.Goo(await Bar())?.ToString()?.Length > 1;
        }

        static string Goo(this string s, string arg1)
        {
            return s + arg1;
        }

        static async Task<string> Bar()
        {
            await Task.Yield();
            return ""Bar"";
        }

        static async Task<string> Bar(string arg)
        {
            await Task.Yield();
            return arg;
        }
    }
";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True");
        }

        [Fact]
        public void ConditionalBoolExpr08()
        {
            var source = @"
    using System.Threading.Tasks;
    static class C
    {
        public static void Main()
        {
            System.Console.WriteLine(Test(null).Result);
            System.Console.WriteLine(Test(""q"").Result);
        }

        static async Task<bool> Test(string s)
        {
            return (await Bar(s))?.Insert(0, await Bar())?.ToString()?.Length > 1;
        }

        static async Task<string> Bar()
        {
            await Task.Yield();
            return ""Bar"";
        }

        static async Task<dynamic> Bar(string arg)
        {
            await Task.Yield();
            return arg;
        }
    }";
            var c = CreateCompilationWithMscorlib461(source, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, TestOptions.ReleaseExe);
            var comp = CompileAndVerify(c, expectedOutput: @"False
True");
        }

        [Fact]
        public void ConditionalUserDef01()
        {
            var source = @"
class C
{
    struct S1
    {
        public static bool operator ==(S1? x, S1?y)
        {
            System.Console.Write(""=="");
            return true;
        }

        public static bool operator !=(S1? x, S1? y)
        {
            System.Console.Write(""!="");
            return false;
        }

    }

    class C1
    {
        public S1 Goo()
        {
            return new S1();
        }
    }

    public static void Main()
    {
        System.Console.WriteLine(TestEq(null, new S1()));
        System.Console.WriteLine(TestEq(new C1(), new S1()));

        System.Console.WriteLine(TestNeq(null, new S1()));
        System.Console.WriteLine(TestNeq(new C1(), new S1()));
    }

    static bool TestEq(C1 c, S1 arg)
    {
        return c?.Goo() == arg;
    }

    static bool TestNeq(C1 c, S1 arg)
    {
        return c?.Goo() != arg;
    }

}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"==True
==True
!=False
!=False");

            verifier.VerifyIL("C.TestNeq", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (C.S1? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""C.S1?""
  IL_000b:  ldloc.0
  IL_000c:  br.s       IL_0019
  IL_000e:  ldarg.0
  IL_000f:  call       ""C.S1 C.C1.Goo()""
  IL_0014:  newobj     ""C.S1?..ctor(C.S1)""
  IL_0019:  ldarg.1
  IL_001a:  newobj     ""C.S1?..ctor(C.S1)""
  IL_001f:  call       ""bool C.S1.op_Inequality(C.S1?, C.S1?)""
  IL_0024:  ret
}");
        }

        [Fact]
        public void ConditionalUserDef01n()
        {
            var source = @"
class C
{
    struct S1
    {
        public static bool operator ==(S1? x, S1?y)
        {
            System.Console.Write(""=="");
            return true;
        }

        public static bool operator !=(S1? x, S1? y)
        {
            System.Console.Write(""!="");
            return false;
        }

    }

    class C1
    {
        public S1 Goo()
        {
            return new S1();
        }
    }

    public static void Main()
    {
        System.Console.WriteLine(TestEq(null, new S1()));
        System.Console.WriteLine(TestEq(new C1(), new S1()));
        System.Console.WriteLine(TestEq(new C1(), null));

        System.Console.WriteLine(TestNeq(null, new S1()));
        System.Console.WriteLine(TestNeq(new C1(), new S1()));
        System.Console.WriteLine(TestNeq(new C1(), null));
    }

    static bool TestEq(C1 c, S1? arg)
    {
        return c?.Goo() == arg;
    }

    static bool TestNeq(C1 c, S1? arg)
    {
        return c?.Goo() != arg;
    }

}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"==True
==True
==True
!=False
!=False
!=False");

            verifier.VerifyIL("C.TestNeq", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (C.S1? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""C.S1?""
  IL_000b:  ldloc.0
  IL_000c:  br.s       IL_0019
  IL_000e:  ldarg.0
  IL_000f:  call       ""C.S1 C.C1.Goo()""
  IL_0014:  newobj     ""C.S1?..ctor(C.S1)""
  IL_0019:  ldarg.1
  IL_001a:  call       ""bool C.S1.op_Inequality(C.S1?, C.S1?)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void ConditionalUserDef02()
        {
            var source = @"
class C
{
    struct S1
    {
        public static bool operator ==(S1 x, S1 y)
        {
            System.Console.Write(""=="");
            return true;
        }

        public static bool operator !=(S1 x, S1 y)
        {
            System.Console.Write(""!="");
            return false;
        }

    }

    class C1
    {
        public S1 Goo()
        {
            return new S1();
        }
    }

    public static void Main()
    {
        System.Console.WriteLine(TestEq(null, new S1()));
        System.Console.WriteLine(TestEq(new C1(), new S1()));

        System.Console.WriteLine(TestNeq(null, new S1()));
        System.Console.WriteLine(TestNeq(new C1(), new S1()));
    }

    static bool TestEq(C1 c, S1 arg)
    {
        return c?.Goo() == arg;
    }

    static bool TestNeq(C1 c, S1 arg)
    {
        return c?.Goo() != arg;
    }

}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"False
==True
True
!=False");

            verifier.VerifyIL("C.TestNeq", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.1
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       ""C.S1 C.C1.Goo()""
  IL_000b:  ldarg.1
  IL_000c:  call       ""bool C.S1.op_Inequality(C.S1, C.S1)""
  IL_0011:  ret
}");
        }

        [Fact]
        public void ConditionalUserDef02n()
        {
            var source = @"
class C
{
    struct S1
    {
        public static bool operator ==(S1 x, S1 y)
        {
            System.Console.Write(""=="");
            return true;
        }

        public static bool operator !=(S1 x, S1 y)
        {
            System.Console.Write(""!="");
            return false;
        }

    }

    class C1
    {
        public S1 Goo()
        {
            return new S1();
        }
    }

    public static void Main()
    {
        System.Console.WriteLine(TestEq(null, new S1()));
        System.Console.WriteLine(TestEq(new C1(), new S1()));
        System.Console.WriteLine(TestEq(new C1(), null));

        System.Console.WriteLine(TestNeq(null, new S1()));
        System.Console.WriteLine(TestNeq(new C1(), new S1()));
        System.Console.WriteLine(TestNeq(new C1(), null));
    }

    static bool TestEq(C1 c, S1? arg)
    {
        return c?.Goo() == arg;
    }

    static bool TestNeq(C1 c, S1? arg)
    {
        return c?.Goo() != arg;
    }

}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"False
==True
False
True
!=False
True");

            verifier.VerifyIL("C.TestNeq", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C.S1 V_0,
                C.S1? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000b
  IL_0003:  ldarga.s   V_1
  IL_0005:  call       ""bool C.S1?.HasValue.get""
  IL_000a:  ret
  IL_000b:  ldarg.0
  IL_000c:  call       ""C.S1 C.C1.Goo()""
  IL_0011:  stloc.0
  IL_0012:  ldarg.1
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""bool C.S1?.HasValue.get""
  IL_001b:  brtrue.s   IL_001f
  IL_001d:  ldc.i4.1
  IL_001e:  ret
  IL_001f:  ldloc.0
  IL_0020:  ldloca.s   V_1
  IL_0022:  call       ""C.S1 C.S1?.GetValueOrDefault()""
  IL_0027:  call       ""bool C.S1.op_Inequality(C.S1, C.S1)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void Bug1()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        var c1 = new C1();
        M1(c1);
        M2(c1);
    }
    static void M1(C1 c1)
    {
        if (c1?.P == 1) Console.WriteLine(1);
    }
    static void M2(C1 c1)
    {
        if (c1 != null && c1.P == 1) Console.WriteLine(1);
    }
}
class C1
{
    public int P => 1;
}

";
            var comp = CompileAndVerify(source, expectedOutput: @"1
1");
            comp.VerifyIL("Test.M1", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  call       ""int C1.P.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_0012
  IL_000c:  ldc.i4.1
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  ret
}
");
            comp.VerifyIL("Test.M2", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int C1.P.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_0012
  IL_000c:  ldc.i4.1
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  ret
}
");
        }
        [Fact]
        public void ConditionalBoolExpr02ba()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(NotHasLength(null, 0));
        System.Console.Write(NotHasLength(null, 3));
        System.Console.Write(NotHasLength(1, 2));
    }

    static bool NotHasLength(int? s, int len)
    {
        return s?.GetHashCode() + 1 != len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"TrueTrueFalse");

            verifier.VerifyIL("C.NotHasLength", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  ldc.i4.1
  IL_000a:  ret
  IL_000b:  ldarga.s   V_0
  IL_000d:  call       ""int int?.GetValueOrDefault()""
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       ""int int.GetHashCode()""
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  ldarg.1
  IL_001d:  ceq
  IL_001f:  ldc.i4.0
  IL_0020:  ceq
  IL_0022:  ret
}
");
        }

        [Fact]
        public void ConditionalBoolExpr02bb()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(NotHasLength(null, 0));
        System.Console.Write(NotHasLength(null, 3));
        System.Console.Write(NotHasLength(1, 2));
        System.Console.Write(NotHasLength(null, null));
    }

    static bool NotHasLength(int? s, int? len)
    {
        return s?.GetHashCode() + 1 != len;
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"TrueTrueFalseFalse");

            verifier.VerifyIL("C.NotHasLength", @"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (int? V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brtrue.s   IL_0011
  IL_0009:  ldarga.s   V_1
  IL_000b:  call       ""bool int?.HasValue.get""
  IL_0010:  ret
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       ""int int?.GetValueOrDefault()""
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_1
  IL_001b:  call       ""int int.GetHashCode()""
  IL_0020:  ldc.i4.1
  IL_0021:  add
  IL_0022:  ldarg.1
  IL_0023:  stloc.0
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""int int?.GetValueOrDefault()""
  IL_002b:  ceq
  IL_002d:  ldloca.s   V_0
  IL_002f:  call       ""bool int?.HasValue.get""
  IL_0034:  and
  IL_0035:  ldc.i4.0
  IL_0036:  ceq
  IL_0038:  ret
}");
        }

        [Fact]
        public void ConditionalUnary()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        var x = - - -((string)null)?.Length  ??  - - -string.Empty?.Length;

        System.Console.WriteLine(x);
    }
}

";
            var verifier = CompileAndVerify(source, expectedOutput: @"0");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (int? V_0)
  IL_0000:  ldsfld     ""string string.Empty""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0014
  IL_0008:  pop
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_0021
  IL_0014:  call       ""int string.Length.get""
  IL_0019:  neg
  IL_001a:  neg
  IL_001b:  neg
  IL_001c:  newobj     ""int?..ctor(int)""
  IL_0021:  box        ""int?""
  IL_0026:  call       ""void System.Console.WriteLine(object)""
  IL_002b:  ret
}
");
        }

        [WorkItem(7388, "https://github.com/dotnet/roslyn/issues/7388")]
        [Fact]
        public void ConditionalClassConstrained001()
        {
            var source = @"
using System;

namespace ConsoleApplication9
{
    class Program
    {
        static void Main(string[] args)
        {
            var v = new A<object>();
            System.Console.WriteLine(A<object>.Test(v));
        }

        public class A<T> : object where T : class
        {
            public T Value { get { return (T)(object)42; }}

            public static T Test(A<T> val)
            {
                return val?.Value;
            }
        }
    }
}


";
            var verifier = CompileAndVerify(source, expectedOutput: @"42");

            verifier.VerifyIL("ConsoleApplication9.Program.A<T>.Test(ConsoleApplication9.Program.A<T>)", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  call       ""T ConsoleApplication9.Program.A<T>.Value.get""
  IL_0013:  ret
}");
        }

        [Fact, WorkItem(15670, "https://github.com/dotnet/roslyn/issues/15670")]
        public void ConditionalAccessOffOfUnconstrainedDefault1()
        {
            var source = @"
using System;

public class Test<T>
{
    public string Run()
    {
        return default(T)?.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<string>().Run());
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<int>().Run());
        Console.WriteLine(""--"");
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput:
@"--

--
0
--");

            verifier.VerifyIL("Test<T>.Run", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0,
                string V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  dup
  IL_0004:  initobj    ""T""
  IL_000a:  dup
  IL_000b:  ldobj      ""T""
  IL_0010:  box        ""T""
  IL_0015:  brtrue.s   IL_001b
  IL_0017:  pop
  IL_0018:  ldnull
  IL_0019:  br.s       IL_0026
  IL_001b:  constrained. ""T""
  IL_0021:  callvirt   ""string object.ToString()""
  IL_0026:  stloc.1
  IL_0027:  br.s       IL_0029
  IL_0029:  ldloc.1
  IL_002a:  ret
}");
        }

        [Fact, WorkItem(15670, "https://github.com/dotnet/roslyn/issues/15670")]
        public void ConditionalAccessOffOfUnconstrainedDefault2()
        {
            var source = @"
using System;

public class Test<T>
{
    public string Run()
    {
        var v = default(T);
        return v?.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<string>().Run());
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<int>().Run());
        Console.WriteLine(""--"");
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput:
@"--

--
0
--");

            verifier.VerifyIL("Test<T>.Run", @"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (T V_0, //v
                T V_1,
                string V_2)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""T""
  IL_0013:  ldloc.1
  IL_0014:  box        ""T""
  IL_0019:  brtrue.s   IL_002f
  IL_001b:  ldobj      ""T""
  IL_0020:  stloc.1
  IL_0021:  ldloca.s   V_1
  IL_0023:  ldloc.1
  IL_0024:  box        ""T""
  IL_0029:  brtrue.s   IL_002f
  IL_002b:  pop
  IL_002c:  ldnull
  IL_002d:  br.s       IL_003a
  IL_002f:  constrained. ""T""
  IL_0035:  callvirt   ""string object.ToString()""
  IL_003a:  stloc.2
  IL_003b:  br.s       IL_003d
  IL_003d:  ldloc.2
  IL_003e:  ret
}");
        }

        [Fact, WorkItem(15670, "https://github.com/dotnet/roslyn/issues/15670")]
        public void ConditionalAccessOffOfInterfaceConstrainedDefault1()
        {
            var source = @"
using System;

public class Test<T> where T : IComparable
{
    public string Run()
    {
        return default(T)?.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<string>().Run());
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<int>().Run());
        Console.WriteLine(""--"");
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput:
@"--

--
0
--");

            verifier.VerifyIL("Test<T>.Run", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (T V_0,
                string V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldnull
  IL_0012:  br.s       IL_0028
  IL_0014:  ldloca.s   V_0
  IL_0016:  dup
  IL_0017:  initobj    ""T""
  IL_001d:  constrained. ""T""
  IL_0023:  callvirt   ""string object.ToString()""
  IL_0028:  stloc.1
  IL_0029:  br.s       IL_002b
  IL_002b:  ldloc.1
  IL_002c:  ret
}");
        }

        [Fact, WorkItem(15670, "https://github.com/dotnet/roslyn/issues/15670")]
        public void ConditionalAccessOffOfInterfaceConstrainedDefault2()
        {
            var source = @"
using System;

public class Test<T> where T : IComparable
{
    public string Run()
    {
        var v = default(T);
        return v?.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<string>().Run());
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<int>().Run());
        Console.WriteLine(""--"");
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput:
@"--

--
0
--");

            verifier.VerifyIL("Test<T>.Run", @"
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (T V_0, //v
                string V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldnull
  IL_0012:  br.s       IL_0021
  IL_0014:  ldloca.s   V_0
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  stloc.1
  IL_0022:  br.s       IL_0024
  IL_0024:  ldloc.1
  IL_0025:  ret
}");
        }

        [Fact, WorkItem(15670, "https://github.com/dotnet/roslyn/issues/15670")]
        public void ConditionalAccessOffOfClassConstrainedDefault1()
        {
            var source = @"
using System;

public class Test<T> where T : class
{
    public string Run()
    {
        return default(T)?.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<string>().Run());
        Console.WriteLine(""--"");
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput:
@"--

--");

            verifier.VerifyIL("Test<T>.Run", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (string V_0)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0005
  IL_0005:  ldloc.0
  IL_0006:  ret
}");
        }

        [Fact, WorkItem(15670, "https://github.com/dotnet/roslyn/issues/15670")]
        public void ConditionalAccessOffOfClassConstrainedDefault2()
        {
            var source = @"
using System;

public class Test<T> where T : class
{
    public string Run()
    {
        var v = default(T);
        return v?.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""--"");
        Console.WriteLine(new Test<string>().Run());
        Console.WriteLine(""--"");
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput:
@"--

--");

            verifier.VerifyIL("Test<T>.Run", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (T V_0, //v
                string V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_0016
  IL_0012:  pop
  IL_0013:  ldnull
  IL_0014:  br.s       IL_001b
  IL_0016:  callvirt   ""string object.ToString()""
  IL_001b:  stloc.1
  IL_001c:  br.s       IL_001e
  IL_001e:  ldloc.1
  IL_001f:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void ConditionalAccessOffReadOnlyNullable1()
        {
            var source = @"
using System;

class Program
{
    private static readonly Guid? g = null;

    static void Main()
    {
        Console.WriteLine(g?.ToString());
    }
}
";
            var comp = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"", verify: Verification.Fails);

            comp.VerifyIL("Program.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (System.Guid V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""System.Guid? Program.g""
  IL_0006:  dup
  IL_0007:  call       ""bool System.Guid?.HasValue.get""
  IL_000c:  brtrue.s   IL_0012
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  br.s       IL_0025
  IL_0012:  call       ""System.Guid System.Guid?.GetValueOrDefault()""
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  constrained. ""System.Guid""
  IL_0020:  callvirt   ""string object.ToString()""
  IL_0025:  call       ""void System.Console.WriteLine(string)""
  IL_002a:  nop
  IL_002b:  ret
}");

            comp = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Passes);

            comp.VerifyIL("Program.Main", @"
{
	// Code size       47 (0x2f)
	.maxstack  2
	.locals init (System.Guid? V_0,
	            System.Guid V_1)
	IL_0000:  nop
	IL_0001:  ldsfld     ""System.Guid? Program.g""
	IL_0006:  stloc.0
	IL_0007:  ldloca.s   V_0
	IL_0009:  dup
	IL_000a:  call       ""bool System.Guid?.HasValue.get""
	IL_000f:  brtrue.s   IL_0015
	IL_0011:  pop
	IL_0012:  ldnull
	IL_0013:  br.s       IL_0028
	IL_0015:  call       ""System.Guid System.Guid?.GetValueOrDefault()""
	IL_001a:  stloc.1
	IL_001b:  ldloca.s   V_1
	IL_001d:  constrained. ""System.Guid""
	IL_0023:  callvirt   ""string object.ToString()""
	IL_0028:  call       ""void System.Console.WriteLine(string)""
	IL_002d:  nop
	IL_002e:  ret
}");
        }

        [Fact]
        public void ConditionalAccessOffReadOnlyNullable2()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(default(Guid?)?.ToString());
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"");

            verifier.VerifyIL("Program.Main", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Guid? V_0,
                System.Guid V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  dup
  IL_0004:  initobj    ""System.Guid?""
  IL_000a:  call       ""bool System.Guid?.HasValue.get""
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldnull
  IL_0012:  br.s       IL_0030
  IL_0014:  ldloca.s   V_0
  IL_0016:  dup
  IL_0017:  initobj    ""System.Guid?""
  IL_001d:  call       ""System.Guid System.Guid?.GetValueOrDefault()""
  IL_0022:  stloc.1
  IL_0023:  ldloca.s   V_1
  IL_0025:  constrained. ""System.Guid""
  IL_002b:  callvirt   ""string object.ToString()""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  nop
  IL_0036:  ret
}");
        }

        [Fact]
        [WorkItem(23351, "https://github.com/dotnet/roslyn/issues/23351")]
        public void ConditionalAccessOffConstrainedTypeParameter_Property()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var obj1 = new MyObject1 { MyDate = new DateTime(636461511000000000L) };
        var obj2 = new MyObject2<MyObject1>(obj1);

        System.Console.WriteLine(obj1.MyDate.Ticks);
        System.Console.WriteLine(obj2.CurrentDate.Value.Ticks);
        System.Console.WriteLine(new MyObject2<MyObject1>(null).CurrentDate.HasValue);
    }
}

abstract class MyBaseObject1
{
    public DateTime MyDate { get; set; }
}

class MyObject1 : MyBaseObject1
{ }

class MyObject2<MyObjectType> where MyObjectType : MyBaseObject1, new()
{
    public MyObject2(MyObjectType obj)
    {
        m_CurrentObject1 = obj;
    }

    private MyObjectType m_CurrentObject1 = null;
    public MyObjectType CurrentObject1 => m_CurrentObject1;
    public DateTime? CurrentDate => CurrentObject1?.MyDate;
}
";

            var expectedOutput =
@"
636461511000000000
636461511000000000
False
";
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(23351, "https://github.com/dotnet/roslyn/issues/23351")]
        public void ConditionalAccessOffConstrainedTypeParameter_Field()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var obj1 = new MyObject1 { MyDate = new DateTime(636461511000000000L) };
        var obj2 = new MyObject2<MyObject1>(obj1);

        System.Console.WriteLine(obj1.MyDate.Ticks);
        System.Console.WriteLine(obj2.CurrentDate.Value.Ticks);
        System.Console.WriteLine(new MyObject2<MyObject1>(null).CurrentDate.HasValue);
    }
}

abstract class MyBaseObject1
{
    public DateTime MyDate;
}

class MyObject1 : MyBaseObject1
{ }

class MyObject2<MyObjectType> where MyObjectType : MyBaseObject1, new()
{
    public MyObject2(MyObjectType obj)
    {
        m_CurrentObject1 = obj;
    }

    private MyObjectType m_CurrentObject1 = null;
    public MyObjectType CurrentObject1 => m_CurrentObject1;
    public DateTime? CurrentDate => CurrentObject1?.MyDate;
}
";

            var expectedOutput =
@"
636461511000000000
636461511000000000
False
";
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(57629, "https://github.com/dotnet/roslyn/issues/57629")]
        public void Issue57629()
        {
            var source = @"
namespace OperatorQuestionmarkProblem
{
    public class OuterClass<TValue>
    {
        public class InnerClass
        {
            public TValue SomeInfo() { throw null; }

            public InnerClass Next { get; set; }

            void Test()
            {
                Next?.SomeInfo();
                _ = Next?.SomeInfo();
            }
        }
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyEmitDiagnostics(
                // (15,26): error CS8977: 'TValue' cannot be made nullable.
                //                 _ = Next?.SomeInfo();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".SomeInfo()").WithArguments("TValue").WithLocation(15, 26)
                );
        }
    }
}
