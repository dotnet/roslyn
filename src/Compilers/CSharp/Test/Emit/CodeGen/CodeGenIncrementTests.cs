// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenIncrementTests : CSharpTestBase
    {
        //{0} is a numeric type
        //{1} is some value
        //{2} is one greater than {1}
        private const string NUMERIC_INCREMENT_TEMPLATE = @"
using System.Globalization;

class C
{{
    static void Main()
    {{
        {0} x = {1};
        {0} y = x++;
        System.Console.WriteLine(x.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(y.ToString(CultureInfo.InvariantCulture));
        
        x = {1};
        y = ++x;
        System.Console.WriteLine(x.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(y.ToString(CultureInfo.InvariantCulture));

        x = {2};
        y = x--;
        System.Console.WriteLine(x.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(y.ToString(CultureInfo.InvariantCulture));

        x = {2};
        y = --x;
        System.Console.WriteLine(x.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(y.ToString(CultureInfo.InvariantCulture));
    }}
}}
";

        //{0} is some value, {1} is one greater
        private const string NUMERIC_OUTPUT_TEMPLATE = @"
{1}
{0}
{1}
{1}
{0}
{1}
{0}
{0}
";

        [Fact]
        public void TestIncrementInt()
        {
            TestIncrementCompilationAndOutput<int>(int.MaxValue, int.MinValue);
        }

        [Fact]
        public void TestIncrementUInt()
        {
            TestIncrementCompilationAndOutput<uint>(uint.MaxValue, uint.MinValue);
        }

        [Fact]
        public void TestIncrementLong()
        {
            TestIncrementCompilationAndOutput<long>(long.MaxValue, long.MinValue);
        }

        [Fact]
        public void TestIncrementULong()
        {
            TestIncrementCompilationAndOutput<ulong>(ulong.MaxValue, ulong.MinValue);
        }

        [Fact]
        public void TestIncrementSByte()
        {
            TestIncrementCompilationAndOutput<sbyte>(sbyte.MaxValue, sbyte.MinValue);
        }

        [Fact]
        public void TestIncrementByte()
        {
            TestIncrementCompilationAndOutput<byte>(byte.MaxValue, byte.MinValue);
        }

        [Fact]
        public void TestIncrementShort()
        {
            TestIncrementCompilationAndOutput<short>(short.MaxValue, short.MinValue);
        }

        [Fact]
        public void TestIncrementUShort()
        {
            TestIncrementCompilationAndOutput<int>(int.MaxValue, int.MinValue);
        }

        [Fact]
        public void TestIncrementFloat()
        {
            TestIncrementCompilationAndOutput<float>(0, 1);
        }

        [Fact]
        [WorkItem(32576, "https://github.com/dotnet/roslyn/issues/32576")]
        public void TestIncrementDecimal()
        {
            TestIncrementCompilationAndOutput<decimal>(-1, 0);
        }

        [Fact]
        public void TestIncrementDouble()
        {
            TestIncrementCompilationAndOutput<double>(-0.5, 0.5);
        }

        [Fact]
        public void TestIncrementChar()
        {
            string source = string.Format(NUMERIC_INCREMENT_TEMPLATE, typeof(char).FullName, "'a'", "'b'");
            string expectedOutput = string.Format(NUMERIC_OUTPUT_TEMPLATE, 'a', 'b');

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestIncrementEnum()
        {
            string source = @"
class C
{
    enum E
    {
        A = 13,
        B,
    }
    static void Main()
    {
        E x = E.A;
        E y = x++;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
        
        x = E.A;
        y = ++x;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);

        x = E.B;
        y = x--;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);

        x = E.B;
        y = --x;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);

        x = E.B;
        y = x++;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);

        x = E.B;
        y = ++x;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);

        x = E.A;
        y = x--;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);

        x = E.A;
        y = --x;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}
";

            string expectedOutput = @"
B
A
B
B
A
B
A
A
15
B
15
15
12
A
12
12
";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestIncrementNonLocal()
        {
            string source = @"
class C
{
    int field;
    double[] arrayField;
    uint Property { get; set; }

    static char staticField;
    static short[] staticArrayField;
    static sbyte StaticProperty { get; set; }

    static void Main()
    {
        C c = new C();

        c.field = 2;
        int fieldTmp = c.field++;
        System.Console.WriteLine(c.field);
        System.Console.WriteLine(fieldTmp);

        c.arrayField = new double[] { 3, 4 };
        double arrayFieldTmp = ++c.arrayField[0];
        System.Console.WriteLine(c.arrayField[0]);
        System.Console.WriteLine(arrayFieldTmp);

        c.Property = 5;
        uint propertyTmp = c.Property--;
        System.Console.WriteLine(c.Property);
        System.Console.WriteLine(propertyTmp);
         
        C.staticField = 'b';
        char staticFieldTmp = --C.staticField;
        System.Console.WriteLine(C.staticField);
        System.Console.WriteLine(staticFieldTmp);

        C.staticArrayField = new short[] { 6, 7 };
        short staticArrayFieldTmp = C.staticArrayField[1]++;
        System.Console.WriteLine(C.staticArrayField[1]);
        System.Console.WriteLine(staticArrayFieldTmp);

        C.StaticProperty = 8;
        sbyte staticPropertyTmp = C.StaticProperty++;
        System.Console.WriteLine(C.StaticProperty);
        System.Console.WriteLine(staticPropertyTmp);
    }
}
";

            string expectedOutput = @"
3
2
4
4
4
5
a
a
8
7
9
8
";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestIncrementIL()
        {
            string source = @"
class C
{
    enum E { A, B }
    static void Main()
    {
        sbyte a = 1;
        byte b = 1;
        short c = 1;
        ushort d = 1;
        int e = 1;
        uint f = 1;
        long g = 1;
        ulong h = 1;
        char i = (char)1;
        float j = 1;
        decimal k = 1;
        double l = 1;
        E m = E.A;

        sbyte a2;
        byte b2;
        short c2;
        ushort d2;
        int e2;
        uint f2;
        long g2;
        ulong h2;
        char i2;
        float j2;
        decimal k2;
        double l2;
        E m2;

        a2 = a++;
        System.Console.WriteLine(a2);
        a2 = ++a;
        System.Console.WriteLine(a2);
        a2 = a--;
        System.Console.WriteLine(a2);
        a2 = --a;
        System.Console.WriteLine(a2);

        b2 = b++;
        System.Console.WriteLine(b2);
        b2 = ++b;
        System.Console.WriteLine(b2);
        b2 = b--;
        System.Console.WriteLine(b2);
        b2 = --b;
        System.Console.WriteLine(b2);

        c2 = c++;
        System.Console.WriteLine(c2);
        c2 = ++c;
        System.Console.WriteLine(c2);
        c2 = c--;
        System.Console.WriteLine(c2);
        c2 = --c;
        System.Console.WriteLine(c2);

        d2 = d++;
        System.Console.WriteLine(d2);
        d2 = ++d;
        System.Console.WriteLine(d2);
        d2 = d--;
        System.Console.WriteLine(d2);
        d2 = --d;
        System.Console.WriteLine(d2);

        e2 = e++;
        System.Console.WriteLine(e2);
        e2 = ++e;
        System.Console.WriteLine(e2);
        e2 = e--;
        System.Console.WriteLine(e2);
        e2 = --e;
        System.Console.WriteLine(e2);

        f2 = f++;
        System.Console.WriteLine(f2);
        f2 = ++f;
        System.Console.WriteLine(f2);
        f2 = f--;
        System.Console.WriteLine(f2);
        f2 = --f;
        System.Console.WriteLine(f2);

        g2 = g++;
        System.Console.WriteLine(g2);
        g2 = ++g;
        System.Console.WriteLine(g2);
        g2 = g--;
        System.Console.WriteLine(g2);
        g2 = --g;
        System.Console.WriteLine(g2);

        h2 = h++;
        System.Console.WriteLine(h2);
        h2 = ++h;
        System.Console.WriteLine(h2);
        h2 = h--;
        System.Console.WriteLine(h2);
        h2 = --h;
        System.Console.WriteLine(h2);

        i2 = i++;
        System.Console.WriteLine(i2);
        i2 = ++i;
        System.Console.WriteLine(i2);
        i2 = i--;
        System.Console.WriteLine(i2);
        i2 = --i;
        System.Console.WriteLine(i2);

        j2 = j++;
        System.Console.WriteLine(j2);
        j2 = ++j;
        System.Console.WriteLine(j2);
        j2 = j--;
        System.Console.WriteLine(j2);
        j2 = --j;
        System.Console.WriteLine(j2);

        k2 = k++;
        System.Console.WriteLine(k2);
        k2 = ++k;
        System.Console.WriteLine(k2);
        k2 = k--;
        System.Console.WriteLine(k2);
        k2 = --k;
        System.Console.WriteLine(k2);

        l2 = l++;
        System.Console.WriteLine(l2);
        l2 = ++l;
        System.Console.WriteLine(l2);
        l2 = l--;
        System.Console.WriteLine(l2);
        l2 = --l;
        System.Console.WriteLine(l2);

        m2 = m++;
        System.Console.WriteLine(m2);
        m2 = ++m;
        System.Console.WriteLine(m2);
        m2 = m--;
        System.Console.WriteLine(m2);
        m2 = --m;
        System.Console.WriteLine(m2);
    }
}
";

            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Main", @"
{
  // Code size      754 (0x2f2)
  .maxstack  3
  .locals init (sbyte V_0, //a
  byte V_1, //b
  short V_2, //c
  ushort V_3, //d
  int V_4, //e
  uint V_5, //f
  long V_6, //g
  ulong V_7, //h
  char V_8, //i
  float V_9, //j
  decimal V_10, //k
  double V_11, //l
  C.E V_12) //m
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  stloc.2
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.3
  IL_0008:  ldc.i4.1
  IL_0009:  stloc.s    V_4
  IL_000b:  ldc.i4.1
  IL_000c:  stloc.s    V_5
  IL_000e:  ldc.i4.1
  IL_000f:  conv.i8
  IL_0010:  stloc.s    V_6
  IL_0012:  ldc.i4.1
  IL_0013:  conv.i8
  IL_0014:  stloc.s    V_7
  IL_0016:  ldc.i4.1
  IL_0017:  stloc.s    V_8
  IL_0019:  ldc.r4     1
  IL_001e:  stloc.s    V_9
  IL_0020:  ldsfld     ""decimal decimal.One""
  IL_0025:  stloc.s    V_10
  IL_0027:  ldc.r8     1
  IL_0030:  stloc.s    V_11
  IL_0032:  ldc.i4.0
  IL_0033:  stloc.s    V_12
  IL_0035:  ldloc.0
  IL_0036:  dup
  IL_0037:  ldc.i4.1
  IL_0038:  add
  IL_0039:  conv.i1
  IL_003a:  stloc.0
  IL_003b:  call       ""void System.Console.WriteLine(int)""
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.1
  IL_0042:  add
  IL_0043:  conv.i1
  IL_0044:  dup
  IL_0045:  stloc.0
  IL_0046:  call       ""void System.Console.WriteLine(int)""
  IL_004b:  ldloc.0
  IL_004c:  dup
  IL_004d:  ldc.i4.1
  IL_004e:  sub
  IL_004f:  conv.i1
  IL_0050:  stloc.0
  IL_0051:  call       ""void System.Console.WriteLine(int)""
  IL_0056:  ldloc.0
  IL_0057:  ldc.i4.1
  IL_0058:  sub
  IL_0059:  conv.i1
  IL_005a:  dup
  IL_005b:  stloc.0
  IL_005c:  call       ""void System.Console.WriteLine(int)""
  IL_0061:  ldloc.1
  IL_0062:  dup
  IL_0063:  ldc.i4.1
  IL_0064:  add
  IL_0065:  conv.u1
  IL_0066:  stloc.1
  IL_0067:  call       ""void System.Console.WriteLine(int)""
  IL_006c:  ldloc.1
  IL_006d:  ldc.i4.1
  IL_006e:  add
  IL_006f:  conv.u1
  IL_0070:  dup
  IL_0071:  stloc.1
  IL_0072:  call       ""void System.Console.WriteLine(int)""
  IL_0077:  ldloc.1
  IL_0078:  dup
  IL_0079:  ldc.i4.1
  IL_007a:  sub
  IL_007b:  conv.u1
  IL_007c:  stloc.1
  IL_007d:  call       ""void System.Console.WriteLine(int)""
  IL_0082:  ldloc.1
  IL_0083:  ldc.i4.1
  IL_0084:  sub
  IL_0085:  conv.u1
  IL_0086:  dup
  IL_0087:  stloc.1
  IL_0088:  call       ""void System.Console.WriteLine(int)""
  IL_008d:  ldloc.2
  IL_008e:  dup
  IL_008f:  ldc.i4.1
  IL_0090:  add
  IL_0091:  conv.i2
  IL_0092:  stloc.2
  IL_0093:  call       ""void System.Console.WriteLine(int)""
  IL_0098:  ldloc.2
  IL_0099:  ldc.i4.1
  IL_009a:  add
  IL_009b:  conv.i2
  IL_009c:  dup
  IL_009d:  stloc.2
  IL_009e:  call       ""void System.Console.WriteLine(int)""
  IL_00a3:  ldloc.2
  IL_00a4:  dup
  IL_00a5:  ldc.i4.1
  IL_00a6:  sub
  IL_00a7:  conv.i2
  IL_00a8:  stloc.2
  IL_00a9:  call       ""void System.Console.WriteLine(int)""
  IL_00ae:  ldloc.2
  IL_00af:  ldc.i4.1
  IL_00b0:  sub
  IL_00b1:  conv.i2
  IL_00b2:  dup
  IL_00b3:  stloc.2
  IL_00b4:  call       ""void System.Console.WriteLine(int)""
  IL_00b9:  ldloc.3
  IL_00ba:  dup
  IL_00bb:  ldc.i4.1
  IL_00bc:  add
  IL_00bd:  conv.u2
  IL_00be:  stloc.3
  IL_00bf:  call       ""void System.Console.WriteLine(int)""
  IL_00c4:  ldloc.3
  IL_00c5:  ldc.i4.1
  IL_00c6:  add
  IL_00c7:  conv.u2
  IL_00c8:  dup
  IL_00c9:  stloc.3
  IL_00ca:  call       ""void System.Console.WriteLine(int)""
  IL_00cf:  ldloc.3
  IL_00d0:  dup
  IL_00d1:  ldc.i4.1
  IL_00d2:  sub
  IL_00d3:  conv.u2
  IL_00d4:  stloc.3
  IL_00d5:  call       ""void System.Console.WriteLine(int)""
  IL_00da:  ldloc.3
  IL_00db:  ldc.i4.1
  IL_00dc:  sub
  IL_00dd:  conv.u2
  IL_00de:  dup
  IL_00df:  stloc.3
  IL_00e0:  call       ""void System.Console.WriteLine(int)""
  IL_00e5:  ldloc.s    V_4
  IL_00e7:  dup
  IL_00e8:  ldc.i4.1
  IL_00e9:  add
  IL_00ea:  stloc.s    V_4
  IL_00ec:  call       ""void System.Console.WriteLine(int)""
  IL_00f1:  ldloc.s    V_4
  IL_00f3:  ldc.i4.1
  IL_00f4:  add
  IL_00f5:  dup
  IL_00f6:  stloc.s    V_4
  IL_00f8:  call       ""void System.Console.WriteLine(int)""
  IL_00fd:  ldloc.s    V_4
  IL_00ff:  dup
  IL_0100:  ldc.i4.1
  IL_0101:  sub
  IL_0102:  stloc.s    V_4
  IL_0104:  call       ""void System.Console.WriteLine(int)""
  IL_0109:  ldloc.s    V_4
  IL_010b:  ldc.i4.1
  IL_010c:  sub
  IL_010d:  dup
  IL_010e:  stloc.s    V_4
  IL_0110:  call       ""void System.Console.WriteLine(int)""
  IL_0115:  ldloc.s    V_5
  IL_0117:  dup
  IL_0118:  ldc.i4.1
  IL_0119:  add
  IL_011a:  stloc.s    V_5
  IL_011c:  call       ""void System.Console.WriteLine(uint)""
  IL_0121:  ldloc.s    V_5
  IL_0123:  ldc.i4.1
  IL_0124:  add
  IL_0125:  dup
  IL_0126:  stloc.s    V_5
  IL_0128:  call       ""void System.Console.WriteLine(uint)""
  IL_012d:  ldloc.s    V_5
  IL_012f:  dup
  IL_0130:  ldc.i4.1
  IL_0131:  sub
  IL_0132:  stloc.s    V_5
  IL_0134:  call       ""void System.Console.WriteLine(uint)""
  IL_0139:  ldloc.s    V_5
  IL_013b:  ldc.i4.1
  IL_013c:  sub
  IL_013d:  dup
  IL_013e:  stloc.s    V_5
  IL_0140:  call       ""void System.Console.WriteLine(uint)""
  IL_0145:  ldloc.s    V_6
  IL_0147:  dup
  IL_0148:  ldc.i4.1
  IL_0149:  conv.i8
  IL_014a:  add
  IL_014b:  stloc.s    V_6
  IL_014d:  call       ""void System.Console.WriteLine(long)""
  IL_0152:  ldloc.s    V_6
  IL_0154:  ldc.i4.1
  IL_0155:  conv.i8
  IL_0156:  add
  IL_0157:  dup
  IL_0158:  stloc.s    V_6
  IL_015a:  call       ""void System.Console.WriteLine(long)""
  IL_015f:  ldloc.s    V_6
  IL_0161:  dup
  IL_0162:  ldc.i4.1
  IL_0163:  conv.i8
  IL_0164:  sub
  IL_0165:  stloc.s    V_6
  IL_0167:  call       ""void System.Console.WriteLine(long)""
  IL_016c:  ldloc.s    V_6
  IL_016e:  ldc.i4.1
  IL_016f:  conv.i8
  IL_0170:  sub
  IL_0171:  dup
  IL_0172:  stloc.s    V_6
  IL_0174:  call       ""void System.Console.WriteLine(long)""
  IL_0179:  ldloc.s    V_7
  IL_017b:  dup
  IL_017c:  ldc.i4.1
  IL_017d:  conv.i8
  IL_017e:  add
  IL_017f:  stloc.s    V_7
  IL_0181:  call       ""void System.Console.WriteLine(ulong)""
  IL_0186:  ldloc.s    V_7
  IL_0188:  ldc.i4.1
  IL_0189:  conv.i8
  IL_018a:  add
  IL_018b:  dup
  IL_018c:  stloc.s    V_7
  IL_018e:  call       ""void System.Console.WriteLine(ulong)""
  IL_0193:  ldloc.s    V_7
  IL_0195:  dup
  IL_0196:  ldc.i4.1
  IL_0197:  conv.i8
  IL_0198:  sub
  IL_0199:  stloc.s    V_7
  IL_019b:  call       ""void System.Console.WriteLine(ulong)""
  IL_01a0:  ldloc.s    V_7
  IL_01a2:  ldc.i4.1
  IL_01a3:  conv.i8
  IL_01a4:  sub
  IL_01a5:  dup
  IL_01a6:  stloc.s    V_7
  IL_01a8:  call       ""void System.Console.WriteLine(ulong)""
  IL_01ad:  ldloc.s    V_8
  IL_01af:  dup
  IL_01b0:  ldc.i4.1
  IL_01b1:  add
  IL_01b2:  conv.u2
  IL_01b3:  stloc.s    V_8
  IL_01b5:  call       ""void System.Console.WriteLine(char)""
  IL_01ba:  ldloc.s    V_8
  IL_01bc:  ldc.i4.1
  IL_01bd:  add
  IL_01be:  conv.u2
  IL_01bf:  dup
  IL_01c0:  stloc.s    V_8
  IL_01c2:  call       ""void System.Console.WriteLine(char)""
  IL_01c7:  ldloc.s    V_8
  IL_01c9:  dup
  IL_01ca:  ldc.i4.1
  IL_01cb:  sub
  IL_01cc:  conv.u2
  IL_01cd:  stloc.s    V_8
  IL_01cf:  call       ""void System.Console.WriteLine(char)""
  IL_01d4:  ldloc.s    V_8
  IL_01d6:  ldc.i4.1
  IL_01d7:  sub
  IL_01d8:  conv.u2
  IL_01d9:  dup
  IL_01da:  stloc.s    V_8
  IL_01dc:  call       ""void System.Console.WriteLine(char)""
  IL_01e1:  ldloc.s    V_9
  IL_01e3:  dup
  IL_01e4:  ldc.r4     1
  IL_01e9:  add
  IL_01ea:  stloc.s    V_9
  IL_01ec:  call       ""void System.Console.WriteLine(float)""
  IL_01f1:  ldloc.s    V_9
  IL_01f3:  ldc.r4     1
  IL_01f8:  add
  IL_01f9:  dup
  IL_01fa:  stloc.s    V_9
  IL_01fc:  call       ""void System.Console.WriteLine(float)""
  IL_0201:  ldloc.s    V_9
  IL_0203:  dup
  IL_0204:  ldc.r4     1
  IL_0209:  sub
  IL_020a:  stloc.s    V_9
  IL_020c:  call       ""void System.Console.WriteLine(float)""
  IL_0211:  ldloc.s    V_9
  IL_0213:  ldc.r4     1
  IL_0218:  sub
  IL_0219:  dup
  IL_021a:  stloc.s    V_9
  IL_021c:  call       ""void System.Console.WriteLine(float)""
  IL_0221:  ldloc.s    V_10
  IL_0223:  dup
  IL_0224:  call       ""decimal decimal.op_Increment(decimal)""
  IL_0229:  stloc.s    V_10
  IL_022b:  call       ""void System.Console.WriteLine(decimal)""
  IL_0230:  ldloc.s    V_10
  IL_0232:  call       ""decimal decimal.op_Increment(decimal)""
  IL_0237:  dup
  IL_0238:  stloc.s    V_10
  IL_023a:  call       ""void System.Console.WriteLine(decimal)""
  IL_023f:  ldloc.s    V_10
  IL_0241:  dup
  IL_0242:  call       ""decimal decimal.op_Decrement(decimal)""
  IL_0247:  stloc.s    V_10
  IL_0249:  call       ""void System.Console.WriteLine(decimal)""
  IL_024e:  ldloc.s    V_10
  IL_0250:  call       ""decimal decimal.op_Decrement(decimal)""
  IL_0255:  dup
  IL_0256:  stloc.s    V_10
  IL_0258:  call       ""void System.Console.WriteLine(decimal)""
  IL_025d:  ldloc.s    V_11
  IL_025f:  dup
  IL_0260:  ldc.r8     1
  IL_0269:  add
  IL_026a:  stloc.s    V_11
  IL_026c:  call       ""void System.Console.WriteLine(double)""
  IL_0271:  ldloc.s    V_11
  IL_0273:  ldc.r8     1
  IL_027c:  add
  IL_027d:  dup
  IL_027e:  stloc.s    V_11
  IL_0280:  call       ""void System.Console.WriteLine(double)""
  IL_0285:  ldloc.s    V_11
  IL_0287:  dup
  IL_0288:  ldc.r8     1
  IL_0291:  sub
  IL_0292:  stloc.s    V_11
  IL_0294:  call       ""void System.Console.WriteLine(double)""
  IL_0299:  ldloc.s    V_11
  IL_029b:  ldc.r8     1
  IL_02a4:  sub
  IL_02a5:  dup
  IL_02a6:  stloc.s    V_11
  IL_02a8:  call       ""void System.Console.WriteLine(double)""
  IL_02ad:  ldloc.s    V_12
  IL_02af:  dup
  IL_02b0:  ldc.i4.1
  IL_02b1:  add
  IL_02b2:  stloc.s    V_12
  IL_02b4:  box        ""C.E""
  IL_02b9:  call       ""void System.Console.WriteLine(object)""
  IL_02be:  ldloc.s    V_12
  IL_02c0:  ldc.i4.1
  IL_02c1:  add
  IL_02c2:  dup
  IL_02c3:  stloc.s    V_12
  IL_02c5:  box        ""C.E""
  IL_02ca:  call       ""void System.Console.WriteLine(object)""
  IL_02cf:  ldloc.s    V_12
  IL_02d1:  dup
  IL_02d2:  ldc.i4.1
  IL_02d3:  sub
  IL_02d4:  stloc.s    V_12
  IL_02d6:  box        ""C.E""
  IL_02db:  call       ""void System.Console.WriteLine(object)""
  IL_02e0:  ldloc.s    V_12
  IL_02e2:  ldc.i4.1
  IL_02e3:  sub
  IL_02e4:  dup
  IL_02e5:  stloc.s    V_12
  IL_02e7:  box        ""C.E""
  IL_02ec:  call       ""void System.Console.WriteLine(object)""
  IL_02f1:  ret
}
");
        }

        [WorkItem(540718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540718")]
        [Fact]
        public void GenConditionalBranchTempForInc()
        {
            var source = @"using System;
class Test
{
    void M(int i)
    {
        if (i++ == 0)
        {
            return;
        }
    }
}
";
            base.CompileAndVerify(source).
                VerifyIL("Test.M",
@"
{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  dup
  IL_0002:  ldc.i4.1
  IL_0003:  add
  IL_0004:  starg.s    V_1
  IL_0006:  pop
  IL_0007:  ret
}
"
                );
        }

        [WorkItem(540718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540718")]
        [Fact]
        public void IncrementField()
        {
            var source = @"
using System;
class Test
{
    private int i = 0;

    void M()
    {
        this.i++;
        ++this.i;

        this.i+=1;
    }
}
";
            base.CompileAndVerify(source).
                VerifyIL("Test.M",
@"
{
  // Code size       43 (0x2b)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int Test.i""
  IL_0007:  ldc.i4.1
  IL_0008:  add
  IL_0009:  stfld      ""int Test.i""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""int Test.i""
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stfld      ""int Test.i""
  IL_001c:  ldarg.0
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""int Test.i""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  stfld      ""int Test.i""
  IL_002a:  ret
}
"
                );
        }

        [WorkItem(540723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540723")]
        [Fact]
        public void MissingIncInFinallyBlock()
        {
            var source = @"using System;

class My
{
    static void Main()
    {
        int i = 0;
        try { }
        finally { i++; }

        Console.Write(i);
    }
}
";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(540810, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540810")]
        [Fact]
        public void NestedIncrement()
        {
            var source = @"
using System;
class My
{
    static void Main()
    {
        int[] a = { 0 };
        int i = 0;
        a[i++]++;
        Console.Write(a[0]);
        Console.Write(i);
    }
}
";
            CompileAndVerify(source, expectedOutput: "11");
        }

        [WorkItem(540810, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540810")]
        [Fact]
        public void IncrementSideEffects()
        {
            var source = @"
class Class
{
    static int[] array = new int[1];

    static void Main()
    {
        System.Console.WriteLine(Array()[Zero()]++);
        System.Console.WriteLine(++Array()[Zero()]);
        System.Console.WriteLine(Array()[Zero()]--);
        System.Console.WriteLine(--Array()[Zero()]);
    }

    static int Zero()
    {
        System.Console.WriteLine(""Zero"");
        return 0;
    }

    static int[] Array()
    {
        System.Console.WriteLine(""Array"");
        return array;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Array
Zero
0
Array
Zero
2
Array
Zero
2
Array
Zero
0");
        }

        private void TestIncrementCompilationAndOutput<T>(T value, T valuePlusOne) where T : struct
        {
            Type type = typeof(T);
            Assert.True(type.IsPrimitive || type == typeof(decimal), string.Format("Type {0} is neither primitive nor decimal", type));

            // Explicitly provide InvariantCulture to use the proper C# decimal separator '.' in the source regardless of the current culture
            string source = string.Format(CultureInfo.InvariantCulture, NUMERIC_INCREMENT_TEMPLATE, type.FullName, value, valuePlusOne);
            string expectedOutput = string.Format(CultureInfo.InvariantCulture, NUMERIC_OUTPUT_TEMPLATE, value, valuePlusOne);

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(720742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720742")]
        [Fact]
        public void IncrementRefVal()
        {
            var source = @"
using System;

public class Test
{
    public static void Main()
    {
        short x = 3;
        var r = __makeref(x);

        __refvalue(r, short) += 7;

        __refvalue(r, short)++;
        ++ __refvalue(r,short);

        System.Console.WriteLine( __refvalue(r, short));
    }
}
";
            base.CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: "12").
                VerifyIL("Test.Main",
@"
{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (short V_0) //x
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  mkrefany   ""short""
  IL_0009:  dup
  IL_000a:  refanyval  ""short""
  IL_000f:  dup
  IL_0010:  ldind.i2
  IL_0011:  ldc.i4.7
  IL_0012:  add
  IL_0013:  conv.i2
  IL_0014:  stind.i2
  IL_0015:  dup
  IL_0016:  refanyval  ""short""
  IL_001b:  dup
  IL_001c:  ldind.i2
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  conv.i2
  IL_0020:  stind.i2
  IL_0021:  dup
  IL_0022:  refanyval  ""short""
  IL_0027:  dup
  IL_0028:  ldind.i2
  IL_0029:  ldc.i4.1
  IL_002a:  add
  IL_002b:  conv.i2
  IL_002c:  stind.i2
  IL_002d:  refanyval  ""short""
  IL_0032:  ldind.i2
  IL_0033:  call       ""void System.Console.WriteLine(int)""
  IL_0038:  ret
}
"
                );
        }
    }
}
