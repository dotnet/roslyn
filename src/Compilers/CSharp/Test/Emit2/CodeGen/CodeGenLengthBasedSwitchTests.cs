// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenLengthBasedSwitchTests : CSharpTestBase
{
    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void MixOfBucketSizes()
    {
        var source = """
assert(null, "default");
assert("", "blank");
assert("a", "a");
assert("b", "b");
assert("c", "c");
assert("no");
assert("yes");
assert("four");
assert("blurb");
assert("hello");
assert("lamps");
assert("lambs");
assert("names");
assert("slurp");
assert("towed");
assert("words");
assert("not", "default");
assert("other", "default");
assert("y", "default");
assert("longer example", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string o)
    {
        return o switch
        {
            "" => "blank",
            "a" => "a",
            "b" => "b",
            "c" => "c",
            "no" => "no",
            "yes" => "yes",
            "four" => "four",
            "alice" => "alice",
            "blurb" => "blurb",
            "hello" => "hello",
            "lamps" => "lamps",
            "lambs" => "lambs",
            "lower" => "lower",
            "names" => "names",
            "slurp" => "slurp",
            "towed" => "towed",
            "words" => "words",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: false);
        verifier.VerifyIL("C.M", """
{
  // Code size      584 (0x248)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0240
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  switch    (
        IL_01b5,
        IL_0030,
        IL_00ae,
        IL_00c3,
        IL_00d8,
        IL_0052)
  IL_002b:  br         IL_0240
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.0
  IL_0032:  call       "char string.this[int].get"
  IL_0037:  stloc.2
  IL_0038:  ldloc.2
  IL_0039:  ldc.i4.s   97
  IL_003b:  sub
  IL_003c:  switch    (
        IL_01c0,
        IL_01c8,
        IL_01d0)
  IL_004d:  br         IL_0240
  IL_0052:  ldarg.0
  IL_0053:  ldc.i4.0
  IL_0054:  call       "char string.this[int].get"
  IL_0059:  stloc.2
  IL_005a:  ldloc.2
  IL_005b:  ldc.i4.s   104
  IL_005d:  bgt.un.s   IL_007c
  IL_005f:  ldloc.2
  IL_0060:  ldc.i4.s   97
  IL_0062:  beq        IL_00ed
  IL_0067:  ldloc.2
  IL_0068:  ldc.i4.s   98
  IL_006a:  beq        IL_0102
  IL_006f:  ldloc.2
  IL_0070:  ldc.i4.s   104
  IL_0072:  beq        IL_0117
  IL_0077:  br         IL_0240
  IL_007c:  ldloc.2
  IL_007d:  ldc.i4.s   108
  IL_007f:  beq        IL_012c
  IL_0084:  ldloc.2
  IL_0085:  ldc.i4.s   110
  IL_0087:  beq        IL_0161
  IL_008c:  ldloc.2
  IL_008d:  ldc.i4.s   115
  IL_008f:  sub
  IL_0090:  switch    (
        IL_0176,
        IL_018b,
        IL_0240,
        IL_0240,
        IL_01a0)
  IL_00a9:  br         IL_0240
  IL_00ae:  ldarg.0
  IL_00af:  ldstr      "no"
  IL_00b4:  call       "bool string.op_Equality(string, string)"
  IL_00b9:  brtrue     IL_01d8
  IL_00be:  br         IL_0240
  IL_00c3:  ldarg.0
  IL_00c4:  ldstr      "yes"
  IL_00c9:  call       "bool string.op_Equality(string, string)"
  IL_00ce:  brtrue     IL_01e0
  IL_00d3:  br         IL_0240
  IL_00d8:  ldarg.0
  IL_00d9:  ldstr      "four"
  IL_00de:  call       "bool string.op_Equality(string, string)"
  IL_00e3:  brtrue     IL_01e8
  IL_00e8:  br         IL_0240
  IL_00ed:  ldarg.0
  IL_00ee:  ldstr      "alice"
  IL_00f3:  call       "bool string.op_Equality(string, string)"
  IL_00f8:  brtrue     IL_01f0
  IL_00fd:  br         IL_0240
  IL_0102:  ldarg.0
  IL_0103:  ldstr      "blurb"
  IL_0108:  call       "bool string.op_Equality(string, string)"
  IL_010d:  brtrue     IL_01f8
  IL_0112:  br         IL_0240
  IL_0117:  ldarg.0
  IL_0118:  ldstr      "hello"
  IL_011d:  call       "bool string.op_Equality(string, string)"
  IL_0122:  brtrue     IL_0200
  IL_0127:  br         IL_0240
  IL_012c:  ldarg.0
  IL_012d:  ldstr      "lamps"
  IL_0132:  call       "bool string.op_Equality(string, string)"
  IL_0137:  brtrue     IL_0208
  IL_013c:  ldarg.0
  IL_013d:  ldstr      "lambs"
  IL_0142:  call       "bool string.op_Equality(string, string)"
  IL_0147:  brtrue     IL_0210
  IL_014c:  ldarg.0
  IL_014d:  ldstr      "lower"
  IL_0152:  call       "bool string.op_Equality(string, string)"
  IL_0157:  brtrue     IL_0218
  IL_015c:  br         IL_0240
  IL_0161:  ldarg.0
  IL_0162:  ldstr      "names"
  IL_0167:  call       "bool string.op_Equality(string, string)"
  IL_016c:  brtrue     IL_0220
  IL_0171:  br         IL_0240
  IL_0176:  ldarg.0
  IL_0177:  ldstr      "slurp"
  IL_017c:  call       "bool string.op_Equality(string, string)"
  IL_0181:  brtrue     IL_0228
  IL_0186:  br         IL_0240
  IL_018b:  ldarg.0
  IL_018c:  ldstr      "towed"
  IL_0191:  call       "bool string.op_Equality(string, string)"
  IL_0196:  brtrue     IL_0230
  IL_019b:  br         IL_0240
  IL_01a0:  ldarg.0
  IL_01a1:  ldstr      "words"
  IL_01a6:  call       "bool string.op_Equality(string, string)"
  IL_01ab:  brtrue     IL_0238
  IL_01b0:  br         IL_0240
  IL_01b5:  ldstr      "blank"
  IL_01ba:  stloc.0
  IL_01bb:  br         IL_0246
  IL_01c0:  ldstr      "a"
  IL_01c5:  stloc.0
  IL_01c6:  br.s       IL_0246
  IL_01c8:  ldstr      "b"
  IL_01cd:  stloc.0
  IL_01ce:  br.s       IL_0246
  IL_01d0:  ldstr      "c"
  IL_01d5:  stloc.0
  IL_01d6:  br.s       IL_0246
  IL_01d8:  ldstr      "no"
  IL_01dd:  stloc.0
  IL_01de:  br.s       IL_0246
  IL_01e0:  ldstr      "yes"
  IL_01e5:  stloc.0
  IL_01e6:  br.s       IL_0246
  IL_01e8:  ldstr      "four"
  IL_01ed:  stloc.0
  IL_01ee:  br.s       IL_0246
  IL_01f0:  ldstr      "alice"
  IL_01f5:  stloc.0
  IL_01f6:  br.s       IL_0246
  IL_01f8:  ldstr      "blurb"
  IL_01fd:  stloc.0
  IL_01fe:  br.s       IL_0246
  IL_0200:  ldstr      "hello"
  IL_0205:  stloc.0
  IL_0206:  br.s       IL_0246
  IL_0208:  ldstr      "lamps"
  IL_020d:  stloc.0
  IL_020e:  br.s       IL_0246
  IL_0210:  ldstr      "lambs"
  IL_0215:  stloc.0
  IL_0216:  br.s       IL_0246
  IL_0218:  ldstr      "lower"
  IL_021d:  stloc.0
  IL_021e:  br.s       IL_0246
  IL_0220:  ldstr      "names"
  IL_0225:  stloc.0
  IL_0226:  br.s       IL_0246
  IL_0228:  ldstr      "slurp"
  IL_022d:  stloc.0
  IL_022e:  br.s       IL_0246
  IL_0230:  ldstr      "towed"
  IL_0235:  stloc.0
  IL_0236:  br.s       IL_0246
  IL_0238:  ldstr      "words"
  IL_023d:  stloc.0
  IL_023e:  br.s       IL_0246
  IL_0240:  ldstr      "default"
  IL_0245:  stloc.0
  IL_0246:  ldloc.0
  IL_0247:  ret
}
""");
        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL("<PrivateImplementationDetails>." + PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: true);

        verifier.VerifyIL("C.M", """
{
  // Code size      786 (0x312)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x62366ffa
  IL_000d:  bgt.un     IL_0096
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x3ec721c0
  IL_0018:  bgt.un.s   IL_0058
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4     0x2f69f5a5
  IL_0020:  bgt.un.s   IL_003d
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x242602e6
  IL_0028:  beq        IL_0201
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0x2f69f5a5
  IL_0033:  beq        IL_0198
  IL_0038:  br         IL_030a
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4     0x3448ae58
  IL_0043:  beq        IL_01ec
  IL_0048:  ldloc.1
  IL_0049:  ldc.i4     0x3ec721c0
  IL_004e:  beq        IL_0255
  IL_0053:  br         IL_030a
  IL_0058:  ldloc.1
  IL_0059:  ldc.i4     0x4f9f2cab
  IL_005e:  bgt.un.s   IL_007b
  IL_0060:  ldloc.1
  IL_0061:  ldc.i4     0x4e9f3590
  IL_0066:  beq        IL_0183
  IL_006b:  ldloc.1
  IL_006c:  ldc.i4     0x4f9f2cab
  IL_0071:  beq        IL_01d7
  IL_0076:  br         IL_030a
  IL_007b:  ldloc.1
  IL_007c:  ldc.i4     0x5cb7aa8a
  IL_0081:  beq        IL_026a
  IL_0086:  ldloc.1
  IL_0087:  ldc.i4     0x62366ffa
  IL_008c:  beq        IL_016e
  IL_0091:  br         IL_030a
  IL_0096:  ldloc.1
  IL_0097:  ldc.i4     0xb51d04ba
  IL_009c:  bgt.un.s   IL_00d9
  IL_009e:  ldloc.1
  IL_009f:  ldc.i4     0x811c9dc5
  IL_00a4:  bgt.un.s   IL_00be
  IL_00a6:  ldloc.1
  IL_00a7:  ldc.i4     0x63cb5a27
  IL_00ac:  beq        IL_0240
  IL_00b1:  ldloc.1
  IL_00b2:  ldc.i4     0x811c9dc5
  IL_00b7:  beq.s      IL_0119
  IL_00b9:  br         IL_030a
  IL_00be:  ldloc.1
  IL_00bf:  ldc.i4     0x872213e7
  IL_00c4:  beq        IL_01ad
  IL_00c9:  ldloc.1
  IL_00ca:  ldc.i4     0xb51d04ba
  IL_00cf:  beq        IL_0216
  IL_00d4:  br         IL_030a
  IL_00d9:  ldloc.1
  IL_00da:  ldc.i4     0xe60c2c52
  IL_00df:  bgt.un.s   IL_00f6
  IL_00e1:  ldloc.1
  IL_00e2:  ldc.i4     0xe40c292c
  IL_00e7:  beq.s      IL_012f
  IL_00e9:  ldloc.1
  IL_00ea:  ldc.i4     0xe60c2c52
  IL_00ef:  beq.s      IL_0159
  IL_00f1:  br         IL_030a
  IL_00f6:  ldloc.1
  IL_00f7:  ldc.i4     0xe6e5718f
  IL_00fc:  beq        IL_022b
  IL_0101:  ldloc.1
  IL_0102:  ldc.i4     0xe70c2de5
  IL_0107:  beq.s      IL_0144
  IL_0109:  ldloc.1
  IL_010a:  ldc.i4     0xf76b8c8e
  IL_010f:  beq        IL_01c2
  IL_0114:  br         IL_030a
  IL_0119:  ldarg.0
  IL_011a:  brfalse    IL_030a
  IL_011f:  ldarg.0
  IL_0120:  call       "int string.Length.get"
  IL_0125:  brfalse    IL_027f
  IL_012a:  br         IL_030a
  IL_012f:  ldarg.0
  IL_0130:  ldstr      "a"
  IL_0135:  call       "bool string.op_Equality(string, string)"
  IL_013a:  brtrue     IL_028a
  IL_013f:  br         IL_030a
  IL_0144:  ldarg.0
  IL_0145:  ldstr      "b"
  IL_014a:  call       "bool string.op_Equality(string, string)"
  IL_014f:  brtrue     IL_0292
  IL_0154:  br         IL_030a
  IL_0159:  ldarg.0
  IL_015a:  ldstr      "c"
  IL_015f:  call       "bool string.op_Equality(string, string)"
  IL_0164:  brtrue     IL_029a
  IL_0169:  br         IL_030a
  IL_016e:  ldarg.0
  IL_016f:  ldstr      "no"
  IL_0174:  call       "bool string.op_Equality(string, string)"
  IL_0179:  brtrue     IL_02a2
  IL_017e:  br         IL_030a
  IL_0183:  ldarg.0
  IL_0184:  ldstr      "yes"
  IL_0189:  call       "bool string.op_Equality(string, string)"
  IL_018e:  brtrue     IL_02aa
  IL_0193:  br         IL_030a
  IL_0198:  ldarg.0
  IL_0199:  ldstr      "four"
  IL_019e:  call       "bool string.op_Equality(string, string)"
  IL_01a3:  brtrue     IL_02b2
  IL_01a8:  br         IL_030a
  IL_01ad:  ldarg.0
  IL_01ae:  ldstr      "alice"
  IL_01b3:  call       "bool string.op_Equality(string, string)"
  IL_01b8:  brtrue     IL_02ba
  IL_01bd:  br         IL_030a
  IL_01c2:  ldarg.0
  IL_01c3:  ldstr      "blurb"
  IL_01c8:  call       "bool string.op_Equality(string, string)"
  IL_01cd:  brtrue     IL_02c2
  IL_01d2:  br         IL_030a
  IL_01d7:  ldarg.0
  IL_01d8:  ldstr      "hello"
  IL_01dd:  call       "bool string.op_Equality(string, string)"
  IL_01e2:  brtrue     IL_02ca
  IL_01e7:  br         IL_030a
  IL_01ec:  ldarg.0
  IL_01ed:  ldstr      "lamps"
  IL_01f2:  call       "bool string.op_Equality(string, string)"
  IL_01f7:  brtrue     IL_02d2
  IL_01fc:  br         IL_030a
  IL_0201:  ldarg.0
  IL_0202:  ldstr      "lambs"
  IL_0207:  call       "bool string.op_Equality(string, string)"
  IL_020c:  brtrue     IL_02da
  IL_0211:  br         IL_030a
  IL_0216:  ldarg.0
  IL_0217:  ldstr      "lower"
  IL_021c:  call       "bool string.op_Equality(string, string)"
  IL_0221:  brtrue     IL_02e2
  IL_0226:  br         IL_030a
  IL_022b:  ldarg.0
  IL_022c:  ldstr      "names"
  IL_0231:  call       "bool string.op_Equality(string, string)"
  IL_0236:  brtrue     IL_02ea
  IL_023b:  br         IL_030a
  IL_0240:  ldarg.0
  IL_0241:  ldstr      "slurp"
  IL_0246:  call       "bool string.op_Equality(string, string)"
  IL_024b:  brtrue     IL_02f2
  IL_0250:  br         IL_030a
  IL_0255:  ldarg.0
  IL_0256:  ldstr      "towed"
  IL_025b:  call       "bool string.op_Equality(string, string)"
  IL_0260:  brtrue     IL_02fa
  IL_0265:  br         IL_030a
  IL_026a:  ldarg.0
  IL_026b:  ldstr      "words"
  IL_0270:  call       "bool string.op_Equality(string, string)"
  IL_0275:  brtrue     IL_0302
  IL_027a:  br         IL_030a
  IL_027f:  ldstr      "blank"
  IL_0284:  stloc.0
  IL_0285:  br         IL_0310
  IL_028a:  ldstr      "a"
  IL_028f:  stloc.0
  IL_0290:  br.s       IL_0310
  IL_0292:  ldstr      "b"
  IL_0297:  stloc.0
  IL_0298:  br.s       IL_0310
  IL_029a:  ldstr      "c"
  IL_029f:  stloc.0
  IL_02a0:  br.s       IL_0310
  IL_02a2:  ldstr      "no"
  IL_02a7:  stloc.0
  IL_02a8:  br.s       IL_0310
  IL_02aa:  ldstr      "yes"
  IL_02af:  stloc.0
  IL_02b0:  br.s       IL_0310
  IL_02b2:  ldstr      "four"
  IL_02b7:  stloc.0
  IL_02b8:  br.s       IL_0310
  IL_02ba:  ldstr      "alice"
  IL_02bf:  stloc.0
  IL_02c0:  br.s       IL_0310
  IL_02c2:  ldstr      "blurb"
  IL_02c7:  stloc.0
  IL_02c8:  br.s       IL_0310
  IL_02ca:  ldstr      "hello"
  IL_02cf:  stloc.0
  IL_02d0:  br.s       IL_0310
  IL_02d2:  ldstr      "lamps"
  IL_02d7:  stloc.0
  IL_02d8:  br.s       IL_0310
  IL_02da:  ldstr      "lambs"
  IL_02df:  stloc.0
  IL_02e0:  br.s       IL_0310
  IL_02e2:  ldstr      "lower"
  IL_02e7:  stloc.0
  IL_02e8:  br.s       IL_0310
  IL_02ea:  ldstr      "names"
  IL_02ef:  stloc.0
  IL_02f0:  br.s       IL_0310
  IL_02f2:  ldstr      "slurp"
  IL_02f7:  stloc.0
  IL_02f8:  br.s       IL_0310
  IL_02fa:  ldstr      "towed"
  IL_02ff:  stloc.0
  IL_0300:  br.s       IL_0310
  IL_0302:  ldstr      "words"
  IL_0307:  stloc.0
  IL_0308:  br.s       IL_0310
  IL_030a:  ldstr      "default"
  IL_030f:  stloc.0
  IL_0310:  ldloc.0
  IL_0311:  ret
}
""");

        comp = CreateCompilation(source, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      819 (0x333)
  .maxstack  2
  .locals init (string V_0,
                uint V_1,
                string V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  brtrue.s   IL_0005
  IL_0004:  nop
  IL_0005:  ldarg.0
  IL_0006:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4     0x62366ffa
  IL_0012:  bgt.un     IL_00a3
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4     0x3ec721c0
  IL_001d:  bgt.un.s   IL_0061
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4     0x2f69f5a5
  IL_0025:  bgt.un.s   IL_0044
  IL_0027:  ldloc.1
  IL_0028:  ldc.i4     0x242602e6
  IL_002d:  beq        IL_0215
  IL_0032:  br.s       IL_0034
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4     0x2f69f5a5
  IL_003a:  beq        IL_01ac
  IL_003f:  br         IL_0321
  IL_0044:  ldloc.1
  IL_0045:  ldc.i4     0x3448ae58
  IL_004a:  beq        IL_0200
  IL_004f:  br.s       IL_0051
  IL_0051:  ldloc.1
  IL_0052:  ldc.i4     0x3ec721c0
  IL_0057:  beq        IL_0269
  IL_005c:  br         IL_0321
  IL_0061:  ldloc.1
  IL_0062:  ldc.i4     0x4f9f2cab
  IL_0067:  bgt.un.s   IL_0086
  IL_0069:  ldloc.1
  IL_006a:  ldc.i4     0x4e9f3590
  IL_006f:  beq        IL_0197
  IL_0074:  br.s       IL_0076
  IL_0076:  ldloc.1
  IL_0077:  ldc.i4     0x4f9f2cab
  IL_007c:  beq        IL_01eb
  IL_0081:  br         IL_0321
  IL_0086:  ldloc.1
  IL_0087:  ldc.i4     0x5cb7aa8a
  IL_008c:  beq        IL_027e
  IL_0091:  br.s       IL_0093
  IL_0093:  ldloc.1
  IL_0094:  ldc.i4     0x62366ffa
  IL_0099:  beq        IL_0182
  IL_009e:  br         IL_0321
  IL_00a3:  ldloc.1
  IL_00a4:  ldc.i4     0xb51d04ba
  IL_00a9:  bgt.un.s   IL_00ea
  IL_00ab:  ldloc.1
  IL_00ac:  ldc.i4     0x811c9dc5
  IL_00b1:  bgt.un.s   IL_00cd
  IL_00b3:  ldloc.1
  IL_00b4:  ldc.i4     0x63cb5a27
  IL_00b9:  beq        IL_0254
  IL_00be:  br.s       IL_00c0
  IL_00c0:  ldloc.1
  IL_00c1:  ldc.i4     0x811c9dc5
  IL_00c6:  beq.s      IL_0130
  IL_00c8:  br         IL_0321
  IL_00cd:  ldloc.1
  IL_00ce:  ldc.i4     0x872213e7
  IL_00d3:  beq        IL_01c1
  IL_00d8:  br.s       IL_00da
  IL_00da:  ldloc.1
  IL_00db:  ldc.i4     0xb51d04ba
  IL_00e0:  beq        IL_022a
  IL_00e5:  br         IL_0321
  IL_00ea:  ldloc.1
  IL_00eb:  ldc.i4     0xe60c2c52
  IL_00f0:  bgt.un.s   IL_0109
  IL_00f2:  ldloc.1
  IL_00f3:  ldc.i4     0xe40c292c
  IL_00f8:  beq.s      IL_0143
  IL_00fa:  br.s       IL_00fc
  IL_00fc:  ldloc.1
  IL_00fd:  ldc.i4     0xe60c2c52
  IL_0102:  beq.s      IL_016d
  IL_0104:  br         IL_0321
  IL_0109:  ldloc.1
  IL_010a:  ldc.i4     0xe6e5718f
  IL_010f:  beq        IL_023f
  IL_0114:  br.s       IL_0116
  IL_0116:  ldloc.1
  IL_0117:  ldc.i4     0xe70c2de5
  IL_011c:  beq.s      IL_0158
  IL_011e:  br.s       IL_0120
  IL_0120:  ldloc.1
  IL_0121:  ldc.i4     0xf76b8c8e
  IL_0126:  beq        IL_01d6
  IL_012b:  br         IL_0321
  IL_0130:  ldarg.0
  IL_0131:  brfalse.s  IL_013e
  IL_0133:  ldarg.0
  IL_0134:  call       "int string.Length.get"
  IL_0139:  brfalse    IL_0293
  IL_013e:  br         IL_0321
  IL_0143:  ldarg.0
  IL_0144:  ldstr      "a"
  IL_0149:  call       "bool string.op_Equality(string, string)"
  IL_014e:  brtrue     IL_029e
  IL_0153:  br         IL_0321
  IL_0158:  ldarg.0
  IL_0159:  ldstr      "b"
  IL_015e:  call       "bool string.op_Equality(string, string)"
  IL_0163:  brtrue     IL_02a9
  IL_0168:  br         IL_0321
  IL_016d:  ldarg.0
  IL_016e:  ldstr      "c"
  IL_0173:  call       "bool string.op_Equality(string, string)"
  IL_0178:  brtrue     IL_02b1
  IL_017d:  br         IL_0321
  IL_0182:  ldarg.0
  IL_0183:  ldstr      "no"
  IL_0188:  call       "bool string.op_Equality(string, string)"
  IL_018d:  brtrue     IL_02b9
  IL_0192:  br         IL_0321
  IL_0197:  ldarg.0
  IL_0198:  ldstr      "yes"
  IL_019d:  call       "bool string.op_Equality(string, string)"
  IL_01a2:  brtrue     IL_02c1
  IL_01a7:  br         IL_0321
  IL_01ac:  ldarg.0
  IL_01ad:  ldstr      "four"
  IL_01b2:  call       "bool string.op_Equality(string, string)"
  IL_01b7:  brtrue     IL_02c9
  IL_01bc:  br         IL_0321
  IL_01c1:  ldarg.0
  IL_01c2:  ldstr      "alice"
  IL_01c7:  call       "bool string.op_Equality(string, string)"
  IL_01cc:  brtrue     IL_02d1
  IL_01d1:  br         IL_0321
  IL_01d6:  ldarg.0
  IL_01d7:  ldstr      "blurb"
  IL_01dc:  call       "bool string.op_Equality(string, string)"
  IL_01e1:  brtrue     IL_02d9
  IL_01e6:  br         IL_0321
  IL_01eb:  ldarg.0
  IL_01ec:  ldstr      "hello"
  IL_01f1:  call       "bool string.op_Equality(string, string)"
  IL_01f6:  brtrue     IL_02e1
  IL_01fb:  br         IL_0321
  IL_0200:  ldarg.0
  IL_0201:  ldstr      "lamps"
  IL_0206:  call       "bool string.op_Equality(string, string)"
  IL_020b:  brtrue     IL_02e9
  IL_0210:  br         IL_0321
  IL_0215:  ldarg.0
  IL_0216:  ldstr      "lambs"
  IL_021b:  call       "bool string.op_Equality(string, string)"
  IL_0220:  brtrue     IL_02f1
  IL_0225:  br         IL_0321
  IL_022a:  ldarg.0
  IL_022b:  ldstr      "lower"
  IL_0230:  call       "bool string.op_Equality(string, string)"
  IL_0235:  brtrue     IL_02f9
  IL_023a:  br         IL_0321
  IL_023f:  ldarg.0
  IL_0240:  ldstr      "names"
  IL_0245:  call       "bool string.op_Equality(string, string)"
  IL_024a:  brtrue     IL_0301
  IL_024f:  br         IL_0321
  IL_0254:  ldarg.0
  IL_0255:  ldstr      "slurp"
  IL_025a:  call       "bool string.op_Equality(string, string)"
  IL_025f:  brtrue     IL_0309
  IL_0264:  br         IL_0321
  IL_0269:  ldarg.0
  IL_026a:  ldstr      "towed"
  IL_026f:  call       "bool string.op_Equality(string, string)"
  IL_0274:  brtrue     IL_0311
  IL_0279:  br         IL_0321
  IL_027e:  ldarg.0
  IL_027f:  ldstr      "words"
  IL_0284:  call       "bool string.op_Equality(string, string)"
  IL_0289:  brtrue     IL_0319
  IL_028e:  br         IL_0321
  IL_0293:  ldstr      "blank"
  IL_0298:  stloc.0
  IL_0299:  br         IL_0329
  IL_029e:  ldstr      "a"
  IL_02a3:  stloc.0
  IL_02a4:  br         IL_0329
  IL_02a9:  ldstr      "b"
  IL_02ae:  stloc.0
  IL_02af:  br.s       IL_0329
  IL_02b1:  ldstr      "c"
  IL_02b6:  stloc.0
  IL_02b7:  br.s       IL_0329
  IL_02b9:  ldstr      "no"
  IL_02be:  stloc.0
  IL_02bf:  br.s       IL_0329
  IL_02c1:  ldstr      "yes"
  IL_02c6:  stloc.0
  IL_02c7:  br.s       IL_0329
  IL_02c9:  ldstr      "four"
  IL_02ce:  stloc.0
  IL_02cf:  br.s       IL_0329
  IL_02d1:  ldstr      "alice"
  IL_02d6:  stloc.0
  IL_02d7:  br.s       IL_0329
  IL_02d9:  ldstr      "blurb"
  IL_02de:  stloc.0
  IL_02df:  br.s       IL_0329
  IL_02e1:  ldstr      "hello"
  IL_02e6:  stloc.0
  IL_02e7:  br.s       IL_0329
  IL_02e9:  ldstr      "lamps"
  IL_02ee:  stloc.0
  IL_02ef:  br.s       IL_0329
  IL_02f1:  ldstr      "lambs"
  IL_02f6:  stloc.0
  IL_02f7:  br.s       IL_0329
  IL_02f9:  ldstr      "lower"
  IL_02fe:  stloc.0
  IL_02ff:  br.s       IL_0329
  IL_0301:  ldstr      "names"
  IL_0306:  stloc.0
  IL_0307:  br.s       IL_0329
  IL_0309:  ldstr      "slurp"
  IL_030e:  stloc.0
  IL_030f:  br.s       IL_0329
  IL_0311:  ldstr      "towed"
  IL_0316:  stloc.0
  IL_0317:  br.s       IL_0329
  IL_0319:  ldstr      "words"
  IL_031e:  stloc.0
  IL_031f:  br.s       IL_0329
  IL_0321:  ldstr      "default"
  IL_0326:  stloc.0
  IL_0327:  br.s       IL_0329
  IL_0329:  ldc.i4.1
  IL_032a:  brtrue.s   IL_032d
  IL_032c:  nop
  IL_032d:  ldloc.0
  IL_032e:  stloc.2
  IL_032f:  br.s       IL_0331
  IL_0331:  ldloc.2
  IL_0332:  ret
}
""");
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void MixOfBucketSizes_Span(bool useReadonly)
    {
        var keyType = useReadonly ? "System.ReadOnlySpan<char>" : "System.Span<char>";
        var source = $$"""
assert("", "blank");
assert("a", "a");
assert("b", "b");
assert("c", "c");
assert("no");
assert("yes");
assert("four");
assert("blurb");
assert("hello");
assert("lamps");
assert("lambs");
assert("names");
assert("slurp");
assert("towed");
assert("words");
assert("not", "default");
assert("other", "default");
assert("y", "default");
assert("longer example", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    var inputAsKeyType = new {{keyType}}(input.ToCharArray());
    var result = C.M(inputAsKeyType);
    if (result != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {result}");
    }
}

public class C
{
    public static string M({{keyType}} o)
    {
        return o switch
        {
            "" => "blank",
            "a" => "a",
            "b" => "b",
            "c" => "c",
            "no" => "no",
            "yes" => "yes",
            "four" => "four",
            "alice" => "alice",
            "blurb" => "blurb",
            "hello" => "hello",
            "lamps" => "lamps",
            "lambs" => "lambs",
            "lower" => "lower",
            "names" => "names",
            "slurp" => "slurp",
            "towed" => "towed",
            "words" => "words",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN", verify: Verification.Skipped);
        var indexer = useReadonly ? $$"""ref readonly char {{keyType}}.this[int].get""" : $$"""ref char {{keyType}}.this[int].get""";
        verifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: false);
        verifier.VerifyIL("C.M", $$"""
{
  // Code size      648 (0x288)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "int {{keyType}}.Length.get"
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  switch    (
        IL_01f5,
        IL_002b,
        IL_00ad,
        IL_00c7,
        IL_00e1,
        IL_004f)
  IL_0026:  br         IL_0280
  IL_002b:  ldarga.s   V_0
  IL_002d:  ldc.i4.0
  IL_002e:  call       "{{indexer}}"
  IL_0033:  ldind.u2
  IL_0034:  stloc.2
  IL_0035:  ldloc.2
  IL_0036:  ldc.i4.s   97
  IL_0038:  sub
  IL_0039:  switch    (
        IL_0200,
        IL_0208,
        IL_0210)
  IL_004a:  br         IL_0280
  IL_004f:  ldarga.s   V_0
  IL_0051:  ldc.i4.0
  IL_0052:  call       "{{indexer}}"
  IL_0057:  ldind.u2
  IL_0058:  stloc.2
  IL_0059:  ldloc.2
  IL_005a:  ldc.i4.s   104
  IL_005c:  bgt.un.s   IL_007b
  IL_005e:  ldloc.2
  IL_005f:  ldc.i4.s   97
  IL_0061:  beq        IL_00fb
  IL_0066:  ldloc.2
  IL_0067:  ldc.i4.s   98
  IL_0069:  beq        IL_0115
  IL_006e:  ldloc.2
  IL_006f:  ldc.i4.s   104
  IL_0071:  beq        IL_012f
  IL_0076:  br         IL_0280
  IL_007b:  ldloc.2
  IL_007c:  ldc.i4.s   108
  IL_007e:  beq        IL_0149
  IL_0083:  ldloc.2
  IL_0084:  ldc.i4.s   110
  IL_0086:  beq        IL_018d
  IL_008b:  ldloc.2
  IL_008c:  ldc.i4.s   115
  IL_008e:  sub
  IL_008f:  switch    (
        IL_01a7,
        IL_01c1,
        IL_0280,
        IL_0280,
        IL_01db)
  IL_00a8:  br         IL_0280
  IL_00ad:  ldarg.0
  IL_00ae:  ldstr      "no"
  IL_00b3:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00b8:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_00bd:  brtrue     IL_0218
  IL_00c2:  br         IL_0280
  IL_00c7:  ldarg.0
  IL_00c8:  ldstr      "yes"
  IL_00cd:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00d2:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_00d7:  brtrue     IL_0220
  IL_00dc:  br         IL_0280
  IL_00e1:  ldarg.0
  IL_00e2:  ldstr      "four"
  IL_00e7:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00ec:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_00f1:  brtrue     IL_0228
  IL_00f6:  br         IL_0280
  IL_00fb:  ldarg.0
  IL_00fc:  ldstr      "alice"
  IL_0101:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0106:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_010b:  brtrue     IL_0230
  IL_0110:  br         IL_0280
  IL_0115:  ldarg.0
  IL_0116:  ldstr      "blurb"
  IL_011b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0120:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0125:  brtrue     IL_0238
  IL_012a:  br         IL_0280
  IL_012f:  ldarg.0
  IL_0130:  ldstr      "hello"
  IL_0135:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_013a:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_013f:  brtrue     IL_0240
  IL_0144:  br         IL_0280
  IL_0149:  ldarg.0
  IL_014a:  ldstr      "lamps"
  IL_014f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0154:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0159:  brtrue     IL_0248
  IL_015e:  ldarg.0
  IL_015f:  ldstr      "lambs"
  IL_0164:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0169:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_016e:  brtrue     IL_0250
  IL_0173:  ldarg.0
  IL_0174:  ldstr      "lower"
  IL_0179:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_017e:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0183:  brtrue     IL_0258
  IL_0188:  br         IL_0280
  IL_018d:  ldarg.0
  IL_018e:  ldstr      "names"
  IL_0193:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0198:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_019d:  brtrue     IL_0260
  IL_01a2:  br         IL_0280
  IL_01a7:  ldarg.0
  IL_01a8:  ldstr      "slurp"
  IL_01ad:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_01b2:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01b7:  brtrue     IL_0268
  IL_01bc:  br         IL_0280
  IL_01c1:  ldarg.0
  IL_01c2:  ldstr      "towed"
  IL_01c7:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_01cc:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01d1:  brtrue     IL_0270
  IL_01d6:  br         IL_0280
  IL_01db:  ldarg.0
  IL_01dc:  ldstr      "words"
  IL_01e1:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_01e6:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01eb:  brtrue     IL_0278
  IL_01f0:  br         IL_0280
  IL_01f5:  ldstr      "blank"
  IL_01fa:  stloc.0
  IL_01fb:  br         IL_0286
  IL_0200:  ldstr      "a"
  IL_0205:  stloc.0
  IL_0206:  br.s       IL_0286
  IL_0208:  ldstr      "b"
  IL_020d:  stloc.0
  IL_020e:  br.s       IL_0286
  IL_0210:  ldstr      "c"
  IL_0215:  stloc.0
  IL_0216:  br.s       IL_0286
  IL_0218:  ldstr      "no"
  IL_021d:  stloc.0
  IL_021e:  br.s       IL_0286
  IL_0220:  ldstr      "yes"
  IL_0225:  stloc.0
  IL_0226:  br.s       IL_0286
  IL_0228:  ldstr      "four"
  IL_022d:  stloc.0
  IL_022e:  br.s       IL_0286
  IL_0230:  ldstr      "alice"
  IL_0235:  stloc.0
  IL_0236:  br.s       IL_0286
  IL_0238:  ldstr      "blurb"
  IL_023d:  stloc.0
  IL_023e:  br.s       IL_0286
  IL_0240:  ldstr      "hello"
  IL_0245:  stloc.0
  IL_0246:  br.s       IL_0286
  IL_0248:  ldstr      "lamps"
  IL_024d:  stloc.0
  IL_024e:  br.s       IL_0286
  IL_0250:  ldstr      "lambs"
  IL_0255:  stloc.0
  IL_0256:  br.s       IL_0286
  IL_0258:  ldstr      "lower"
  IL_025d:  stloc.0
  IL_025e:  br.s       IL_0286
  IL_0260:  ldstr      "names"
  IL_0265:  stloc.0
  IL_0266:  br.s       IL_0286
  IL_0268:  ldstr      "slurp"
  IL_026d:  stloc.0
  IL_026e:  br.s       IL_0286
  IL_0270:  ldstr      "towed"
  IL_0275:  stloc.0
  IL_0276:  br.s       IL_0286
  IL_0278:  ldstr      "words"
  IL_027d:  stloc.0
  IL_027e:  br.s       IL_0286
  IL_0280:  ldstr      "default"
  IL_0285:  stloc.0
  IL_0286:  ldloc.0
  IL_0287:  ret
}
""");

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        var helper = useReadonly ? "ComputeReadOnlySpanHash(System.ReadOnlySpan<char>)" : "ComputeSpanHash(System.Span<char>)";
        verifier.VerifyMemberInIL("<PrivateImplementationDetails>." + helper, expected: true);
        verifier.VerifyIL("C.M", $$"""
{
  // Code size      861 (0x35d)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.{{helper}}"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x62366ffa
  IL_000d:  bgt.un     IL_0096
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x3ec721c0
  IL_0018:  bgt.un.s   IL_0058
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4     0x2f69f5a5
  IL_0020:  bgt.un.s   IL_003d
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x242602e6
  IL_0028:  beq        IL_022e
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0x2f69f5a5
  IL_0033:  beq        IL_01ac
  IL_0038:  br         IL_0355
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4     0x3448ae58
  IL_0043:  beq        IL_0214
  IL_0048:  ldloc.1
  IL_0049:  ldc.i4     0x3ec721c0
  IL_004e:  beq        IL_0296
  IL_0053:  br         IL_0355
  IL_0058:  ldloc.1
  IL_0059:  ldc.i4     0x4f9f2cab
  IL_005e:  bgt.un.s   IL_007b
  IL_0060:  ldloc.1
  IL_0061:  ldc.i4     0x4e9f3590
  IL_0066:  beq        IL_0192
  IL_006b:  ldloc.1
  IL_006c:  ldc.i4     0x4f9f2cab
  IL_0071:  beq        IL_01fa
  IL_0076:  br         IL_0355
  IL_007b:  ldloc.1
  IL_007c:  ldc.i4     0x5cb7aa8a
  IL_0081:  beq        IL_02b0
  IL_0086:  ldloc.1
  IL_0087:  ldc.i4     0x62366ffa
  IL_008c:  beq        IL_0178
  IL_0091:  br         IL_0355
  IL_0096:  ldloc.1
  IL_0097:  ldc.i4     0xb51d04ba
  IL_009c:  bgt.un.s   IL_00d9
  IL_009e:  ldloc.1
  IL_009f:  ldc.i4     0x811c9dc5
  IL_00a4:  bgt.un.s   IL_00be
  IL_00a6:  ldloc.1
  IL_00a7:  ldc.i4     0x63cb5a27
  IL_00ac:  beq        IL_027c
  IL_00b1:  ldloc.1
  IL_00b2:  ldc.i4     0x811c9dc5
  IL_00b7:  beq.s      IL_0119
  IL_00b9:  br         IL_0355
  IL_00be:  ldloc.1
  IL_00bf:  ldc.i4     0x872213e7
  IL_00c4:  beq        IL_01c6
  IL_00c9:  ldloc.1
  IL_00ca:  ldc.i4     0xb51d04ba
  IL_00cf:  beq        IL_0248
  IL_00d4:  br         IL_0355
  IL_00d9:  ldloc.1
  IL_00da:  ldc.i4     0xe60c2c52
  IL_00df:  bgt.un.s   IL_00f6
  IL_00e1:  ldloc.1
  IL_00e2:  ldc.i4     0xe40c292c
  IL_00e7:  beq.s      IL_012a
  IL_00e9:  ldloc.1
  IL_00ea:  ldc.i4     0xe60c2c52
  IL_00ef:  beq.s      IL_015e
  IL_00f1:  br         IL_0355
  IL_00f6:  ldloc.1
  IL_00f7:  ldc.i4     0xe6e5718f
  IL_00fc:  beq        IL_0262
  IL_0101:  ldloc.1
  IL_0102:  ldc.i4     0xe70c2de5
  IL_0107:  beq.s      IL_0144
  IL_0109:  ldloc.1
  IL_010a:  ldc.i4     0xf76b8c8e
  IL_010f:  beq        IL_01e0
  IL_0114:  br         IL_0355
  IL_0119:  ldarga.s   V_0
  IL_011b:  call       "int {{keyType}}.Length.get"
  IL_0120:  brfalse    IL_02ca
  IL_0125:  br         IL_0355
  IL_012a:  ldarg.0
  IL_012b:  ldstr      "a"
  IL_0130:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0135:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_013a:  brtrue     IL_02d5
  IL_013f:  br         IL_0355
  IL_0144:  ldarg.0
  IL_0145:  ldstr      "b"
  IL_014a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_014f:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0154:  brtrue     IL_02dd
  IL_0159:  br         IL_0355
  IL_015e:  ldarg.0
  IL_015f:  ldstr      "c"
  IL_0164:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0169:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_016e:  brtrue     IL_02e5
  IL_0173:  br         IL_0355
  IL_0178:  ldarg.0
  IL_0179:  ldstr      "no"
  IL_017e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0183:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0188:  brtrue     IL_02ed
  IL_018d:  br         IL_0355
  IL_0192:  ldarg.0
  IL_0193:  ldstr      "yes"
  IL_0198:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_019d:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01a2:  brtrue     IL_02f5
  IL_01a7:  br         IL_0355
  IL_01ac:  ldarg.0
  IL_01ad:  ldstr      "four"
  IL_01b2:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_01b7:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01bc:  brtrue     IL_02fd
  IL_01c1:  br         IL_0355
  IL_01c6:  ldarg.0
  IL_01c7:  ldstr      "alice"
  IL_01cc:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_01d1:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01d6:  brtrue     IL_0305
  IL_01db:  br         IL_0355
  IL_01e0:  ldarg.0
  IL_01e1:  ldstr      "blurb"
  IL_01e6:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_01eb:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_01f0:  brtrue     IL_030d
  IL_01f5:  br         IL_0355
  IL_01fa:  ldarg.0
  IL_01fb:  ldstr      "hello"
  IL_0200:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0205:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_020a:  brtrue     IL_0315
  IL_020f:  br         IL_0355
  IL_0214:  ldarg.0
  IL_0215:  ldstr      "lamps"
  IL_021a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_021f:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0224:  brtrue     IL_031d
  IL_0229:  br         IL_0355
  IL_022e:  ldarg.0
  IL_022f:  ldstr      "lambs"
  IL_0234:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0239:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_023e:  brtrue     IL_0325
  IL_0243:  br         IL_0355
  IL_0248:  ldarg.0
  IL_0249:  ldstr      "lower"
  IL_024e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0253:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0258:  brtrue     IL_032d
  IL_025d:  br         IL_0355
  IL_0262:  ldarg.0
  IL_0263:  ldstr      "names"
  IL_0268:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_026d:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_0272:  brtrue     IL_0335
  IL_0277:  br         IL_0355
  IL_027c:  ldarg.0
  IL_027d:  ldstr      "slurp"
  IL_0282:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0287:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_028c:  brtrue     IL_033d
  IL_0291:  br         IL_0355
  IL_0296:  ldarg.0
  IL_0297:  ldstr      "towed"
  IL_029c:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_02a1:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_02a6:  brtrue     IL_0345
  IL_02ab:  br         IL_0355
  IL_02b0:  ldarg.0
  IL_02b1:  ldstr      "words"
  IL_02b6:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_02bb:  call       "bool System.MemoryExtensions.SequenceEqual<char>({{keyType}}, System.ReadOnlySpan<char>)"
  IL_02c0:  brtrue     IL_034d
  IL_02c5:  br         IL_0355
  IL_02ca:  ldstr      "blank"
  IL_02cf:  stloc.0
  IL_02d0:  br         IL_035b
  IL_02d5:  ldstr      "a"
  IL_02da:  stloc.0
  IL_02db:  br.s       IL_035b
  IL_02dd:  ldstr      "b"
  IL_02e2:  stloc.0
  IL_02e3:  br.s       IL_035b
  IL_02e5:  ldstr      "c"
  IL_02ea:  stloc.0
  IL_02eb:  br.s       IL_035b
  IL_02ed:  ldstr      "no"
  IL_02f2:  stloc.0
  IL_02f3:  br.s       IL_035b
  IL_02f5:  ldstr      "yes"
  IL_02fa:  stloc.0
  IL_02fb:  br.s       IL_035b
  IL_02fd:  ldstr      "four"
  IL_0302:  stloc.0
  IL_0303:  br.s       IL_035b
  IL_0305:  ldstr      "alice"
  IL_030a:  stloc.0
  IL_030b:  br.s       IL_035b
  IL_030d:  ldstr      "blurb"
  IL_0312:  stloc.0
  IL_0313:  br.s       IL_035b
  IL_0315:  ldstr      "hello"
  IL_031a:  stloc.0
  IL_031b:  br.s       IL_035b
  IL_031d:  ldstr      "lamps"
  IL_0322:  stloc.0
  IL_0323:  br.s       IL_035b
  IL_0325:  ldstr      "lambs"
  IL_032a:  stloc.0
  IL_032b:  br.s       IL_035b
  IL_032d:  ldstr      "lower"
  IL_0332:  stloc.0
  IL_0333:  br.s       IL_035b
  IL_0335:  ldstr      "names"
  IL_033a:  stloc.0
  IL_033b:  br.s       IL_035b
  IL_033d:  ldstr      "slurp"
  IL_0342:  stloc.0
  IL_0343:  br.s       IL_035b
  IL_0345:  ldstr      "towed"
  IL_034a:  stloc.0
  IL_034b:  br.s       IL_035b
  IL_034d:  ldstr      "words"
  IL_0352:  stloc.0
  IL_0353:  br.s       IL_035b
  IL_0355:  ldstr      "default"
  IL_035a:  stloc.0
  IL_035b:  ldloc.0
  IL_035c:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void TwoNullCases()
    {
        var source = """
public class C
{
    public static string M(object o)
    {
        return o switch
        {
            null => "null1",
            null => "null2",
            "" => "blank",
            "a" => "a",
            "b" => "b",
            "c" => "c",
            "no" => "no",
            "yes" => "yes",
            "four" => "four",
            "alice" => "alice",
            "blurb" => "blurb",
            "hello" => "hello",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             null => "null2",
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "null").WithLocation(8, 13)
            );
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void BucketSizeOne()
    {
        var source = """
assert(null, "default");
assert("", "default");
assert("a");
assert("b", "default");
assert("ab");
assert("abc");
assert("abcd");
assert("abcde");
assert("abcdef");
assert("not", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(object o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: false);
        verifier.VerifyIL("C.M", """
{
  // Code size      272 (0x110)
  .maxstack  2
  .locals init (string V_0,
                string V_1,
                int V_2,
                char V_3)
  IL_0000:  ldarg.0
  IL_0001:  isinst     "string"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse    IL_0108
  IL_000d:  ldloc.1
  IL_000e:  brfalse    IL_0108
  IL_0013:  ldloc.1
  IL_0014:  call       "int string.Length.get"
  IL_0019:  stloc.2
  IL_001a:  ldloc.2
  IL_001b:  ldc.i4.1
  IL_001c:  sub
  IL_001d:  switch    (
        IL_0047,
        IL_0059,
        IL_006b,
        IL_007d,
        IL_008c,
        IL_009b,
        IL_00aa,
        IL_00b9)
  IL_0042:  br         IL_0108
  IL_0047:  ldloc.1
  IL_0048:  ldstr      "a"
  IL_004d:  call       "bool string.op_Equality(string, string)"
  IL_0052:  brtrue.s   IL_00c8
  IL_0054:  br         IL_0108
  IL_0059:  ldloc.1
  IL_005a:  ldstr      "ab"
  IL_005f:  call       "bool string.op_Equality(string, string)"
  IL_0064:  brtrue.s   IL_00d0
  IL_0066:  br         IL_0108
  IL_006b:  ldloc.1
  IL_006c:  ldstr      "abc"
  IL_0071:  call       "bool string.op_Equality(string, string)"
  IL_0076:  brtrue.s   IL_00d8
  IL_0078:  br         IL_0108
  IL_007d:  ldloc.1
  IL_007e:  ldstr      "abcd"
  IL_0083:  call       "bool string.op_Equality(string, string)"
  IL_0088:  brtrue.s   IL_00e0
  IL_008a:  br.s       IL_0108
  IL_008c:  ldloc.1
  IL_008d:  ldstr      "abcde"
  IL_0092:  call       "bool string.op_Equality(string, string)"
  IL_0097:  brtrue.s   IL_00e8
  IL_0099:  br.s       IL_0108
  IL_009b:  ldloc.1
  IL_009c:  ldstr      "abcdef"
  IL_00a1:  call       "bool string.op_Equality(string, string)"
  IL_00a6:  brtrue.s   IL_00f0
  IL_00a8:  br.s       IL_0108
  IL_00aa:  ldloc.1
  IL_00ab:  ldstr      "abcdefg"
  IL_00b0:  call       "bool string.op_Equality(string, string)"
  IL_00b5:  brtrue.s   IL_00f8
  IL_00b7:  br.s       IL_0108
  IL_00b9:  ldloc.1
  IL_00ba:  ldstr      "abcdefgh"
  IL_00bf:  call       "bool string.op_Equality(string, string)"
  IL_00c4:  brtrue.s   IL_0100
  IL_00c6:  br.s       IL_0108
  IL_00c8:  ldstr      "a"
  IL_00cd:  stloc.0
  IL_00ce:  br.s       IL_010e
  IL_00d0:  ldstr      "ab"
  IL_00d5:  stloc.0
  IL_00d6:  br.s       IL_010e
  IL_00d8:  ldstr      "abc"
  IL_00dd:  stloc.0
  IL_00de:  br.s       IL_010e
  IL_00e0:  ldstr      "abcd"
  IL_00e5:  stloc.0
  IL_00e6:  br.s       IL_010e
  IL_00e8:  ldstr      "abcde"
  IL_00ed:  stloc.0
  IL_00ee:  br.s       IL_010e
  IL_00f0:  ldstr      "abcdef"
  IL_00f5:  stloc.0
  IL_00f6:  br.s       IL_010e
  IL_00f8:  ldstr      "abcdefg"
  IL_00fd:  stloc.0
  IL_00fe:  br.s       IL_010e
  IL_0100:  ldstr      "abcdefgh"
  IL_0105:  stloc.0
  IL_0106:  br.s       IL_010e
  IL_0108:  ldstr      "default"
  IL_010d:  stloc.0
  IL_010e:  ldloc.0
  IL_010f:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL("<PrivateImplementationDetails>." + PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: true);
        verifier.VerifyIL("C.M", """
{
  // Code size      335 (0x14f)
  .maxstack  2
  .locals init (string V_0,
                string V_1,
                uint V_2)
  IL_0000:  ldarg.0
  IL_0001:  isinst     "string"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse    IL_0147
  IL_000d:  ldloc.1
  IL_000e:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0013:  stloc.2
  IL_0014:  ldloc.2
  IL_0015:  ldc.i4     0x749bcf08
  IL_001a:  bgt.un.s   IL_0051
  IL_001c:  ldloc.2
  IL_001d:  ldc.i4     0x2a9eb737
  IL_0022:  bgt.un.s   IL_003c
  IL_0024:  ldloc.2
  IL_0025:  ldc.i4     0x1a47e90b
  IL_002a:  beq.s      IL_00aa
  IL_002c:  ldloc.2
  IL_002d:  ldc.i4     0x2a9eb737
  IL_0032:  beq        IL_00e9
  IL_0037:  br         IL_0147
  IL_003c:  ldloc.2
  IL_003d:  ldc.i4     0x4d2505ca
  IL_0042:  beq.s      IL_0098
  IL_0044:  ldloc.2
  IL_0045:  ldc.i4     0x749bcf08
  IL_004a:  beq.s      IL_00cb
  IL_004c:  br         IL_0147
  IL_0051:  ldloc.2
  IL_0052:  ldc.i4     0xce3479bd
  IL_0057:  bgt.un.s   IL_0071
  IL_0059:  ldloc.2
  IL_005a:  ldc.i4     0x76daaa8d
  IL_005f:  beq        IL_00f8
  IL_0064:  ldloc.2
  IL_0065:  ldc.i4     0xce3479bd
  IL_006a:  beq.s      IL_00bc
  IL_006c:  br         IL_0147
  IL_0071:  ldloc.2
  IL_0072:  ldc.i4     0xe40c292c
  IL_0077:  beq.s      IL_0086
  IL_0079:  ldloc.2
  IL_007a:  ldc.i4     0xff478a2a
  IL_007f:  beq.s      IL_00da
  IL_0081:  br         IL_0147
  IL_0086:  ldloc.1
  IL_0087:  ldstr      "a"
  IL_008c:  call       "bool string.op_Equality(string, string)"
  IL_0091:  brtrue.s   IL_0107
  IL_0093:  br         IL_0147
  IL_0098:  ldloc.1
  IL_0099:  ldstr      "ab"
  IL_009e:  call       "bool string.op_Equality(string, string)"
  IL_00a3:  brtrue.s   IL_010f
  IL_00a5:  br         IL_0147
  IL_00aa:  ldloc.1
  IL_00ab:  ldstr      "abc"
  IL_00b0:  call       "bool string.op_Equality(string, string)"
  IL_00b5:  brtrue.s   IL_0117
  IL_00b7:  br         IL_0147
  IL_00bc:  ldloc.1
  IL_00bd:  ldstr      "abcd"
  IL_00c2:  call       "bool string.op_Equality(string, string)"
  IL_00c7:  brtrue.s   IL_011f
  IL_00c9:  br.s       IL_0147
  IL_00cb:  ldloc.1
  IL_00cc:  ldstr      "abcde"
  IL_00d1:  call       "bool string.op_Equality(string, string)"
  IL_00d6:  brtrue.s   IL_0127
  IL_00d8:  br.s       IL_0147
  IL_00da:  ldloc.1
  IL_00db:  ldstr      "abcdef"
  IL_00e0:  call       "bool string.op_Equality(string, string)"
  IL_00e5:  brtrue.s   IL_012f
  IL_00e7:  br.s       IL_0147
  IL_00e9:  ldloc.1
  IL_00ea:  ldstr      "abcdefg"
  IL_00ef:  call       "bool string.op_Equality(string, string)"
  IL_00f4:  brtrue.s   IL_0137
  IL_00f6:  br.s       IL_0147
  IL_00f8:  ldloc.1
  IL_00f9:  ldstr      "abcdefgh"
  IL_00fe:  call       "bool string.op_Equality(string, string)"
  IL_0103:  brtrue.s   IL_013f
  IL_0105:  br.s       IL_0147
  IL_0107:  ldstr      "a"
  IL_010c:  stloc.0
  IL_010d:  br.s       IL_014d
  IL_010f:  ldstr      "ab"
  IL_0114:  stloc.0
  IL_0115:  br.s       IL_014d
  IL_0117:  ldstr      "abc"
  IL_011c:  stloc.0
  IL_011d:  br.s       IL_014d
  IL_011f:  ldstr      "abcd"
  IL_0124:  stloc.0
  IL_0125:  br.s       IL_014d
  IL_0127:  ldstr      "abcde"
  IL_012c:  stloc.0
  IL_012d:  br.s       IL_014d
  IL_012f:  ldstr      "abcdef"
  IL_0134:  stloc.0
  IL_0135:  br.s       IL_014d
  IL_0137:  ldstr      "abcdefg"
  IL_013c:  stloc.0
  IL_013d:  br.s       IL_014d
  IL_013f:  ldstr      "abcdefgh"
  IL_0144:  stloc.0
  IL_0145:  br.s       IL_014d
  IL_0147:  ldstr      "default"
  IL_014c:  stloc.0
  IL_014d:  ldloc.0
  IL_014e:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void BucketSizeOne_WithIntValues()
    {
        var source = """
assert(null, "default");
assert("", "default");
assert("a");
assert("b", "default");
assert("ab");
assert("abc");
assert("abcd");
assert("abcde");
assert("abcdef");
assert("not", "default");
assert("other", "default");
assert(0, "0");
assert(1, "default");
System.Console.Write("RAN");

void assert(object input, string expected = null)
{
    if (C.M(input) != (expected ?? input.ToString()))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(object o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            0 => "0",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: false);
        verifier.VerifyIL("C.M", """
{
  // Code size      310 (0x136)
  .maxstack  2
  .locals init (string V_0,
                string V_1,
                int V_2,
                char V_3)
  IL_0000:  ldarg.0
  IL_0001:  isinst     "string"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse    IL_00d4
  IL_000d:  ldloc.1
  IL_000e:  brfalse    IL_012e
  IL_0013:  ldloc.1
  IL_0014:  call       "int string.Length.get"
  IL_0019:  stloc.2
  IL_001a:  ldloc.2
  IL_001b:  ldc.i4.1
  IL_001c:  sub
  IL_001d:  switch    (
        IL_0047,
        IL_005c,
        IL_0071,
        IL_0083,
        IL_0095,
        IL_00a7,
        IL_00b6,
        IL_00c5)
  IL_0042:  br         IL_012e
  IL_0047:  ldloc.1
  IL_0048:  ldstr      "a"
  IL_004d:  call       "bool string.op_Equality(string, string)"
  IL_0052:  brtrue     IL_00e6
  IL_0057:  br         IL_012e
  IL_005c:  ldloc.1
  IL_005d:  ldstr      "ab"
  IL_0062:  call       "bool string.op_Equality(string, string)"
  IL_0067:  brtrue     IL_00ee
  IL_006c:  br         IL_012e
  IL_0071:  ldloc.1
  IL_0072:  ldstr      "abc"
  IL_0077:  call       "bool string.op_Equality(string, string)"
  IL_007c:  brtrue.s   IL_00f6
  IL_007e:  br         IL_012e
  IL_0083:  ldloc.1
  IL_0084:  ldstr      "abcd"
  IL_0089:  call       "bool string.op_Equality(string, string)"
  IL_008e:  brtrue.s   IL_00fe
  IL_0090:  br         IL_012e
  IL_0095:  ldloc.1
  IL_0096:  ldstr      "abcde"
  IL_009b:  call       "bool string.op_Equality(string, string)"
  IL_00a0:  brtrue.s   IL_0106
  IL_00a2:  br         IL_012e
  IL_00a7:  ldloc.1
  IL_00a8:  ldstr      "abcdef"
  IL_00ad:  call       "bool string.op_Equality(string, string)"
  IL_00b2:  brtrue.s   IL_010e
  IL_00b4:  br.s       IL_012e
  IL_00b6:  ldloc.1
  IL_00b7:  ldstr      "abcdefg"
  IL_00bc:  call       "bool string.op_Equality(string, string)"
  IL_00c1:  brtrue.s   IL_0116
  IL_00c3:  br.s       IL_012e
  IL_00c5:  ldloc.1
  IL_00c6:  ldstr      "abcdefgh"
  IL_00cb:  call       "bool string.op_Equality(string, string)"
  IL_00d0:  brtrue.s   IL_011e
  IL_00d2:  br.s       IL_012e
  IL_00d4:  ldarg.0
  IL_00d5:  isinst     "int"
  IL_00da:  brfalse.s  IL_012e
  IL_00dc:  ldarg.0
  IL_00dd:  unbox.any  "int"
  IL_00e2:  brfalse.s  IL_0126
  IL_00e4:  br.s       IL_012e
  IL_00e6:  ldstr      "a"
  IL_00eb:  stloc.0
  IL_00ec:  br.s       IL_0134
  IL_00ee:  ldstr      "ab"
  IL_00f3:  stloc.0
  IL_00f4:  br.s       IL_0134
  IL_00f6:  ldstr      "abc"
  IL_00fb:  stloc.0
  IL_00fc:  br.s       IL_0134
  IL_00fe:  ldstr      "abcd"
  IL_0103:  stloc.0
  IL_0104:  br.s       IL_0134
  IL_0106:  ldstr      "abcde"
  IL_010b:  stloc.0
  IL_010c:  br.s       IL_0134
  IL_010e:  ldstr      "abcdef"
  IL_0113:  stloc.0
  IL_0114:  br.s       IL_0134
  IL_0116:  ldstr      "abcdefg"
  IL_011b:  stloc.0
  IL_011c:  br.s       IL_0134
  IL_011e:  ldstr      "abcdefgh"
  IL_0123:  stloc.0
  IL_0124:  br.s       IL_0134
  IL_0126:  ldstr      "0"
  IL_012b:  stloc.0
  IL_012c:  br.s       IL_0134
  IL_012e:  ldstr      "default"
  IL_0133:  stloc.0
  IL_0134:  ldloc.0
  IL_0135:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingStringLength()
    {
        var source = """
public class C
{
    public static string M(string o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.MakeMemberMissing(SpecialMember.System_String__Length);
        comp.VerifyEmitDiagnostics(
            // error CS0656: Missing compiler required member 'System.String.get_Length'
            //
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "").WithArguments("System.String", "get_Length").WithLocation(1, 1)
            );

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.MakeMemberMissing(SpecialMember.System_String__Length);
        comp.VerifyEmitDiagnostics(
            // error CS0656: Missing compiler required member 'System.String.get_Length'
            //
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "").WithArguments("System.String", "get_Length").WithLocation(1, 1)
            );
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingStringIndexer()
    {
        var source = """
public class C
{
    public static string M(string o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.MakeMemberMissing(SpecialMember.System_String__Chars);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        verifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: false);
        verifier.VerifyIL("C.M", """
{
  // Code size      178 (0xb2)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "a"
  IL_0006:  call       "bool string.op_Equality(string, string)"
  IL_000b:  brtrue.s   IL_006a
  IL_000d:  ldarg.0
  IL_000e:  ldstr      "ab"
  IL_0013:  call       "bool string.op_Equality(string, string)"
  IL_0018:  brtrue.s   IL_0072
  IL_001a:  ldarg.0
  IL_001b:  ldstr      "abc"
  IL_0020:  call       "bool string.op_Equality(string, string)"
  IL_0025:  brtrue.s   IL_007a
  IL_0027:  ldarg.0
  IL_0028:  ldstr      "abcd"
  IL_002d:  call       "bool string.op_Equality(string, string)"
  IL_0032:  brtrue.s   IL_0082
  IL_0034:  ldarg.0
  IL_0035:  ldstr      "abcde"
  IL_003a:  call       "bool string.op_Equality(string, string)"
  IL_003f:  brtrue.s   IL_008a
  IL_0041:  ldarg.0
  IL_0042:  ldstr      "abcdef"
  IL_0047:  call       "bool string.op_Equality(string, string)"
  IL_004c:  brtrue.s   IL_0092
  IL_004e:  ldarg.0
  IL_004f:  ldstr      "abcdefg"
  IL_0054:  call       "bool string.op_Equality(string, string)"
  IL_0059:  brtrue.s   IL_009a
  IL_005b:  ldarg.0
  IL_005c:  ldstr      "abcdefgh"
  IL_0061:  call       "bool string.op_Equality(string, string)"
  IL_0066:  brtrue.s   IL_00a2
  IL_0068:  br.s       IL_00aa
  IL_006a:  ldstr      "a"
  IL_006f:  stloc.0
  IL_0070:  br.s       IL_00b0
  IL_0072:  ldstr      "ab"
  IL_0077:  stloc.0
  IL_0078:  br.s       IL_00b0
  IL_007a:  ldstr      "abc"
  IL_007f:  stloc.0
  IL_0080:  br.s       IL_00b0
  IL_0082:  ldstr      "abcd"
  IL_0087:  stloc.0
  IL_0088:  br.s       IL_00b0
  IL_008a:  ldstr      "abcde"
  IL_008f:  stloc.0
  IL_0090:  br.s       IL_00b0
  IL_0092:  ldstr      "abcdef"
  IL_0097:  stloc.0
  IL_0098:  br.s       IL_00b0
  IL_009a:  ldstr      "abcdefg"
  IL_009f:  stloc.0
  IL_00a0:  br.s       IL_00b0
  IL_00a2:  ldstr      "abcdefgh"
  IL_00a7:  stloc.0
  IL_00a8:  br.s       IL_00b0
  IL_00aa:  ldstr      "default"
  IL_00af:  stloc.0
  IL_00b0:  ldloc.0
  IL_00b1:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingSpanLength()
    {
        var source = """
public class C
{
    public static string M(System.Span<char> o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Length);
        comp.VerifyEmitDiagnostics(
            // (7,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "a" => "a",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""a""").WithArguments("System.Span`1", "get_Length").WithLocation(7, 13),
            // (8,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "ab" => "ab",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""ab""").WithArguments("System.Span`1", "get_Length").WithLocation(8, 13),
            // (9,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "abc" => "abc",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abc""").WithArguments("System.Span`1", "get_Length").WithLocation(9, 13),
            // (10,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "abcd" => "abcd",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcd""").WithArguments("System.Span`1", "get_Length").WithLocation(10, 13),
            // (11,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "abcde" => "abcde",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcde""").WithArguments("System.Span`1", "get_Length").WithLocation(11, 13),
            // (12,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "abcdef" => "abcdef",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdef""").WithArguments("System.Span`1", "get_Length").WithLocation(12, 13),
            // (13,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "abcdefg" => "abcdefg",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdefg""").WithArguments("System.Span`1", "get_Length").WithLocation(13, 13),
            // (14,13): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
            //             "abcdefgh" => "abcdefgh",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdefgh""").WithArguments("System.Span`1", "get_Length").WithLocation(14, 13)
            );
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingSpanIndexer()
    {
        var source = """
public class C
{
    public static string M(System.Span<char> o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
        verifier.VerifyIL("C.M", """
{
  // Code size      221 (0xdd)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "a"
  IL_0006:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_000b:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0010:  brtrue     IL_0095
  IL_0015:  ldarg.0
  IL_0016:  ldstr      "ab"
  IL_001b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0020:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0025:  brtrue.s   IL_009d
  IL_0027:  ldarg.0
  IL_0028:  ldstr      "abc"
  IL_002d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0032:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0037:  brtrue.s   IL_00a5
  IL_0039:  ldarg.0
  IL_003a:  ldstr      "abcd"
  IL_003f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0044:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0049:  brtrue.s   IL_00ad
  IL_004b:  ldarg.0
  IL_004c:  ldstr      "abcde"
  IL_0051:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0056:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_005b:  brtrue.s   IL_00b5
  IL_005d:  ldarg.0
  IL_005e:  ldstr      "abcdef"
  IL_0063:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0068:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_006d:  brtrue.s   IL_00bd
  IL_006f:  ldarg.0
  IL_0070:  ldstr      "abcdefg"
  IL_0075:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_007a:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_007f:  brtrue.s   IL_00c5
  IL_0081:  ldarg.0
  IL_0082:  ldstr      "abcdefgh"
  IL_0087:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_008c:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0091:  brtrue.s   IL_00cd
  IL_0093:  br.s       IL_00d5
  IL_0095:  ldstr      "a"
  IL_009a:  stloc.0
  IL_009b:  br.s       IL_00db
  IL_009d:  ldstr      "ab"
  IL_00a2:  stloc.0
  IL_00a3:  br.s       IL_00db
  IL_00a5:  ldstr      "abc"
  IL_00aa:  stloc.0
  IL_00ab:  br.s       IL_00db
  IL_00ad:  ldstr      "abcd"
  IL_00b2:  stloc.0
  IL_00b3:  br.s       IL_00db
  IL_00b5:  ldstr      "abcde"
  IL_00ba:  stloc.0
  IL_00bb:  br.s       IL_00db
  IL_00bd:  ldstr      "abcdef"
  IL_00c2:  stloc.0
  IL_00c3:  br.s       IL_00db
  IL_00c5:  ldstr      "abcdefg"
  IL_00ca:  stloc.0
  IL_00cb:  br.s       IL_00db
  IL_00cd:  ldstr      "abcdefgh"
  IL_00d2:  stloc.0
  IL_00d3:  br.s       IL_00db
  IL_00d5:  ldstr      "default"
  IL_00da:  stloc.0
  IL_00db:  ldloc.0
  IL_00dc:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingReadOnlySpanIndexer()
    {
        var source = """
public class C
{
    public static string M(System.ReadOnlySpan<char> o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
        verifier.VerifyIL("C.M", """
{
  // Code size      221 (0xdd)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "a"
  IL_0006:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_000b:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0010:  brtrue     IL_0095
  IL_0015:  ldarg.0
  IL_0016:  ldstr      "ab"
  IL_001b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0020:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0025:  brtrue.s   IL_009d
  IL_0027:  ldarg.0
  IL_0028:  ldstr      "abc"
  IL_002d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0032:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0037:  brtrue.s   IL_00a5
  IL_0039:  ldarg.0
  IL_003a:  ldstr      "abcd"
  IL_003f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0044:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0049:  brtrue.s   IL_00ad
  IL_004b:  ldarg.0
  IL_004c:  ldstr      "abcde"
  IL_0051:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0056:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_005b:  brtrue.s   IL_00b5
  IL_005d:  ldarg.0
  IL_005e:  ldstr      "abcdef"
  IL_0063:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0068:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_006d:  brtrue.s   IL_00bd
  IL_006f:  ldarg.0
  IL_0070:  ldstr      "abcdefg"
  IL_0075:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_007a:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_007f:  brtrue.s   IL_00c5
  IL_0081:  ldarg.0
  IL_0082:  ldstr      "abcdefgh"
  IL_0087:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_008c:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0091:  brtrue.s   IL_00cd
  IL_0093:  br.s       IL_00d5
  IL_0095:  ldstr      "a"
  IL_009a:  stloc.0
  IL_009b:  br.s       IL_00db
  IL_009d:  ldstr      "ab"
  IL_00a2:  stloc.0
  IL_00a3:  br.s       IL_00db
  IL_00a5:  ldstr      "abc"
  IL_00aa:  stloc.0
  IL_00ab:  br.s       IL_00db
  IL_00ad:  ldstr      "abcd"
  IL_00b2:  stloc.0
  IL_00b3:  br.s       IL_00db
  IL_00b5:  ldstr      "abcde"
  IL_00ba:  stloc.0
  IL_00bb:  br.s       IL_00db
  IL_00bd:  ldstr      "abcdef"
  IL_00c2:  stloc.0
  IL_00c3:  br.s       IL_00db
  IL_00c5:  ldstr      "abcdefg"
  IL_00ca:  stloc.0
  IL_00cb:  br.s       IL_00db
  IL_00cd:  ldstr      "abcdefgh"
  IL_00d2:  stloc.0
  IL_00d3:  br.s       IL_00db
  IL_00d5:  ldstr      "default"
  IL_00da:  stloc.0
  IL_00db:  ldloc.0
  IL_00dc:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingAsSpan()
    {
        var source = """
public class C
{
    public static string M(System.Span<char> o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.MakeMemberMissing(WellKnownMember.System_MemoryExtensions__AsSpan_String);
        comp.VerifyEmitDiagnostics(
            // (7,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "a" => "a",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""a""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(7, 13),
            // (8,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "ab" => "ab",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""ab""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(8, 13),
            // (9,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "abc" => "abc",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abc""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(9, 13),
            // (10,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "abcd" => "abcd",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcd""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(10, 13),
            // (11,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "abcde" => "abcde",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcde""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(11, 13),
            // (12,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "abcdef" => "abcdef",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdef""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(12, 13),
            // (13,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "abcdefg" => "abcdefg",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdefg""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(13, 13),
            // (14,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            //             "abcdefgh" => "abcdefgh",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdefgh""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(14, 13)
            );
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    internal void BucketSizeOne_MissingSequenceEquals()
    {
        var source = """
public class C
{
    public static string M(System.Span<char> o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
        comp.MakeMemberMissing(WellKnownMember.System_MemoryExtensions__SequenceEqual_Span_T);
        comp.VerifyEmitDiagnostics(
            // (7,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "a" => "a",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""a""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(7, 13),
            // (8,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "ab" => "ab",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""ab""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(8, 13),
            // (9,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "abc" => "abc",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abc""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(9, 13),
            // (10,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "abcd" => "abcd",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcd""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(10, 13),
            // (11,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "abcde" => "abcde",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcde""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(11, 13),
            // (12,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "abcdef" => "abcdef",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdef""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(12, 13),
            // (13,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "abcdefg" => "abcdefg",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdefg""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(13, 13),
            // (14,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
            //             "abcdefgh" => "abcdefgh",
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""abcdefgh""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(14, 13)
            );
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void BucketSizeOne_WithNull()
    {
        var source = """
assert(null, "null");
assert("", "default");
assert("a");
assert("ab");
assert("abc");
assert("abcd");
assert("abcde");
assert("abcdef");
assert("not", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(object o)
    {
        return o switch
        {
            "a" => "a",
            "ab" => "ab",
            "abc" => "abc",
            "abcd" => "abcd",
            "abcde" => "abcde",
            "abcdef" => "abcdef",
            "abcdefg" => "abcdefg",
            "abcdefgh" => "abcdefgh",
            null => "null",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: false);
        verifier.VerifyIL("C.M", """
{
  // Code size      288 (0x120)
  .maxstack  2
  .locals init (string V_0,
                string V_1,
                int V_2,
                char V_3)
  IL_0000:  ldarg.0
  IL_0001:  isinst     "string"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse    IL_00cb
  IL_000d:  ldloc.1
  IL_000e:  brfalse    IL_0118
  IL_0013:  ldloc.1
  IL_0014:  call       "int string.Length.get"
  IL_0019:  stloc.2
  IL_001a:  ldloc.2
  IL_001b:  ldc.i4.1
  IL_001c:  sub
  IL_001d:  switch    (
        IL_0047,
        IL_0059,
        IL_006b,
        IL_007d,
        IL_008f,
        IL_009e,
        IL_00ad,
        IL_00bc)
  IL_0042:  br         IL_0118
  IL_0047:  ldloc.1
  IL_0048:  ldstr      "a"
  IL_004d:  call       "bool string.op_Equality(string, string)"
  IL_0052:  brtrue.s   IL_00d0
  IL_0054:  br         IL_0118
  IL_0059:  ldloc.1
  IL_005a:  ldstr      "ab"
  IL_005f:  call       "bool string.op_Equality(string, string)"
  IL_0064:  brtrue.s   IL_00d8
  IL_0066:  br         IL_0118
  IL_006b:  ldloc.1
  IL_006c:  ldstr      "abc"
  IL_0071:  call       "bool string.op_Equality(string, string)"
  IL_0076:  brtrue.s   IL_00e0
  IL_0078:  br         IL_0118
  IL_007d:  ldloc.1
  IL_007e:  ldstr      "abcd"
  IL_0083:  call       "bool string.op_Equality(string, string)"
  IL_0088:  brtrue.s   IL_00e8
  IL_008a:  br         IL_0118
  IL_008f:  ldloc.1
  IL_0090:  ldstr      "abcde"
  IL_0095:  call       "bool string.op_Equality(string, string)"
  IL_009a:  brtrue.s   IL_00f0
  IL_009c:  br.s       IL_0118
  IL_009e:  ldloc.1
  IL_009f:  ldstr      "abcdef"
  IL_00a4:  call       "bool string.op_Equality(string, string)"
  IL_00a9:  brtrue.s   IL_00f8
  IL_00ab:  br.s       IL_0118
  IL_00ad:  ldloc.1
  IL_00ae:  ldstr      "abcdefg"
  IL_00b3:  call       "bool string.op_Equality(string, string)"
  IL_00b8:  brtrue.s   IL_0100
  IL_00ba:  br.s       IL_0118
  IL_00bc:  ldloc.1
  IL_00bd:  ldstr      "abcdefgh"
  IL_00c2:  call       "bool string.op_Equality(string, string)"
  IL_00c7:  brtrue.s   IL_0108
  IL_00c9:  br.s       IL_0118
  IL_00cb:  ldarg.0
  IL_00cc:  brfalse.s  IL_0110
  IL_00ce:  br.s       IL_0118
  IL_00d0:  ldstr      "a"
  IL_00d5:  stloc.0
  IL_00d6:  br.s       IL_011e
  IL_00d8:  ldstr      "ab"
  IL_00dd:  stloc.0
  IL_00de:  br.s       IL_011e
  IL_00e0:  ldstr      "abc"
  IL_00e5:  stloc.0
  IL_00e6:  br.s       IL_011e
  IL_00e8:  ldstr      "abcd"
  IL_00ed:  stloc.0
  IL_00ee:  br.s       IL_011e
  IL_00f0:  ldstr      "abcde"
  IL_00f5:  stloc.0
  IL_00f6:  br.s       IL_011e
  IL_00f8:  ldstr      "abcdef"
  IL_00fd:  stloc.0
  IL_00fe:  br.s       IL_011e
  IL_0100:  ldstr      "abcdefg"
  IL_0105:  stloc.0
  IL_0106:  br.s       IL_011e
  IL_0108:  ldstr      "abcdefgh"
  IL_010d:  stloc.0
  IL_010e:  br.s       IL_011e
  IL_0110:  ldstr      "null"
  IL_0115:  stloc.0
  IL_0116:  br.s       IL_011e
  IL_0118:  ldstr      "default"
  IL_011d:  stloc.0
  IL_011e:  ldloc.0
  IL_011f:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyMemberInIL("<PrivateImplementationDetails>." + PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(string)", expected: true);
        verifier.VerifyIL("C.M", """
{
  // Code size      357 (0x165)
  .maxstack  2
  .locals init (string V_0,
                string V_1,
                uint V_2)
  IL_0000:  ldarg.0
  IL_0001:  isinst     "string"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse    IL_0110
  IL_000d:  ldloc.1
  IL_000e:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0013:  stloc.2
  IL_0014:  ldloc.2
  IL_0015:  ldc.i4     0x749bcf08
  IL_001a:  bgt.un.s   IL_0057
  IL_001c:  ldloc.2
  IL_001d:  ldc.i4     0x2a9eb737
  IL_0022:  bgt.un.s   IL_003f
  IL_0024:  ldloc.2
  IL_0025:  ldc.i4     0x1a47e90b
  IL_002a:  beq        IL_00b0
  IL_002f:  ldloc.2
  IL_0030:  ldc.i4     0x2a9eb737
  IL_0035:  beq        IL_00f2
  IL_003a:  br         IL_015d
  IL_003f:  ldloc.2
  IL_0040:  ldc.i4     0x4d2505ca
  IL_0045:  beq.s      IL_009e
  IL_0047:  ldloc.2
  IL_0048:  ldc.i4     0x749bcf08
  IL_004d:  beq        IL_00d4
  IL_0052:  br         IL_015d
  IL_0057:  ldloc.2
  IL_0058:  ldc.i4     0xce3479bd
  IL_005d:  bgt.un.s   IL_0077
  IL_005f:  ldloc.2
  IL_0060:  ldc.i4     0x76daaa8d
  IL_0065:  beq        IL_0101
  IL_006a:  ldloc.2
  IL_006b:  ldc.i4     0xce3479bd
  IL_0070:  beq.s      IL_00c2
  IL_0072:  br         IL_015d
  IL_0077:  ldloc.2
  IL_0078:  ldc.i4     0xe40c292c
  IL_007d:  beq.s      IL_008c
  IL_007f:  ldloc.2
  IL_0080:  ldc.i4     0xff478a2a
  IL_0085:  beq.s      IL_00e3
  IL_0087:  br         IL_015d
  IL_008c:  ldloc.1
  IL_008d:  ldstr      "a"
  IL_0092:  call       "bool string.op_Equality(string, string)"
  IL_0097:  brtrue.s   IL_0115
  IL_0099:  br         IL_015d
  IL_009e:  ldloc.1
  IL_009f:  ldstr      "ab"
  IL_00a4:  call       "bool string.op_Equality(string, string)"
  IL_00a9:  brtrue.s   IL_011d
  IL_00ab:  br         IL_015d
  IL_00b0:  ldloc.1
  IL_00b1:  ldstr      "abc"
  IL_00b6:  call       "bool string.op_Equality(string, string)"
  IL_00bb:  brtrue.s   IL_0125
  IL_00bd:  br         IL_015d
  IL_00c2:  ldloc.1
  IL_00c3:  ldstr      "abcd"
  IL_00c8:  call       "bool string.op_Equality(string, string)"
  IL_00cd:  brtrue.s   IL_012d
  IL_00cf:  br         IL_015d
  IL_00d4:  ldloc.1
  IL_00d5:  ldstr      "abcde"
  IL_00da:  call       "bool string.op_Equality(string, string)"
  IL_00df:  brtrue.s   IL_0135
  IL_00e1:  br.s       IL_015d
  IL_00e3:  ldloc.1
  IL_00e4:  ldstr      "abcdef"
  IL_00e9:  call       "bool string.op_Equality(string, string)"
  IL_00ee:  brtrue.s   IL_013d
  IL_00f0:  br.s       IL_015d
  IL_00f2:  ldloc.1
  IL_00f3:  ldstr      "abcdefg"
  IL_00f8:  call       "bool string.op_Equality(string, string)"
  IL_00fd:  brtrue.s   IL_0145
  IL_00ff:  br.s       IL_015d
  IL_0101:  ldloc.1
  IL_0102:  ldstr      "abcdefgh"
  IL_0107:  call       "bool string.op_Equality(string, string)"
  IL_010c:  brtrue.s   IL_014d
  IL_010e:  br.s       IL_015d
  IL_0110:  ldarg.0
  IL_0111:  brfalse.s  IL_0155
  IL_0113:  br.s       IL_015d
  IL_0115:  ldstr      "a"
  IL_011a:  stloc.0
  IL_011b:  br.s       IL_0163
  IL_011d:  ldstr      "ab"
  IL_0122:  stloc.0
  IL_0123:  br.s       IL_0163
  IL_0125:  ldstr      "abc"
  IL_012a:  stloc.0
  IL_012b:  br.s       IL_0163
  IL_012d:  ldstr      "abcd"
  IL_0132:  stloc.0
  IL_0133:  br.s       IL_0163
  IL_0135:  ldstr      "abcde"
  IL_013a:  stloc.0
  IL_013b:  br.s       IL_0163
  IL_013d:  ldstr      "abcdef"
  IL_0142:  stloc.0
  IL_0143:  br.s       IL_0163
  IL_0145:  ldstr      "abcdefg"
  IL_014a:  stloc.0
  IL_014b:  br.s       IL_0163
  IL_014d:  ldstr      "abcdefgh"
  IL_0152:  stloc.0
  IL_0153:  br.s       IL_0163
  IL_0155:  ldstr      "null"
  IL_015a:  stloc.0
  IL_015b:  br.s       IL_0163
  IL_015d:  ldstr      "default"
  IL_0162:  stloc.0
  IL_0163:  ldloc.0
  IL_0164:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void ContentType()
    {
        var source = """
assert(null, false);
assert("", false);
assert("a", false);
assert("aa", false);
assert("text/xml");
assert("text/css");
assert("text/csv");
assert("image/gif");
assert("image/png");
assert("text/html");
assert("text/plain");
assert("image/jpeg");
assert("application/pdf");
assert("application/xml");
assert("application/zip");
assert("application/grpc");
assert("application/json");
assert("multipart/form-data");
assert("application/javascript");
assert("application/octet-stream");
assert("text/html; charset=utf-8");
assert("text/plain; charset=utf-8");
assert("application/json; charset=utf-8");
assert("application/x-www-form-urlencoded");
System.Console.Write("RAN");

void assert(string input, bool expected = true)
{
    if (C.M(input) != expected)
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static bool M(string contentTypeValue)
    {
        switch (contentTypeValue)
        {
            case "text/xml":
            case "text/css":
            case "text/csv":
            case "image/gif":
            case "image/png":
            case "text/html":
            case "text/plain":
            case "image/jpeg":
            case "application/pdf":
            case "application/xml":
            case "application/zip":
            case "application/grpc":
            case "application/json":
            case "multipart/form-data":
            case "application/javascript":
            case "application/octet-stream":
            case "text/html; charset=utf-8":
            case "text/plain; charset=utf-8":
            case "application/json; charset=utf-8":
            case "application/x-www-form-urlencoded":
                return true;
        }
        return false;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      693 (0x2b5)
  .maxstack  2
  .locals init (int V_0,
                char V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_02b3
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.s   25
  IL_0010:  bgt.s      IL_0068
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.8
  IL_0014:  sub
  IL_0015:  switch    (
        IL_007d,
        IL_00a2,
        IL_00c7,
        IL_02b3,
        IL_02b3,
        IL_02b3,
        IL_02b3,
        IL_00e4,
        IL_010a)
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4.s   19
  IL_0041:  sub
  IL_0042:  switch    (
        IL_024a,
        IL_02b3,
        IL_02b3,
        IL_0259,
        IL_02b3,
        IL_0128,
        IL_0286)
  IL_0063:  br         IL_02b3
  IL_0068:  ldloc.0
  IL_0069:  ldc.i4.s   31
  IL_006b:  beq        IL_0295
  IL_0070:  ldloc.0
  IL_0071:  ldc.i4.s   33
  IL_0073:  beq        IL_02a4
  IL_0078:  br         IL_02b3
  IL_007d:  ldarg.0
  IL_007e:  ldc.i4.7
  IL_007f:  call       "char string.this[int].get"
  IL_0084:  stloc.1
  IL_0085:  ldloc.1
  IL_0086:  ldc.i4.s   108
  IL_0088:  beq        IL_0145
  IL_008d:  ldloc.1
  IL_008e:  ldc.i4.s   115
  IL_0090:  beq        IL_015a
  IL_0095:  ldloc.1
  IL_0096:  ldc.i4.s   118
  IL_0098:  beq        IL_016f
  IL_009d:  br         IL_02b3
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.6
  IL_00a4:  call       "char string.this[int].get"
  IL_00a9:  stloc.1
  IL_00aa:  ldloc.1
  IL_00ab:  ldc.i4.s   103
  IL_00ad:  beq        IL_0184
  IL_00b2:  ldloc.1
  IL_00b3:  ldc.i4.s   112
  IL_00b5:  beq        IL_0199
  IL_00ba:  ldloc.1
  IL_00bb:  ldc.i4.s   116
  IL_00bd:  beq        IL_01ae
  IL_00c2:  br         IL_02b3
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.0
  IL_00c9:  call       "char string.this[int].get"
  IL_00ce:  stloc.1
  IL_00cf:  ldloc.1
  IL_00d0:  ldc.i4.s   105
  IL_00d2:  beq        IL_01d8
  IL_00d7:  ldloc.1
  IL_00d8:  ldc.i4.s   116
  IL_00da:  beq        IL_01c3
  IL_00df:  br         IL_02b3
  IL_00e4:  ldarg.0
  IL_00e5:  ldc.i4.s   12
  IL_00e7:  call       "char string.this[int].get"
  IL_00ec:  stloc.1
  IL_00ed:  ldloc.1
  IL_00ee:  ldc.i4.s   112
  IL_00f0:  beq        IL_01ed
  IL_00f5:  ldloc.1
  IL_00f6:  ldc.i4.s   120
  IL_00f8:  beq        IL_0202
  IL_00fd:  ldloc.1
  IL_00fe:  ldc.i4.s   122
  IL_0100:  beq        IL_0217
  IL_0105:  br         IL_02b3
  IL_010a:  ldarg.0
  IL_010b:  ldc.i4.s   12
  IL_010d:  call       "char string.this[int].get"
  IL_0112:  stloc.1
  IL_0113:  ldloc.1
  IL_0114:  ldc.i4.s   103
  IL_0116:  beq        IL_022c
  IL_011b:  ldloc.1
  IL_011c:  ldc.i4.s   106
  IL_011e:  beq        IL_023b
  IL_0123:  br         IL_02b3
  IL_0128:  ldarg.0
  IL_0129:  ldc.i4.0
  IL_012a:  call       "char string.this[int].get"
  IL_012f:  stloc.1
  IL_0130:  ldloc.1
  IL_0131:  ldc.i4.s   97
  IL_0133:  beq        IL_0268
  IL_0138:  ldloc.1
  IL_0139:  ldc.i4.s   116
  IL_013b:  beq        IL_0277
  IL_0140:  br         IL_02b3
  IL_0145:  ldarg.0
  IL_0146:  ldstr      "text/xml"
  IL_014b:  call       "bool string.op_Equality(string, string)"
  IL_0150:  brtrue     IL_02b1
  IL_0155:  br         IL_02b3
  IL_015a:  ldarg.0
  IL_015b:  ldstr      "text/css"
  IL_0160:  call       "bool string.op_Equality(string, string)"
  IL_0165:  brtrue     IL_02b1
  IL_016a:  br         IL_02b3
  IL_016f:  ldarg.0
  IL_0170:  ldstr      "text/csv"
  IL_0175:  call       "bool string.op_Equality(string, string)"
  IL_017a:  brtrue     IL_02b1
  IL_017f:  br         IL_02b3
  IL_0184:  ldarg.0
  IL_0185:  ldstr      "image/gif"
  IL_018a:  call       "bool string.op_Equality(string, string)"
  IL_018f:  brtrue     IL_02b1
  IL_0194:  br         IL_02b3
  IL_0199:  ldarg.0
  IL_019a:  ldstr      "image/png"
  IL_019f:  call       "bool string.op_Equality(string, string)"
  IL_01a4:  brtrue     IL_02b1
  IL_01a9:  br         IL_02b3
  IL_01ae:  ldarg.0
  IL_01af:  ldstr      "text/html"
  IL_01b4:  call       "bool string.op_Equality(string, string)"
  IL_01b9:  brtrue     IL_02b1
  IL_01be:  br         IL_02b3
  IL_01c3:  ldarg.0
  IL_01c4:  ldstr      "text/plain"
  IL_01c9:  call       "bool string.op_Equality(string, string)"
  IL_01ce:  brtrue     IL_02b1
  IL_01d3:  br         IL_02b3
  IL_01d8:  ldarg.0
  IL_01d9:  ldstr      "image/jpeg"
  IL_01de:  call       "bool string.op_Equality(string, string)"
  IL_01e3:  brtrue     IL_02b1
  IL_01e8:  br         IL_02b3
  IL_01ed:  ldarg.0
  IL_01ee:  ldstr      "application/pdf"
  IL_01f3:  call       "bool string.op_Equality(string, string)"
  IL_01f8:  brtrue     IL_02b1
  IL_01fd:  br         IL_02b3
  IL_0202:  ldarg.0
  IL_0203:  ldstr      "application/xml"
  IL_0208:  call       "bool string.op_Equality(string, string)"
  IL_020d:  brtrue     IL_02b1
  IL_0212:  br         IL_02b3
  IL_0217:  ldarg.0
  IL_0218:  ldstr      "application/zip"
  IL_021d:  call       "bool string.op_Equality(string, string)"
  IL_0222:  brtrue     IL_02b1
  IL_0227:  br         IL_02b3
  IL_022c:  ldarg.0
  IL_022d:  ldstr      "application/grpc"
  IL_0232:  call       "bool string.op_Equality(string, string)"
  IL_0237:  brtrue.s   IL_02b1
  IL_0239:  br.s       IL_02b3
  IL_023b:  ldarg.0
  IL_023c:  ldstr      "application/json"
  IL_0241:  call       "bool string.op_Equality(string, string)"
  IL_0246:  brtrue.s   IL_02b1
  IL_0248:  br.s       IL_02b3
  IL_024a:  ldarg.0
  IL_024b:  ldstr      "multipart/form-data"
  IL_0250:  call       "bool string.op_Equality(string, string)"
  IL_0255:  brtrue.s   IL_02b1
  IL_0257:  br.s       IL_02b3
  IL_0259:  ldarg.0
  IL_025a:  ldstr      "application/javascript"
  IL_025f:  call       "bool string.op_Equality(string, string)"
  IL_0264:  brtrue.s   IL_02b1
  IL_0266:  br.s       IL_02b3
  IL_0268:  ldarg.0
  IL_0269:  ldstr      "application/octet-stream"
  IL_026e:  call       "bool string.op_Equality(string, string)"
  IL_0273:  brtrue.s   IL_02b1
  IL_0275:  br.s       IL_02b3
  IL_0277:  ldarg.0
  IL_0278:  ldstr      "text/html; charset=utf-8"
  IL_027d:  call       "bool string.op_Equality(string, string)"
  IL_0282:  brtrue.s   IL_02b1
  IL_0284:  br.s       IL_02b3
  IL_0286:  ldarg.0
  IL_0287:  ldstr      "text/plain; charset=utf-8"
  IL_028c:  call       "bool string.op_Equality(string, string)"
  IL_0291:  brtrue.s   IL_02b1
  IL_0293:  br.s       IL_02b3
  IL_0295:  ldarg.0
  IL_0296:  ldstr      "application/json; charset=utf-8"
  IL_029b:  call       "bool string.op_Equality(string, string)"
  IL_02a0:  brtrue.s   IL_02b1
  IL_02a2:  br.s       IL_02b3
  IL_02a4:  ldarg.0
  IL_02a5:  ldstr      "application/x-www-form-urlencoded"
  IL_02aa:  call       "bool string.op_Equality(string, string)"
  IL_02af:  brfalse.s  IL_02b3
  IL_02b1:  ldc.i4.1
  IL_02b2:  ret
  IL_02b3:  ldc.i4.0
  IL_02b4:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      694 (0x2b6)
  .maxstack  2
  .locals init (uint V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0xad238cee
  IL_000d:  bgt.un     IL_00ac
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x498c93a5
  IL_0018:  bgt.un.s   IL_0063
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4     0x12648e18
  IL_0020:  bgt.un.s   IL_003d
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4     0xa54742b
  IL_0028:  beq        IL_0287
  IL_002d:  ldloc.0
  IL_002e:  ldc.i4     0x12648e18
  IL_0033:  beq        IL_0146
  IL_0038:  br         IL_02b4
  IL_003d:  ldloc.0
  IL_003e:  ldc.i4     0x335c6202
  IL_0043:  beq        IL_0278
  IL_0048:  ldloc.0
  IL_0049:  ldc.i4     0x3e4bb053
  IL_004e:  beq        IL_0203
  IL_0053:  ldloc.0
  IL_0054:  ldc.i4     0x498c93a5
  IL_0059:  beq        IL_025a
  IL_005e:  br         IL_02b4
  IL_0063:  ldloc.0
  IL_0064:  ldc.i4     0x75d8656e
  IL_0069:  bgt.un.s   IL_0086
  IL_006b:  ldloc.0
  IL_006c:  ldc.i4     0x69e8e6a8
  IL_0071:  beq        IL_023c
  IL_0076:  ldloc.0
  IL_0077:  ldc.i4     0x75d8656e
  IL_007c:  beq        IL_01af
  IL_0081:  br         IL_02b4
  IL_0086:  ldloc.0
  IL_0087:  ldc.i4     0x84ebb23d
  IL_008c:  beq        IL_0218
  IL_0091:  ldloc.0
  IL_0092:  ldc.i4     0xacccdd84
  IL_0097:  beq        IL_01ee
  IL_009c:  ldloc.0
  IL_009d:  ldc.i4     0xad238cee
  IL_00a2:  beq        IL_015b
  IL_00a7:  br         IL_02b4
  IL_00ac:  ldloc.0
  IL_00ad:  ldc.i4     0xd23bd685
  IL_00b2:  bgt.un.s   IL_00fd
  IL_00b4:  ldloc.0
  IL_00b5:  ldc.i4     0xb22394cd
  IL_00ba:  bgt.un.s   IL_00d7
  IL_00bc:  ldloc.0
  IL_00bd:  ldc.i4     0xb00abf3a
  IL_00c2:  beq        IL_019a
  IL_00c7:  ldloc.0
  IL_00c8:  ldc.i4     0xb22394cd
  IL_00cd:  beq        IL_0170
  IL_00d2:  br         IL_02b4
  IL_00d7:  ldloc.0
  IL_00d8:  ldc.i4     0xb71b3e3f
  IL_00dd:  beq        IL_0185
  IL_00e2:  ldloc.0
  IL_00e3:  ldc.i4     0xc0fe6950
  IL_00e8:  beq        IL_0269
  IL_00ed:  ldloc.0
  IL_00ee:  ldc.i4     0xd23bd685
  IL_00f3:  beq        IL_01c4
  IL_00f8:  br         IL_02b4
  IL_00fd:  ldloc.0
  IL_00fe:  ldc.i4     0xdced728d
  IL_0103:  bgt.un.s   IL_0120
  IL_0105:  ldloc.0
  IL_0106:  ldc.i4     0xd4121ccc
  IL_010b:  beq        IL_0296
  IL_0110:  ldloc.0
  IL_0111:  ldc.i4     0xdced728d
  IL_0116:  beq        IL_024b
  IL_011b:  br         IL_02b4
  IL_0120:  ldloc.0
  IL_0121:  ldc.i4     0xe88a7c5d
  IL_0126:  beq        IL_01d9
  IL_012b:  ldloc.0
  IL_012c:  ldc.i4     0xef625ac9
  IL_0131:  beq        IL_02a5
  IL_0136:  ldloc.0
  IL_0137:  ldc.i4     0xfb352546
  IL_013c:  beq        IL_022d
  IL_0141:  br         IL_02b4
  IL_0146:  ldarg.0
  IL_0147:  ldstr      "text/xml"
  IL_014c:  call       "bool string.op_Equality(string, string)"
  IL_0151:  brtrue     IL_02b2
  IL_0156:  br         IL_02b4
  IL_015b:  ldarg.0
  IL_015c:  ldstr      "text/css"
  IL_0161:  call       "bool string.op_Equality(string, string)"
  IL_0166:  brtrue     IL_02b2
  IL_016b:  br         IL_02b4
  IL_0170:  ldarg.0
  IL_0171:  ldstr      "text/csv"
  IL_0176:  call       "bool string.op_Equality(string, string)"
  IL_017b:  brtrue     IL_02b2
  IL_0180:  br         IL_02b4
  IL_0185:  ldarg.0
  IL_0186:  ldstr      "image/gif"
  IL_018b:  call       "bool string.op_Equality(string, string)"
  IL_0190:  brtrue     IL_02b2
  IL_0195:  br         IL_02b4
  IL_019a:  ldarg.0
  IL_019b:  ldstr      "image/png"
  IL_01a0:  call       "bool string.op_Equality(string, string)"
  IL_01a5:  brtrue     IL_02b2
  IL_01aa:  br         IL_02b4
  IL_01af:  ldarg.0
  IL_01b0:  ldstr      "text/html"
  IL_01b5:  call       "bool string.op_Equality(string, string)"
  IL_01ba:  brtrue     IL_02b2
  IL_01bf:  br         IL_02b4
  IL_01c4:  ldarg.0
  IL_01c5:  ldstr      "text/plain"
  IL_01ca:  call       "bool string.op_Equality(string, string)"
  IL_01cf:  brtrue     IL_02b2
  IL_01d4:  br         IL_02b4
  IL_01d9:  ldarg.0
  IL_01da:  ldstr      "image/jpeg"
  IL_01df:  call       "bool string.op_Equality(string, string)"
  IL_01e4:  brtrue     IL_02b2
  IL_01e9:  br         IL_02b4
  IL_01ee:  ldarg.0
  IL_01ef:  ldstr      "application/pdf"
  IL_01f4:  call       "bool string.op_Equality(string, string)"
  IL_01f9:  brtrue     IL_02b2
  IL_01fe:  br         IL_02b4
  IL_0203:  ldarg.0
  IL_0204:  ldstr      "application/xml"
  IL_0209:  call       "bool string.op_Equality(string, string)"
  IL_020e:  brtrue     IL_02b2
  IL_0213:  br         IL_02b4
  IL_0218:  ldarg.0
  IL_0219:  ldstr      "application/zip"
  IL_021e:  call       "bool string.op_Equality(string, string)"
  IL_0223:  brtrue     IL_02b2
  IL_0228:  br         IL_02b4
  IL_022d:  ldarg.0
  IL_022e:  ldstr      "application/grpc"
  IL_0233:  call       "bool string.op_Equality(string, string)"
  IL_0238:  brtrue.s   IL_02b2
  IL_023a:  br.s       IL_02b4
  IL_023c:  ldarg.0
  IL_023d:  ldstr      "application/json"
  IL_0242:  call       "bool string.op_Equality(string, string)"
  IL_0247:  brtrue.s   IL_02b2
  IL_0249:  br.s       IL_02b4
  IL_024b:  ldarg.0
  IL_024c:  ldstr      "multipart/form-data"
  IL_0251:  call       "bool string.op_Equality(string, string)"
  IL_0256:  brtrue.s   IL_02b2
  IL_0258:  br.s       IL_02b4
  IL_025a:  ldarg.0
  IL_025b:  ldstr      "application/javascript"
  IL_0260:  call       "bool string.op_Equality(string, string)"
  IL_0265:  brtrue.s   IL_02b2
  IL_0267:  br.s       IL_02b4
  IL_0269:  ldarg.0
  IL_026a:  ldstr      "application/octet-stream"
  IL_026f:  call       "bool string.op_Equality(string, string)"
  IL_0274:  brtrue.s   IL_02b2
  IL_0276:  br.s       IL_02b4
  IL_0278:  ldarg.0
  IL_0279:  ldstr      "text/html; charset=utf-8"
  IL_027e:  call       "bool string.op_Equality(string, string)"
  IL_0283:  brtrue.s   IL_02b2
  IL_0285:  br.s       IL_02b4
  IL_0287:  ldarg.0
  IL_0288:  ldstr      "text/plain; charset=utf-8"
  IL_028d:  call       "bool string.op_Equality(string, string)"
  IL_0292:  brtrue.s   IL_02b2
  IL_0294:  br.s       IL_02b4
  IL_0296:  ldarg.0
  IL_0297:  ldstr      "application/json; charset=utf-8"
  IL_029c:  call       "bool string.op_Equality(string, string)"
  IL_02a1:  brtrue.s   IL_02b2
  IL_02a3:  br.s       IL_02b4
  IL_02a5:  ldarg.0
  IL_02a6:  ldstr      "application/x-www-form-urlencoded"
  IL_02ab:  call       "bool string.op_Equality(string, string)"
  IL_02b0:  brfalse.s  IL_02b4
  IL_02b2:  ldc.i4.1
  IL_02b3:  ret
  IL_02b4:  ldc.i4.0
  IL_02b5:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void BucketSizeTen()
    {
        var source = """
for (int i = 0; i < 50; i++)
{
    assert(i.ToString("D2"));
}

for (int i = 0; i < 10; i++)
{
    assert("A" + i.ToString("D1"), "default");
    assert(i.ToString("D1") + "A", "default");
}

assert(null, "default");
assert("", "default");
assert("50", "default");

System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    expected ??= input;
    if (C.M(input) != expected)
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string x)
    {
        return x switch
        {
            "00" => "00",
            "01" => "01",
            "02" => "02",
            "03" => "03",
            "04" => "04",
            "05" => "05",
            "06" => "06",
            "07" => "07",
            "08" => "08",
            "09" => "09",
            "10" => "10",
            "11" => "11",
            "12" => "12",
            "13" => "13",
            "14" => "14",
            "15" => "15",
            "16" => "16",
            "17" => "17",
            "18" => "18",
            "19" => "19",
            "20" => "20",
            "21" => "21",
            "22" => "22",
            "23" => "23",
            "24" => "24",
            "25" => "25",
            "26" => "26",
            "27" => "27",
            "28" => "28",
            "29" => "29",
            "30" => "30",
            "31" => "31",
            "32" => "32",
            "33" => "33",
            "34" => "34",
            "35" => "35",
            "36" => "36",
            "37" => "37",
            "38" => "38",
            "39" => "39",
            "40" => "40",
            "41" => "41",
            "42" => "42",
            "43" => "43",
            "44" => "44",
            "45" => "45",
            "46" => "46",
            "47" => "47",
            "48" => "48",
            "49" => "49",
            "59" => "59",
            "69" => "69",
            "79" => "79",
            "89" => "89",
            "99" => "99",
            _ => "default"
        };
    }
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     2632 (0xa48)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x87f05176
  IL_000d:  bgt.un     IL_01c5
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x1bed68db
  IL_0018:  bgt.un     IL_00e5
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4     0x18eb258b
  IL_0023:  bgt.un.s   IL_0079
  IL_0025:  ldloc.1
  IL_0026:  ldc.i4     0x14eb1f3f
  IL_002b:  bgt.un.s   IL_0053
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0xce8d410
  IL_0033:  beq        IL_07bf
  IL_0038:  ldloc.1
  IL_0039:  ldc.i4     0x13eb1dac
  IL_003e:  beq        IL_050a
  IL_0043:  ldloc.1
  IL_0044:  ldc.i4     0x14eb1f3f
  IL_0049:  beq        IL_051f
  IL_004e:  br         IL_0a40
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4     0x14fea6f7
  IL_0059:  beq        IL_07fe
  IL_005e:  ldloc.1
  IL_005f:  ldc.i4     0x17eb23f8
  IL_0064:  beq        IL_04b6
  IL_0069:  ldloc.1
  IL_006a:  ldc.i4     0x18eb258b
  IL_006f:  beq        IL_04cb
  IL_0074:  br         IL_0a40
  IL_0079:  ldloc.1
  IL_007a:  ldc.i4     0x19ed65b5
  IL_007f:  bgt.un.s   IL_00a7
  IL_0081:  ldloc.1
  IL_0082:  ldc.i4     0x18ed6422
  IL_0087:  beq        IL_044d
  IL_008c:  ldloc.1
  IL_008d:  ldc.i4     0x19eb271e
  IL_0092:  beq        IL_04e0
  IL_0097:  ldloc.1
  IL_0098:  ldc.i4     0x19ed65b5
  IL_009d:  beq        IL_0438
  IL_00a2:  br         IL_0a40
  IL_00a7:  ldloc.1
  IL_00a8:  ldc.i4     0x1aed6748
  IL_00ad:  bgt.un.s   IL_00ca
  IL_00af:  ldloc.1
  IL_00b0:  ldc.i4     0x1aeb28b1
  IL_00b5:  beq        IL_04f5
  IL_00ba:  ldloc.1
  IL_00bb:  ldc.i4     0x1aed6748
  IL_00c0:  beq        IL_0423
  IL_00c5:  br         IL_0a40
  IL_00ca:  ldloc.1
  IL_00cb:  ldc.i4     0x1beb2a44
  IL_00d0:  beq        IL_0462
  IL_00d5:  ldloc.1
  IL_00d6:  ldc.i4     0x1bed68db
  IL_00db:  beq        IL_040e
  IL_00e0:  br         IL_0a40
  IL_00e5:  ldloc.1
  IL_00e6:  ldc.i4     0x1fed6f27
  IL_00eb:  bgt.un.s   IL_0159
  IL_00ed:  ldloc.1
  IL_00ee:  ldc.i4     0x1deb2d6a
  IL_00f3:  bgt.un.s   IL_011b
  IL_00f5:  ldloc.1
  IL_00f6:  ldc.i4     0x1ceb2bd7
  IL_00fb:  beq        IL_0477
  IL_0100:  ldloc.1
  IL_0101:  ldc.i4     0x1ced6a6e
  IL_0106:  beq        IL_03f9
  IL_010b:  ldloc.1
  IL_010c:  ldc.i4     0x1deb2d6a
  IL_0111:  beq        IL_048c
  IL_0116:  br         IL_0a40
  IL_011b:  ldloc.1
  IL_011c:  ldc.i4     0x1eeb2efd
  IL_0121:  bgt.un.s   IL_013e
  IL_0123:  ldloc.1
  IL_0124:  ldc.i4     0x1ded6c01
  IL_0129:  beq        IL_03e4
  IL_012e:  ldloc.1
  IL_012f:  ldc.i4     0x1eeb2efd
  IL_0134:  beq        IL_04a1
  IL_0139:  br         IL_0a40
  IL_013e:  ldloc.1
  IL_013f:  ldc.i4     0x1eed6d94
  IL_0144:  beq        IL_03cf
  IL_0149:  ldloc.1
  IL_014a:  ldc.i4     0x1fed6f27
  IL_014f:  beq        IL_03ba
  IL_0154:  br         IL_0a40
  IL_0159:  ldloc.1
  IL_015a:  ldc.i4     0x85f04e50
  IL_015f:  bgt.un.s   IL_0187
  IL_0161:  ldloc.1
  IL_0162:  ldc.i4     0x20ed70ba
  IL_0167:  beq        IL_03a5
  IL_016c:  ldloc.1
  IL_016d:  ldc.i4     0x21ed724d
  IL_0172:  beq        IL_0390
  IL_0177:  ldloc.1
  IL_0178:  ldc.i4     0x85f04e50
  IL_017d:  beq        IL_0630
  IL_0182:  br         IL_0a40
  IL_0187:  ldloc.1
  IL_0188:  ldc.i4     0x86f04fe3
  IL_018d:  bgt.un.s   IL_01aa
  IL_018f:  ldloc.1
  IL_0190:  ldc.i4     0x86e383f0
  IL_0195:  beq        IL_0717
  IL_019a:  ldloc.1
  IL_019b:  ldc.i4     0x86f04fe3
  IL_01a0:  beq        IL_0645
  IL_01a5:  br         IL_0a40
  IL_01aa:  ldloc.1
  IL_01ab:  ldc.i4     0x87e38583
  IL_01b0:  beq        IL_0702
  IL_01b5:  ldloc.1
  IL_01b6:  ldc.i4     0x87f05176
  IL_01bb:  beq        IL_0606
  IL_01c0:  br         IL_0a40
  IL_01c5:  ldloc.1
  IL_01c6:  ldc.i4     0x8ce38d62
  IL_01cb:  bgt.un     IL_02b0
  IL_01d0:  ldloc.1
  IL_01d1:  ldc.i4     0x8ae38a3c
  IL_01d6:  bgt.un.s   IL_0244
  IL_01d8:  ldloc.1
  IL_01d9:  ldc.i4     0x88f291a0
  IL_01de:  bgt.un.s   IL_0206
  IL_01e0:  ldloc.1
  IL_01e1:  ldc.i4     0x88e38716
  IL_01e6:  beq        IL_06ed
  IL_01eb:  ldloc.1
  IL_01ec:  ldc.i4     0x88f05309
  IL_01f1:  beq        IL_061b
  IL_01f6:  ldloc.1
  IL_01f7:  ldc.i4     0x88f291a0
  IL_01fc:  beq        IL_059d
  IL_0201:  br         IL_0a40
  IL_0206:  ldloc.1
  IL_0207:  ldc.i4     0x89f0549c
  IL_020c:  bgt.un.s   IL_0229
  IL_020e:  ldloc.1
  IL_020f:  ldc.i4     0x89e388a9
  IL_0214:  beq        IL_06d8
  IL_0219:  ldloc.1
  IL_021a:  ldc.i4     0x89f0549c
  IL_021f:  beq        IL_0684
  IL_0224:  br         IL_0a40
  IL_0229:  ldloc.1
  IL_022a:  ldc.i4     0x89f29333
  IL_022f:  beq        IL_0588
  IL_0234:  ldloc.1
  IL_0235:  ldc.i4     0x8ae38a3c
  IL_023a:  beq        IL_076b
  IL_023f:  br         IL_0a40
  IL_0244:  ldloc.1
  IL_0245:  ldc.i4     0x8be38bcf
  IL_024a:  bgt.un.s   IL_0272
  IL_024c:  ldloc.1
  IL_024d:  ldc.i4     0x8af0562f
  IL_0252:  beq        IL_0699
  IL_0257:  ldloc.1
  IL_0258:  ldc.i4     0x8af294c6
  IL_025d:  beq        IL_05c7
  IL_0262:  ldloc.1
  IL_0263:  ldc.i4     0x8be38bcf
  IL_0268:  beq        IL_0756
  IL_026d:  br         IL_0a40
  IL_0272:  ldloc.1
  IL_0273:  ldc.i4     0x8bf29659
  IL_0278:  bgt.un.s   IL_0295
  IL_027a:  ldloc.1
  IL_027b:  ldc.i4     0x8bf057c2
  IL_0280:  beq        IL_065a
  IL_0285:  ldloc.1
  IL_0286:  ldc.i4     0x8bf29659
  IL_028b:  beq        IL_05b2
  IL_0290:  br         IL_0a40
  IL_0295:  ldloc.1
  IL_0296:  ldc.i4     0x8ce14ecb
  IL_029b:  beq        IL_07aa
  IL_02a0:  ldloc.1
  IL_02a1:  ldc.i4     0x8ce38d62
  IL_02a6:  beq        IL_0741
  IL_02ab:  br         IL_0a40
  IL_02b0:  ldloc.1
  IL_02b1:  ldc.i4     0x8ff29ca5
  IL_02b6:  bgt.un.s   IL_0324
  IL_02b8:  ldloc.1
  IL_02b9:  ldc.i4     0x8de38ef5
  IL_02be:  bgt.un.s   IL_02e6
  IL_02c0:  ldloc.1
  IL_02c1:  ldc.i4     0x8cf05955
  IL_02c6:  beq        IL_066f
  IL_02cb:  ldloc.1
  IL_02cc:  ldc.i4     0x8cf297ec
  IL_02d1:  beq        IL_0549
  IL_02d6:  ldloc.1
  IL_02d7:  ldc.i4     0x8de38ef5
  IL_02dc:  beq        IL_072c
  IL_02e1:  br         IL_0a40
  IL_02e6:  ldloc.1
  IL_02e7:  ldc.i4     0x8ef29b12
  IL_02ec:  bgt.un.s   IL_0309
  IL_02ee:  ldloc.1
  IL_02ef:  ldc.i4     0x8df2997f
  IL_02f4:  beq        IL_0534
  IL_02f9:  ldloc.1
  IL_02fa:  ldc.i4     0x8ef29b12
  IL_02ff:  beq        IL_0573
  IL_0304:  br         IL_0a40
  IL_0309:  ldloc.1
  IL_030a:  ldc.i4     0x8ff05e0e
  IL_030f:  beq        IL_06ae
  IL_0314:  ldloc.1
  IL_0315:  ldc.i4     0x8ff29ca5
  IL_031a:  beq        IL_055e
  IL_031f:  br         IL_0a40
  IL_0324:  ldloc.1
  IL_0325:  ldc.i4     0x91e39541
  IL_032a:  bgt.un.s   IL_0352
  IL_032c:  ldloc.1
  IL_032d:  ldc.i4     0x90e393ae
  IL_0332:  beq        IL_0795
  IL_0337:  ldloc.1
  IL_0338:  ldc.i4     0x90f05fa1
  IL_033d:  beq        IL_06c3
  IL_0342:  ldloc.1
  IL_0343:  ldc.i4     0x91e39541
  IL_0348:  beq        IL_0780
  IL_034d:  br         IL_0a40
  IL_0352:  ldloc.1
  IL_0353:  ldc.i4     0x95f2a617
  IL_0358:  bgt.un.s   IL_0375
  IL_035a:  ldloc.1
  IL_035b:  ldc.i4     0x94f2a484
  IL_0360:  beq        IL_05f1
  IL_0365:  ldloc.1
  IL_0366:  ldc.i4     0x95f2a617
  IL_036b:  beq        IL_05dc
  IL_0370:  br         IL_0a40
  IL_0375:  ldloc.1
  IL_0376:  ldc.i4     0x98e5dedd
  IL_037b:  beq        IL_07d4
  IL_0380:  ldloc.1
  IL_0381:  ldc.i4     0x9901b55a
  IL_0386:  beq        IL_07e9
  IL_038b:  br         IL_0a40
  IL_0390:  ldarg.0
  IL_0391:  ldstr      "00"
  IL_0396:  call       "bool string.op_Equality(string, string)"
  IL_039b:  brtrue     IL_0813
  IL_03a0:  br         IL_0a40
  IL_03a5:  ldarg.0
  IL_03a6:  ldstr      "01"
  IL_03ab:  call       "bool string.op_Equality(string, string)"
  IL_03b0:  brtrue     IL_081e
  IL_03b5:  br         IL_0a40
  IL_03ba:  ldarg.0
  IL_03bb:  ldstr      "02"
  IL_03c0:  call       "bool string.op_Equality(string, string)"
  IL_03c5:  brtrue     IL_0829
  IL_03ca:  br         IL_0a40
  IL_03cf:  ldarg.0
  IL_03d0:  ldstr      "03"
  IL_03d5:  call       "bool string.op_Equality(string, string)"
  IL_03da:  brtrue     IL_0834
  IL_03df:  br         IL_0a40
  IL_03e4:  ldarg.0
  IL_03e5:  ldstr      "04"
  IL_03ea:  call       "bool string.op_Equality(string, string)"
  IL_03ef:  brtrue     IL_083f
  IL_03f4:  br         IL_0a40
  IL_03f9:  ldarg.0
  IL_03fa:  ldstr      "05"
  IL_03ff:  call       "bool string.op_Equality(string, string)"
  IL_0404:  brtrue     IL_084a
  IL_0409:  br         IL_0a40
  IL_040e:  ldarg.0
  IL_040f:  ldstr      "06"
  IL_0414:  call       "bool string.op_Equality(string, string)"
  IL_0419:  brtrue     IL_0855
  IL_041e:  br         IL_0a40
  IL_0423:  ldarg.0
  IL_0424:  ldstr      "07"
  IL_0429:  call       "bool string.op_Equality(string, string)"
  IL_042e:  brtrue     IL_0860
  IL_0433:  br         IL_0a40
  IL_0438:  ldarg.0
  IL_0439:  ldstr      "08"
  IL_043e:  call       "bool string.op_Equality(string, string)"
  IL_0443:  brtrue     IL_086b
  IL_0448:  br         IL_0a40
  IL_044d:  ldarg.0
  IL_044e:  ldstr      "09"
  IL_0453:  call       "bool string.op_Equality(string, string)"
  IL_0458:  brtrue     IL_0876
  IL_045d:  br         IL_0a40
  IL_0462:  ldarg.0
  IL_0463:  ldstr      "10"
  IL_0468:  call       "bool string.op_Equality(string, string)"
  IL_046d:  brtrue     IL_0881
  IL_0472:  br         IL_0a40
  IL_0477:  ldarg.0
  IL_0478:  ldstr      "11"
  IL_047d:  call       "bool string.op_Equality(string, string)"
  IL_0482:  brtrue     IL_088c
  IL_0487:  br         IL_0a40
  IL_048c:  ldarg.0
  IL_048d:  ldstr      "12"
  IL_0492:  call       "bool string.op_Equality(string, string)"
  IL_0497:  brtrue     IL_0897
  IL_049c:  br         IL_0a40
  IL_04a1:  ldarg.0
  IL_04a2:  ldstr      "13"
  IL_04a7:  call       "bool string.op_Equality(string, string)"
  IL_04ac:  brtrue     IL_08a2
  IL_04b1:  br         IL_0a40
  IL_04b6:  ldarg.0
  IL_04b7:  ldstr      "14"
  IL_04bc:  call       "bool string.op_Equality(string, string)"
  IL_04c1:  brtrue     IL_08ad
  IL_04c6:  br         IL_0a40
  IL_04cb:  ldarg.0
  IL_04cc:  ldstr      "15"
  IL_04d1:  call       "bool string.op_Equality(string, string)"
  IL_04d6:  brtrue     IL_08b8
  IL_04db:  br         IL_0a40
  IL_04e0:  ldarg.0
  IL_04e1:  ldstr      "16"
  IL_04e6:  call       "bool string.op_Equality(string, string)"
  IL_04eb:  brtrue     IL_08c3
  IL_04f0:  br         IL_0a40
  IL_04f5:  ldarg.0
  IL_04f6:  ldstr      "17"
  IL_04fb:  call       "bool string.op_Equality(string, string)"
  IL_0500:  brtrue     IL_08ce
  IL_0505:  br         IL_0a40
  IL_050a:  ldarg.0
  IL_050b:  ldstr      "18"
  IL_0510:  call       "bool string.op_Equality(string, string)"
  IL_0515:  brtrue     IL_08d9
  IL_051a:  br         IL_0a40
  IL_051f:  ldarg.0
  IL_0520:  ldstr      "19"
  IL_0525:  call       "bool string.op_Equality(string, string)"
  IL_052a:  brtrue     IL_08e4
  IL_052f:  br         IL_0a40
  IL_0534:  ldarg.0
  IL_0535:  ldstr      "20"
  IL_053a:  call       "bool string.op_Equality(string, string)"
  IL_053f:  brtrue     IL_08ef
  IL_0544:  br         IL_0a40
  IL_0549:  ldarg.0
  IL_054a:  ldstr      "21"
  IL_054f:  call       "bool string.op_Equality(string, string)"
  IL_0554:  brtrue     IL_08fa
  IL_0559:  br         IL_0a40
  IL_055e:  ldarg.0
  IL_055f:  ldstr      "22"
  IL_0564:  call       "bool string.op_Equality(string, string)"
  IL_0569:  brtrue     IL_0905
  IL_056e:  br         IL_0a40
  IL_0573:  ldarg.0
  IL_0574:  ldstr      "23"
  IL_0579:  call       "bool string.op_Equality(string, string)"
  IL_057e:  brtrue     IL_0910
  IL_0583:  br         IL_0a40
  IL_0588:  ldarg.0
  IL_0589:  ldstr      "24"
  IL_058e:  call       "bool string.op_Equality(string, string)"
  IL_0593:  brtrue     IL_091b
  IL_0598:  br         IL_0a40
  IL_059d:  ldarg.0
  IL_059e:  ldstr      "25"
  IL_05a3:  call       "bool string.op_Equality(string, string)"
  IL_05a8:  brtrue     IL_0926
  IL_05ad:  br         IL_0a40
  IL_05b2:  ldarg.0
  IL_05b3:  ldstr      "26"
  IL_05b8:  call       "bool string.op_Equality(string, string)"
  IL_05bd:  brtrue     IL_0931
  IL_05c2:  br         IL_0a40
  IL_05c7:  ldarg.0
  IL_05c8:  ldstr      "27"
  IL_05cd:  call       "bool string.op_Equality(string, string)"
  IL_05d2:  brtrue     IL_093c
  IL_05d7:  br         IL_0a40
  IL_05dc:  ldarg.0
  IL_05dd:  ldstr      "28"
  IL_05e2:  call       "bool string.op_Equality(string, string)"
  IL_05e7:  brtrue     IL_0947
  IL_05ec:  br         IL_0a40
  IL_05f1:  ldarg.0
  IL_05f2:  ldstr      "29"
  IL_05f7:  call       "bool string.op_Equality(string, string)"
  IL_05fc:  brtrue     IL_0952
  IL_0601:  br         IL_0a40
  IL_0606:  ldarg.0
  IL_0607:  ldstr      "30"
  IL_060c:  call       "bool string.op_Equality(string, string)"
  IL_0611:  brtrue     IL_095d
  IL_0616:  br         IL_0a40
  IL_061b:  ldarg.0
  IL_061c:  ldstr      "31"
  IL_0621:  call       "bool string.op_Equality(string, string)"
  IL_0626:  brtrue     IL_0968
  IL_062b:  br         IL_0a40
  IL_0630:  ldarg.0
  IL_0631:  ldstr      "32"
  IL_0636:  call       "bool string.op_Equality(string, string)"
  IL_063b:  brtrue     IL_0973
  IL_0640:  br         IL_0a40
  IL_0645:  ldarg.0
  IL_0646:  ldstr      "33"
  IL_064b:  call       "bool string.op_Equality(string, string)"
  IL_0650:  brtrue     IL_097e
  IL_0655:  br         IL_0a40
  IL_065a:  ldarg.0
  IL_065b:  ldstr      "34"
  IL_0660:  call       "bool string.op_Equality(string, string)"
  IL_0665:  brtrue     IL_0989
  IL_066a:  br         IL_0a40
  IL_066f:  ldarg.0
  IL_0670:  ldstr      "35"
  IL_0675:  call       "bool string.op_Equality(string, string)"
  IL_067a:  brtrue     IL_0994
  IL_067f:  br         IL_0a40
  IL_0684:  ldarg.0
  IL_0685:  ldstr      "36"
  IL_068a:  call       "bool string.op_Equality(string, string)"
  IL_068f:  brtrue     IL_099f
  IL_0694:  br         IL_0a40
  IL_0699:  ldarg.0
  IL_069a:  ldstr      "37"
  IL_069f:  call       "bool string.op_Equality(string, string)"
  IL_06a4:  brtrue     IL_09aa
  IL_06a9:  br         IL_0a40
  IL_06ae:  ldarg.0
  IL_06af:  ldstr      "38"
  IL_06b4:  call       "bool string.op_Equality(string, string)"
  IL_06b9:  brtrue     IL_09b5
  IL_06be:  br         IL_0a40
  IL_06c3:  ldarg.0
  IL_06c4:  ldstr      "39"
  IL_06c9:  call       "bool string.op_Equality(string, string)"
  IL_06ce:  brtrue     IL_09c0
  IL_06d3:  br         IL_0a40
  IL_06d8:  ldarg.0
  IL_06d9:  ldstr      "40"
  IL_06de:  call       "bool string.op_Equality(string, string)"
  IL_06e3:  brtrue     IL_09c8
  IL_06e8:  br         IL_0a40
  IL_06ed:  ldarg.0
  IL_06ee:  ldstr      "41"
  IL_06f3:  call       "bool string.op_Equality(string, string)"
  IL_06f8:  brtrue     IL_09d0
  IL_06fd:  br         IL_0a40
  IL_0702:  ldarg.0
  IL_0703:  ldstr      "42"
  IL_0708:  call       "bool string.op_Equality(string, string)"
  IL_070d:  brtrue     IL_09d8
  IL_0712:  br         IL_0a40
  IL_0717:  ldarg.0
  IL_0718:  ldstr      "43"
  IL_071d:  call       "bool string.op_Equality(string, string)"
  IL_0722:  brtrue     IL_09e0
  IL_0727:  br         IL_0a40
  IL_072c:  ldarg.0
  IL_072d:  ldstr      "44"
  IL_0732:  call       "bool string.op_Equality(string, string)"
  IL_0737:  brtrue     IL_09e8
  IL_073c:  br         IL_0a40
  IL_0741:  ldarg.0
  IL_0742:  ldstr      "45"
  IL_0747:  call       "bool string.op_Equality(string, string)"
  IL_074c:  brtrue     IL_09f0
  IL_0751:  br         IL_0a40
  IL_0756:  ldarg.0
  IL_0757:  ldstr      "46"
  IL_075c:  call       "bool string.op_Equality(string, string)"
  IL_0761:  brtrue     IL_09f8
  IL_0766:  br         IL_0a40
  IL_076b:  ldarg.0
  IL_076c:  ldstr      "47"
  IL_0771:  call       "bool string.op_Equality(string, string)"
  IL_0776:  brtrue     IL_0a00
  IL_077b:  br         IL_0a40
  IL_0780:  ldarg.0
  IL_0781:  ldstr      "48"
  IL_0786:  call       "bool string.op_Equality(string, string)"
  IL_078b:  brtrue     IL_0a08
  IL_0790:  br         IL_0a40
  IL_0795:  ldarg.0
  IL_0796:  ldstr      "49"
  IL_079b:  call       "bool string.op_Equality(string, string)"
  IL_07a0:  brtrue     IL_0a10
  IL_07a5:  br         IL_0a40
  IL_07aa:  ldarg.0
  IL_07ab:  ldstr      "59"
  IL_07b0:  call       "bool string.op_Equality(string, string)"
  IL_07b5:  brtrue     IL_0a18
  IL_07ba:  br         IL_0a40
  IL_07bf:  ldarg.0
  IL_07c0:  ldstr      "69"
  IL_07c5:  call       "bool string.op_Equality(string, string)"
  IL_07ca:  brtrue     IL_0a20
  IL_07cf:  br         IL_0a40
  IL_07d4:  ldarg.0
  IL_07d5:  ldstr      "79"
  IL_07da:  call       "bool string.op_Equality(string, string)"
  IL_07df:  brtrue     IL_0a28
  IL_07e4:  br         IL_0a40
  IL_07e9:  ldarg.0
  IL_07ea:  ldstr      "89"
  IL_07ef:  call       "bool string.op_Equality(string, string)"
  IL_07f4:  brtrue     IL_0a30
  IL_07f9:  br         IL_0a40
  IL_07fe:  ldarg.0
  IL_07ff:  ldstr      "99"
  IL_0804:  call       "bool string.op_Equality(string, string)"
  IL_0809:  brtrue     IL_0a38
  IL_080e:  br         IL_0a40
  IL_0813:  ldstr      "00"
  IL_0818:  stloc.0
  IL_0819:  br         IL_0a46
  IL_081e:  ldstr      "01"
  IL_0823:  stloc.0
  IL_0824:  br         IL_0a46
  IL_0829:  ldstr      "02"
  IL_082e:  stloc.0
  IL_082f:  br         IL_0a46
  IL_0834:  ldstr      "03"
  IL_0839:  stloc.0
  IL_083a:  br         IL_0a46
  IL_083f:  ldstr      "04"
  IL_0844:  stloc.0
  IL_0845:  br         IL_0a46
  IL_084a:  ldstr      "05"
  IL_084f:  stloc.0
  IL_0850:  br         IL_0a46
  IL_0855:  ldstr      "06"
  IL_085a:  stloc.0
  IL_085b:  br         IL_0a46
  IL_0860:  ldstr      "07"
  IL_0865:  stloc.0
  IL_0866:  br         IL_0a46
  IL_086b:  ldstr      "08"
  IL_0870:  stloc.0
  IL_0871:  br         IL_0a46
  IL_0876:  ldstr      "09"
  IL_087b:  stloc.0
  IL_087c:  br         IL_0a46
  IL_0881:  ldstr      "10"
  IL_0886:  stloc.0
  IL_0887:  br         IL_0a46
  IL_088c:  ldstr      "11"
  IL_0891:  stloc.0
  IL_0892:  br         IL_0a46
  IL_0897:  ldstr      "12"
  IL_089c:  stloc.0
  IL_089d:  br         IL_0a46
  IL_08a2:  ldstr      "13"
  IL_08a7:  stloc.0
  IL_08a8:  br         IL_0a46
  IL_08ad:  ldstr      "14"
  IL_08b2:  stloc.0
  IL_08b3:  br         IL_0a46
  IL_08b8:  ldstr      "15"
  IL_08bd:  stloc.0
  IL_08be:  br         IL_0a46
  IL_08c3:  ldstr      "16"
  IL_08c8:  stloc.0
  IL_08c9:  br         IL_0a46
  IL_08ce:  ldstr      "17"
  IL_08d3:  stloc.0
  IL_08d4:  br         IL_0a46
  IL_08d9:  ldstr      "18"
  IL_08de:  stloc.0
  IL_08df:  br         IL_0a46
  IL_08e4:  ldstr      "19"
  IL_08e9:  stloc.0
  IL_08ea:  br         IL_0a46
  IL_08ef:  ldstr      "20"
  IL_08f4:  stloc.0
  IL_08f5:  br         IL_0a46
  IL_08fa:  ldstr      "21"
  IL_08ff:  stloc.0
  IL_0900:  br         IL_0a46
  IL_0905:  ldstr      "22"
  IL_090a:  stloc.0
  IL_090b:  br         IL_0a46
  IL_0910:  ldstr      "23"
  IL_0915:  stloc.0
  IL_0916:  br         IL_0a46
  IL_091b:  ldstr      "24"
  IL_0920:  stloc.0
  IL_0921:  br         IL_0a46
  IL_0926:  ldstr      "25"
  IL_092b:  stloc.0
  IL_092c:  br         IL_0a46
  IL_0931:  ldstr      "26"
  IL_0936:  stloc.0
  IL_0937:  br         IL_0a46
  IL_093c:  ldstr      "27"
  IL_0941:  stloc.0
  IL_0942:  br         IL_0a46
  IL_0947:  ldstr      "28"
  IL_094c:  stloc.0
  IL_094d:  br         IL_0a46
  IL_0952:  ldstr      "29"
  IL_0957:  stloc.0
  IL_0958:  br         IL_0a46
  IL_095d:  ldstr      "30"
  IL_0962:  stloc.0
  IL_0963:  br         IL_0a46
  IL_0968:  ldstr      "31"
  IL_096d:  stloc.0
  IL_096e:  br         IL_0a46
  IL_0973:  ldstr      "32"
  IL_0978:  stloc.0
  IL_0979:  br         IL_0a46
  IL_097e:  ldstr      "33"
  IL_0983:  stloc.0
  IL_0984:  br         IL_0a46
  IL_0989:  ldstr      "34"
  IL_098e:  stloc.0
  IL_098f:  br         IL_0a46
  IL_0994:  ldstr      "35"
  IL_0999:  stloc.0
  IL_099a:  br         IL_0a46
  IL_099f:  ldstr      "36"
  IL_09a4:  stloc.0
  IL_09a5:  br         IL_0a46
  IL_09aa:  ldstr      "37"
  IL_09af:  stloc.0
  IL_09b0:  br         IL_0a46
  IL_09b5:  ldstr      "38"
  IL_09ba:  stloc.0
  IL_09bb:  br         IL_0a46
  IL_09c0:  ldstr      "39"
  IL_09c5:  stloc.0
  IL_09c6:  br.s       IL_0a46
  IL_09c8:  ldstr      "40"
  IL_09cd:  stloc.0
  IL_09ce:  br.s       IL_0a46
  IL_09d0:  ldstr      "41"
  IL_09d5:  stloc.0
  IL_09d6:  br.s       IL_0a46
  IL_09d8:  ldstr      "42"
  IL_09dd:  stloc.0
  IL_09de:  br.s       IL_0a46
  IL_09e0:  ldstr      "43"
  IL_09e5:  stloc.0
  IL_09e6:  br.s       IL_0a46
  IL_09e8:  ldstr      "44"
  IL_09ed:  stloc.0
  IL_09ee:  br.s       IL_0a46
  IL_09f0:  ldstr      "45"
  IL_09f5:  stloc.0
  IL_09f6:  br.s       IL_0a46
  IL_09f8:  ldstr      "46"
  IL_09fd:  stloc.0
  IL_09fe:  br.s       IL_0a46
  IL_0a00:  ldstr      "47"
  IL_0a05:  stloc.0
  IL_0a06:  br.s       IL_0a46
  IL_0a08:  ldstr      "48"
  IL_0a0d:  stloc.0
  IL_0a0e:  br.s       IL_0a46
  IL_0a10:  ldstr      "49"
  IL_0a15:  stloc.0
  IL_0a16:  br.s       IL_0a46
  IL_0a18:  ldstr      "59"
  IL_0a1d:  stloc.0
  IL_0a1e:  br.s       IL_0a46
  IL_0a20:  ldstr      "69"
  IL_0a25:  stloc.0
  IL_0a26:  br.s       IL_0a46
  IL_0a28:  ldstr      "79"
  IL_0a2d:  stloc.0
  IL_0a2e:  br.s       IL_0a46
  IL_0a30:  ldstr      "89"
  IL_0a35:  stloc.0
  IL_0a36:  br.s       IL_0a46
  IL_0a38:  ldstr      "99"
  IL_0a3d:  stloc.0
  IL_0a3e:  br.s       IL_0a46
  IL_0a40:  ldstr      "default"
  IL_0a45:  stloc.0
  IL_0a46:  ldloc.0
  IL_0a47:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void GetDriveType()
    {
        // Based on https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/Interop/Unix/System.Native/Interop.MountPoints.FormatInfo.cs#L78-L327A
        // Buckets: 5, 3, 1, 3, 2, 2, 1, 1, 2, 1, 2, 1, 1, 3, 1, 1, 1, 1, 1, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 2, 3, 2, 4, 1, 2, 3, 1, 2, 4, 3, 2, 1, 2, 1, 1, 1, 1, 1, 2, 3, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 10, 3, 2, 5, 1, 1, 3, 1, 1, 2, 2, 3, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 2, 3, 2, 1, 1, 2, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("cddafs", "cddafs");
assert("cd9660", "cddafs");
assert("iso", "cddafs");
assert("isofs", "cddafs");
assert("iso9660", "cddafs");
assert("fuseiso", "cddafs");
assert("fuseiso9660", "cddafs");
assert("udf", "cddafs");
assert("umview-mod-umfuseiso9660", "cddafs");

assert("aafs", "aafs");
assert("adfs", "aafs");
assert("affs", "aafs");
assert("anoninode", "aafs");
assert("anon-inode FS", "aafs");
assert("apfs", "aafs");
assert("balloon-kvm-fs", "aafs");
assert("bdevfs", "aafs");
assert("befs", "aafs");
assert("bfs", "aafs");
assert("bootfs", "aafs");
assert("bpf_fs", "aafs");
assert("btrfs", "aafs");
assert("btrfs_test", "aafs");
assert("coh", "aafs");
assert("daxfs", "aafs");
assert("drvfs", "aafs");
assert("efivarfs", "aafs");
assert("efs", "aafs");
assert("exfat", "aafs");
assert("exofs", "aafs");
assert("ext", "aafs");
assert("ext2", "aafs");
assert("ext2_old", "aafs");
assert("ext3", "aafs");
assert("ext2/ext3", "aafs");
assert("ext4", "aafs");
assert("ext4dev", "aafs");
assert("f2fs", "aafs");
assert("fat", "aafs");
assert("fuseext2", "aafs");
assert("fusefat", "aafs");
assert("hfs", "aafs");
assert("hfs+", "aafs");
assert("hfsplus", "aafs");
assert("hfsx", "aafs");
assert("hostfs", "aafs");
assert("hpfs", "aafs");
assert("inodefs", "aafs");
assert("inotifyfs", "aafs");
assert("jbd", "aafs");
assert("jbd2", "aafs");
assert("jffs", "aafs");
assert("jffs2", "aafs");
assert("jfs", "aafs");
assert("lofs", "aafs");
assert("logfs", "aafs");
assert("lxfs", "aafs");
assert("minix (30 char.)", "aafs");
assert("minix v2 (30 char.)", "aafs");
assert("minix v2", "aafs");
assert("minix", "aafs");
assert("minix_old", "aafs");
assert("minix2", "aafs");
assert("minix2v2", "aafs");
assert("minix2 v2", "aafs");
assert("minix3", "aafs");
assert("mlfs", "aafs");
assert("msdos", "aafs");
assert("nilfs", "aafs");
assert("nsfs", "aafs");
assert("ntfs", "aafs");
assert("ntfs-3g", "aafs");
assert("ocfs2", "aafs");
assert("omfs", "aafs");
assert("overlay", "aafs");
assert("overlayfs", "aafs");
assert("pstorefs", "aafs");
assert("qnx4", "aafs");
assert("qnx6", "aafs");
assert("reiserfs", "aafs");
assert("rpc_pipefs", "aafs");
assert("sffs", "aafs");
assert("smackfs", "aafs");
assert("squashfs", "aafs");
assert("swap", "aafs");
assert("sysv", "aafs");
assert("sysv2", "aafs");
assert("sysv4", "aafs");
assert("tracefs", "aafs");
assert("ubifs", "aafs");
assert("ufs", "aafs");
assert("ufscigam", "aafs");
assert("ufs2", "aafs");
assert("umsdos", "aafs");
assert("umview-mod-umfuseext2", "aafs");
assert("v9fs", "aafs");
assert("vagrant", "aafs");
assert("vboxfs", "aafs");
assert("vxfs", "aafs");
assert("vxfs_olt", "aafs");
assert("vzfs", "aafs");
assert("wslfs", "aafs");
assert("xenix", "aafs");
assert("xfs", "aafs");
assert("xia", "aafs");
assert("xiafs", "aafs");
assert("xmount", "aafs");
assert("zfs", "aafs");
assert("zfs-fuse", "aafs");
assert("zsmallocfs", "aafs");

assert("9p", "9p");
assert("acfs", "9p");
assert("afp", "9p");
assert("afpfs", "9p");
assert("afs", "9p");
assert("aufs", "9p");
assert("autofs", "9p");
assert("autofs4", "9p");
assert("beaglefs", "9p");
assert("ceph", "9p");
assert("cifs", "9p");
assert("coda", "9p");
assert("coherent", "9p");
assert("curlftpfs", "9p");
assert("davfs2", "9p");
assert("dlm", "9p");
assert("ecryptfs", "9p");
assert("eCryptfs", "9p");
assert("fhgfs", "9p");
assert("flickrfs", "9p");
assert("ftp", "9p");
assert("fuse", "9p");
assert("fuseblk", "9p");
assert("fusedav", "9p");
assert("fusesmb", "9p");
assert("gfsgfs2", "9p");
assert("gfs/gfs2", "9p");
assert("gfs2", "9p");
assert("glusterfs-client", "9p");
assert("gmailfs", "9p");
assert("gpfs", "9p");
assert("ibrix", "9p");
assert("k-afs", "9p");
assert("kafs", "9p");
assert("kbfuse", "9p");
assert("ltspfs", "9p");
assert("lustre", "9p");
assert("ncp", "9p");
assert("ncpfs", "9p");
assert("nfs", "9p");
assert("nfs4", "9p");
assert("nfsd", "9p");
assert("novell", "9p");
assert("obexfs", "9p");
assert("panfs", "9p");
assert("prl_fs", "9p");
assert("s3ql", "9p");
assert("samba", "9p");
assert("smb", "9p");
assert("smb2", "9p");
assert("smbfs", "9p");
assert("snfs", "9p");
assert("sshfs", "9p");
assert("vmhgfs", "9p");
assert("webdav", "9p");
assert("wikipediafs", "9p");
assert("xenfs", "9p");

assert("anon_inode", "anon_inode");
assert("anon_inodefs", "anon_inode");
assert("aptfs", "anon_inode");
assert("avfs", "anon_inode");
assert("bdev", "anon_inode");
assert("binfmt_misc", "anon_inode");
assert("cgroup", "anon_inode");
assert("cgroupfs", "anon_inode");
assert("cgroup2fs", "anon_inode");
assert("configfs", "anon_inode");
assert("cpuset", "anon_inode");
assert("cramfs", "anon_inode");
assert("cramfs-wend", "anon_inode");
assert("cryptkeeper", "anon_inode");
assert("ctfs", "anon_inode");
assert("debugfs", "anon_inode");
assert("dev", "anon_inode");
assert("devfs", "anon_inode");
assert("devpts", "anon_inode");
assert("devtmpfs", "anon_inode");
assert("encfs", "anon_inode");
assert("fd", "anon_inode");
assert("fdesc", "anon_inode");
assert("fuse.gvfsd-fuse", "anon_inode");
assert("fusectl", "anon_inode");
assert("futexfs", "anon_inode");
assert("hugetlbfs", "anon_inode");
assert("libpam-encfs", "anon_inode");
assert("ibpam-mount", "anon_inode");
assert("mntfs", "anon_inode");
assert("mqueue", "anon_inode");
assert("mtpfs", "anon_inode");
assert("mythtvfs", "anon_inode");
assert("objfs", "anon_inode");
assert("openprom", "anon_inode");
assert("openpromfs", "anon_inode");
assert("pipefs", "anon_inode");
assert("plptools", "anon_inode");
assert("proc", "anon_inode");
assert("pstore", "anon_inode");
assert("pytagsfs", "anon_inode");
assert("ramfs", "anon_inode");
assert("rofs", "anon_inode");
assert("romfs", "anon_inode");
assert("rootfs", "anon_inode");
assert("securityfs", "anon_inode");
assert("selinux", "anon_inode");
assert("selinuxfs", "anon_inode");
assert("sharefs", "anon_inode");
assert("sockfs", "anon_inode");
assert("sysfs", "anon_inode");
assert("tmpfs", "anon_inode");
assert("udev", "anon_inode");
assert("usbdev", "anon_inode");
assert("usbdevfs", "anon_inode");

assert("gphotofs", "gphotofs");
assert("sdcardfs", "gphotofs");
assert("usbfs", "gphotofs");
assert("usbdevice", "gphotofs");
assert("vfat", "gphotofs");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    expected ??= input;
    if (C.M(input) != expected)
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string fileSystemName)
    {
        switch (fileSystemName)
        {
            case "cddafs":
            case "cd9660":
            case "iso":
            case "isofs":
            case "iso9660":
            case "fuseiso":
            case "fuseiso9660":
            case "udf":
            case "umview-mod-umfuseiso9660":
                return  "cddafs";

            case "aafs":
            case "adfs":
            case "affs":
            case "anoninode":
            case "anon-inode FS":
            case "apfs":
            case "balloon-kvm-fs":
            case "bdevfs":
            case "befs":
            case "bfs":
            case "bootfs":
            case "bpf_fs":
            case "btrfs":
            case "btrfs_test":
            case "coh":
            case "daxfs":
            case "drvfs":
            case "efivarfs":
            case "efs":
            case "exfat":
            case "exofs":
            case "ext":
            case "ext2":
            case "ext2_old":
            case "ext3":
            case "ext2/ext3":
            case "ext4":
            case "ext4dev":
            case "f2fs":
            case "fat":
            case "fuseext2":
            case "fusefat":
            case "hfs":
            case "hfs+":
            case "hfsplus":
            case "hfsx":
            case "hostfs":
            case "hpfs":
            case "inodefs":
            case "inotifyfs":
            case "jbd":
            case "jbd2":
            case "jffs":
            case "jffs2":
            case "jfs":
            case "lofs":
            case "logfs":
            case "lxfs":
            case "minix (30 char.)":
            case "minix v2 (30 char.)":
            case "minix v2":
            case "minix":
            case "minix_old":
            case "minix2":
            case "minix2v2":
            case "minix2 v2":
            case "minix3":
            case "mlfs":
            case "msdos":
            case "nilfs":
            case "nsfs":
            case "ntfs":
            case "ntfs-3g":
            case "ocfs2":
            case "omfs":
            case "overlay":
            case "overlayfs":
            case "pstorefs":
            case "qnx4":
            case "qnx6":
            case "reiserfs":
            case "rpc_pipefs":
            case "sffs":
            case "smackfs":
            case "squashfs":
            case "swap":
            case "sysv":
            case "sysv2":
            case "sysv4":
            case "tracefs":
            case "ubifs":
            case "ufs":
            case "ufscigam":
            case "ufs2":
            case "umsdos":
            case "umview-mod-umfuseext2":
            case "v9fs":
            case "vagrant":
            case "vboxfs":
            case "vxfs":
            case "vxfs_olt":
            case "vzfs":
            case "wslfs":
            case "xenix":
            case "xfs":
            case "xia":
            case "xiafs":
            case "xmount":
            case "zfs":
            case "zfs-fuse":
            case "zsmallocfs":
                return "aafs";

            case "9p":
            case "acfs":
            case "afp":
            case "afpfs":
            case "afs":
            case "aufs":
            case "autofs":
            case "autofs4":
            case "beaglefs":
            case "ceph":
            case "cifs":
            case "coda":
            case "coherent":
            case "curlftpfs":
            case "davfs2":
            case "dlm":
            case "ecryptfs":
            case "eCryptfs":
            case "fhgfs":
            case "flickrfs":
            case "ftp":
            case "fuse":
            case "fuseblk":
            case "fusedav":
            case "fusesmb":
            case "gfsgfs2":
            case "gfs/gfs2":
            case "gfs2":
            case "glusterfs-client":
            case "gmailfs":
            case "gpfs":
            case "ibrix":
            case "k-afs":
            case "kafs":
            case "kbfuse":
            case "ltspfs":
            case "lustre":
            case "ncp":
            case "ncpfs":
            case "nfs":
            case "nfs4":
            case "nfsd":
            case "novell":
            case "obexfs":
            case "panfs":
            case "prl_fs":
            case "s3ql":
            case "samba":
            case "smb":
            case "smb2":
            case "smbfs":
            case "snfs":
            case "sshfs":
            case "vmhgfs":
            case "webdav":
            case "wikipediafs":
            case "xenfs":
                return "9p";

            case "anon_inode":
            case "anon_inodefs":
            case "aptfs":
            case "avfs":
            case "bdev":
            case "binfmt_misc":
            case "cgroup":
            case "cgroupfs":
            case "cgroup2fs":
            case "configfs":
            case "cpuset":
            case "cramfs":
            case "cramfs-wend":
            case "cryptkeeper":
            case "ctfs":
            case "debugfs":
            case "dev":
            case "devfs":
            case "devpts":
            case "devtmpfs":
            case "encfs":
            case "fd":
            case "fdesc":
            case "fuse.gvfsd-fuse":
            case "fusectl":
            case "futexfs":
            case "hugetlbfs":
            case "libpam-encfs":
            case "ibpam-mount":
            case "mntfs":
            case "mqueue":
            case "mtpfs":
            case "mythtvfs":
            case "objfs":
            case "openprom":
            case "openpromfs":
            case "pipefs":
            case "plptools":
            case "proc":
            case "pstore":
            case "pytagsfs":
            case "ramfs":
            case "rofs":
            case "romfs":
            case "rootfs":
            case "securityfs":
            case "selinux":
            case "selinuxfs":
            case "sharefs":
            case "sockfs":
            case "sysfs":
            case "tmpfs":
            case "udev":
            case "usbdev":
            case "usbdevfs":
                return "anon_inode";

            case "gphotofs":
            case "sdcardfs":
            case "usbfs":
            case "usbdevice":
            case "vfat":
                return "gphotofs";

            default:
                return "default";
        }
    }
}
""";

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     8586 (0x218a)
  .maxstack  2
  .locals init (uint V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0x93254270
  IL_000d:  bgt.un     IL_0777
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x38c6cc37
  IL_0018:  bgt.un     IL_03be
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4     0x2065d503
  IL_0023:  bgt.un     IL_01f3
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4     0xe306532
  IL_002e:  bgt.un     IL_0113
  IL_0033:  ldloc.0
  IL_0034:  ldc.i4     0x9cd23fb
  IL_0039:  bgt.un.s   IL_00a7
  IL_003b:  ldloc.0
  IL_003c:  ldc.i4     0x4a3c0d1
  IL_0041:  bgt.un.s   IL_0069
  IL_0043:  ldloc.0
  IL_0044:  ldc.i4     0x1dcdad6
  IL_0049:  beq        IL_12e1
  IL_004e:  ldloc.0
  IL_004f:  ldc.i4     0x3a1df42
  IL_0054:  beq        IL_18c9
  IL_0059:  ldloc.0
  IL_005a:  ldc.i4     0x4a3c0d1
  IL_005f:  beq        IL_20d3
  IL_0064:  br         IL_2184
  IL_0069:  ldloc.0
  IL_006a:  ldc.i4     0x73ff364
  IL_006f:  bgt.un.s   IL_008c
  IL_0071:  ldloc.0
  IL_0072:  ldc.i4     0x4f8da09
  IL_0077:  beq        IL_1470
  IL_007c:  ldloc.0
  IL_007d:  ldc.i4     0x73ff364
  IL_0082:  beq        IL_1518
  IL_0087:  br         IL_2184
  IL_008c:  ldloc.0
  IL_008d:  ldc.i4     0x8305bc0
  IL_0092:  beq        IL_11d0
  IL_0097:  ldloc.0
  IL_0098:  ldc.i4     0x9cd23fb
  IL_009d:  beq        IL_11bb
  IL_00a2:  br         IL_2184
  IL_00a7:  ldloc.0
  IL_00a8:  ldc.i4     0xb177b39
  IL_00ad:  bgt.un.s   IL_00d5
  IL_00af:  ldloc.0
  IL_00b0:  ldc.i4     0xa57b005
  IL_00b5:  beq        IL_15ab
  IL_00ba:  ldloc.0
  IL_00bb:  ldc.i4     0xaa0d1c1
  IL_00c0:  beq        IL_1263
  IL_00c5:  ldloc.0
  IL_00c6:  ldc.i4     0xb177b39
  IL_00cb:  beq        IL_1e1e
  IL_00d0:  br         IL_2184
  IL_00d5:  ldloc.0
  IL_00d6:  ldc.i4     0xd347e19
  IL_00db:  bgt.un.s   IL_00f8
  IL_00dd:  ldloc.0
  IL_00de:  ldc.i4     0xb1a5699
  IL_00e3:  beq        IL_1875
  IL_00e8:  ldloc.0
  IL_00e9:  ldc.i4     0xd347e19
  IL_00ee:  beq        IL_20be
  IL_00f3:  br         IL_2184
  IL_00f8:  ldloc.0
  IL_00f9:  ldc.i4     0xdb92b7a
  IL_00fe:  beq        IL_202b
  IL_0103:  ldloc.0
  IL_0104:  ldc.i4     0xe306532
  IL_0109:  beq        IL_117c
  IL_010e:  br         IL_2184
  IL_0113:  ldloc.0
  IL_0114:  ldc.i4     0x14361444
  IL_0119:  bgt.un.s   IL_0187
  IL_011b:  ldloc.0
  IL_011c:  ldc.i4     0x11854a78
  IL_0121:  bgt.un.s   IL_0149
  IL_0123:  ldloc.0
  IL_0124:  ldc.i4     0xf3066c5
  IL_0129:  beq        IL_11a6
  IL_012e:  ldloc.0
  IL_012f:  ldc.i4     0x1036061b
  IL_0134:  beq        IL_212a
  IL_0139:  ldloc.0
  IL_013a:  ldc.i4     0x11854a78
  IL_013f:  beq        IL_1aeb
  IL_0144:  br         IL_2184
  IL_0149:  ldloc.0
  IL_014a:  ldc.i4     0x1283563d
  IL_014f:  bgt.un.s   IL_016c
  IL_0151:  ldloc.0
  IL_0152:  ldc.i4     0x11e3e89a
  IL_0157:  beq        IL_19ef
  IL_015c:  ldloc.0
  IL_015d:  ldc.i4     0x1283563d
  IL_0162:  beq        IL_1ac1
  IL_0167:  br         IL_2184
  IL_016c:  ldloc.0
  IL_016d:  ldc.i4     0x1312c917
  IL_0172:  beq        IL_1239
  IL_0177:  ldloc.0
  IL_0178:  ldc.i4     0x14361444
  IL_017d:  beq        IL_17cd
  IL_0182:  br         IL_2184
  IL_0187:  ldloc.0
  IL_0188:  ldc.i4     0x1c2d7204
  IL_018d:  bgt.un.s   IL_01b5
  IL_018f:  ldloc.0
  IL_0190:  ldc.i4     0x194bd3e1
  IL_0195:  beq        IL_1986
  IL_019a:  ldloc.0
  IL_019b:  ldc.i4     0x1b223d32
  IL_01a0:  beq        IL_1c50
  IL_01a5:  ldloc.0
  IL_01a6:  ldc.i4     0x1c2d7204
  IL_01ab:  beq        IL_16fb
  IL_01b0:  br         IL_2184
  IL_01b5:  ldloc.0
  IL_01b6:  ldc.i4     0x1d950792
  IL_01bb:  bgt.un.s   IL_01d8
  IL_01bd:  ldloc.0
  IL_01be:  ldc.i4     0x1ca03ef2
  IL_01c3:  beq        IL_1b2a
  IL_01c8:  ldloc.0
  IL_01c9:  ldc.i4     0x1d950792
  IL_01ce:  beq        IL_1b00
  IL_01d3:  br         IL_2184
  IL_01d8:  ldloc.0
  IL_01d9:  ldc.i4     0x2032cda8
  IL_01de:  beq        IL_20fd
  IL_01e3:  ldloc.0
  IL_01e4:  ldc.i4     0x2065d503
  IL_01e9:  beq        IL_1503
  IL_01ee:  br         IL_2184
  IL_01f3:  ldloc.0
  IL_01f4:  ldc.i4     0x2a1ea797
  IL_01f9:  bgt.un     IL_02de
  IL_01fe:  ldloc.0
  IL_01ff:  ldc.i4     0x2520eead
  IL_0204:  bgt.un.s   IL_0272
  IL_0206:  ldloc.0
  IL_0207:  ldc.i4     0x2161f7c8
  IL_020c:  bgt.un.s   IL_0234
  IL_020e:  ldloc.0
  IL_020f:  ldc.i4     0x20f48093
  IL_0214:  beq        IL_1b69
  IL_0219:  ldloc.0
  IL_021a:  ldc.i4     0x2117f6c8
  IL_021f:  beq        IL_1002
  IL_0224:  ldloc.0
  IL_0225:  ldc.i4     0x2161f7c8
  IL_022a:  beq        IL_156c
  IL_022f:  br         IL_2184
  IL_0234:  ldloc.0
  IL_0235:  ldc.i4     0x2465d7a1
  IL_023a:  bgt.un.s   IL_0257
  IL_023c:  ldloc.0
  IL_023d:  ldc.i4     0x22edfc61
  IL_0242:  beq        IL_0fd8
  IL_0247:  ldloc.0
  IL_0248:  ldc.i4     0x2465d7a1
  IL_024d:  beq        IL_1bd2
  IL_0252:  br         IL_2184
  IL_0257:  ldloc.0
  IL_0258:  ldc.i4     0x24b9c5bb
  IL_025d:  beq        IL_130b
  IL_0262:  ldloc.0
  IL_0263:  ldc.i4     0x2520eead
  IL_0268:  beq        IL_1f05
  IL_026d:  br         IL_2184
  IL_0272:  ldloc.0
  IL_0273:  ldc.i4     0x277c5a36
  IL_0278:  bgt.un.s   IL_02a0
  IL_027a:  ldloc.0
  IL_027b:  ldc.i4     0x2548e1fd
  IL_0280:  beq        IL_1dca
  IL_0285:  ldloc.0
  IL_0286:  ldc.i4     0x27767257
  IL_028b:  beq        IL_188a
  IL_0290:  ldloc.0
  IL_0291:  ldc.i4     0x277c5a36
  IL_0296:  beq        IL_14d9
  IL_029b:  br         IL_2184
  IL_02a0:  ldloc.0
  IL_02a1:  ldc.i4     0x2a0e6591
  IL_02a6:  bgt.un.s   IL_02c3
  IL_02a8:  ldloc.0
  IL_02a9:  ldc.i4     0x286b88ce
  IL_02ae:  beq        IL_1080
  IL_02b3:  ldloc.0
  IL_02b4:  ldc.i4     0x2a0e6591
  IL_02b9:  beq        IL_2016
  IL_02be:  br         IL_2184
  IL_02c3:  ldloc.0
  IL_02c4:  ldc.i4     0x2a1b6620
  IL_02c9:  beq        IL_2157
  IL_02ce:  ldloc.0
  IL_02cf:  ldc.i4     0x2a1ea797
  IL_02d4:  beq        IL_1971
  IL_02d9:  br         IL_2184
  IL_02de:  ldloc.0
  IL_02df:  ldc.i4     0x315a74f8
  IL_02e4:  bgt.un.s   IL_0352
  IL_02e6:  ldloc.0
  IL_02e7:  ldc.i4     0x2f4cca7c
  IL_02ec:  bgt.un.s   IL_0314
  IL_02ee:  ldloc.0
  IL_02ef:  ldc.i4     0x2a932d90
  IL_02f4:  beq        IL_0f6f
  IL_02f9:  ldloc.0
  IL_02fa:  ldc.i4     0x2c81f023
  IL_02ff:  beq        IL_19da
  IL_0304:  ldloc.0
  IL_0305:  ldc.i4     0x2f4cca7c
  IL_030a:  beq        IL_1908
  IL_030f:  br         IL_2184
  IL_0314:  ldloc.0
  IL_0315:  ldc.i4     0x2fb5fb43
  IL_031a:  bgt.un.s   IL_0337
  IL_031c:  ldloc.0
  IL_031d:  ldc.i4     0x2f5c2137
  IL_0322:  beq        IL_1596
  IL_0327:  ldloc.0
  IL_0328:  ldc.i4     0x2fb5fb43
  IL_032d:  beq        IL_167d
  IL_0332:  br         IL_2184
  IL_0337:  ldloc.0
  IL_0338:  ldc.i4     0x305d8a52
  IL_033d:  beq        IL_178e
  IL_0342:  ldloc.0
  IL_0343:  ldc.i4     0x315a74f8
  IL_0348:  beq        IL_1191
  IL_034d:  br         IL_2184
  IL_0352:  ldloc.0
  IL_0353:  ldc.i4     0x341798d6
  IL_0358:  bgt.un.s   IL_0380
  IL_035a:  ldloc.0
  IL_035b:  ldc.i4     0x323d8177
  IL_0360:  beq        IL_184b
  IL_0365:  ldloc.0
  IL_0366:  ldc.i4     0x333d830a
  IL_036b:  beq        IL_1821
  IL_0370:  ldloc.0
  IL_0371:  ldc.i4     0x341798d6
  IL_0376:  beq        IL_180c
  IL_037b:  br         IL_2184
  IL_0380:  ldloc.0
  IL_0381:  ldc.i4     0x35b53372
  IL_0386:  bgt.un.s   IL_03a3
  IL_0388:  ldloc.0
  IL_0389:  ldc.i4     0x34bfd292
  IL_038e:  beq        IL_1aac
  IL_0393:  ldloc.0
  IL_0394:  ldc.i4     0x35b53372
  IL_0399:  beq        IL_1710
  IL_039e:  br         IL_2184
  IL_03a3:  ldloc.0
  IL_03a4:  ldc.i4     0x35c0eda3
  IL_03a9:  beq        IL_1653
  IL_03ae:  ldloc.0
  IL_03af:  ldc.i4     0x38c6cc37
  IL_03b4:  beq        IL_1485
  IL_03b9:  br         IL_2184
  IL_03be:  ldloc.0
  IL_03bf:  ldc.i4     0x64ed874e
  IL_03c4:  bgt.un     IL_0594
  IL_03c9:  ldloc.0
  IL_03ca:  ldc.i4     0x4bfefd8c
  IL_03cf:  bgt.un     IL_04b4
  IL_03d4:  ldloc.0
  IL_03d5:  ldc.i4     0x455f7d3b
  IL_03da:  bgt.un.s   IL_0448
  IL_03dc:  ldloc.0
  IL_03dd:  ldc.i4     0x409a11e6
  IL_03e2:  bgt.un.s   IL_040a
  IL_03e4:  ldloc.0
  IL_03e5:  ldc.i4     0x3cbefaf8
  IL_03ea:  beq        IL_10fe
  IL_03ef:  ldloc.0
  IL_03f0:  ldc.i4     0x3db1994e
  IL_03f5:  beq        IL_1a58
  IL_03fa:  ldloc.0
  IL_03fb:  ldc.i4     0x409a11e6
  IL_0400:  beq        IL_1581
  IL_0405:  br         IL_2184
  IL_040a:  ldloc.0
  IL_040b:  ldc.i4     0x43473ee5
  IL_0410:  bgt.un.s   IL_042d
  IL_0412:  ldloc.0
  IL_0413:  ldc.i4     0x40ad1df2
  IL_0418:  beq        IL_1e87
  IL_041d:  ldloc.0
  IL_041e:  ldc.i4     0x43473ee5
  IL_0423:  beq        IL_1a04
  IL_0428:  br         IL_2184
  IL_042d:  ldloc.0
  IL_042e:  ldc.i4     0x43dde827
  IL_0433:  beq        IL_11e5
  IL_0438:  ldloc.0
  IL_0439:  ldc.i4     0x455f7d3b
  IL_043e:  beq        IL_18f3
  IL_0443:  br         IL_2184
  IL_0448:  ldloc.0
  IL_0449:  ldc.i4     0x49dd7dcb
  IL_044e:  bgt.un.s   IL_0476
  IL_0450:  ldloc.0
  IL_0451:  ldc.i4     0x46d186b0
  IL_0456:  beq        IL_17a3
  IL_045b:  ldloc.0
  IL_045c:  ldc.i4     0x474bacf2
  IL_0461:  beq        IL_11fa
  IL_0466:  ldloc.0
  IL_0467:  ldc.i4     0x49dd7dcb
  IL_046c:  beq        IL_0fc3
  IL_0471:  br         IL_2184
  IL_0476:  ldloc.0
  IL_0477:  ldc.i4     0x4a74c64a
  IL_047c:  bgt.un.s   IL_0499
  IL_047e:  ldloc.0
  IL_047f:  ldc.i4     0x4a2124db
  IL_0484:  beq        IL_152d
  IL_0489:  ldloc.0
  IL_048a:  ldc.i4     0x4a74c64a
  IL_048f:  beq        IL_1389
  IL_0494:  br         IL_2184
  IL_0499:  ldloc.0
  IL_049a:  ldc.i4     0x4bdfa75f
  IL_049f:  beq        IL_19c5
  IL_04a4:  ldloc.0
  IL_04a5:  ldc.i4     0x4bfefd8c
  IL_04aa:  beq        IL_17f7
  IL_04af:  br         IL_2184
  IL_04b4:  ldloc.0
  IL_04b5:  ldc.i4     0x5ae00ad0
  IL_04ba:  bgt.un.s   IL_0528
  IL_04bc:  ldloc.0
  IL_04bd:  ldc.i4     0x53ca5aaf
  IL_04c2:  bgt.un.s   IL_04ea
  IL_04c4:  ldloc.0
  IL_04c5:  ldc.i4     0x5078a99e
  IL_04ca:  beq        IL_0fed
  IL_04cf:  ldloc.0
  IL_04d0:  ldc.i4     0x52ca591c
  IL_04d5:  beq        IL_1407
  IL_04da:  ldloc.0
  IL_04db:  ldc.i4     0x53ca5aaf
  IL_04e0:  beq        IL_1446
  IL_04e5:  br         IL_2184
  IL_04ea:  ldloc.0
  IL_04eb:  ldc.i4     0x58bca78e
  IL_04f0:  bgt.un.s   IL_050d
  IL_04f2:  ldloc.0
  IL_04f3:  ldc.i4     0x572a4188
  IL_04f8:  beq        IL_12a2
  IL_04fd:  ldloc.0
  IL_04fe:  ldc.i4     0x58bca78e
  IL_0503:  beq        IL_0f84
  IL_0508:  br         IL_2184
  IL_050d:  ldloc.0
  IL_050e:  ldc.i4     0x5a5f7d91
  IL_0513:  beq        IL_0f30
  IL_0518:  ldloc.0
  IL_0519:  ldc.i4     0x5ae00ad0
  IL_051e:  beq        IL_0fae
  IL_0523:  br         IL_2184
  IL_0528:  ldloc.0
  IL_0529:  ldc.i4     0x6063309a
  IL_052e:  bgt.un.s   IL_0556
  IL_0530:  ldloc.0
  IL_0531:  ldc.i4     0x5b72871c
  IL_0536:  beq        IL_13f2
  IL_053b:  ldloc.0
  IL_053c:  ldc.i4     0x5fede3ca
  IL_0541:  beq        IL_173a
  IL_0546:  ldloc.0
  IL_0547:  ldc.i4     0x6063309a
  IL_054c:  beq        IL_1f98
  IL_0551:  br         IL_2184
  IL_0556:  ldloc.0
  IL_0557:  ldc.i4     0x61f6c9d5
  IL_055c:  bgt.un.s   IL_0579
  IL_055e:  ldloc.0
  IL_055f:  ldc.i4     0x61d89cab
  IL_0564:  beq        IL_2094
  IL_0569:  ldloc.0
  IL_056a:  ldc.i4     0x61f6c9d5
  IL_056f:  beq        IL_0f5a
  IL_0574:  br         IL_2184
  IL_0579:  ldloc.0
  IL_057a:  ldc.i4     0x64e4818a
  IL_057f:  beq        IL_13b3
  IL_0584:  ldloc.0
  IL_0585:  ldc.i4     0x64ed874e
  IL_058a:  beq        IL_15d5
  IL_058f:  br         IL_2184
  IL_0594:  ldloc.0
  IL_0595:  ldc.i4     0x7e34f956
  IL_059a:  bgt.un     IL_067f
  IL_059f:  ldloc.0
  IL_05a0:  ldc.i4     0x6b08265a
  IL_05a5:  bgt.un.s   IL_0613
  IL_05a7:  ldloc.0
  IL_05a8:  ldc.i4     0x69082334
  IL_05ad:  bgt.un.s   IL_05d5
  IL_05af:  ldloc.0
  IL_05b0:  ldc.i4     0x65795313
  IL_05b5:  beq        IL_1128
  IL_05ba:  ldloc.0
  IL_05bb:  ldc.i4     0x66f9fd96
  IL_05c0:  beq        IL_1f44
  IL_05c5:  ldloc.0
  IL_05c6:  ldc.i4     0x69082334
  IL_05cb:  beq        IL_1557
  IL_05d0:  br         IL_2184
  IL_05d5:  ldloc.0
  IL_05d6:  ldc.i4     0x6922f347
  IL_05db:  bgt.un.s   IL_05f8
  IL_05dd:  ldloc.0
  IL_05de:  ldc.i4     0x690dd584
  IL_05e3:  beq        IL_10aa
  IL_05e8:  ldloc.0
  IL_05e9:  ldc.i4     0x6922f347
  IL_05ee:  beq        IL_1e5d
  IL_05f3:  br         IL_2184
  IL_05f8:  ldloc.0
  IL_05f9:  ldc.i4     0x6a103d13
  IL_05fe:  beq        IL_1cce
  IL_0603:  ldloc.0
  IL_0604:  ldc.i4     0x6b08265a
  IL_0609:  beq        IL_1542
  IL_060e:  br         IL_2184
  IL_0613:  ldloc.0
  IL_0614:  ldc.i4     0x707a4a2f
  IL_0619:  bgt.un.s   IL_0641
  IL_061b:  ldloc.0
  IL_061c:  ldc.i4     0x6cab0635
  IL_0621:  beq        IL_18b4
  IL_0626:  ldloc.0
  IL_0627:  ldc.i4     0x6fb5337e
  IL_062c:  beq        IL_106b
  IL_0631:  ldloc.0
  IL_0632:  ldc.i4     0x707a4a2f
  IL_0637:  beq        IL_1947
  IL_063c:  br         IL_2184
  IL_0641:  ldloc.0
  IL_0642:  ldc.i4     0x76564766
  IL_0647:  bgt.un.s   IL_0664
  IL_0649:  ldloc.0
  IL_064a:  ldc.i4     0x727a797c
  IL_064f:  beq        IL_1431
  IL_0654:  ldloc.0
  IL_0655:  ldc.i4     0x76564766
  IL_065a:  beq        IL_1e33
  IL_065f:  br         IL_2184
  IL_0664:  ldloc.0
  IL_0665:  ldc.i4     0x76a68d74
  IL_066a:  beq        IL_1167
  IL_066f:  ldloc.0
  IL_0670:  ldc.i4     0x7e34f956
  IL_0675:  beq        IL_139e
  IL_067a:  br         IL_2184
  IL_067f:  ldloc.0
  IL_0680:  ldc.i4     0x83ce448e
  IL_0685:  bgt.un.s   IL_06f3
  IL_0687:  ldloc.0
  IL_0688:  ldc.i4     0x803babfe
  IL_068d:  bgt.un.s   IL_06b5
  IL_068f:  ldloc.0
  IL_0690:  ldc.i4     0x7e62165c
  IL_0695:  beq        IL_1860
  IL_069a:  ldloc.0
  IL_069b:  ldc.i4     0x7e762f73
  IL_06a0:  beq        IL_191d
  IL_06a5:  ldloc.0
  IL_06a6:  ldc.i4     0x803babfe
  IL_06ab:  beq        IL_12b7
  IL_06b0:  br         IL_2184
  IL_06b5:  ldloc.0
  IL_06b6:  ldc.i4     0x81dd4224
  IL_06bb:  bgt.un.s   IL_06d8
  IL_06bd:  ldloc.0
  IL_06be:  ldc.i4     0x8091e1ba
  IL_06c3:  beq        IL_16a7
  IL_06c8:  ldloc.0
  IL_06c9:  ldc.i4     0x81dd4224
  IL_06ce:  beq        IL_1e72
  IL_06d3:  br         IL_2184
  IL_06d8:  ldloc.0
  IL_06d9:  ldc.i4     0x82982bae
  IL_06de:  beq        IL_1eb1
  IL_06e3:  ldloc.0
  IL_06e4:  ldc.i4     0x83ce448e
  IL_06e9:  beq        IL_1bbd
  IL_06ee:  br         IL_2184
  IL_06f3:  ldloc.0
  IL_06f4:  ldc.i4     0x895f16b3
  IL_06f9:  bgt.un.s   IL_0739
  IL_06fb:  ldloc.0
  IL_06fc:  ldc.i4     0x86e56312
  IL_0701:  bgt.un.s   IL_071e
  IL_0703:  ldloc.0
  IL_0704:  ldc.i4     0x8495dbf6
  IL_0709:  beq        IL_207f
  IL_070e:  ldloc.0
  IL_070f:  ldc.i4     0x86e56312
  IL_0714:  beq        IL_1c3b
  IL_0719:  br         IL_2184
  IL_071e:  ldloc.0
  IL_071f:  ldc.i4     0x886d57ef
  IL_0724:  beq        IL_1c26
  IL_0729:  ldloc.0
  IL_072a:  ldc.i4     0x895f16b3
  IL_072f:  beq        IL_1056
  IL_0734:  br         IL_2184
  IL_0739:  ldloc.0
  IL_073a:  ldc.i4     0x89867abb
  IL_073f:  bgt.un.s   IL_075c
  IL_0741:  ldloc.0
  IL_0742:  ldc.i4     0x8965390c
  IL_0747:  beq        IL_1fec
  IL_074c:  ldloc.0
  IL_074d:  ldc.i4     0x89867abb
  IL_0752:  beq        IL_210c
  IL_0757:  br         IL_2184
  IL_075c:  ldloc.0
  IL_075d:  ldc.i4     0x8a546774
  IL_0762:  beq        IL_1c65
  IL_0767:  ldloc.0
  IL_0768:  ldc.i4     0x93254270
  IL_076d:  beq        IL_1335
  IL_0772:  br         IL_2184
  IL_0777:  ldloc.0
  IL_0778:  ldc.i4     0xc6816b27
  IL_077d:  bgt.un     IL_0b3b
  IL_0782:  ldloc.0
  IL_0783:  ldc.i4     0xabd163b5
  IL_0788:  bgt.un     IL_0958
  IL_078d:  ldloc.0
  IL_078e:  ldc.i4     0xa19e0b35
  IL_0793:  bgt.un     IL_0878
  IL_0798:  ldloc.0
  IL_0799:  ldc.i4     0x9b727df3
  IL_079e:  bgt.un.s   IL_080c
  IL_07a0:  ldloc.0
  IL_07a1:  ldc.i4     0x95604137
  IL_07a6:  bgt.un.s   IL_07ce
  IL_07a8:  ldloc.0
  IL_07a9:  ldc.i4     0x937219ca
  IL_07ae:  beq        IL_0ef1
  IL_07b3:  ldloc.0
  IL_07b4:  ldc.i4     0x9472756b
  IL_07b9:  beq        IL_1ec6
  IL_07be:  ldloc.0
  IL_07bf:  ldc.i4     0x95604137
  IL_07c4:  beq        IL_0f45
  IL_07c9:  br         IL_2184
  IL_07ce:  ldloc.0
  IL_07cf:  ldc.i4     0x9888188f
  IL_07d4:  bgt.un.s   IL_07f1
  IL_07d6:  ldloc.0
  IL_07d7:  ldc.i4     0x96939bc6
  IL_07dc:  beq        IL_17b8
  IL_07e1:  ldloc.0
  IL_07e2:  ldc.i4     0x9888188f
  IL_07e7:  beq        IL_195c
  IL_07ec:  br         IL_2184
  IL_07f1:  ldloc.0
  IL_07f2:  ldc.i4     0x992dbe8d
  IL_07f7:  beq        IL_1a6d
  IL_07fc:  ldloc.0
  IL_07fd:  ldc.i4     0x9b727df3
  IL_0802:  beq        IL_1224
  IL_0807:  br         IL_2184
  IL_080c:  ldloc.0
  IL_080d:  ldc.i4     0x9f1949ce
  IL_0812:  bgt.un.s   IL_083a
  IL_0814:  ldloc.0
  IL_0815:  ldc.i4     0x9b8cfbe6
  IL_081a:  beq        IL_163e
  IL_081f:  ldloc.0
  IL_0820:  ldc.i4     0x9d09dd0e
  IL_0825:  beq        IL_0f1b
  IL_082a:  ldloc.0
  IL_082b:  ldc.i4     0x9f1949ce
  IL_0830:  beq        IL_20a9
  IL_0835:  br         IL_2184
  IL_083a:  ldloc.0
  IL_083b:  ldc.i4     0xa0ae197d
  IL_0840:  bgt.un.s   IL_085d
  IL_0842:  ldloc.0
  IL_0843:  ldc.i4     0x9f8d2aad
  IL_0848:  beq        IL_1bfc
  IL_084d:  ldloc.0
  IL_084e:  ldc.i4     0xa0ae197d
  IL_0853:  beq        IL_206a
  IL_0858:  br         IL_2184
  IL_085d:  ldloc.0
  IL_085e:  ldc.i4     0xa1369482
  IL_0863:  beq        IL_1692
  IL_0868:  ldloc.0
  IL_0869:  ldc.i4     0xa19e0b35
  IL_086e:  beq        IL_1113
  IL_0873:  br         IL_2184
  IL_0878:  ldloc.0
  IL_0879:  ldc.i4     0xa72856a7
  IL_087e:  bgt.un.s   IL_08ec
  IL_0880:  ldloc.0
  IL_0881:  ldc.i4     0xa45b7edf
  IL_0886:  bgt.un.s   IL_08ae
  IL_0888:  ldloc.0
  IL_0889:  ldc.i4     0xa26d6489
  IL_088e:  beq        IL_149a
  IL_0893:  ldloc.0
  IL_0894:  ldc.i4     0xa445e39b
  IL_0899:  beq        IL_1ca4
  IL_089e:  ldloc.0
  IL_089f:  ldc.i4     0xa45b7edf
  IL_08a4:  beq        IL_15c0
  IL_08a9:  br         IL_2184
  IL_08ae:  ldloc.0
  IL_08af:  ldc.i4     0xa6428622
  IL_08b4:  bgt.un.s   IL_08d1
  IL_08b6:  ldloc.0
  IL_08b7:  ldc.i4     0xa517cc3f
  IL_08bc:  beq        IL_113d
  IL_08c1:  ldloc.0
  IL_08c2:  ldc.i4     0xa6428622
  IL_08c7:  beq        IL_1b54
  IL_08cc:  br         IL_2184
  IL_08d1:  ldloc.0
  IL_08d2:  ldc.i4     0xa6d43b30
  IL_08d7:  beq        IL_1278
  IL_08dc:  ldloc.0
  IL_08dd:  ldc.i4     0xa72856a7
  IL_08e2:  beq        IL_1095
  IL_08e7:  br         IL_2184
  IL_08ec:  ldloc.0
  IL_08ed:  ldc.i4     0xaa88ad61
  IL_08f2:  bgt.un.s   IL_091a
  IL_08f4:  ldloc.0
  IL_08f5:  ldc.i4     0xa7855456
  IL_08fa:  beq        IL_1668
  IL_08ff:  ldloc.0
  IL_0900:  ldc.i4     0xa974f9e2
  IL_0905:  beq        IL_1320
  IL_090a:  ldloc.0
  IL_090b:  ldc.i4     0xaa88ad61
  IL_0910:  beq        IL_174f
  IL_0915:  br         IL_2184
  IL_091a:  ldloc.0
  IL_091b:  ldc.i4     0xaac796e8
  IL_0920:  bgt.un.s   IL_093d
  IL_0922:  ldloc.0
  IL_0923:  ldc.i4     0xaa8947f7
  IL_0928:  beq        IL_1a97
  IL_092d:  ldloc.0
  IL_092e:  ldc.i4     0xaac796e8
  IL_0933:  beq        IL_1edb
  IL_0938:  br         IL_2184
  IL_093d:  ldloc.0
  IL_093e:  ldc.i4     0xaaf89cf7
  IL_0943:  beq        IL_20e8
  IL_0948:  ldloc.0
  IL_0949:  ldc.i4     0xabd163b5
  IL_094e:  beq        IL_10e9
  IL_0953:  br         IL_2184
  IL_0958:  ldloc.0
  IL_0959:  ldc.i4     0xbaa50aa9
  IL_095e:  bgt.un     IL_0a43
  IL_0963:  ldloc.0
  IL_0964:  ldc.i4     0xb372c522
  IL_0969:  bgt.un.s   IL_09d7
  IL_096b:  ldloc.0
  IL_096c:  ldc.i4     0xafa908eb
  IL_0971:  bgt.un.s   IL_0999
  IL_0973:  ldloc.0
  IL_0974:  ldc.i4     0xabdd3b18
  IL_0979:  beq        IL_1ef0
  IL_097e:  ldloc.0
  IL_097f:  ldc.i4     0xac1855c9
  IL_0984:  beq        IL_14c4
  IL_0989:  ldloc.0
  IL_098a:  ldc.i4     0xafa908eb
  IL_098f:  beq        IL_1c8f
  IL_0994:  br         IL_2184
  IL_0999:  ldloc.0
  IL_099a:  ldc.i4     0xb0ced153
  IL_099f:  bgt.un.s   IL_09bc
  IL_09a1:  ldloc.0
  IL_09a2:  ldc.i4     0xb0068fc4
  IL_09a7:  beq        IL_2001
  IL_09ac:  ldloc.0
  IL_09ad:  ldc.i4     0xb0ced153
  IL_09b2:  beq        IL_12cc
  IL_09b7:  br         IL_2184
  IL_09bc:  ldloc.0
  IL_09bd:  ldc.i4     0xb1ed8222
  IL_09c2:  beq        IL_1fad
  IL_09c7:  ldloc.0
  IL_09c8:  ldc.i4     0xb372c522
  IL_09cd:  beq        IL_1d0d
  IL_09d2:  br         IL_2184
  IL_09d7:  ldloc.0
  IL_09d8:  ldc.i4     0xb867b0a7
  IL_09dd:  bgt.un.s   IL_0a05
  IL_09df:  ldloc.0
  IL_09e0:  ldc.i4     0xb7a04f18
  IL_09e5:  beq        IL_128d
  IL_09ea:  ldloc.0
  IL_09eb:  ldc.i4     0xb7c1d5fe
  IL_09f0:  beq        IL_1152
  IL_09f5:  ldloc.0
  IL_09f6:  ldc.i4     0xb867b0a7
  IL_09fb:  beq        IL_1f2f
  IL_0a00:  br         IL_2184
  IL_0a05:  ldloc.0
  IL_0a06:  ldc.i4     0xb9ca4931
  IL_0a0b:  bgt.un.s   IL_0a28
  IL_0a0d:  ldloc.0
  IL_0a0e:  ldc.i4     0xb8ddd025
  IL_0a13:  beq        IL_199b
  IL_0a18:  ldloc.0
  IL_0a19:  ldc.i4     0xb9ca4931
  IL_0a1e:  beq        IL_1e9c
  IL_0a23:  br         IL_2184
  IL_0a28:  ldloc.0
  IL_0a29:  ldc.i4     0xb9cc5c0b
  IL_0a2e:  beq        IL_1ddf
  IL_0a33:  ldloc.0
  IL_0a34:  ldc.i4     0xbaa50aa9
  IL_0a39:  beq        IL_1a82
  IL_0a3e:  br         IL_2184
  IL_0a43:  ldloc.0
  IL_0a44:  ldc.i4     0xbf024ba2
  IL_0a49:  bgt.un.s   IL_0ab7
  IL_0a4b:  ldloc.0
  IL_0a4c:  ldc.i4     0xbd5a7ac0
  IL_0a51:  bgt.un.s   IL_0a79
  IL_0a53:  ldloc.0
  IL_0a54:  ldc.i4     0xbae65831
  IL_0a59:  beq        IL_1629
  IL_0a5e:  ldloc.0
  IL_0a5f:  ldc.i4     0xbaeb1d00
  IL_0a64:  beq        IL_1d61
  IL_0a69:  ldloc.0
  IL_0a6a:  ldc.i4     0xbd5a7ac0
  IL_0a6f:  beq        IL_1764
  IL_0a74:  br         IL_2184
  IL_0a79:  ldloc.0
  IL_0a7a:  ldc.i4     0xbe9c6125
  IL_0a7f:  bgt.un.s   IL_0a9c
  IL_0a81:  ldloc.0
  IL_0a82:  ldc.i4     0xbdd584ee
  IL_0a87:  beq        IL_1d4c
  IL_0a8c:  ldloc.0
  IL_0a8d:  ldc.i4     0xbe9c6125
  IL_0a92:  beq        IL_1f59
  IL_0a97:  br         IL_2184
  IL_0a9c:  ldloc.0
  IL_0a9d:  ldc.i4     0xbef81d14
  IL_0aa2:  beq        IL_120f
  IL_0aa7:  ldloc.0
  IL_0aa8:  ldc.i4     0xbf024ba2
  IL_0aad:  beq        IL_1da0
  IL_0ab2:  br         IL_2184
  IL_0ab7:  ldloc.0
  IL_0ab8:  ldc.i4     0xc158896f
  IL_0abd:  bgt.un.s   IL_0afd
  IL_0abf:  ldloc.0
  IL_0ac0:  ldc.i4     0xbfcdf9de
  IL_0ac5:  bgt.un.s   IL_0ae2
  IL_0ac7:  ldloc.0
  IL_0ac8:  ldc.i4     0xbf7ad191
  IL_0acd:  beq        IL_1779
  IL_0ad2:  ldloc.0
  IL_0ad3:  ldc.i4     0xbfcdf9de
  IL_0ad8:  beq        IL_2040
  IL_0add:  br         IL_2184
  IL_0ae2:  ldloc.0
  IL_0ae3:  ldc.i4     0xc0d01d2f
  IL_0ae8:  beq        IL_2148
  IL_0aed:  ldloc.0
  IL_0aee:  ldc.i4     0xc158896f
  IL_0af3:  beq        IL_0f99
  IL_0af8:  br         IL_2184
  IL_0afd:  ldloc.0
  IL_0afe:  ldc.i4     0xc3cf479f
  IL_0b03:  bgt.un.s   IL_0b20
  IL_0b05:  ldloc.0
  IL_0b06:  ldc.i4     0xc2d30dbd
  IL_0b0b:  beq        IL_1b15
  IL_0b10:  ldloc.0
  IL_0b11:  ldc.i4     0xc3cf479f
  IL_0b16:  beq        IL_1017
  IL_0b1b:  br         IL_2184
  IL_0b20:  ldloc.0
  IL_0b21:  ldc.i4     0xc4135709
  IL_0b26:  beq        IL_1db5
  IL_0b2b:  ldloc.0
  IL_0b2c:  ldc.i4     0xc6816b27
  IL_0b31:  beq        IL_145b
  IL_0b36:  br         IL_2184
  IL_0b3b:  ldloc.0
  IL_0b3c:  ldc.i4     0xec3e2fe1
  IL_0b41:  bgt.un     IL_0d11
  IL_0b46:  ldloc.0
  IL_0b47:  ldc.i4     0xd6b51455
  IL_0b4c:  bgt.un     IL_0c31
  IL_0b51:  ldloc.0
  IL_0b52:  ldc.i4     0xcc9a2e03
  IL_0b57:  bgt.un.s   IL_0bc5
  IL_0b59:  ldloc.0
  IL_0b5a:  ldc.i4     0xc9f4640b
  IL_0b5f:  bgt.un.s   IL_0b87
  IL_0b61:  ldloc.0
  IL_0b62:  ldc.i4     0xc6eb64f6
  IL_0b67:  beq        IL_134a
  IL_0b6c:  ldloc.0
  IL_0b6d:  ldc.i4     0xc736a907
  IL_0b72:  beq        IL_1041
  IL_0b77:  ldloc.0
  IL_0b78:  ldc.i4     0xc9f4640b
  IL_0b7d:  beq        IL_12f6
  IL_0b82:  br         IL_2184
  IL_0b87:  ldloc.0
  IL_0b88:  ldc.i4     0xcc0d0856
  IL_0b8d:  bgt.un.s   IL_0baa
  IL_0b8f:  ldloc.0
  IL_0b90:  ldc.i4     0xcb416d65
  IL_0b95:  beq        IL_1d22
  IL_0b9a:  ldloc.0
  IL_0b9b:  ldc.i4     0xcc0d0856
  IL_0ba0:  beq        IL_15ff
  IL_0ba5:  br         IL_2184
  IL_0baa:  ldloc.0
  IL_0bab:  ldc.i4     0xcc6f4fe0
  IL_0bb0:  beq        IL_1932
  IL_0bb5:  ldloc.0
  IL_0bb6:  ldc.i4     0xcc9a2e03
  IL_0bbb:  beq        IL_1c7a
  IL_0bc0:  br         IL_2184
  IL_0bc5:  ldloc.0
  IL_0bc6:  ldc.i4     0xd05f1663
  IL_0bcb:  bgt.un.s   IL_0bf3
  IL_0bcd:  ldloc.0
  IL_0bce:  ldc.i4     0xcd190185
  IL_0bd3:  beq        IL_1d8b
  IL_0bd8:  ldloc.0
  IL_0bd9:  ldc.i4     0xce0d0b7c
  IL_0bde:  beq        IL_1614
  IL_0be3:  ldloc.0
  IL_0be4:  ldc.i4     0xd05f1663
  IL_0be9:  beq        IL_16bc
  IL_0bee:  br         IL_2184
  IL_0bf3:  ldloc.0
  IL_0bf4:  ldc.i4     0xd642d1b2
  IL_0bf9:  bgt.un.s   IL_0c16
  IL_0bfb:  ldloc.0
  IL_0bfc:  ldc.i4     0xd55997bc
  IL_0c01:  beq        IL_1df4
  IL_0c06:  ldloc.0
  IL_0c07:  ldc.i4     0xd642d1b2
  IL_0c0c:  beq        IL_1b3f
  IL_0c11:  br         IL_2184
  IL_0c16:  ldloc.0
  IL_0c17:  ldc.i4     0xd67d0512
  IL_0c1c:  beq        IL_1374
  IL_0c21:  ldloc.0
  IL_0c22:  ldc.i4     0xd6b51455
  IL_0c27:  beq        IL_1836
  IL_0c2c:  br         IL_2184
  IL_0c31:  ldloc.0
  IL_0c32:  ldc.i4     0xe7009fb7
  IL_0c37:  bgt.un.s   IL_0ca5
  IL_0c39:  ldloc.0
  IL_0c3a:  ldc.i4     0xe06f4684
  IL_0c3f:  bgt.un.s   IL_0c67
  IL_0c41:  ldloc.0
  IL_0c42:  ldc.i4     0xda6a3615
  IL_0c47:  beq        IL_1ba8
  IL_0c4c:  ldloc.0
  IL_0c4d:  ldc.i4     0xde091dac
  IL_0c52:  beq        IL_1e48
  IL_0c57:  ldloc.0
  IL_0c58:  ldc.i4     0xe06f4684
  IL_0c5d:  beq        IL_141c
  IL_0c62:  br         IL_2184
  IL_0c67:  ldloc.0
  IL_0c68:  ldc.i4     0xe4b51449
  IL_0c6d:  bgt.un.s   IL_0c8a
  IL_0c6f:  ldloc.0
  IL_0c70:  ldc.i4     0xe482e420
  IL_0c75:  beq        IL_15ea
  IL_0c7a:  ldloc.0
  IL_0c7b:  ldc.i4     0xe4b51449
  IL_0c80:  beq        IL_16e6
  IL_0c85:  br         IL_2184
  IL_0c8a:  ldloc.0
  IL_0c8b:  ldc.i4     0xe600b237
  IL_0c90:  beq        IL_1b93
  IL_0c95:  ldloc.0
  IL_0c96:  ldc.i4     0xe7009fb7
  IL_0c9b:  beq        IL_135f
  IL_0ca0:  br         IL_2184
  IL_0ca5:  ldloc.0
  IL_0ca6:  ldc.i4     0xea4b9afc
  IL_0cab:  bgt.un.s   IL_0cd3
  IL_0cad:  ldloc.0
  IL_0cae:  ldc.i4     0xe8a3d0e1
  IL_0cb3:  beq        IL_1ad6
  IL_0cb8:  ldloc.0
  IL_0cb9:  ldc.i4     0xe8cf3efe
  IL_0cbe:  beq        IL_1d37
  IL_0cc3:  ldloc.0
  IL_0cc4:  ldc.i4     0xea4b9afc
  IL_0cc9:  beq        IL_2055
  IL_0cce:  br         IL_2184
  IL_0cd3:  ldloc.0
  IL_0cd4:  ldc.i4     0xebdb35fa
  IL_0cd9:  bgt.un.s   IL_0cf6
  IL_0cdb:  ldloc.0
  IL_0cdc:  ldc.i4     0xeacd5737
  IL_0ce1:  beq        IL_1f1a
  IL_0ce6:  ldloc.0
  IL_0ce7:  ldc.i4     0xebdb35fa
  IL_0cec:  beq        IL_13c8
  IL_0cf1:  br         IL_2184
  IL_0cf6:  ldloc.0
  IL_0cf7:  ldc.i4     0xec3aca8d
  IL_0cfc:  beq        IL_1be7
  IL_0d01:  ldloc.0
  IL_0d02:  ldc.i4     0xec3e2fe1
  IL_0d07:  beq        IL_1fc2
  IL_0d0c:  br         IL_2184
  IL_0d11:  ldloc.0
  IL_0d12:  ldc.i4     0xf5e05a6b
  IL_0d17:  bgt.un     IL_0dfc
  IL_0d1c:  ldloc.0
  IL_0d1d:  ldc.i4     0xf1e6c9c8
  IL_0d22:  bgt.un.s   IL_0d90
  IL_0d24:  ldloc.0
  IL_0d25:  ldc.i4     0xee3feea8
  IL_0d2a:  bgt.un.s   IL_0d52
  IL_0d2c:  ldloc.0
  IL_0d2d:  ldc.i4     0xeccec0be
  IL_0d32:  beq        IL_16d1
  IL_0d37:  ldloc.0
  IL_0d38:  ldc.i4     0xedb243f0
  IL_0d3d:  beq        IL_124e
  IL_0d42:  ldloc.0
  IL_0d43:  ldc.i4     0xee3feea8
  IL_0d48:  beq        IL_189f
  IL_0d4d:  br         IL_2184
  IL_0d52:  ldloc.0
  IL_0d53:  ldc.i4     0xef81b6ca
  IL_0d58:  bgt.un.s   IL_0d75
  IL_0d5a:  ldloc.0
  IL_0d5b:  ldc.i4     0xeed17667
  IL_0d60:  beq        IL_1e09
  IL_0d65:  ldloc.0
  IL_0d66:  ldc.i4     0xef81b6ca
  IL_0d6b:  beq        IL_14ee
  IL_0d70:  br         IL_2184
  IL_0d75:  ldloc.0
  IL_0d76:  ldc.i4     0xf1a689a6
  IL_0d7b:  beq        IL_1c11
  IL_0d80:  ldloc.0
  IL_0d81:  ldc.i4     0xf1e6c9c8
  IL_0d86:  beq        IL_1fd7
  IL_0d8b:  br         IL_2184
  IL_0d90:  ldloc.0
  IL_0d91:  ldc.i4     0xf2c43079
  IL_0d96:  bgt.un.s   IL_0dbe
  IL_0d98:  ldloc.0
  IL_0d99:  ldc.i4     0xf24af6dc
  IL_0d9e:  beq        IL_1cb9
  IL_0da3:  ldloc.0
  IL_0da4:  ldc.i4     0xf2963331
  IL_0da9:  beq        IL_1ce3
  IL_0dae:  ldloc.0
  IL_0daf:  ldc.i4     0xf2c43079
  IL_0db4:  beq        IL_10bf
  IL_0db9:  br         IL_2184
  IL_0dbe:  ldloc.0
  IL_0dbf:  ldc.i4     0xf53166a8
  IL_0dc4:  bgt.un.s   IL_0de1
  IL_0dc6:  ldloc.0
  IL_0dc7:  ldc.i4     0xf43f03a6
  IL_0dcc:  beq        IL_1a43
  IL_0dd1:  ldloc.0
  IL_0dd2:  ldc.i4     0xf53166a8
  IL_0dd7:  beq        IL_18de
  IL_0ddc:  br         IL_2184
  IL_0de1:  ldloc.0
  IL_0de2:  ldc.i4     0xf565000a
  IL_0de7:  beq        IL_1725
  IL_0dec:  ldloc.0
  IL_0ded:  ldc.i4     0xf5e05a6b
  IL_0df2:  beq        IL_102c
  IL_0df7:  br         IL_2184
  IL_0dfc:  ldloc.0
  IL_0dfd:  ldc.i4     0xf89e7910
  IL_0e02:  bgt.un.s   IL_0e70
  IL_0e04:  ldloc.0
  IL_0e05:  ldc.i4     0xf68ddabd
  IL_0e0a:  bgt.un.s   IL_0e32
  IL_0e0c:  ldloc.0
  IL_0e0d:  ldc.i4     0xf61ee173
  IL_0e12:  beq        IL_1f6e
  IL_0e17:  ldloc.0
  IL_0e18:  ldc.i4     0xf68c7b31
  IL_0e1d:  beq        IL_211b
  IL_0e22:  ldloc.0
  IL_0e23:  ldc.i4     0xf68ddabd
  IL_0e28:  beq        IL_10d4
  IL_0e2d:  br         IL_2184
  IL_0e32:  ldloc.0
  IL_0e33:  ldc.i4     0xf718ff34
  IL_0e38:  bgt.un.s   IL_0e55
  IL_0e3a:  ldloc.0
  IL_0e3b:  ldc.i4     0xf6aeaa6e
  IL_0e40:  beq        IL_1a19
  IL_0e45:  ldloc.0
  IL_0e46:  ldc.i4     0xf718ff34
  IL_0e4b:  beq        IL_1f83
  IL_0e50:  br         IL_2184
  IL_0e55:  ldloc.0
  IL_0e56:  ldc.i4     0xf722ab01
  IL_0e5b:  beq        IL_1a2e
  IL_0e60:  ldloc.0
  IL_0e61:  ldc.i4     0xf89e7910
  IL_0e66:  beq        IL_19b0
  IL_0e6b:  br         IL_2184
  IL_0e70:  ldloc.0
  IL_0e71:  ldc.i4     0xf9c905c2
  IL_0e76:  bgt.un.s   IL_0eb6
  IL_0e78:  ldloc.0
  IL_0e79:  ldc.i4     0xf93dc85c
  IL_0e7e:  bgt.un.s   IL_0e9b
  IL_0e80:  ldloc.0
  IL_0e81:  ldc.i4     0xf91ad389
  IL_0e86:  beq        IL_17e2
  IL_0e8b:  ldloc.0
  IL_0e8c:  ldc.i4     0xf93dc85c
  IL_0e91:  beq        IL_1cf8
  IL_0e96:  br         IL_2184
  IL_0e9b:  ldloc.0
  IL_0e9c:  ldc.i4     0xf93f63c6
  IL_0ea1:  beq        IL_13dd
  IL_0ea6:  ldloc.0
  IL_0ea7:  ldc.i4     0xf9c905c2
  IL_0eac:  beq        IL_2139
  IL_0eb1:  br         IL_2184
  IL_0eb6:  ldloc.0
  IL_0eb7:  ldc.i4     0xfafbe318
  IL_0ebc:  bgt.un.s   IL_0ed9
  IL_0ebe:  ldloc.0
  IL_0ebf:  ldc.i4     0xfa57fe02
  IL_0ec4:  beq        IL_1b7e
  IL_0ec9:  ldloc.0
  IL_0eca:  ldc.i4     0xfafbe318
  IL_0ecf:  beq        IL_14af
  IL_0ed4:  br         IL_2184
  IL_0ed9:  ldloc.0
  IL_0eda:  ldc.i4     0xfb42d85f
  IL_0edf:  beq        IL_1d76
  IL_0ee4:  ldloc.0
  IL_0ee5:  ldc.i4     0xff1d7ceb
  IL_0eea:  beq.s      IL_0f06
  IL_0eec:  br         IL_2184
  IL_0ef1:  ldarg.0
  IL_0ef2:  ldstr      "cddafs"
  IL_0ef7:  call       "bool string.op_Equality(string, string)"
  IL_0efc:  brtrue     IL_2166
  IL_0f01:  br         IL_2184
  IL_0f06:  ldarg.0
  IL_0f07:  ldstr      "cd9660"
  IL_0f0c:  call       "bool string.op_Equality(string, string)"
  IL_0f11:  brtrue     IL_2166
  IL_0f16:  br         IL_2184
  IL_0f1b:  ldarg.0
  IL_0f1c:  ldstr      "iso"
  IL_0f21:  call       "bool string.op_Equality(string, string)"
  IL_0f26:  brtrue     IL_2166
  IL_0f2b:  br         IL_2184
  IL_0f30:  ldarg.0
  IL_0f31:  ldstr      "isofs"
  IL_0f36:  call       "bool string.op_Equality(string, string)"
  IL_0f3b:  brtrue     IL_2166
  IL_0f40:  br         IL_2184
  IL_0f45:  ldarg.0
  IL_0f46:  ldstr      "iso9660"
  IL_0f4b:  call       "bool string.op_Equality(string, string)"
  IL_0f50:  brtrue     IL_2166
  IL_0f55:  br         IL_2184
  IL_0f5a:  ldarg.0
  IL_0f5b:  ldstr      "fuseiso"
  IL_0f60:  call       "bool string.op_Equality(string, string)"
  IL_0f65:  brtrue     IL_2166
  IL_0f6a:  br         IL_2184
  IL_0f6f:  ldarg.0
  IL_0f70:  ldstr      "fuseiso9660"
  IL_0f75:  call       "bool string.op_Equality(string, string)"
  IL_0f7a:  brtrue     IL_2166
  IL_0f7f:  br         IL_2184
  IL_0f84:  ldarg.0
  IL_0f85:  ldstr      "udf"
  IL_0f8a:  call       "bool string.op_Equality(string, string)"
  IL_0f8f:  brtrue     IL_2166
  IL_0f94:  br         IL_2184
  IL_0f99:  ldarg.0
  IL_0f9a:  ldstr      "umview-mod-umfuseiso9660"
  IL_0f9f:  call       "bool string.op_Equality(string, string)"
  IL_0fa4:  brtrue     IL_2166
  IL_0fa9:  br         IL_2184
  IL_0fae:  ldarg.0
  IL_0faf:  ldstr      "aafs"
  IL_0fb4:  call       "bool string.op_Equality(string, string)"
  IL_0fb9:  brtrue     IL_216c
  IL_0fbe:  br         IL_2184
  IL_0fc3:  ldarg.0
  IL_0fc4:  ldstr      "adfs"
  IL_0fc9:  call       "bool string.op_Equality(string, string)"
  IL_0fce:  brtrue     IL_216c
  IL_0fd3:  br         IL_2184
  IL_0fd8:  ldarg.0
  IL_0fd9:  ldstr      "affs"
  IL_0fde:  call       "bool string.op_Equality(string, string)"
  IL_0fe3:  brtrue     IL_216c
  IL_0fe8:  br         IL_2184
  IL_0fed:  ldarg.0
  IL_0fee:  ldstr      "anoninode"
  IL_0ff3:  call       "bool string.op_Equality(string, string)"
  IL_0ff8:  brtrue     IL_216c
  IL_0ffd:  br         IL_2184
  IL_1002:  ldarg.0
  IL_1003:  ldstr      "anon-inode FS"
  IL_1008:  call       "bool string.op_Equality(string, string)"
  IL_100d:  brtrue     IL_216c
  IL_1012:  br         IL_2184
  IL_1017:  ldarg.0
  IL_1018:  ldstr      "apfs"
  IL_101d:  call       "bool string.op_Equality(string, string)"
  IL_1022:  brtrue     IL_216c
  IL_1027:  br         IL_2184
  IL_102c:  ldarg.0
  IL_102d:  ldstr      "balloon-kvm-fs"
  IL_1032:  call       "bool string.op_Equality(string, string)"
  IL_1037:  brtrue     IL_216c
  IL_103c:  br         IL_2184
  IL_1041:  ldarg.0
  IL_1042:  ldstr      "bdevfs"
  IL_1047:  call       "bool string.op_Equality(string, string)"
  IL_104c:  brtrue     IL_216c
  IL_1051:  br         IL_2184
  IL_1056:  ldarg.0
  IL_1057:  ldstr      "befs"
  IL_105c:  call       "bool string.op_Equality(string, string)"
  IL_1061:  brtrue     IL_216c
  IL_1066:  br         IL_2184
  IL_106b:  ldarg.0
  IL_106c:  ldstr      "bfs"
  IL_1071:  call       "bool string.op_Equality(string, string)"
  IL_1076:  brtrue     IL_216c
  IL_107b:  br         IL_2184
  IL_1080:  ldarg.0
  IL_1081:  ldstr      "bootfs"
  IL_1086:  call       "bool string.op_Equality(string, string)"
  IL_108b:  brtrue     IL_216c
  IL_1090:  br         IL_2184
  IL_1095:  ldarg.0
  IL_1096:  ldstr      "bpf_fs"
  IL_109b:  call       "bool string.op_Equality(string, string)"
  IL_10a0:  brtrue     IL_216c
  IL_10a5:  br         IL_2184
  IL_10aa:  ldarg.0
  IL_10ab:  ldstr      "btrfs"
  IL_10b0:  call       "bool string.op_Equality(string, string)"
  IL_10b5:  brtrue     IL_216c
  IL_10ba:  br         IL_2184
  IL_10bf:  ldarg.0
  IL_10c0:  ldstr      "btrfs_test"
  IL_10c5:  call       "bool string.op_Equality(string, string)"
  IL_10ca:  brtrue     IL_216c
  IL_10cf:  br         IL_2184
  IL_10d4:  ldarg.0
  IL_10d5:  ldstr      "coh"
  IL_10da:  call       "bool string.op_Equality(string, string)"
  IL_10df:  brtrue     IL_216c
  IL_10e4:  br         IL_2184
  IL_10e9:  ldarg.0
  IL_10ea:  ldstr      "daxfs"
  IL_10ef:  call       "bool string.op_Equality(string, string)"
  IL_10f4:  brtrue     IL_216c
  IL_10f9:  br         IL_2184
  IL_10fe:  ldarg.0
  IL_10ff:  ldstr      "drvfs"
  IL_1104:  call       "bool string.op_Equality(string, string)"
  IL_1109:  brtrue     IL_216c
  IL_110e:  br         IL_2184
  IL_1113:  ldarg.0
  IL_1114:  ldstr      "efivarfs"
  IL_1119:  call       "bool string.op_Equality(string, string)"
  IL_111e:  brtrue     IL_216c
  IL_1123:  br         IL_2184
  IL_1128:  ldarg.0
  IL_1129:  ldstr      "efs"
  IL_112e:  call       "bool string.op_Equality(string, string)"
  IL_1133:  brtrue     IL_216c
  IL_1138:  br         IL_2184
  IL_113d:  ldarg.0
  IL_113e:  ldstr      "exfat"
  IL_1143:  call       "bool string.op_Equality(string, string)"
  IL_1148:  brtrue     IL_216c
  IL_114d:  br         IL_2184
  IL_1152:  ldarg.0
  IL_1153:  ldstr      "exofs"
  IL_1158:  call       "bool string.op_Equality(string, string)"
  IL_115d:  brtrue     IL_216c
  IL_1162:  br         IL_2184
  IL_1167:  ldarg.0
  IL_1168:  ldstr      "ext"
  IL_116d:  call       "bool string.op_Equality(string, string)"
  IL_1172:  brtrue     IL_216c
  IL_1177:  br         IL_2184
  IL_117c:  ldarg.0
  IL_117d:  ldstr      "ext2"
  IL_1182:  call       "bool string.op_Equality(string, string)"
  IL_1187:  brtrue     IL_216c
  IL_118c:  br         IL_2184
  IL_1191:  ldarg.0
  IL_1192:  ldstr      "ext2_old"
  IL_1197:  call       "bool string.op_Equality(string, string)"
  IL_119c:  brtrue     IL_216c
  IL_11a1:  br         IL_2184
  IL_11a6:  ldarg.0
  IL_11a7:  ldstr      "ext3"
  IL_11ac:  call       "bool string.op_Equality(string, string)"
  IL_11b1:  brtrue     IL_216c
  IL_11b6:  br         IL_2184
  IL_11bb:  ldarg.0
  IL_11bc:  ldstr      "ext2/ext3"
  IL_11c1:  call       "bool string.op_Equality(string, string)"
  IL_11c6:  brtrue     IL_216c
  IL_11cb:  br         IL_2184
  IL_11d0:  ldarg.0
  IL_11d1:  ldstr      "ext4"
  IL_11d6:  call       "bool string.op_Equality(string, string)"
  IL_11db:  brtrue     IL_216c
  IL_11e0:  br         IL_2184
  IL_11e5:  ldarg.0
  IL_11e6:  ldstr      "ext4dev"
  IL_11eb:  call       "bool string.op_Equality(string, string)"
  IL_11f0:  brtrue     IL_216c
  IL_11f5:  br         IL_2184
  IL_11fa:  ldarg.0
  IL_11fb:  ldstr      "f2fs"
  IL_1200:  call       "bool string.op_Equality(string, string)"
  IL_1205:  brtrue     IL_216c
  IL_120a:  br         IL_2184
  IL_120f:  ldarg.0
  IL_1210:  ldstr      "fat"
  IL_1215:  call       "bool string.op_Equality(string, string)"
  IL_121a:  brtrue     IL_216c
  IL_121f:  br         IL_2184
  IL_1224:  ldarg.0
  IL_1225:  ldstr      "fuseext2"
  IL_122a:  call       "bool string.op_Equality(string, string)"
  IL_122f:  brtrue     IL_216c
  IL_1234:  br         IL_2184
  IL_1239:  ldarg.0
  IL_123a:  ldstr      "fusefat"
  IL_123f:  call       "bool string.op_Equality(string, string)"
  IL_1244:  brtrue     IL_216c
  IL_1249:  br         IL_2184
  IL_124e:  ldarg.0
  IL_124f:  ldstr      "hfs"
  IL_1254:  call       "bool string.op_Equality(string, string)"
  IL_1259:  brtrue     IL_216c
  IL_125e:  br         IL_2184
  IL_1263:  ldarg.0
  IL_1264:  ldstr      "hfs+"
  IL_1269:  call       "bool string.op_Equality(string, string)"
  IL_126e:  brtrue     IL_216c
  IL_1273:  br         IL_2184
  IL_1278:  ldarg.0
  IL_1279:  ldstr      "hfsplus"
  IL_127e:  call       "bool string.op_Equality(string, string)"
  IL_1283:  brtrue     IL_216c
  IL_1288:  br         IL_2184
  IL_128d:  ldarg.0
  IL_128e:  ldstr      "hfsx"
  IL_1293:  call       "bool string.op_Equality(string, string)"
  IL_1298:  brtrue     IL_216c
  IL_129d:  br         IL_2184
  IL_12a2:  ldarg.0
  IL_12a3:  ldstr      "hostfs"
  IL_12a8:  call       "bool string.op_Equality(string, string)"
  IL_12ad:  brtrue     IL_216c
  IL_12b2:  br         IL_2184
  IL_12b7:  ldarg.0
  IL_12b8:  ldstr      "hpfs"
  IL_12bd:  call       "bool string.op_Equality(string, string)"
  IL_12c2:  brtrue     IL_216c
  IL_12c7:  br         IL_2184
  IL_12cc:  ldarg.0
  IL_12cd:  ldstr      "inodefs"
  IL_12d2:  call       "bool string.op_Equality(string, string)"
  IL_12d7:  brtrue     IL_216c
  IL_12dc:  br         IL_2184
  IL_12e1:  ldarg.0
  IL_12e2:  ldstr      "inotifyfs"
  IL_12e7:  call       "bool string.op_Equality(string, string)"
  IL_12ec:  brtrue     IL_216c
  IL_12f1:  br         IL_2184
  IL_12f6:  ldarg.0
  IL_12f7:  ldstr      "jbd"
  IL_12fc:  call       "bool string.op_Equality(string, string)"
  IL_1301:  brtrue     IL_216c
  IL_1306:  br         IL_2184
  IL_130b:  ldarg.0
  IL_130c:  ldstr      "jbd2"
  IL_1311:  call       "bool string.op_Equality(string, string)"
  IL_1316:  brtrue     IL_216c
  IL_131b:  br         IL_2184
  IL_1320:  ldarg.0
  IL_1321:  ldstr      "jffs"
  IL_1326:  call       "bool string.op_Equality(string, string)"
  IL_132b:  brtrue     IL_216c
  IL_1330:  br         IL_2184
  IL_1335:  ldarg.0
  IL_1336:  ldstr      "jffs2"
  IL_133b:  call       "bool string.op_Equality(string, string)"
  IL_1340:  brtrue     IL_216c
  IL_1345:  br         IL_2184
  IL_134a:  ldarg.0
  IL_134b:  ldstr      "jfs"
  IL_1350:  call       "bool string.op_Equality(string, string)"
  IL_1355:  brtrue     IL_216c
  IL_135a:  br         IL_2184
  IL_135f:  ldarg.0
  IL_1360:  ldstr      "lofs"
  IL_1365:  call       "bool string.op_Equality(string, string)"
  IL_136a:  brtrue     IL_216c
  IL_136f:  br         IL_2184
  IL_1374:  ldarg.0
  IL_1375:  ldstr      "logfs"
  IL_137a:  call       "bool string.op_Equality(string, string)"
  IL_137f:  brtrue     IL_216c
  IL_1384:  br         IL_2184
  IL_1389:  ldarg.0
  IL_138a:  ldstr      "lxfs"
  IL_138f:  call       "bool string.op_Equality(string, string)"
  IL_1394:  brtrue     IL_216c
  IL_1399:  br         IL_2184
  IL_139e:  ldarg.0
  IL_139f:  ldstr      "minix (30 char.)"
  IL_13a4:  call       "bool string.op_Equality(string, string)"
  IL_13a9:  brtrue     IL_216c
  IL_13ae:  br         IL_2184
  IL_13b3:  ldarg.0
  IL_13b4:  ldstr      "minix v2 (30 char.)"
  IL_13b9:  call       "bool string.op_Equality(string, string)"
  IL_13be:  brtrue     IL_216c
  IL_13c3:  br         IL_2184
  IL_13c8:  ldarg.0
  IL_13c9:  ldstr      "minix v2"
  IL_13ce:  call       "bool string.op_Equality(string, string)"
  IL_13d3:  brtrue     IL_216c
  IL_13d8:  br         IL_2184
  IL_13dd:  ldarg.0
  IL_13de:  ldstr      "minix"
  IL_13e3:  call       "bool string.op_Equality(string, string)"
  IL_13e8:  brtrue     IL_216c
  IL_13ed:  br         IL_2184
  IL_13f2:  ldarg.0
  IL_13f3:  ldstr      "minix_old"
  IL_13f8:  call       "bool string.op_Equality(string, string)"
  IL_13fd:  brtrue     IL_216c
  IL_1402:  br         IL_2184
  IL_1407:  ldarg.0
  IL_1408:  ldstr      "minix2"
  IL_140d:  call       "bool string.op_Equality(string, string)"
  IL_1412:  brtrue     IL_216c
  IL_1417:  br         IL_2184
  IL_141c:  ldarg.0
  IL_141d:  ldstr      "minix2v2"
  IL_1422:  call       "bool string.op_Equality(string, string)"
  IL_1427:  brtrue     IL_216c
  IL_142c:  br         IL_2184
  IL_1431:  ldarg.0
  IL_1432:  ldstr      "minix2 v2"
  IL_1437:  call       "bool string.op_Equality(string, string)"
  IL_143c:  brtrue     IL_216c
  IL_1441:  br         IL_2184
  IL_1446:  ldarg.0
  IL_1447:  ldstr      "minix3"
  IL_144c:  call       "bool string.op_Equality(string, string)"
  IL_1451:  brtrue     IL_216c
  IL_1456:  br         IL_2184
  IL_145b:  ldarg.0
  IL_145c:  ldstr      "mlfs"
  IL_1461:  call       "bool string.op_Equality(string, string)"
  IL_1466:  brtrue     IL_216c
  IL_146b:  br         IL_2184
  IL_1470:  ldarg.0
  IL_1471:  ldstr      "msdos"
  IL_1476:  call       "bool string.op_Equality(string, string)"
  IL_147b:  brtrue     IL_216c
  IL_1480:  br         IL_2184
  IL_1485:  ldarg.0
  IL_1486:  ldstr      "nilfs"
  IL_148b:  call       "bool string.op_Equality(string, string)"
  IL_1490:  brtrue     IL_216c
  IL_1495:  br         IL_2184
  IL_149a:  ldarg.0
  IL_149b:  ldstr      "nsfs"
  IL_14a0:  call       "bool string.op_Equality(string, string)"
  IL_14a5:  brtrue     IL_216c
  IL_14aa:  br         IL_2184
  IL_14af:  ldarg.0
  IL_14b0:  ldstr      "ntfs"
  IL_14b5:  call       "bool string.op_Equality(string, string)"
  IL_14ba:  brtrue     IL_216c
  IL_14bf:  br         IL_2184
  IL_14c4:  ldarg.0
  IL_14c5:  ldstr      "ntfs-3g"
  IL_14ca:  call       "bool string.op_Equality(string, string)"
  IL_14cf:  brtrue     IL_216c
  IL_14d4:  br         IL_2184
  IL_14d9:  ldarg.0
  IL_14da:  ldstr      "ocfs2"
  IL_14df:  call       "bool string.op_Equality(string, string)"
  IL_14e4:  brtrue     IL_216c
  IL_14e9:  br         IL_2184
  IL_14ee:  ldarg.0
  IL_14ef:  ldstr      "omfs"
  IL_14f4:  call       "bool string.op_Equality(string, string)"
  IL_14f9:  brtrue     IL_216c
  IL_14fe:  br         IL_2184
  IL_1503:  ldarg.0
  IL_1504:  ldstr      "overlay"
  IL_1509:  call       "bool string.op_Equality(string, string)"
  IL_150e:  brtrue     IL_216c
  IL_1513:  br         IL_2184
  IL_1518:  ldarg.0
  IL_1519:  ldstr      "overlayfs"
  IL_151e:  call       "bool string.op_Equality(string, string)"
  IL_1523:  brtrue     IL_216c
  IL_1528:  br         IL_2184
  IL_152d:  ldarg.0
  IL_152e:  ldstr      "pstorefs"
  IL_1533:  call       "bool string.op_Equality(string, string)"
  IL_1538:  brtrue     IL_216c
  IL_153d:  br         IL_2184
  IL_1542:  ldarg.0
  IL_1543:  ldstr      "qnx4"
  IL_1548:  call       "bool string.op_Equality(string, string)"
  IL_154d:  brtrue     IL_216c
  IL_1552:  br         IL_2184
  IL_1557:  ldarg.0
  IL_1558:  ldstr      "qnx6"
  IL_155d:  call       "bool string.op_Equality(string, string)"
  IL_1562:  brtrue     IL_216c
  IL_1567:  br         IL_2184
  IL_156c:  ldarg.0
  IL_156d:  ldstr      "reiserfs"
  IL_1572:  call       "bool string.op_Equality(string, string)"
  IL_1577:  brtrue     IL_216c
  IL_157c:  br         IL_2184
  IL_1581:  ldarg.0
  IL_1582:  ldstr      "rpc_pipefs"
  IL_1587:  call       "bool string.op_Equality(string, string)"
  IL_158c:  brtrue     IL_216c
  IL_1591:  br         IL_2184
  IL_1596:  ldarg.0
  IL_1597:  ldstr      "sffs"
  IL_159c:  call       "bool string.op_Equality(string, string)"
  IL_15a1:  brtrue     IL_216c
  IL_15a6:  br         IL_2184
  IL_15ab:  ldarg.0
  IL_15ac:  ldstr      "smackfs"
  IL_15b1:  call       "bool string.op_Equality(string, string)"
  IL_15b6:  brtrue     IL_216c
  IL_15bb:  br         IL_2184
  IL_15c0:  ldarg.0
  IL_15c1:  ldstr      "squashfs"
  IL_15c6:  call       "bool string.op_Equality(string, string)"
  IL_15cb:  brtrue     IL_216c
  IL_15d0:  br         IL_2184
  IL_15d5:  ldarg.0
  IL_15d6:  ldstr      "swap"
  IL_15db:  call       "bool string.op_Equality(string, string)"
  IL_15e0:  brtrue     IL_216c
  IL_15e5:  br         IL_2184
  IL_15ea:  ldarg.0
  IL_15eb:  ldstr      "sysv"
  IL_15f0:  call       "bool string.op_Equality(string, string)"
  IL_15f5:  brtrue     IL_216c
  IL_15fa:  br         IL_2184
  IL_15ff:  ldarg.0
  IL_1600:  ldstr      "sysv2"
  IL_1605:  call       "bool string.op_Equality(string, string)"
  IL_160a:  brtrue     IL_216c
  IL_160f:  br         IL_2184
  IL_1614:  ldarg.0
  IL_1615:  ldstr      "sysv4"
  IL_161a:  call       "bool string.op_Equality(string, string)"
  IL_161f:  brtrue     IL_216c
  IL_1624:  br         IL_2184
  IL_1629:  ldarg.0
  IL_162a:  ldstr      "tracefs"
  IL_162f:  call       "bool string.op_Equality(string, string)"
  IL_1634:  brtrue     IL_216c
  IL_1639:  br         IL_2184
  IL_163e:  ldarg.0
  IL_163f:  ldstr      "ubifs"
  IL_1644:  call       "bool string.op_Equality(string, string)"
  IL_1649:  brtrue     IL_216c
  IL_164e:  br         IL_2184
  IL_1653:  ldarg.0
  IL_1654:  ldstr      "ufs"
  IL_1659:  call       "bool string.op_Equality(string, string)"
  IL_165e:  brtrue     IL_216c
  IL_1663:  br         IL_2184
  IL_1668:  ldarg.0
  IL_1669:  ldstr      "ufscigam"
  IL_166e:  call       "bool string.op_Equality(string, string)"
  IL_1673:  brtrue     IL_216c
  IL_1678:  br         IL_2184
  IL_167d:  ldarg.0
  IL_167e:  ldstr      "ufs2"
  IL_1683:  call       "bool string.op_Equality(string, string)"
  IL_1688:  brtrue     IL_216c
  IL_168d:  br         IL_2184
  IL_1692:  ldarg.0
  IL_1693:  ldstr      "umsdos"
  IL_1698:  call       "bool string.op_Equality(string, string)"
  IL_169d:  brtrue     IL_216c
  IL_16a2:  br         IL_2184
  IL_16a7:  ldarg.0
  IL_16a8:  ldstr      "umview-mod-umfuseext2"
  IL_16ad:  call       "bool string.op_Equality(string, string)"
  IL_16b2:  brtrue     IL_216c
  IL_16b7:  br         IL_2184
  IL_16bc:  ldarg.0
  IL_16bd:  ldstr      "v9fs"
  IL_16c2:  call       "bool string.op_Equality(string, string)"
  IL_16c7:  brtrue     IL_216c
  IL_16cc:  br         IL_2184
  IL_16d1:  ldarg.0
  IL_16d2:  ldstr      "vagrant"
  IL_16d7:  call       "bool string.op_Equality(string, string)"
  IL_16dc:  brtrue     IL_216c
  IL_16e1:  br         IL_2184
  IL_16e6:  ldarg.0
  IL_16e7:  ldstr      "vboxfs"
  IL_16ec:  call       "bool string.op_Equality(string, string)"
  IL_16f1:  brtrue     IL_216c
  IL_16f6:  br         IL_2184
  IL_16fb:  ldarg.0
  IL_16fc:  ldstr      "vxfs"
  IL_1701:  call       "bool string.op_Equality(string, string)"
  IL_1706:  brtrue     IL_216c
  IL_170b:  br         IL_2184
  IL_1710:  ldarg.0
  IL_1711:  ldstr      "vxfs_olt"
  IL_1716:  call       "bool string.op_Equality(string, string)"
  IL_171b:  brtrue     IL_216c
  IL_1720:  br         IL_2184
  IL_1725:  ldarg.0
  IL_1726:  ldstr      "vzfs"
  IL_172b:  call       "bool string.op_Equality(string, string)"
  IL_1730:  brtrue     IL_216c
  IL_1735:  br         IL_2184
  IL_173a:  ldarg.0
  IL_173b:  ldstr      "wslfs"
  IL_1740:  call       "bool string.op_Equality(string, string)"
  IL_1745:  brtrue     IL_216c
  IL_174a:  br         IL_2184
  IL_174f:  ldarg.0
  IL_1750:  ldstr      "xenix"
  IL_1755:  call       "bool string.op_Equality(string, string)"
  IL_175a:  brtrue     IL_216c
  IL_175f:  br         IL_2184
  IL_1764:  ldarg.0
  IL_1765:  ldstr      "xfs"
  IL_176a:  call       "bool string.op_Equality(string, string)"
  IL_176f:  brtrue     IL_216c
  IL_1774:  br         IL_2184
  IL_1779:  ldarg.0
  IL_177a:  ldstr      "xia"
  IL_177f:  call       "bool string.op_Equality(string, string)"
  IL_1784:  brtrue     IL_216c
  IL_1789:  br         IL_2184
  IL_178e:  ldarg.0
  IL_178f:  ldstr      "xiafs"
  IL_1794:  call       "bool string.op_Equality(string, string)"
  IL_1799:  brtrue     IL_216c
  IL_179e:  br         IL_2184
  IL_17a3:  ldarg.0
  IL_17a4:  ldstr      "xmount"
  IL_17a9:  call       "bool string.op_Equality(string, string)"
  IL_17ae:  brtrue     IL_216c
  IL_17b3:  br         IL_2184
  IL_17b8:  ldarg.0
  IL_17b9:  ldstr      "zfs"
  IL_17be:  call       "bool string.op_Equality(string, string)"
  IL_17c3:  brtrue     IL_216c
  IL_17c8:  br         IL_2184
  IL_17cd:  ldarg.0
  IL_17ce:  ldstr      "zfs-fuse"
  IL_17d3:  call       "bool string.op_Equality(string, string)"
  IL_17d8:  brtrue     IL_216c
  IL_17dd:  br         IL_2184
  IL_17e2:  ldarg.0
  IL_17e3:  ldstr      "zsmallocfs"
  IL_17e8:  call       "bool string.op_Equality(string, string)"
  IL_17ed:  brtrue     IL_216c
  IL_17f2:  br         IL_2184
  IL_17f7:  ldarg.0
  IL_17f8:  ldstr      "9p"
  IL_17fd:  call       "bool string.op_Equality(string, string)"
  IL_1802:  brtrue     IL_2172
  IL_1807:  br         IL_2184
  IL_180c:  ldarg.0
  IL_180d:  ldstr      "acfs"
  IL_1812:  call       "bool string.op_Equality(string, string)"
  IL_1817:  brtrue     IL_2172
  IL_181c:  br         IL_2184
  IL_1821:  ldarg.0
  IL_1822:  ldstr      "afp"
  IL_1827:  call       "bool string.op_Equality(string, string)"
  IL_182c:  brtrue     IL_2172
  IL_1831:  br         IL_2184
  IL_1836:  ldarg.0
  IL_1837:  ldstr      "afpfs"
  IL_183c:  call       "bool string.op_Equality(string, string)"
  IL_1841:  brtrue     IL_2172
  IL_1846:  br         IL_2184
  IL_184b:  ldarg.0
  IL_184c:  ldstr      "afs"
  IL_1851:  call       "bool string.op_Equality(string, string)"
  IL_1856:  brtrue     IL_2172
  IL_185b:  br         IL_2184
  IL_1860:  ldarg.0
  IL_1861:  ldstr      "aufs"
  IL_1866:  call       "bool string.op_Equality(string, string)"
  IL_186b:  brtrue     IL_2172
  IL_1870:  br         IL_2184
  IL_1875:  ldarg.0
  IL_1876:  ldstr      "autofs"
  IL_187b:  call       "bool string.op_Equality(string, string)"
  IL_1880:  brtrue     IL_2172
  IL_1885:  br         IL_2184
  IL_188a:  ldarg.0
  IL_188b:  ldstr      "autofs4"
  IL_1890:  call       "bool string.op_Equality(string, string)"
  IL_1895:  brtrue     IL_2172
  IL_189a:  br         IL_2184
  IL_189f:  ldarg.0
  IL_18a0:  ldstr      "beaglefs"
  IL_18a5:  call       "bool string.op_Equality(string, string)"
  IL_18aa:  brtrue     IL_2172
  IL_18af:  br         IL_2184
  IL_18b4:  ldarg.0
  IL_18b5:  ldstr      "ceph"
  IL_18ba:  call       "bool string.op_Equality(string, string)"
  IL_18bf:  brtrue     IL_2172
  IL_18c4:  br         IL_2184
  IL_18c9:  ldarg.0
  IL_18ca:  ldstr      "cifs"
  IL_18cf:  call       "bool string.op_Equality(string, string)"
  IL_18d4:  brtrue     IL_2172
  IL_18d9:  br         IL_2184
  IL_18de:  ldarg.0
  IL_18df:  ldstr      "coda"
  IL_18e4:  call       "bool string.op_Equality(string, string)"
  IL_18e9:  brtrue     IL_2172
  IL_18ee:  br         IL_2184
  IL_18f3:  ldarg.0
  IL_18f4:  ldstr      "coherent"
  IL_18f9:  call       "bool string.op_Equality(string, string)"
  IL_18fe:  brtrue     IL_2172
  IL_1903:  br         IL_2184
  IL_1908:  ldarg.0
  IL_1909:  ldstr      "curlftpfs"
  IL_190e:  call       "bool string.op_Equality(string, string)"
  IL_1913:  brtrue     IL_2172
  IL_1918:  br         IL_2184
  IL_191d:  ldarg.0
  IL_191e:  ldstr      "davfs2"
  IL_1923:  call       "bool string.op_Equality(string, string)"
  IL_1928:  brtrue     IL_2172
  IL_192d:  br         IL_2184
  IL_1932:  ldarg.0
  IL_1933:  ldstr      "dlm"
  IL_1938:  call       "bool string.op_Equality(string, string)"
  IL_193d:  brtrue     IL_2172
  IL_1942:  br         IL_2184
  IL_1947:  ldarg.0
  IL_1948:  ldstr      "ecryptfs"
  IL_194d:  call       "bool string.op_Equality(string, string)"
  IL_1952:  brtrue     IL_2172
  IL_1957:  br         IL_2184
  IL_195c:  ldarg.0
  IL_195d:  ldstr      "eCryptfs"
  IL_1962:  call       "bool string.op_Equality(string, string)"
  IL_1967:  brtrue     IL_2172
  IL_196c:  br         IL_2184
  IL_1971:  ldarg.0
  IL_1972:  ldstr      "fhgfs"
  IL_1977:  call       "bool string.op_Equality(string, string)"
  IL_197c:  brtrue     IL_2172
  IL_1981:  br         IL_2184
  IL_1986:  ldarg.0
  IL_1987:  ldstr      "flickrfs"
  IL_198c:  call       "bool string.op_Equality(string, string)"
  IL_1991:  brtrue     IL_2172
  IL_1996:  br         IL_2184
  IL_199b:  ldarg.0
  IL_199c:  ldstr      "ftp"
  IL_19a1:  call       "bool string.op_Equality(string, string)"
  IL_19a6:  brtrue     IL_2172
  IL_19ab:  br         IL_2184
  IL_19b0:  ldarg.0
  IL_19b1:  ldstr      "fuse"
  IL_19b6:  call       "bool string.op_Equality(string, string)"
  IL_19bb:  brtrue     IL_2172
  IL_19c0:  br         IL_2184
  IL_19c5:  ldarg.0
  IL_19c6:  ldstr      "fuseblk"
  IL_19cb:  call       "bool string.op_Equality(string, string)"
  IL_19d0:  brtrue     IL_2172
  IL_19d5:  br         IL_2184
  IL_19da:  ldarg.0
  IL_19db:  ldstr      "fusedav"
  IL_19e0:  call       "bool string.op_Equality(string, string)"
  IL_19e5:  brtrue     IL_2172
  IL_19ea:  br         IL_2184
  IL_19ef:  ldarg.0
  IL_19f0:  ldstr      "fusesmb"
  IL_19f5:  call       "bool string.op_Equality(string, string)"
  IL_19fa:  brtrue     IL_2172
  IL_19ff:  br         IL_2184
  IL_1a04:  ldarg.0
  IL_1a05:  ldstr      "gfsgfs2"
  IL_1a0a:  call       "bool string.op_Equality(string, string)"
  IL_1a0f:  brtrue     IL_2172
  IL_1a14:  br         IL_2184
  IL_1a19:  ldarg.0
  IL_1a1a:  ldstr      "gfs/gfs2"
  IL_1a1f:  call       "bool string.op_Equality(string, string)"
  IL_1a24:  brtrue     IL_2172
  IL_1a29:  br         IL_2184
  IL_1a2e:  ldarg.0
  IL_1a2f:  ldstr      "gfs2"
  IL_1a34:  call       "bool string.op_Equality(string, string)"
  IL_1a39:  brtrue     IL_2172
  IL_1a3e:  br         IL_2184
  IL_1a43:  ldarg.0
  IL_1a44:  ldstr      "glusterfs-client"
  IL_1a49:  call       "bool string.op_Equality(string, string)"
  IL_1a4e:  brtrue     IL_2172
  IL_1a53:  br         IL_2184
  IL_1a58:  ldarg.0
  IL_1a59:  ldstr      "gmailfs"
  IL_1a5e:  call       "bool string.op_Equality(string, string)"
  IL_1a63:  brtrue     IL_2172
  IL_1a68:  br         IL_2184
  IL_1a6d:  ldarg.0
  IL_1a6e:  ldstr      "gpfs"
  IL_1a73:  call       "bool string.op_Equality(string, string)"
  IL_1a78:  brtrue     IL_2172
  IL_1a7d:  br         IL_2184
  IL_1a82:  ldarg.0
  IL_1a83:  ldstr      "ibrix"
  IL_1a88:  call       "bool string.op_Equality(string, string)"
  IL_1a8d:  brtrue     IL_2172
  IL_1a92:  br         IL_2184
  IL_1a97:  ldarg.0
  IL_1a98:  ldstr      "k-afs"
  IL_1a9d:  call       "bool string.op_Equality(string, string)"
  IL_1aa2:  brtrue     IL_2172
  IL_1aa7:  br         IL_2184
  IL_1aac:  ldarg.0
  IL_1aad:  ldstr      "kafs"
  IL_1ab2:  call       "bool string.op_Equality(string, string)"
  IL_1ab7:  brtrue     IL_2172
  IL_1abc:  br         IL_2184
  IL_1ac1:  ldarg.0
  IL_1ac2:  ldstr      "kbfuse"
  IL_1ac7:  call       "bool string.op_Equality(string, string)"
  IL_1acc:  brtrue     IL_2172
  IL_1ad1:  br         IL_2184
  IL_1ad6:  ldarg.0
  IL_1ad7:  ldstr      "ltspfs"
  IL_1adc:  call       "bool string.op_Equality(string, string)"
  IL_1ae1:  brtrue     IL_2172
  IL_1ae6:  br         IL_2184
  IL_1aeb:  ldarg.0
  IL_1aec:  ldstr      "lustre"
  IL_1af1:  call       "bool string.op_Equality(string, string)"
  IL_1af6:  brtrue     IL_2172
  IL_1afb:  br         IL_2184
  IL_1b00:  ldarg.0
  IL_1b01:  ldstr      "ncp"
  IL_1b06:  call       "bool string.op_Equality(string, string)"
  IL_1b0b:  brtrue     IL_2172
  IL_1b10:  br         IL_2184
  IL_1b15:  ldarg.0
  IL_1b16:  ldstr      "ncpfs"
  IL_1b1b:  call       "bool string.op_Equality(string, string)"
  IL_1b20:  brtrue     IL_2172
  IL_1b25:  br         IL_2184
  IL_1b2a:  ldarg.0
  IL_1b2b:  ldstr      "nfs"
  IL_1b30:  call       "bool string.op_Equality(string, string)"
  IL_1b35:  brtrue     IL_2172
  IL_1b3a:  br         IL_2184
  IL_1b3f:  ldarg.0
  IL_1b40:  ldstr      "nfs4"
  IL_1b45:  call       "bool string.op_Equality(string, string)"
  IL_1b4a:  brtrue     IL_2172
  IL_1b4f:  br         IL_2184
  IL_1b54:  ldarg.0
  IL_1b55:  ldstr      "nfsd"
  IL_1b5a:  call       "bool string.op_Equality(string, string)"
  IL_1b5f:  brtrue     IL_2172
  IL_1b64:  br         IL_2184
  IL_1b69:  ldarg.0
  IL_1b6a:  ldstr      "novell"
  IL_1b6f:  call       "bool string.op_Equality(string, string)"
  IL_1b74:  brtrue     IL_2172
  IL_1b79:  br         IL_2184
  IL_1b7e:  ldarg.0
  IL_1b7f:  ldstr      "obexfs"
  IL_1b84:  call       "bool string.op_Equality(string, string)"
  IL_1b89:  brtrue     IL_2172
  IL_1b8e:  br         IL_2184
  IL_1b93:  ldarg.0
  IL_1b94:  ldstr      "panfs"
  IL_1b99:  call       "bool string.op_Equality(string, string)"
  IL_1b9e:  brtrue     IL_2172
  IL_1ba3:  br         IL_2184
  IL_1ba8:  ldarg.0
  IL_1ba9:  ldstr      "prl_fs"
  IL_1bae:  call       "bool string.op_Equality(string, string)"
  IL_1bb3:  brtrue     IL_2172
  IL_1bb8:  br         IL_2184
  IL_1bbd:  ldarg.0
  IL_1bbe:  ldstr      "s3ql"
  IL_1bc3:  call       "bool string.op_Equality(string, string)"
  IL_1bc8:  brtrue     IL_2172
  IL_1bcd:  br         IL_2184
  IL_1bd2:  ldarg.0
  IL_1bd3:  ldstr      "samba"
  IL_1bd8:  call       "bool string.op_Equality(string, string)"
  IL_1bdd:  brtrue     IL_2172
  IL_1be2:  br         IL_2184
  IL_1be7:  ldarg.0
  IL_1be8:  ldstr      "smb"
  IL_1bed:  call       "bool string.op_Equality(string, string)"
  IL_1bf2:  brtrue     IL_2172
  IL_1bf7:  br         IL_2184
  IL_1bfc:  ldarg.0
  IL_1bfd:  ldstr      "smb2"
  IL_1c02:  call       "bool string.op_Equality(string, string)"
  IL_1c07:  brtrue     IL_2172
  IL_1c0c:  br         IL_2184
  IL_1c11:  ldarg.0
  IL_1c12:  ldstr      "smbfs"
  IL_1c17:  call       "bool string.op_Equality(string, string)"
  IL_1c1c:  brtrue     IL_2172
  IL_1c21:  br         IL_2184
  IL_1c26:  ldarg.0
  IL_1c27:  ldstr      "snfs"
  IL_1c2c:  call       "bool string.op_Equality(string, string)"
  IL_1c31:  brtrue     IL_2172
  IL_1c36:  br         IL_2184
  IL_1c3b:  ldarg.0
  IL_1c3c:  ldstr      "sshfs"
  IL_1c41:  call       "bool string.op_Equality(string, string)"
  IL_1c46:  brtrue     IL_2172
  IL_1c4b:  br         IL_2184
  IL_1c50:  ldarg.0
  IL_1c51:  ldstr      "vmhgfs"
  IL_1c56:  call       "bool string.op_Equality(string, string)"
  IL_1c5b:  brtrue     IL_2172
  IL_1c60:  br         IL_2184
  IL_1c65:  ldarg.0
  IL_1c66:  ldstr      "webdav"
  IL_1c6b:  call       "bool string.op_Equality(string, string)"
  IL_1c70:  brtrue     IL_2172
  IL_1c75:  br         IL_2184
  IL_1c7a:  ldarg.0
  IL_1c7b:  ldstr      "wikipediafs"
  IL_1c80:  call       "bool string.op_Equality(string, string)"
  IL_1c85:  brtrue     IL_2172
  IL_1c8a:  br         IL_2184
  IL_1c8f:  ldarg.0
  IL_1c90:  ldstr      "xenfs"
  IL_1c95:  call       "bool string.op_Equality(string, string)"
  IL_1c9a:  brtrue     IL_2172
  IL_1c9f:  br         IL_2184
  IL_1ca4:  ldarg.0
  IL_1ca5:  ldstr      "anon_inode"
  IL_1caa:  call       "bool string.op_Equality(string, string)"
  IL_1caf:  brtrue     IL_2178
  IL_1cb4:  br         IL_2184
  IL_1cb9:  ldarg.0
  IL_1cba:  ldstr      "anon_inodefs"
  IL_1cbf:  call       "bool string.op_Equality(string, string)"
  IL_1cc4:  brtrue     IL_2178
  IL_1cc9:  br         IL_2184
  IL_1cce:  ldarg.0
  IL_1ccf:  ldstr      "aptfs"
  IL_1cd4:  call       "bool string.op_Equality(string, string)"
  IL_1cd9:  brtrue     IL_2178
  IL_1cde:  br         IL_2184
  IL_1ce3:  ldarg.0
  IL_1ce4:  ldstr      "avfs"
  IL_1ce9:  call       "bool string.op_Equality(string, string)"
  IL_1cee:  brtrue     IL_2178
  IL_1cf3:  br         IL_2184
  IL_1cf8:  ldarg.0
  IL_1cf9:  ldstr      "bdev"
  IL_1cfe:  call       "bool string.op_Equality(string, string)"
  IL_1d03:  brtrue     IL_2178
  IL_1d08:  br         IL_2184
  IL_1d0d:  ldarg.0
  IL_1d0e:  ldstr      "binfmt_misc"
  IL_1d13:  call       "bool string.op_Equality(string, string)"
  IL_1d18:  brtrue     IL_2178
  IL_1d1d:  br         IL_2184
  IL_1d22:  ldarg.0
  IL_1d23:  ldstr      "cgroup"
  IL_1d28:  call       "bool string.op_Equality(string, string)"
  IL_1d2d:  brtrue     IL_2178
  IL_1d32:  br         IL_2184
  IL_1d37:  ldarg.0
  IL_1d38:  ldstr      "cgroupfs"
  IL_1d3d:  call       "bool string.op_Equality(string, string)"
  IL_1d42:  brtrue     IL_2178
  IL_1d47:  br         IL_2184
  IL_1d4c:  ldarg.0
  IL_1d4d:  ldstr      "cgroup2fs"
  IL_1d52:  call       "bool string.op_Equality(string, string)"
  IL_1d57:  brtrue     IL_2178
  IL_1d5c:  br         IL_2184
  IL_1d61:  ldarg.0
  IL_1d62:  ldstr      "configfs"
  IL_1d67:  call       "bool string.op_Equality(string, string)"
  IL_1d6c:  brtrue     IL_2178
  IL_1d71:  br         IL_2184
  IL_1d76:  ldarg.0
  IL_1d77:  ldstr      "cpuset"
  IL_1d7c:  call       "bool string.op_Equality(string, string)"
  IL_1d81:  brtrue     IL_2178
  IL_1d86:  br         IL_2184
  IL_1d8b:  ldarg.0
  IL_1d8c:  ldstr      "cramfs"
  IL_1d91:  call       "bool string.op_Equality(string, string)"
  IL_1d96:  brtrue     IL_2178
  IL_1d9b:  br         IL_2184
  IL_1da0:  ldarg.0
  IL_1da1:  ldstr      "cramfs-wend"
  IL_1da6:  call       "bool string.op_Equality(string, string)"
  IL_1dab:  brtrue     IL_2178
  IL_1db0:  br         IL_2184
  IL_1db5:  ldarg.0
  IL_1db6:  ldstr      "cryptkeeper"
  IL_1dbb:  call       "bool string.op_Equality(string, string)"
  IL_1dc0:  brtrue     IL_2178
  IL_1dc5:  br         IL_2184
  IL_1dca:  ldarg.0
  IL_1dcb:  ldstr      "ctfs"
  IL_1dd0:  call       "bool string.op_Equality(string, string)"
  IL_1dd5:  brtrue     IL_2178
  IL_1dda:  br         IL_2184
  IL_1ddf:  ldarg.0
  IL_1de0:  ldstr      "debugfs"
  IL_1de5:  call       "bool string.op_Equality(string, string)"
  IL_1dea:  brtrue     IL_2178
  IL_1def:  br         IL_2184
  IL_1df4:  ldarg.0
  IL_1df5:  ldstr      "dev"
  IL_1dfa:  call       "bool string.op_Equality(string, string)"
  IL_1dff:  brtrue     IL_2178
  IL_1e04:  br         IL_2184
  IL_1e09:  ldarg.0
  IL_1e0a:  ldstr      "devfs"
  IL_1e0f:  call       "bool string.op_Equality(string, string)"
  IL_1e14:  brtrue     IL_2178
  IL_1e19:  br         IL_2184
  IL_1e1e:  ldarg.0
  IL_1e1f:  ldstr      "devpts"
  IL_1e24:  call       "bool string.op_Equality(string, string)"
  IL_1e29:  brtrue     IL_2178
  IL_1e2e:  br         IL_2184
  IL_1e33:  ldarg.0
  IL_1e34:  ldstr      "devtmpfs"
  IL_1e39:  call       "bool string.op_Equality(string, string)"
  IL_1e3e:  brtrue     IL_2178
  IL_1e43:  br         IL_2184
  IL_1e48:  ldarg.0
  IL_1e49:  ldstr      "encfs"
  IL_1e4e:  call       "bool string.op_Equality(string, string)"
  IL_1e53:  brtrue     IL_2178
  IL_1e58:  br         IL_2184
  IL_1e5d:  ldarg.0
  IL_1e5e:  ldstr      "fd"
  IL_1e63:  call       "bool string.op_Equality(string, string)"
  IL_1e68:  brtrue     IL_2178
  IL_1e6d:  br         IL_2184
  IL_1e72:  ldarg.0
  IL_1e73:  ldstr      "fdesc"
  IL_1e78:  call       "bool string.op_Equality(string, string)"
  IL_1e7d:  brtrue     IL_2178
  IL_1e82:  br         IL_2184
  IL_1e87:  ldarg.0
  IL_1e88:  ldstr      "fuse.gvfsd-fuse"
  IL_1e8d:  call       "bool string.op_Equality(string, string)"
  IL_1e92:  brtrue     IL_2178
  IL_1e97:  br         IL_2184
  IL_1e9c:  ldarg.0
  IL_1e9d:  ldstr      "fusectl"
  IL_1ea2:  call       "bool string.op_Equality(string, string)"
  IL_1ea7:  brtrue     IL_2178
  IL_1eac:  br         IL_2184
  IL_1eb1:  ldarg.0
  IL_1eb2:  ldstr      "futexfs"
  IL_1eb7:  call       "bool string.op_Equality(string, string)"
  IL_1ebc:  brtrue     IL_2178
  IL_1ec1:  br         IL_2184
  IL_1ec6:  ldarg.0
  IL_1ec7:  ldstr      "hugetlbfs"
  IL_1ecc:  call       "bool string.op_Equality(string, string)"
  IL_1ed1:  brtrue     IL_2178
  IL_1ed6:  br         IL_2184
  IL_1edb:  ldarg.0
  IL_1edc:  ldstr      "libpam-encfs"
  IL_1ee1:  call       "bool string.op_Equality(string, string)"
  IL_1ee6:  brtrue     IL_2178
  IL_1eeb:  br         IL_2184
  IL_1ef0:  ldarg.0
  IL_1ef1:  ldstr      "ibpam-mount"
  IL_1ef6:  call       "bool string.op_Equality(string, string)"
  IL_1efb:  brtrue     IL_2178
  IL_1f00:  br         IL_2184
  IL_1f05:  ldarg.0
  IL_1f06:  ldstr      "mntfs"
  IL_1f0b:  call       "bool string.op_Equality(string, string)"
  IL_1f10:  brtrue     IL_2178
  IL_1f15:  br         IL_2184
  IL_1f1a:  ldarg.0
  IL_1f1b:  ldstr      "mqueue"
  IL_1f20:  call       "bool string.op_Equality(string, string)"
  IL_1f25:  brtrue     IL_2178
  IL_1f2a:  br         IL_2184
  IL_1f2f:  ldarg.0
  IL_1f30:  ldstr      "mtpfs"
  IL_1f35:  call       "bool string.op_Equality(string, string)"
  IL_1f3a:  brtrue     IL_2178
  IL_1f3f:  br         IL_2184
  IL_1f44:  ldarg.0
  IL_1f45:  ldstr      "mythtvfs"
  IL_1f4a:  call       "bool string.op_Equality(string, string)"
  IL_1f4f:  brtrue     IL_2178
  IL_1f54:  br         IL_2184
  IL_1f59:  ldarg.0
  IL_1f5a:  ldstr      "objfs"
  IL_1f5f:  call       "bool string.op_Equality(string, string)"
  IL_1f64:  brtrue     IL_2178
  IL_1f69:  br         IL_2184
  IL_1f6e:  ldarg.0
  IL_1f6f:  ldstr      "openprom"
  IL_1f74:  call       "bool string.op_Equality(string, string)"
  IL_1f79:  brtrue     IL_2178
  IL_1f7e:  br         IL_2184
  IL_1f83:  ldarg.0
  IL_1f84:  ldstr      "openpromfs"
  IL_1f89:  call       "bool string.op_Equality(string, string)"
  IL_1f8e:  brtrue     IL_2178
  IL_1f93:  br         IL_2184
  IL_1f98:  ldarg.0
  IL_1f99:  ldstr      "pipefs"
  IL_1f9e:  call       "bool string.op_Equality(string, string)"
  IL_1fa3:  brtrue     IL_2178
  IL_1fa8:  br         IL_2184
  IL_1fad:  ldarg.0
  IL_1fae:  ldstr      "plptools"
  IL_1fb3:  call       "bool string.op_Equality(string, string)"
  IL_1fb8:  brtrue     IL_2178
  IL_1fbd:  br         IL_2184
  IL_1fc2:  ldarg.0
  IL_1fc3:  ldstr      "proc"
  IL_1fc8:  call       "bool string.op_Equality(string, string)"
  IL_1fcd:  brtrue     IL_2178
  IL_1fd2:  br         IL_2184
  IL_1fd7:  ldarg.0
  IL_1fd8:  ldstr      "pstore"
  IL_1fdd:  call       "bool string.op_Equality(string, string)"
  IL_1fe2:  brtrue     IL_2178
  IL_1fe7:  br         IL_2184
  IL_1fec:  ldarg.0
  IL_1fed:  ldstr      "pytagsfs"
  IL_1ff2:  call       "bool string.op_Equality(string, string)"
  IL_1ff7:  brtrue     IL_2178
  IL_1ffc:  br         IL_2184
  IL_2001:  ldarg.0
  IL_2002:  ldstr      "ramfs"
  IL_2007:  call       "bool string.op_Equality(string, string)"
  IL_200c:  brtrue     IL_2178
  IL_2011:  br         IL_2184
  IL_2016:  ldarg.0
  IL_2017:  ldstr      "rofs"
  IL_201c:  call       "bool string.op_Equality(string, string)"
  IL_2021:  brtrue     IL_2178
  IL_2026:  br         IL_2184
  IL_202b:  ldarg.0
  IL_202c:  ldstr      "romfs"
  IL_2031:  call       "bool string.op_Equality(string, string)"
  IL_2036:  brtrue     IL_2178
  IL_203b:  br         IL_2184
  IL_2040:  ldarg.0
  IL_2041:  ldstr      "rootfs"
  IL_2046:  call       "bool string.op_Equality(string, string)"
  IL_204b:  brtrue     IL_2178
  IL_2050:  br         IL_2184
  IL_2055:  ldarg.0
  IL_2056:  ldstr      "securityfs"
  IL_205b:  call       "bool string.op_Equality(string, string)"
  IL_2060:  brtrue     IL_2178
  IL_2065:  br         IL_2184
  IL_206a:  ldarg.0
  IL_206b:  ldstr      "selinux"
  IL_2070:  call       "bool string.op_Equality(string, string)"
  IL_2075:  brtrue     IL_2178
  IL_207a:  br         IL_2184
  IL_207f:  ldarg.0
  IL_2080:  ldstr      "selinuxfs"
  IL_2085:  call       "bool string.op_Equality(string, string)"
  IL_208a:  brtrue     IL_2178
  IL_208f:  br         IL_2184
  IL_2094:  ldarg.0
  IL_2095:  ldstr      "sharefs"
  IL_209a:  call       "bool string.op_Equality(string, string)"
  IL_209f:  brtrue     IL_2178
  IL_20a4:  br         IL_2184
  IL_20a9:  ldarg.0
  IL_20aa:  ldstr      "sockfs"
  IL_20af:  call       "bool string.op_Equality(string, string)"
  IL_20b4:  brtrue     IL_2178
  IL_20b9:  br         IL_2184
  IL_20be:  ldarg.0
  IL_20bf:  ldstr      "sysfs"
  IL_20c4:  call       "bool string.op_Equality(string, string)"
  IL_20c9:  brtrue     IL_2178
  IL_20ce:  br         IL_2184
  IL_20d3:  ldarg.0
  IL_20d4:  ldstr      "tmpfs"
  IL_20d9:  call       "bool string.op_Equality(string, string)"
  IL_20de:  brtrue     IL_2178
  IL_20e3:  br         IL_2184
  IL_20e8:  ldarg.0
  IL_20e9:  ldstr      "udev"
  IL_20ee:  call       "bool string.op_Equality(string, string)"
  IL_20f3:  brtrue     IL_2178
  IL_20f8:  br         IL_2184
  IL_20fd:  ldarg.0
  IL_20fe:  ldstr      "usbdev"
  IL_2103:  call       "bool string.op_Equality(string, string)"
  IL_2108:  brtrue.s   IL_2178
  IL_210a:  br.s       IL_2184
  IL_210c:  ldarg.0
  IL_210d:  ldstr      "usbdevfs"
  IL_2112:  call       "bool string.op_Equality(string, string)"
  IL_2117:  brtrue.s   IL_2178
  IL_2119:  br.s       IL_2184
  IL_211b:  ldarg.0
  IL_211c:  ldstr      "gphotofs"
  IL_2121:  call       "bool string.op_Equality(string, string)"
  IL_2126:  brtrue.s   IL_217e
  IL_2128:  br.s       IL_2184
  IL_212a:  ldarg.0
  IL_212b:  ldstr      "sdcardfs"
  IL_2130:  call       "bool string.op_Equality(string, string)"
  IL_2135:  brtrue.s   IL_217e
  IL_2137:  br.s       IL_2184
  IL_2139:  ldarg.0
  IL_213a:  ldstr      "usbfs"
  IL_213f:  call       "bool string.op_Equality(string, string)"
  IL_2144:  brtrue.s   IL_217e
  IL_2146:  br.s       IL_2184
  IL_2148:  ldarg.0
  IL_2149:  ldstr      "usbdevice"
  IL_214e:  call       "bool string.op_Equality(string, string)"
  IL_2153:  brtrue.s   IL_217e
  IL_2155:  br.s       IL_2184
  IL_2157:  ldarg.0
  IL_2158:  ldstr      "vfat"
  IL_215d:  call       "bool string.op_Equality(string, string)"
  IL_2162:  brtrue.s   IL_217e
  IL_2164:  br.s       IL_2184
  IL_2166:  ldstr      "cddafs"
  IL_216b:  ret
  IL_216c:  ldstr      "aafs"
  IL_2171:  ret
  IL_2172:  ldstr      "9p"
  IL_2177:  ret
  IL_2178:  ldstr      "anon_inode"
  IL_217d:  ret
  IL_217e:  ldstr      "gphotofs"
  IL_2183:  ret
  IL_2184:  ldstr      "default"
  IL_2189:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void GetContents()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Formats.Asn1/src/System/Formats/Asn1/WellKnownOids.cs#L317-L419
        // Buckets: 5, 2, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 4, 2, 2, 1, 1, 3, 2, 2, 1, 3, 3, 2, 2, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 2, 1, 1, 2, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1
        var source = """
assert("1.2.840.113549.1.5.13");
assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "1.2.840.10040.4.1" => "1.2.840.10040.4.1",
            "1.2.840.10040.4.3" => "1.2.840.10040.4.3",
            "1.2.840.10045.2.1" => "1.2.840.10045.2.1",
            "1.2.840.10045.1.1" => "1.2.840.10045.1.1",
            "1.2.840.10045.1.2" => "1.2.840.10045.1.2",
            "1.2.840.10045.3.1.7" => "1.2.840.10045.3.1.7",
            "1.2.840.10045.4.1" => "1.2.840.10045.4.1",
            "1.2.840.10045.4.3.2" => "1.2.840.10045.4.3.2",
            "1.2.840.10045.4.3.3" => "1.2.840.10045.4.3.3",
            "1.2.840.10045.4.3.4" => "1.2.840.10045.4.3.4",
            "1.2.840.113549.1.1.1" => "1.2.840.113549.1.1.1",
            "1.2.840.113549.1.1.5" => "1.2.840.113549.1.1.5",
            "1.2.840.113549.1.1.7" => "1.2.840.113549.1.1.7",
            "1.2.840.113549.1.1.8" => "1.2.840.113549.1.1.8",
            "1.2.840.113549.1.1.9" => "1.2.840.113549.1.1.9",
            "1.2.840.113549.1.1.10" => "1.2.840.113549.1.1.10",
            "1.2.840.113549.1.1.11" => "1.2.840.113549.1.1.11",
            "1.2.840.113549.1.1.12" => "1.2.840.113549.1.1.12",
            "1.2.840.113549.1.1.13" => "1.2.840.113549.1.1.13",
            "1.2.840.113549.1.5.3" => "1.2.840.113549.1.5.3",
            "1.2.840.113549.1.5.10" => "1.2.840.113549.1.5.10",
            "1.2.840.113549.1.5.11" => "1.2.840.113549.1.5.11",
            "1.2.840.113549.1.5.12" => "1.2.840.113549.1.5.12",
            "1.2.840.113549.1.5.13" => "1.2.840.113549.1.5.13",
            "1.2.840.113549.1.7.1" => "1.2.840.113549.1.7.1",
            "1.2.840.113549.1.7.2" => "1.2.840.113549.1.7.2",
            "1.2.840.113549.1.7.3" => "1.2.840.113549.1.7.3",
            "1.2.840.113549.1.7.6" => "1.2.840.113549.1.7.6",
            "1.2.840.113549.1.9.1" => "1.2.840.113549.1.9.1",
            "1.2.840.113549.1.9.3" => "1.2.840.113549.1.9.3",
            "1.2.840.113549.1.9.4" => "1.2.840.113549.1.9.4",
            "1.2.840.113549.1.9.5" => "1.2.840.113549.1.9.5",
            "1.2.840.113549.1.9.6" => "1.2.840.113549.1.9.6",
            "1.2.840.113549.1.9.7" => "1.2.840.113549.1.9.7",
            "1.2.840.113549.1.9.14" => "1.2.840.113549.1.9.14",
            "1.2.840.113549.1.9.15" => "1.2.840.113549.1.9.15",
            "1.2.840.113549.1.9.16.1.4" => "1.2.840.113549.1.9.16.1.4",
            "1.2.840.113549.1.9.16.2.12" => "1.2.840.113549.1.9.16.2.12",
            "1.2.840.113549.1.9.16.2.14" => "1.2.840.113549.1.9.16.2.14",
            "1.2.840.113549.1.9.16.2.47" => "1.2.840.113549.1.9.16.2.47",
            "1.2.840.113549.1.9.20" => "1.2.840.113549.1.9.20",
            "1.2.840.113549.1.9.21" => "1.2.840.113549.1.9.21",
            "1.2.840.113549.1.9.22.1" => "1.2.840.113549.1.9.22.1",
            "1.2.840.113549.1.12.1.3" => "1.2.840.113549.1.12.1.3",
            "1.2.840.113549.1.12.1.5" => "1.2.840.113549.1.12.1.5",
            "1.2.840.113549.1.12.1.6" => "1.2.840.113549.1.12.1.6",
            "1.2.840.113549.1.12.10.1.1" => "1.2.840.113549.1.12.10.1.1",
            "1.2.840.113549.1.12.10.1.2" => "1.2.840.113549.1.12.10.1.2",
            "1.2.840.113549.1.12.10.1.3" => "1.2.840.113549.1.12.10.1.3",
            "1.2.840.113549.1.12.10.1.5" => "1.2.840.113549.1.12.10.1.5",
            "1.2.840.113549.1.12.10.1.6" => "1.2.840.113549.1.12.10.1.6",
            "1.2.840.113549.2.5" => "1.2.840.113549.2.5",
            "1.2.840.113549.2.7" => "1.2.840.113549.2.7",
            "1.2.840.113549.2.9" => "1.2.840.113549.2.9",
            "1.2.840.113549.2.10" => "1.2.840.113549.2.10",
            "1.2.840.113549.2.11" => "1.2.840.113549.2.11",
            "1.2.840.113549.3.2" => "1.2.840.113549.3.2",
            "1.2.840.113549.3.7" => "1.2.840.113549.3.7",
            "1.3.6.1.4.1.311.17.1" => "1.3.6.1.4.1.311.17.1",
            "1.3.6.1.4.1.311.17.3.20" => "1.3.6.1.4.1.311.17.3.20",
            "1.3.6.1.4.1.311.20.2.3" => "1.3.6.1.4.1.311.20.2.3",
            "1.3.6.1.4.1.311.88.2.1" => "1.3.6.1.4.1.311.88.2.1",
            "1.3.6.1.4.1.311.88.2.2" => "1.3.6.1.4.1.311.88.2.2",
            "1.3.6.1.5.5.7.3.1" => "1.3.6.1.5.5.7.3.1",
            "1.3.6.1.5.5.7.3.2" => "1.3.6.1.5.5.7.3.2",
            "1.3.6.1.5.5.7.3.3" => "1.3.6.1.5.5.7.3.3",
            "1.3.6.1.5.5.7.3.4" => "1.3.6.1.5.5.7.3.4",
            "1.3.6.1.5.5.7.3.8" => "1.3.6.1.5.5.7.3.8",
            "1.3.6.1.5.5.7.3.9" => "1.3.6.1.5.5.7.3.9",
            "1.3.6.1.5.5.7.6.2" => "1.3.6.1.5.5.7.6.2",
            "1.3.6.1.5.5.7.48.1" => "1.3.6.1.5.5.7.48.1",
            "1.3.6.1.5.5.7.48.1.2" => "1.3.6.1.5.5.7.48.1.2",
            "1.3.6.1.5.5.7.48.2" => "1.3.6.1.5.5.7.48.2",
            "1.3.14.3.2.26" => "1.3.14.3.2.26",
            "1.3.14.3.2.7" => "1.3.14.3.2.7",
            "1.3.132.0.34" => "1.3.132.0.34",
            "1.3.132.0.35" => "1.3.132.0.35",
            "2.5.4.3" => "2.5.4.3",
            "2.5.4.5" => "2.5.4.5",
            "2.5.4.6" => "2.5.4.6",
            "2.5.4.7" => "2.5.4.7",
            "2.5.4.8" => "2.5.4.8",
            "2.5.4.10" => "2.5.4.10",
            "2.5.4.11" => "2.5.4.11",
            "2.5.4.97" => "2.5.4.97",
            "2.5.29.14" => "2.5.29.14",
            "2.5.29.15" => "2.5.29.15",
            "2.5.29.17" => "2.5.29.17",
            "2.5.29.19" => "2.5.29.19",
            "2.5.29.20" => "2.5.29.20",
            "2.5.29.35" => "2.5.29.35",
            "2.16.840.1.101.3.4.1.2" => "2.16.840.1.101.3.4.1.2",
            "2.16.840.1.101.3.4.1.22" => "2.16.840.1.101.3.4.1.22",
            "2.16.840.1.101.3.4.1.42" => "2.16.840.1.101.3.4.1.42",
            "2.16.840.1.101.3.4.2.1" => "2.16.840.1.101.3.4.2.1",
            "2.16.840.1.101.3.4.2.2" => "2.16.840.1.101.3.4.2.2",
            "2.16.840.1.101.3.4.2.3" => "2.16.840.1.101.3.4.2.3",
            "2.23.140.1.2.1" => "2.23.140.1.2.1",
            "2.23.140.1.2.2" => "2.23.140.1.2.2",
            _ => "default",
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     3704 (0xe78)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0e70
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.7
  IL_000f:  sub
  IL_0010:  switch    (
        IL_0234,
        IL_0262,
        IL_0287,
        IL_0e70,
        IL_0e70,
        IL_020d,
        IL_08c0,
        IL_02bd,
        IL_0e70,
        IL_0e70,
        IL_006a,
        IL_01ac,
        IL_00a5,
        IL_00dc,
        IL_0117,
        IL_01e7,
        IL_0179,
        IL_0e70,
        IL_0672,
        IL_0146)
  IL_0065:  br         IL_0e70
  IL_006a:  ldarg.0
  IL_006b:  ldc.i4.s   16
  IL_006d:  call       "char string.this[int].get"
  IL_0072:  stloc.2
  IL_0073:  ldloc.2
  IL_0074:  ldc.i4.s   49
  IL_0076:  sub
  IL_0077:  switch    (
        IL_02db,
        IL_0355,
        IL_0330,
        IL_038a,
        IL_0e70,
        IL_0e70,
        IL_0e70,
        IL_039f,
        IL_03b4)
  IL_00a0:  br         IL_0e70
  IL_00a5:  ldarg.0
  IL_00a6:  ldc.i4.s   18
  IL_00a8:  call       "char string.this[int].get"
  IL_00ad:  stloc.2
  IL_00ae:  ldloc.2
  IL_00af:  ldc.i4.s   48
  IL_00b1:  sub
  IL_00b2:  switch    (
        IL_041d,
        IL_0432,
        IL_03de,
        IL_03f3,
        IL_0408,
        IL_0e70,
        IL_0e70,
        IL_03c9)
  IL_00d7:  br         IL_0e70
  IL_00dc:  ldarg.0
  IL_00dd:  ldc.i4.s   19
  IL_00df:  call       "char string.this[int].get"
  IL_00e4:  stloc.2
  IL_00e5:  ldloc.2
  IL_00e6:  ldc.i4.s   49
  IL_00e8:  sub
  IL_00e9:  switch    (
        IL_0447,
        IL_0535,
        IL_0500,
        IL_057f,
        IL_048c,
        IL_055a,
        IL_04b1,
        IL_04d6,
        IL_04eb)
  IL_0112:  br         IL_0e70
  IL_0117:  ldarg.0
  IL_0118:  ldc.i4.s   20
  IL_011a:  call       "char string.this[int].get"
  IL_011f:  stloc.2
  IL_0120:  ldloc.2
  IL_0121:  ldc.i4.s   48
  IL_0123:  sub
  IL_0124:  switch    (
        IL_0594,
        IL_05c9,
        IL_05fe,
        IL_0623,
        IL_0648,
        IL_065d)
  IL_0141:  br         IL_0e70
  IL_0146:  ldarg.0
  IL_0147:  ldc.i4.s   25
  IL_0149:  call       "char string.this[int].get"
  IL_014e:  stloc.2
  IL_014f:  ldloc.2
  IL_0150:  ldc.i4.s   49
  IL_0152:  sub
  IL_0153:  switch    (
        IL_06d6,
        IL_0687,
        IL_06eb,
        IL_06ac,
        IL_0700,
        IL_0715,
        IL_06c1)
  IL_0174:  br         IL_0e70
  IL_0179:  ldarg.0
  IL_017a:  ldc.i4.s   22
  IL_017c:  call       "char string.this[int].get"
  IL_0181:  stloc.2
  IL_0182:  ldloc.2
  IL_0183:  ldc.i4.s   48
  IL_0185:  sub
  IL_0186:  switch    (
        IL_077e,
        IL_072a,
        IL_0793,
        IL_073f,
        IL_0e70,
        IL_0754,
        IL_0769)
  IL_01a7:  br         IL_0e70
  IL_01ac:  ldarg.0
  IL_01ad:  ldc.i4.s   17
  IL_01af:  call       "char string.this[int].get"
  IL_01b4:  stloc.2
  IL_01b5:  ldloc.2
  IL_01b6:  ldc.i4.s   49
  IL_01b8:  sub
  IL_01b9:  switch    (
        IL_082c,
        IL_0807,
        IL_0e70,
        IL_0e70,
        IL_07b8,
        IL_0e70,
        IL_07cd,
        IL_0e70,
        IL_07f2)
  IL_01e2:  br         IL_0e70
  IL_01e7:  ldarg.0
  IL_01e8:  ldc.i4.s   16
  IL_01ea:  call       "char string.this[int].get"
  IL_01ef:  stloc.2
  IL_01f0:  ldloc.2
  IL_01f1:  ldc.i4.s   46
  IL_01f3:  beq        IL_087b
  IL_01f8:  ldloc.2
  IL_01f9:  ldc.i4.s   50
  IL_01fb:  beq        IL_0841
  IL_0200:  ldloc.2
  IL_0201:  ldc.i4.s   56
  IL_0203:  beq        IL_0856
  IL_0208:  br         IL_0e70
  IL_020d:  ldarg.0
  IL_020e:  ldc.i4.s   11
  IL_0210:  call       "char string.this[int].get"
  IL_0215:  stloc.2
  IL_0216:  ldloc.2
  IL_0217:  ldc.i4.s   52
  IL_0219:  sub
  IL_021a:  switch    (
        IL_08ea,
        IL_08ff,
        IL_0e70,
        IL_08d5)
  IL_022f:  br         IL_0e70
  IL_0234:  ldarg.0
  IL_0235:  ldc.i4.6
  IL_0236:  call       "char string.this[int].get"
  IL_023b:  stloc.2
  IL_023c:  ldloc.2
  IL_023d:  ldc.i4.s   51
  IL_023f:  sub
  IL_0240:  switch    (
        IL_0914,
        IL_0e70,
        IL_0929,
        IL_093e,
        IL_0953,
        IL_0968)
  IL_025d:  br         IL_0e70
  IL_0262:  ldarg.0
  IL_0263:  ldc.i4.7
  IL_0264:  call       "char string.this[int].get"
  IL_0269:  stloc.2
  IL_026a:  ldloc.2
  IL_026b:  ldc.i4.s   48
  IL_026d:  beq        IL_097d
  IL_0272:  ldloc.2
  IL_0273:  ldc.i4.s   49
  IL_0275:  beq        IL_0992
  IL_027a:  ldloc.2
  IL_027b:  ldc.i4.s   55
  IL_027d:  beq        IL_09a7
  IL_0282:  br         IL_0e70
  IL_0287:  ldarg.0
  IL_0288:  ldc.i4.8
  IL_0289:  call       "char string.this[int].get"
  IL_028e:  stloc.2
  IL_028f:  ldloc.2
  IL_0290:  ldc.i4.s   48
  IL_0292:  beq        IL_0a20
  IL_0297:  ldloc.2
  IL_0298:  ldc.i4.s   52
  IL_029a:  sub
  IL_029b:  switch    (
        IL_09bc,
        IL_09d1,
        IL_0e70,
        IL_09f6,
        IL_0e70,
        IL_0a0b)
  IL_02b8:  br         IL_0e70
  IL_02bd:  ldarg.0
  IL_02be:  ldc.i4.s   13
  IL_02c0:  call       "char string.this[int].get"
  IL_02c5:  stloc.2
  IL_02c6:  ldloc.2
  IL_02c7:  ldc.i4.s   49
  IL_02c9:  beq        IL_0a35
  IL_02ce:  ldloc.2
  IL_02cf:  ldc.i4.s   50
  IL_02d1:  beq        IL_0a4a
  IL_02d6:  br         IL_0e70
  IL_02db:  ldarg.0
  IL_02dc:  ldstr      "1.2.840.10040.4.1"
  IL_02e1:  call       "bool string.op_Equality(string, string)"
  IL_02e6:  brtrue     IL_0a5f
  IL_02eb:  ldarg.0
  IL_02ec:  ldstr      "1.2.840.10045.2.1"
  IL_02f1:  call       "bool string.op_Equality(string, string)"
  IL_02f6:  brtrue     IL_0a75
  IL_02fb:  ldarg.0
  IL_02fc:  ldstr      "1.2.840.10045.1.1"
  IL_0301:  call       "bool string.op_Equality(string, string)"
  IL_0306:  brtrue     IL_0a80
  IL_030b:  ldarg.0
  IL_030c:  ldstr      "1.2.840.10045.4.1"
  IL_0311:  call       "bool string.op_Equality(string, string)"
  IL_0316:  brtrue     IL_0aa1
  IL_031b:  ldarg.0
  IL_031c:  ldstr      "1.3.6.1.5.5.7.3.1"
  IL_0321:  call       "bool string.op_Equality(string, string)"
  IL_0326:  brtrue     IL_0d14
  IL_032b:  br         IL_0e70
  IL_0330:  ldarg.0
  IL_0331:  ldstr      "1.2.840.10040.4.3"
  IL_0336:  call       "bool string.op_Equality(string, string)"
  IL_033b:  brtrue     IL_0a6a
  IL_0340:  ldarg.0
  IL_0341:  ldstr      "1.3.6.1.5.5.7.3.3"
  IL_0346:  call       "bool string.op_Equality(string, string)"
  IL_034b:  brtrue     IL_0d2a
  IL_0350:  br         IL_0e70
  IL_0355:  ldarg.0
  IL_0356:  ldstr      "1.2.840.10045.1.2"
  IL_035b:  call       "bool string.op_Equality(string, string)"
  IL_0360:  brtrue     IL_0a8b
  IL_0365:  ldarg.0
  IL_0366:  ldstr      "1.3.6.1.5.5.7.3.2"
  IL_036b:  call       "bool string.op_Equality(string, string)"
  IL_0370:  brtrue     IL_0d1f
  IL_0375:  ldarg.0
  IL_0376:  ldstr      "1.3.6.1.5.5.7.6.2"
  IL_037b:  call       "bool string.op_Equality(string, string)"
  IL_0380:  brtrue     IL_0d56
  IL_0385:  br         IL_0e70
  IL_038a:  ldarg.0
  IL_038b:  ldstr      "1.3.6.1.5.5.7.3.4"
  IL_0390:  call       "bool string.op_Equality(string, string)"
  IL_0395:  brtrue     IL_0d35
  IL_039a:  br         IL_0e70
  IL_039f:  ldarg.0
  IL_03a0:  ldstr      "1.3.6.1.5.5.7.3.8"
  IL_03a5:  call       "bool string.op_Equality(string, string)"
  IL_03aa:  brtrue     IL_0d40
  IL_03af:  br         IL_0e70
  IL_03b4:  ldarg.0
  IL_03b5:  ldstr      "1.3.6.1.5.5.7.3.9"
  IL_03ba:  call       "bool string.op_Equality(string, string)"
  IL_03bf:  brtrue     IL_0d4b
  IL_03c4:  br         IL_0e70
  IL_03c9:  ldarg.0
  IL_03ca:  ldstr      "1.2.840.10045.3.1.7"
  IL_03cf:  call       "bool string.op_Equality(string, string)"
  IL_03d4:  brtrue     IL_0a96
  IL_03d9:  br         IL_0e70
  IL_03de:  ldarg.0
  IL_03df:  ldstr      "1.2.840.10045.4.3.2"
  IL_03e4:  call       "bool string.op_Equality(string, string)"
  IL_03e9:  brtrue     IL_0aac
  IL_03ee:  br         IL_0e70
  IL_03f3:  ldarg.0
  IL_03f4:  ldstr      "1.2.840.10045.4.3.3"
  IL_03f9:  call       "bool string.op_Equality(string, string)"
  IL_03fe:  brtrue     IL_0ab7
  IL_0403:  br         IL_0e70
  IL_0408:  ldarg.0
  IL_0409:  ldstr      "1.2.840.10045.4.3.4"
  IL_040e:  call       "bool string.op_Equality(string, string)"
  IL_0413:  brtrue     IL_0ac2
  IL_0418:  br         IL_0e70
  IL_041d:  ldarg.0
  IL_041e:  ldstr      "1.2.840.113549.2.10"
  IL_0423:  call       "bool string.op_Equality(string, string)"
  IL_0428:  brtrue     IL_0cb1
  IL_042d:  br         IL_0e70
  IL_0432:  ldarg.0
  IL_0433:  ldstr      "1.2.840.113549.2.11"
  IL_0438:  call       "bool string.op_Equality(string, string)"
  IL_043d:  brtrue     IL_0cbc
  IL_0442:  br         IL_0e70
  IL_0447:  ldarg.0
  IL_0448:  ldstr      "1.2.840.113549.1.1.1"
  IL_044d:  call       "bool string.op_Equality(string, string)"
  IL_0452:  brtrue     IL_0acd
  IL_0457:  ldarg.0
  IL_0458:  ldstr      "1.2.840.113549.1.7.1"
  IL_045d:  call       "bool string.op_Equality(string, string)"
  IL_0462:  brtrue     IL_0b67
  IL_0467:  ldarg.0
  IL_0468:  ldstr      "1.2.840.113549.1.9.1"
  IL_046d:  call       "bool string.op_Equality(string, string)"
  IL_0472:  brtrue     IL_0b93
  IL_0477:  ldarg.0
  IL_0478:  ldstr      "1.3.6.1.4.1.311.17.1"
  IL_047d:  call       "bool string.op_Equality(string, string)"
  IL_0482:  brtrue     IL_0cdd
  IL_0487:  br         IL_0e70
  IL_048c:  ldarg.0
  IL_048d:  ldstr      "1.2.840.113549.1.1.5"
  IL_0492:  call       "bool string.op_Equality(string, string)"
  IL_0497:  brtrue     IL_0ad8
  IL_049c:  ldarg.0
  IL_049d:  ldstr      "1.2.840.113549.1.9.5"
  IL_04a2:  call       "bool string.op_Equality(string, string)"
  IL_04a7:  brtrue     IL_0bb4
  IL_04ac:  br         IL_0e70
  IL_04b1:  ldarg.0
  IL_04b2:  ldstr      "1.2.840.113549.1.1.7"
  IL_04b7:  call       "bool string.op_Equality(string, string)"
  IL_04bc:  brtrue     IL_0ae3
  IL_04c1:  ldarg.0
  IL_04c2:  ldstr      "1.2.840.113549.1.9.7"
  IL_04c7:  call       "bool string.op_Equality(string, string)"
  IL_04cc:  brtrue     IL_0bca
  IL_04d1:  br         IL_0e70
  IL_04d6:  ldarg.0
  IL_04d7:  ldstr      "1.2.840.113549.1.1.8"
  IL_04dc:  call       "bool string.op_Equality(string, string)"
  IL_04e1:  brtrue     IL_0aee
  IL_04e6:  br         IL_0e70
  IL_04eb:  ldarg.0
  IL_04ec:  ldstr      "1.2.840.113549.1.1.9"
  IL_04f1:  call       "bool string.op_Equality(string, string)"
  IL_04f6:  brtrue     IL_0af9
  IL_04fb:  br         IL_0e70
  IL_0500:  ldarg.0
  IL_0501:  ldstr      "1.2.840.113549.1.5.3"
  IL_0506:  call       "bool string.op_Equality(string, string)"
  IL_050b:  brtrue     IL_0b30
  IL_0510:  ldarg.0
  IL_0511:  ldstr      "1.2.840.113549.1.7.3"
  IL_0516:  call       "bool string.op_Equality(string, string)"
  IL_051b:  brtrue     IL_0b7d
  IL_0520:  ldarg.0
  IL_0521:  ldstr      "1.2.840.113549.1.9.3"
  IL_0526:  call       "bool string.op_Equality(string, string)"
  IL_052b:  brtrue     IL_0b9e
  IL_0530:  br         IL_0e70
  IL_0535:  ldarg.0
  IL_0536:  ldstr      "1.2.840.113549.1.7.2"
  IL_053b:  call       "bool string.op_Equality(string, string)"
  IL_0540:  brtrue     IL_0b72
  IL_0545:  ldarg.0
  IL_0546:  ldstr      "1.3.6.1.5.5.7.48.1.2"
  IL_054b:  call       "bool string.op_Equality(string, string)"
  IL_0550:  brtrue     IL_0d6c
  IL_0555:  br         IL_0e70
  IL_055a:  ldarg.0
  IL_055b:  ldstr      "1.2.840.113549.1.7.6"
  IL_0560:  call       "bool string.op_Equality(string, string)"
  IL_0565:  brtrue     IL_0b88
  IL_056a:  ldarg.0
  IL_056b:  ldstr      "1.2.840.113549.1.9.6"
  IL_0570:  call       "bool string.op_Equality(string, string)"
  IL_0575:  brtrue     IL_0bbf
  IL_057a:  br         IL_0e70
  IL_057f:  ldarg.0
  IL_0580:  ldstr      "1.2.840.113549.1.9.4"
  IL_0585:  call       "bool string.op_Equality(string, string)"
  IL_058a:  brtrue     IL_0ba9
  IL_058f:  br         IL_0e70
  IL_0594:  ldarg.0
  IL_0595:  ldstr      "1.2.840.113549.1.1.10"
  IL_059a:  call       "bool string.op_Equality(string, string)"
  IL_059f:  brtrue     IL_0b04
  IL_05a4:  ldarg.0
  IL_05a5:  ldstr      "1.2.840.113549.1.5.10"
  IL_05aa:  call       "bool string.op_Equality(string, string)"
  IL_05af:  brtrue     IL_0b3b
  IL_05b4:  ldarg.0
  IL_05b5:  ldstr      "1.2.840.113549.1.9.20"
  IL_05ba:  call       "bool string.op_Equality(string, string)"
  IL_05bf:  brtrue     IL_0c17
  IL_05c4:  br         IL_0e70
  IL_05c9:  ldarg.0
  IL_05ca:  ldstr      "1.2.840.113549.1.1.11"
  IL_05cf:  call       "bool string.op_Equality(string, string)"
  IL_05d4:  brtrue     IL_0b0f
  IL_05d9:  ldarg.0
  IL_05da:  ldstr      "1.2.840.113549.1.5.11"
  IL_05df:  call       "bool string.op_Equality(string, string)"
  IL_05e4:  brtrue     IL_0b46
  IL_05e9:  ldarg.0
  IL_05ea:  ldstr      "1.2.840.113549.1.9.21"
  IL_05ef:  call       "bool string.op_Equality(string, string)"
  IL_05f4:  brtrue     IL_0c22
  IL_05f9:  br         IL_0e70
  IL_05fe:  ldarg.0
  IL_05ff:  ldstr      "1.2.840.113549.1.1.12"
  IL_0604:  call       "bool string.op_Equality(string, string)"
  IL_0609:  brtrue     IL_0b1a
  IL_060e:  ldarg.0
  IL_060f:  ldstr      "1.2.840.113549.1.5.12"
  IL_0614:  call       "bool string.op_Equality(string, string)"
  IL_0619:  brtrue     IL_0b51
  IL_061e:  br         IL_0e70
  IL_0623:  ldarg.0
  IL_0624:  ldstr      "1.2.840.113549.1.1.13"
  IL_0629:  call       "bool string.op_Equality(string, string)"
  IL_062e:  brtrue     IL_0b25
  IL_0633:  ldarg.0
  IL_0634:  ldstr      "1.2.840.113549.1.5.13"
  IL_0639:  call       "bool string.op_Equality(string, string)"
  IL_063e:  brtrue     IL_0b5c
  IL_0643:  br         IL_0e70
  IL_0648:  ldarg.0
  IL_0649:  ldstr      "1.2.840.113549.1.9.14"
  IL_064e:  call       "bool string.op_Equality(string, string)"
  IL_0653:  brtrue     IL_0bd5
  IL_0658:  br         IL_0e70
  IL_065d:  ldarg.0
  IL_065e:  ldstr      "1.2.840.113549.1.9.15"
  IL_0663:  call       "bool string.op_Equality(string, string)"
  IL_0668:  brtrue     IL_0be0
  IL_066d:  br         IL_0e70
  IL_0672:  ldarg.0
  IL_0673:  ldstr      "1.2.840.113549.1.9.16.1.4"
  IL_0678:  call       "bool string.op_Equality(string, string)"
  IL_067d:  brtrue     IL_0beb
  IL_0682:  br         IL_0e70
  IL_0687:  ldarg.0
  IL_0688:  ldstr      "1.2.840.113549.1.9.16.2.12"
  IL_068d:  call       "bool string.op_Equality(string, string)"
  IL_0692:  brtrue     IL_0bf6
  IL_0697:  ldarg.0
  IL_0698:  ldstr      "1.2.840.113549.1.12.10.1.2"
  IL_069d:  call       "bool string.op_Equality(string, string)"
  IL_06a2:  brtrue     IL_0c64
  IL_06a7:  br         IL_0e70
  IL_06ac:  ldarg.0
  IL_06ad:  ldstr      "1.2.840.113549.1.9.16.2.14"
  IL_06b2:  call       "bool string.op_Equality(string, string)"
  IL_06b7:  brtrue     IL_0c01
  IL_06bc:  br         IL_0e70
  IL_06c1:  ldarg.0
  IL_06c2:  ldstr      "1.2.840.113549.1.9.16.2.47"
  IL_06c7:  call       "bool string.op_Equality(string, string)"
  IL_06cc:  brtrue     IL_0c0c
  IL_06d1:  br         IL_0e70
  IL_06d6:  ldarg.0
  IL_06d7:  ldstr      "1.2.840.113549.1.12.10.1.1"
  IL_06dc:  call       "bool string.op_Equality(string, string)"
  IL_06e1:  brtrue     IL_0c59
  IL_06e6:  br         IL_0e70
  IL_06eb:  ldarg.0
  IL_06ec:  ldstr      "1.2.840.113549.1.12.10.1.3"
  IL_06f1:  call       "bool string.op_Equality(string, string)"
  IL_06f6:  brtrue     IL_0c6f
  IL_06fb:  br         IL_0e70
  IL_0700:  ldarg.0
  IL_0701:  ldstr      "1.2.840.113549.1.12.10.1.5"
  IL_0706:  call       "bool string.op_Equality(string, string)"
  IL_070b:  brtrue     IL_0c7a
  IL_0710:  br         IL_0e70
  IL_0715:  ldarg.0
  IL_0716:  ldstr      "1.2.840.113549.1.12.10.1.6"
  IL_071b:  call       "bool string.op_Equality(string, string)"
  IL_0720:  brtrue     IL_0c85
  IL_0725:  br         IL_0e70
  IL_072a:  ldarg.0
  IL_072b:  ldstr      "1.2.840.113549.1.9.22.1"
  IL_0730:  call       "bool string.op_Equality(string, string)"
  IL_0735:  brtrue     IL_0c2d
  IL_073a:  br         IL_0e70
  IL_073f:  ldarg.0
  IL_0740:  ldstr      "1.2.840.113549.1.12.1.3"
  IL_0745:  call       "bool string.op_Equality(string, string)"
  IL_074a:  brtrue     IL_0c38
  IL_074f:  br         IL_0e70
  IL_0754:  ldarg.0
  IL_0755:  ldstr      "1.2.840.113549.1.12.1.5"
  IL_075a:  call       "bool string.op_Equality(string, string)"
  IL_075f:  brtrue     IL_0c43
  IL_0764:  br         IL_0e70
  IL_0769:  ldarg.0
  IL_076a:  ldstr      "1.2.840.113549.1.12.1.6"
  IL_076f:  call       "bool string.op_Equality(string, string)"
  IL_0774:  brtrue     IL_0c4e
  IL_0779:  br         IL_0e70
  IL_077e:  ldarg.0
  IL_077f:  ldstr      "1.3.6.1.4.1.311.17.3.20"
  IL_0784:  call       "bool string.op_Equality(string, string)"
  IL_0789:  brtrue     IL_0ce8
  IL_078e:  br         IL_0e70
  IL_0793:  ldarg.0
  IL_0794:  ldstr      "2.16.840.1.101.3.4.1.22"
  IL_0799:  call       "bool string.op_Equality(string, string)"
  IL_079e:  brtrue     IL_0e38
  IL_07a3:  ldarg.0
  IL_07a4:  ldstr      "2.16.840.1.101.3.4.1.42"
  IL_07a9:  call       "bool string.op_Equality(string, string)"
  IL_07ae:  brtrue     IL_0e40
  IL_07b3:  br         IL_0e70
  IL_07b8:  ldarg.0
  IL_07b9:  ldstr      "1.2.840.113549.2.5"
  IL_07be:  call       "bool string.op_Equality(string, string)"
  IL_07c3:  brtrue     IL_0c90
  IL_07c8:  br         IL_0e70
  IL_07cd:  ldarg.0
  IL_07ce:  ldstr      "1.2.840.113549.2.7"
  IL_07d3:  call       "bool string.op_Equality(string, string)"
  IL_07d8:  brtrue     IL_0c9b
  IL_07dd:  ldarg.0
  IL_07de:  ldstr      "1.2.840.113549.3.7"
  IL_07e3:  call       "bool string.op_Equality(string, string)"
  IL_07e8:  brtrue     IL_0cd2
  IL_07ed:  br         IL_0e70
  IL_07f2:  ldarg.0
  IL_07f3:  ldstr      "1.2.840.113549.2.9"
  IL_07f8:  call       "bool string.op_Equality(string, string)"
  IL_07fd:  brtrue     IL_0ca6
  IL_0802:  br         IL_0e70
  IL_0807:  ldarg.0
  IL_0808:  ldstr      "1.2.840.113549.3.2"
  IL_080d:  call       "bool string.op_Equality(string, string)"
  IL_0812:  brtrue     IL_0cc7
  IL_0817:  ldarg.0
  IL_0818:  ldstr      "1.3.6.1.5.5.7.48.2"
  IL_081d:  call       "bool string.op_Equality(string, string)"
  IL_0822:  brtrue     IL_0d77
  IL_0827:  br         IL_0e70
  IL_082c:  ldarg.0
  IL_082d:  ldstr      "1.3.6.1.5.5.7.48.1"
  IL_0832:  call       "bool string.op_Equality(string, string)"
  IL_0837:  brtrue     IL_0d61
  IL_083c:  br         IL_0e70
  IL_0841:  ldarg.0
  IL_0842:  ldstr      "1.3.6.1.4.1.311.20.2.3"
  IL_0847:  call       "bool string.op_Equality(string, string)"
  IL_084c:  brtrue     IL_0cf3
  IL_0851:  br         IL_0e70
  IL_0856:  ldarg.0
  IL_0857:  ldstr      "1.3.6.1.4.1.311.88.2.1"
  IL_085c:  call       "bool string.op_Equality(string, string)"
  IL_0861:  brtrue     IL_0cfe
  IL_0866:  ldarg.0
  IL_0867:  ldstr      "1.3.6.1.4.1.311.88.2.2"
  IL_086c:  call       "bool string.op_Equality(string, string)"
  IL_0871:  brtrue     IL_0d09
  IL_0876:  br         IL_0e70
  IL_087b:  ldarg.0
  IL_087c:  ldstr      "2.16.840.1.101.3.4.1.2"
  IL_0881:  call       "bool string.op_Equality(string, string)"
  IL_0886:  brtrue     IL_0e30
  IL_088b:  ldarg.0
  IL_088c:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_0891:  call       "bool string.op_Equality(string, string)"
  IL_0896:  brtrue     IL_0e48
  IL_089b:  ldarg.0
  IL_089c:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_08a1:  call       "bool string.op_Equality(string, string)"
  IL_08a6:  brtrue     IL_0e50
  IL_08ab:  ldarg.0
  IL_08ac:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_08b1:  call       "bool string.op_Equality(string, string)"
  IL_08b6:  brtrue     IL_0e58
  IL_08bb:  br         IL_0e70
  IL_08c0:  ldarg.0
  IL_08c1:  ldstr      "1.3.14.3.2.26"
  IL_08c6:  call       "bool string.op_Equality(string, string)"
  IL_08cb:  brtrue     IL_0d82
  IL_08d0:  br         IL_0e70
  IL_08d5:  ldarg.0
  IL_08d6:  ldstr      "1.3.14.3.2.7"
  IL_08db:  call       "bool string.op_Equality(string, string)"
  IL_08e0:  brtrue     IL_0d8d
  IL_08e5:  br         IL_0e70
  IL_08ea:  ldarg.0
  IL_08eb:  ldstr      "1.3.132.0.34"
  IL_08f0:  call       "bool string.op_Equality(string, string)"
  IL_08f5:  brtrue     IL_0d98
  IL_08fa:  br         IL_0e70
  IL_08ff:  ldarg.0
  IL_0900:  ldstr      "1.3.132.0.35"
  IL_0905:  call       "bool string.op_Equality(string, string)"
  IL_090a:  brtrue     IL_0da3
  IL_090f:  br         IL_0e70
  IL_0914:  ldarg.0
  IL_0915:  ldstr      "2.5.4.3"
  IL_091a:  call       "bool string.op_Equality(string, string)"
  IL_091f:  brtrue     IL_0dae
  IL_0924:  br         IL_0e70
  IL_0929:  ldarg.0
  IL_092a:  ldstr      "2.5.4.5"
  IL_092f:  call       "bool string.op_Equality(string, string)"
  IL_0934:  brtrue     IL_0db9
  IL_0939:  br         IL_0e70
  IL_093e:  ldarg.0
  IL_093f:  ldstr      "2.5.4.6"
  IL_0944:  call       "bool string.op_Equality(string, string)"
  IL_0949:  brtrue     IL_0dc4
  IL_094e:  br         IL_0e70
  IL_0953:  ldarg.0
  IL_0954:  ldstr      "2.5.4.7"
  IL_0959:  call       "bool string.op_Equality(string, string)"
  IL_095e:  brtrue     IL_0dcf
  IL_0963:  br         IL_0e70
  IL_0968:  ldarg.0
  IL_0969:  ldstr      "2.5.4.8"
  IL_096e:  call       "bool string.op_Equality(string, string)"
  IL_0973:  brtrue     IL_0dda
  IL_0978:  br         IL_0e70
  IL_097d:  ldarg.0
  IL_097e:  ldstr      "2.5.4.10"
  IL_0983:  call       "bool string.op_Equality(string, string)"
  IL_0988:  brtrue     IL_0de5
  IL_098d:  br         IL_0e70
  IL_0992:  ldarg.0
  IL_0993:  ldstr      "2.5.4.11"
  IL_0998:  call       "bool string.op_Equality(string, string)"
  IL_099d:  brtrue     IL_0df0
  IL_09a2:  br         IL_0e70
  IL_09a7:  ldarg.0
  IL_09a8:  ldstr      "2.5.4.97"
  IL_09ad:  call       "bool string.op_Equality(string, string)"
  IL_09b2:  brtrue     IL_0df8
  IL_09b7:  br         IL_0e70
  IL_09bc:  ldarg.0
  IL_09bd:  ldstr      "2.5.29.14"
  IL_09c2:  call       "bool string.op_Equality(string, string)"
  IL_09c7:  brtrue     IL_0e00
  IL_09cc:  br         IL_0e70
  IL_09d1:  ldarg.0
  IL_09d2:  ldstr      "2.5.29.15"
  IL_09d7:  call       "bool string.op_Equality(string, string)"
  IL_09dc:  brtrue     IL_0e08
  IL_09e1:  ldarg.0
  IL_09e2:  ldstr      "2.5.29.35"
  IL_09e7:  call       "bool string.op_Equality(string, string)"
  IL_09ec:  brtrue     IL_0e28
  IL_09f1:  br         IL_0e70
  IL_09f6:  ldarg.0
  IL_09f7:  ldstr      "2.5.29.17"
  IL_09fc:  call       "bool string.op_Equality(string, string)"
  IL_0a01:  brtrue     IL_0e10
  IL_0a06:  br         IL_0e70
  IL_0a0b:  ldarg.0
  IL_0a0c:  ldstr      "2.5.29.19"
  IL_0a11:  call       "bool string.op_Equality(string, string)"
  IL_0a16:  brtrue     IL_0e18
  IL_0a1b:  br         IL_0e70
  IL_0a20:  ldarg.0
  IL_0a21:  ldstr      "2.5.29.20"
  IL_0a26:  call       "bool string.op_Equality(string, string)"
  IL_0a2b:  brtrue     IL_0e20
  IL_0a30:  br         IL_0e70
  IL_0a35:  ldarg.0
  IL_0a36:  ldstr      "2.23.140.1.2.1"
  IL_0a3b:  call       "bool string.op_Equality(string, string)"
  IL_0a40:  brtrue     IL_0e60
  IL_0a45:  br         IL_0e70
  IL_0a4a:  ldarg.0
  IL_0a4b:  ldstr      "2.23.140.1.2.2"
  IL_0a50:  call       "bool string.op_Equality(string, string)"
  IL_0a55:  brtrue     IL_0e68
  IL_0a5a:  br         IL_0e70
  IL_0a5f:  ldstr      "1.2.840.10040.4.1"
  IL_0a64:  stloc.0
  IL_0a65:  br         IL_0e76
  IL_0a6a:  ldstr      "1.2.840.10040.4.3"
  IL_0a6f:  stloc.0
  IL_0a70:  br         IL_0e76
  IL_0a75:  ldstr      "1.2.840.10045.2.1"
  IL_0a7a:  stloc.0
  IL_0a7b:  br         IL_0e76
  IL_0a80:  ldstr      "1.2.840.10045.1.1"
  IL_0a85:  stloc.0
  IL_0a86:  br         IL_0e76
  IL_0a8b:  ldstr      "1.2.840.10045.1.2"
  IL_0a90:  stloc.0
  IL_0a91:  br         IL_0e76
  IL_0a96:  ldstr      "1.2.840.10045.3.1.7"
  IL_0a9b:  stloc.0
  IL_0a9c:  br         IL_0e76
  IL_0aa1:  ldstr      "1.2.840.10045.4.1"
  IL_0aa6:  stloc.0
  IL_0aa7:  br         IL_0e76
  IL_0aac:  ldstr      "1.2.840.10045.4.3.2"
  IL_0ab1:  stloc.0
  IL_0ab2:  br         IL_0e76
  IL_0ab7:  ldstr      "1.2.840.10045.4.3.3"
  IL_0abc:  stloc.0
  IL_0abd:  br         IL_0e76
  IL_0ac2:  ldstr      "1.2.840.10045.4.3.4"
  IL_0ac7:  stloc.0
  IL_0ac8:  br         IL_0e76
  IL_0acd:  ldstr      "1.2.840.113549.1.1.1"
  IL_0ad2:  stloc.0
  IL_0ad3:  br         IL_0e76
  IL_0ad8:  ldstr      "1.2.840.113549.1.1.5"
  IL_0add:  stloc.0
  IL_0ade:  br         IL_0e76
  IL_0ae3:  ldstr      "1.2.840.113549.1.1.7"
  IL_0ae8:  stloc.0
  IL_0ae9:  br         IL_0e76
  IL_0aee:  ldstr      "1.2.840.113549.1.1.8"
  IL_0af3:  stloc.0
  IL_0af4:  br         IL_0e76
  IL_0af9:  ldstr      "1.2.840.113549.1.1.9"
  IL_0afe:  stloc.0
  IL_0aff:  br         IL_0e76
  IL_0b04:  ldstr      "1.2.840.113549.1.1.10"
  IL_0b09:  stloc.0
  IL_0b0a:  br         IL_0e76
  IL_0b0f:  ldstr      "1.2.840.113549.1.1.11"
  IL_0b14:  stloc.0
  IL_0b15:  br         IL_0e76
  IL_0b1a:  ldstr      "1.2.840.113549.1.1.12"
  IL_0b1f:  stloc.0
  IL_0b20:  br         IL_0e76
  IL_0b25:  ldstr      "1.2.840.113549.1.1.13"
  IL_0b2a:  stloc.0
  IL_0b2b:  br         IL_0e76
  IL_0b30:  ldstr      "1.2.840.113549.1.5.3"
  IL_0b35:  stloc.0
  IL_0b36:  br         IL_0e76
  IL_0b3b:  ldstr      "1.2.840.113549.1.5.10"
  IL_0b40:  stloc.0
  IL_0b41:  br         IL_0e76
  IL_0b46:  ldstr      "1.2.840.113549.1.5.11"
  IL_0b4b:  stloc.0
  IL_0b4c:  br         IL_0e76
  IL_0b51:  ldstr      "1.2.840.113549.1.5.12"
  IL_0b56:  stloc.0
  IL_0b57:  br         IL_0e76
  IL_0b5c:  ldstr      "1.2.840.113549.1.5.13"
  IL_0b61:  stloc.0
  IL_0b62:  br         IL_0e76
  IL_0b67:  ldstr      "1.2.840.113549.1.7.1"
  IL_0b6c:  stloc.0
  IL_0b6d:  br         IL_0e76
  IL_0b72:  ldstr      "1.2.840.113549.1.7.2"
  IL_0b77:  stloc.0
  IL_0b78:  br         IL_0e76
  IL_0b7d:  ldstr      "1.2.840.113549.1.7.3"
  IL_0b82:  stloc.0
  IL_0b83:  br         IL_0e76
  IL_0b88:  ldstr      "1.2.840.113549.1.7.6"
  IL_0b8d:  stloc.0
  IL_0b8e:  br         IL_0e76
  IL_0b93:  ldstr      "1.2.840.113549.1.9.1"
  IL_0b98:  stloc.0
  IL_0b99:  br         IL_0e76
  IL_0b9e:  ldstr      "1.2.840.113549.1.9.3"
  IL_0ba3:  stloc.0
  IL_0ba4:  br         IL_0e76
  IL_0ba9:  ldstr      "1.2.840.113549.1.9.4"
  IL_0bae:  stloc.0
  IL_0baf:  br         IL_0e76
  IL_0bb4:  ldstr      "1.2.840.113549.1.9.5"
  IL_0bb9:  stloc.0
  IL_0bba:  br         IL_0e76
  IL_0bbf:  ldstr      "1.2.840.113549.1.9.6"
  IL_0bc4:  stloc.0
  IL_0bc5:  br         IL_0e76
  IL_0bca:  ldstr      "1.2.840.113549.1.9.7"
  IL_0bcf:  stloc.0
  IL_0bd0:  br         IL_0e76
  IL_0bd5:  ldstr      "1.2.840.113549.1.9.14"
  IL_0bda:  stloc.0
  IL_0bdb:  br         IL_0e76
  IL_0be0:  ldstr      "1.2.840.113549.1.9.15"
  IL_0be5:  stloc.0
  IL_0be6:  br         IL_0e76
  IL_0beb:  ldstr      "1.2.840.113549.1.9.16.1.4"
  IL_0bf0:  stloc.0
  IL_0bf1:  br         IL_0e76
  IL_0bf6:  ldstr      "1.2.840.113549.1.9.16.2.12"
  IL_0bfb:  stloc.0
  IL_0bfc:  br         IL_0e76
  IL_0c01:  ldstr      "1.2.840.113549.1.9.16.2.14"
  IL_0c06:  stloc.0
  IL_0c07:  br         IL_0e76
  IL_0c0c:  ldstr      "1.2.840.113549.1.9.16.2.47"
  IL_0c11:  stloc.0
  IL_0c12:  br         IL_0e76
  IL_0c17:  ldstr      "1.2.840.113549.1.9.20"
  IL_0c1c:  stloc.0
  IL_0c1d:  br         IL_0e76
  IL_0c22:  ldstr      "1.2.840.113549.1.9.21"
  IL_0c27:  stloc.0
  IL_0c28:  br         IL_0e76
  IL_0c2d:  ldstr      "1.2.840.113549.1.9.22.1"
  IL_0c32:  stloc.0
  IL_0c33:  br         IL_0e76
  IL_0c38:  ldstr      "1.2.840.113549.1.12.1.3"
  IL_0c3d:  stloc.0
  IL_0c3e:  br         IL_0e76
  IL_0c43:  ldstr      "1.2.840.113549.1.12.1.5"
  IL_0c48:  stloc.0
  IL_0c49:  br         IL_0e76
  IL_0c4e:  ldstr      "1.2.840.113549.1.12.1.6"
  IL_0c53:  stloc.0
  IL_0c54:  br         IL_0e76
  IL_0c59:  ldstr      "1.2.840.113549.1.12.10.1.1"
  IL_0c5e:  stloc.0
  IL_0c5f:  br         IL_0e76
  IL_0c64:  ldstr      "1.2.840.113549.1.12.10.1.2"
  IL_0c69:  stloc.0
  IL_0c6a:  br         IL_0e76
  IL_0c6f:  ldstr      "1.2.840.113549.1.12.10.1.3"
  IL_0c74:  stloc.0
  IL_0c75:  br         IL_0e76
  IL_0c7a:  ldstr      "1.2.840.113549.1.12.10.1.5"
  IL_0c7f:  stloc.0
  IL_0c80:  br         IL_0e76
  IL_0c85:  ldstr      "1.2.840.113549.1.12.10.1.6"
  IL_0c8a:  stloc.0
  IL_0c8b:  br         IL_0e76
  IL_0c90:  ldstr      "1.2.840.113549.2.5"
  IL_0c95:  stloc.0
  IL_0c96:  br         IL_0e76
  IL_0c9b:  ldstr      "1.2.840.113549.2.7"
  IL_0ca0:  stloc.0
  IL_0ca1:  br         IL_0e76
  IL_0ca6:  ldstr      "1.2.840.113549.2.9"
  IL_0cab:  stloc.0
  IL_0cac:  br         IL_0e76
  IL_0cb1:  ldstr      "1.2.840.113549.2.10"
  IL_0cb6:  stloc.0
  IL_0cb7:  br         IL_0e76
  IL_0cbc:  ldstr      "1.2.840.113549.2.11"
  IL_0cc1:  stloc.0
  IL_0cc2:  br         IL_0e76
  IL_0cc7:  ldstr      "1.2.840.113549.3.2"
  IL_0ccc:  stloc.0
  IL_0ccd:  br         IL_0e76
  IL_0cd2:  ldstr      "1.2.840.113549.3.7"
  IL_0cd7:  stloc.0
  IL_0cd8:  br         IL_0e76
  IL_0cdd:  ldstr      "1.3.6.1.4.1.311.17.1"
  IL_0ce2:  stloc.0
  IL_0ce3:  br         IL_0e76
  IL_0ce8:  ldstr      "1.3.6.1.4.1.311.17.3.20"
  IL_0ced:  stloc.0
  IL_0cee:  br         IL_0e76
  IL_0cf3:  ldstr      "1.3.6.1.4.1.311.20.2.3"
  IL_0cf8:  stloc.0
  IL_0cf9:  br         IL_0e76
  IL_0cfe:  ldstr      "1.3.6.1.4.1.311.88.2.1"
  IL_0d03:  stloc.0
  IL_0d04:  br         IL_0e76
  IL_0d09:  ldstr      "1.3.6.1.4.1.311.88.2.2"
  IL_0d0e:  stloc.0
  IL_0d0f:  br         IL_0e76
  IL_0d14:  ldstr      "1.3.6.1.5.5.7.3.1"
  IL_0d19:  stloc.0
  IL_0d1a:  br         IL_0e76
  IL_0d1f:  ldstr      "1.3.6.1.5.5.7.3.2"
  IL_0d24:  stloc.0
  IL_0d25:  br         IL_0e76
  IL_0d2a:  ldstr      "1.3.6.1.5.5.7.3.3"
  IL_0d2f:  stloc.0
  IL_0d30:  br         IL_0e76
  IL_0d35:  ldstr      "1.3.6.1.5.5.7.3.4"
  IL_0d3a:  stloc.0
  IL_0d3b:  br         IL_0e76
  IL_0d40:  ldstr      "1.3.6.1.5.5.7.3.8"
  IL_0d45:  stloc.0
  IL_0d46:  br         IL_0e76
  IL_0d4b:  ldstr      "1.3.6.1.5.5.7.3.9"
  IL_0d50:  stloc.0
  IL_0d51:  br         IL_0e76
  IL_0d56:  ldstr      "1.3.6.1.5.5.7.6.2"
  IL_0d5b:  stloc.0
  IL_0d5c:  br         IL_0e76
  IL_0d61:  ldstr      "1.3.6.1.5.5.7.48.1"
  IL_0d66:  stloc.0
  IL_0d67:  br         IL_0e76
  IL_0d6c:  ldstr      "1.3.6.1.5.5.7.48.1.2"
  IL_0d71:  stloc.0
  IL_0d72:  br         IL_0e76
  IL_0d77:  ldstr      "1.3.6.1.5.5.7.48.2"
  IL_0d7c:  stloc.0
  IL_0d7d:  br         IL_0e76
  IL_0d82:  ldstr      "1.3.14.3.2.26"
  IL_0d87:  stloc.0
  IL_0d88:  br         IL_0e76
  IL_0d8d:  ldstr      "1.3.14.3.2.7"
  IL_0d92:  stloc.0
  IL_0d93:  br         IL_0e76
  IL_0d98:  ldstr      "1.3.132.0.34"
  IL_0d9d:  stloc.0
  IL_0d9e:  br         IL_0e76
  IL_0da3:  ldstr      "1.3.132.0.35"
  IL_0da8:  stloc.0
  IL_0da9:  br         IL_0e76
  IL_0dae:  ldstr      "2.5.4.3"
  IL_0db3:  stloc.0
  IL_0db4:  br         IL_0e76
  IL_0db9:  ldstr      "2.5.4.5"
  IL_0dbe:  stloc.0
  IL_0dbf:  br         IL_0e76
  IL_0dc4:  ldstr      "2.5.4.6"
  IL_0dc9:  stloc.0
  IL_0dca:  br         IL_0e76
  IL_0dcf:  ldstr      "2.5.4.7"
  IL_0dd4:  stloc.0
  IL_0dd5:  br         IL_0e76
  IL_0dda:  ldstr      "2.5.4.8"
  IL_0ddf:  stloc.0
  IL_0de0:  br         IL_0e76
  IL_0de5:  ldstr      "2.5.4.10"
  IL_0dea:  stloc.0
  IL_0deb:  br         IL_0e76
  IL_0df0:  ldstr      "2.5.4.11"
  IL_0df5:  stloc.0
  IL_0df6:  br.s       IL_0e76
  IL_0df8:  ldstr      "2.5.4.97"
  IL_0dfd:  stloc.0
  IL_0dfe:  br.s       IL_0e76
  IL_0e00:  ldstr      "2.5.29.14"
  IL_0e05:  stloc.0
  IL_0e06:  br.s       IL_0e76
  IL_0e08:  ldstr      "2.5.29.15"
  IL_0e0d:  stloc.0
  IL_0e0e:  br.s       IL_0e76
  IL_0e10:  ldstr      "2.5.29.17"
  IL_0e15:  stloc.0
  IL_0e16:  br.s       IL_0e76
  IL_0e18:  ldstr      "2.5.29.19"
  IL_0e1d:  stloc.0
  IL_0e1e:  br.s       IL_0e76
  IL_0e20:  ldstr      "2.5.29.20"
  IL_0e25:  stloc.0
  IL_0e26:  br.s       IL_0e76
  IL_0e28:  ldstr      "2.5.29.35"
  IL_0e2d:  stloc.0
  IL_0e2e:  br.s       IL_0e76
  IL_0e30:  ldstr      "2.16.840.1.101.3.4.1.2"
  IL_0e35:  stloc.0
  IL_0e36:  br.s       IL_0e76
  IL_0e38:  ldstr      "2.16.840.1.101.3.4.1.22"
  IL_0e3d:  stloc.0
  IL_0e3e:  br.s       IL_0e76
  IL_0e40:  ldstr      "2.16.840.1.101.3.4.1.42"
  IL_0e45:  stloc.0
  IL_0e46:  br.s       IL_0e76
  IL_0e48:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_0e4d:  stloc.0
  IL_0e4e:  br.s       IL_0e76
  IL_0e50:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_0e55:  stloc.0
  IL_0e56:  br.s       IL_0e76
  IL_0e58:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_0e5d:  stloc.0
  IL_0e5e:  br.s       IL_0e76
  IL_0e60:  ldstr      "2.23.140.1.2.1"
  IL_0e65:  stloc.0
  IL_0e66:  br.s       IL_0e76
  IL_0e68:  ldstr      "2.23.140.1.2.2"
  IL_0e6d:  stloc.0
  IL_0e6e:  br.s       IL_0e76
  IL_0e70:  ldstr      "default"
  IL_0e75:  stloc.0
  IL_0e76:  ldloc.0
  IL_0e77:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     4692 (0x1254)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x77c08b8d
  IL_000d:  bgt.un     IL_030b
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x3343070f
  IL_0018:  bgt.un     IL_0188
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4     0x23070056
  IL_0023:  bgt.un     IL_00d8
  IL_0028:  ldloc.1
  IL_0029:  ldc.i4     0x1bd19c31
  IL_002e:  bgt.un.s   IL_0084
  IL_0030:  ldloc.1
  IL_0031:  ldc.i4     0xafcddca
  IL_0036:  bgt.un.s   IL_005e
  IL_0038:  ldloc.1
  IL_0039:  ldc.i4     0x7fcd911
  IL_003e:  beq        IL_0c6d
  IL_0043:  ldloc.1
  IL_0044:  ldc.i4     0x9fcdc37
  IL_0049:  beq        IL_0c82
  IL_004e:  ldloc.1
  IL_004f:  ldc.i4     0xafcddca
  IL_0054:  beq        IL_0c97
  IL_0059:  br         IL_124c
  IL_005e:  ldloc.1
  IL_005f:  ldc.i4     0xbfcdf5d
  IL_0064:  beq        IL_0cac
  IL_0069:  ldloc.1
  IL_006a:  ldc.i4     0x11089fa9
  IL_006f:  beq        IL_094f
  IL_0074:  ldloc.1
  IL_0075:  ldc.i4     0x1bd19c31
  IL_007a:  beq        IL_0646
  IL_007f:  br         IL_124c
  IL_0084:  ldloc.1
  IL_0085:  ldc.i4     0x1e6ebeee
  IL_008a:  bgt.un.s   IL_00b2
  IL_008c:  ldloc.1
  IL_008d:  ldc.i4     0x1c6ebbc8
  IL_0092:  beq        IL_061c
  IL_0097:  ldloc.1
  IL_0098:  ldc.i4     0x1d3018af
  IL_009d:  beq        IL_069a
  IL_00a2:  ldloc.1
  IL_00a3:  ldc.i4     0x1e6ebeee
  IL_00a8:  beq        IL_0631
  IL_00ad:  br         IL_124c
  IL_00b2:  ldloc.1
  IL_00b3:  ldc.i4     0x1fdb884c
  IL_00b8:  beq        IL_0742
  IL_00bd:  ldloc.1
  IL_00be:  ldc.i4     0x20db89df
  IL_00c3:  beq        IL_072d
  IL_00c8:  ldloc.1
  IL_00c9:  ldc.i4     0x23070056
  IL_00ce:  beq        IL_06c4
  IL_00d3:  br         IL_124c
  IL_00d8:  ldloc.1
  IL_00d9:  ldc.i4     0x281396fb
  IL_00de:  bgt.un.s   IL_0134
  IL_00e0:  ldloc.1
  IL_00e1:  ldc.i4     0x25db91be
  IL_00e6:  bgt.un.s   IL_010e
  IL_00e8:  ldloc.1
  IL_00e9:  ldc.i4     0x23db8e98
  IL_00ee:  beq        IL_0703
  IL_00f3:  ldloc.1
  IL_00f4:  ldc.i4     0x240701e9
  IL_00f9:  beq        IL_06af
  IL_00fe:  ldloc.1
  IL_00ff:  ldc.i4     0x25db91be
  IL_0104:  beq        IL_0718
  IL_0109:  br         IL_124c
  IL_010e:  ldloc.1
  IL_010f:  ldc.i4     0x2607050f
  IL_0114:  beq        IL_06d9
  IL_0119:  ldloc.1
  IL_011a:  ldc.i4     0x27db94e4
  IL_011f:  beq        IL_06ee
  IL_0124:  ldloc.1
  IL_0125:  ldc.i4     0x281396fb
  IL_012a:  beq        IL_0b1d
  IL_012f:  br         IL_124c
  IL_0134:  ldloc.1
  IL_0135:  ldc.i4     0x3085083d
  IL_013a:  bgt.un.s   IL_0162
  IL_013c:  ldloc.1
  IL_013d:  ldc.i4     0x2913988e
  IL_0142:  beq        IL_0b32
  IL_0147:  ldloc.1
  IL_0148:  ldc.i4     0x2f8506aa
  IL_014d:  beq        IL_0c43
  IL_0152:  ldloc.1
  IL_0153:  ldc.i4     0x3085083d
  IL_0158:  beq        IL_0c58
  IL_015d:  br         IL_124c
  IL_0162:  ldloc.1
  IL_0163:  ldc.i4     0x3243057c
  IL_0168:  beq        IL_0b9b
  IL_016d:  ldloc.1
  IL_016e:  ldc.i4     0x332fa045
  IL_0173:  beq        IL_0c19
  IL_0178:  ldloc.1
  IL_0179:  ldc.i4     0x3343070f
  IL_017e:  beq        IL_0bb0
  IL_0183:  br         IL_124c
  IL_0188:  ldloc.1
  IL_0189:  ldc.i4     0x4f59e06b
  IL_018e:  bgt.un     IL_0243
  IL_0193:  ldloc.1
  IL_0194:  ldc.i4     0x3c43153a
  IL_0199:  bgt.un.s   IL_01ef
  IL_019b:  ldloc.1
  IL_019c:  ldc.i4     0x3b26d12c
  IL_01a1:  bgt.un.s   IL_01c9
  IL_01a3:  ldloc.1
  IL_01a4:  ldc.i4     0x36430bc8
  IL_01a9:  beq        IL_0b86
  IL_01ae:  ldloc.1
  IL_01af:  ldc.i4     0x38f1086c
  IL_01b4:  beq        IL_0d00
  IL_01b9:  ldloc.1
  IL_01ba:  ldc.i4     0x3b26d12c
  IL_01bf:  beq        IL_0979
  IL_01c4:  br         IL_124c
  IL_01c9:  ldloc.1
  IL_01ca:  ldc.i4     0x3b4313a7
  IL_01cf:  beq        IL_0b47
  IL_01d4:  ldloc.1
  IL_01d5:  ldc.i4     0x3c26d2bf
  IL_01da:  beq        IL_0964
  IL_01df:  ldloc.1
  IL_01e0:  ldc.i4     0x3c43153a
  IL_01e5:  beq        IL_0b5c
  IL_01ea:  br         IL_124c
  IL_01ef:  ldloc.1
  IL_01f0:  ldc.i4     0x4804a7c1
  IL_01f5:  bgt.un.s   IL_021d
  IL_01f7:  ldloc.1
  IL_01f8:  ldc.i4     0x3d4316cd
  IL_01fd:  beq        IL_0b71
  IL_0202:  ldloc.1
  IL_0203:  ldc.i4     0x4704a62e
  IL_0208:  beq        IL_0ceb
  IL_020d:  ldloc.1
  IL_020e:  ldc.i4     0x4804a7c1
  IL_0213:  beq        IL_0cd6
  IL_0218:  br         IL_124c
  IL_021d:  ldloc.1
  IL_021e:  ldc.i4     0x4e59ded8
  IL_0223:  beq        IL_07c0
  IL_0228:  ldloc.1
  IL_0229:  ldc.i4     0x4e5d1dcc
  IL_022e:  beq        IL_0bef
  IL_0233:  ldloc.1
  IL_0234:  ldc.i4     0x4f59e06b
  IL_0239:  beq        IL_07d5
  IL_023e:  br         IL_124c
  IL_0243:  ldloc.1
  IL_0244:  ldc.i4     0x595ddeb5
  IL_0249:  bgt.un.s   IL_029f
  IL_024b:  ldloc.1
  IL_024c:  ldc.i4     0x5159e391
  IL_0251:  bgt.un.s   IL_0279
  IL_0253:  ldloc.1
  IL_0254:  ldc.i4     0x4ff97a7f
  IL_0259:  beq        IL_0af3
  IL_025e:  ldloc.1
  IL_025f:  ldc.i4     0x5059e1fe
  IL_0264:  beq        IL_07ea
  IL_0269:  ldloc.1
  IL_026a:  ldc.i4     0x5159e391
  IL_026f:  beq        IL_07ff
  IL_0274:  br         IL_124c
  IL_0279:  ldloc.1
  IL_027a:  ldc.i4     0x53118aff
  IL_027f:  beq        IL_0b08
  IL_0284:  ldloc.1
  IL_0285:  ldc.i4     0x565dd9fc
  IL_028a:  beq        IL_065b
  IL_028f:  ldloc.1
  IL_0290:  ldc.i4     0x595ddeb5
  IL_0295:  beq        IL_0670
  IL_029a:  br         IL_124c
  IL_029f:  ldloc.1
  IL_02a0:  ldc.i4     0x6b2c3ae9
  IL_02a5:  bgt.un.s   IL_02cd
  IL_02a7:  ldloc.1
  IL_02a8:  ldc.i4     0x5a004a99
  IL_02ad:  beq        IL_0685
  IL_02b2:  ldloc.1
  IL_02b3:  ldc.i4     0x69d0cb33
  IL_02b8:  beq        IL_0d93
  IL_02bd:  ldloc.1
  IL_02be:  ldc.i4     0x6b2c3ae9
  IL_02c3:  beq        IL_0910
  IL_02c8:  br         IL_124c
  IL_02cd:  ldloc.1
  IL_02ce:  ldc.i4     0x777b7fe3
  IL_02d3:  bgt.un.s   IL_02f0
  IL_02d5:  ldloc.1
  IL_02d6:  ldc.i4     0x747ce41c
  IL_02db:  beq        IL_0bda
  IL_02e0:  ldloc.1
  IL_02e1:  ldc.i4     0x777b7fe3
  IL_02e6:  beq        IL_0e11
  IL_02eb:  br         IL_124c
  IL_02f0:  ldloc.1
  IL_02f1:  ldc.i4     0x777ce8d5
  IL_02f6:  beq        IL_0c04
  IL_02fb:  ldloc.1
  IL_02fc:  ldc.i4     0x77c08b8d
  IL_0301:  beq        IL_0bc5
  IL_0306:  br         IL_124c
  IL_030b:  ldloc.1
  IL_030c:  ldc.i4     0xafe050dc
  IL_0311:  bgt.un     IL_0499
  IL_0316:  ldloc.1
  IL_0317:  ldc.i4     0x95ab4e75
  IL_031c:  bgt.un     IL_03d1
  IL_0321:  ldloc.1
  IL_0322:  ldc.i4     0x8048ce1e
  IL_0327:  bgt.un.s   IL_037d
  IL_0329:  ldloc.1
  IL_032a:  ldc.i4     0x7becd51b
  IL_032f:  bgt.un.s   IL_0357
  IL_0331:  ldloc.1
  IL_0332:  ldc.i4     0x787b8176
  IL_0337:  beq        IL_0e26
  IL_033c:  ldloc.1
  IL_033d:  ldc.i4     0x7aecd388
  IL_0342:  beq        IL_083e
  IL_0347:  ldloc.1
  IL_0348:  ldc.i4     0x7becd51b
  IL_034d:  beq        IL_0829
  IL_0352:  br         IL_124c
  IL_0357:  ldloc.1
  IL_0358:  ldc.i4     0x7cecd6ae
  IL_035d:  beq        IL_0814
  IL_0362:  ldloc.1
  IL_0363:  ldc.i4     0x7fecdb67
  IL_0368:  beq        IL_0853
  IL_036d:  ldloc.1
  IL_036e:  ldc.i4     0x8048ce1e
  IL_0373:  beq        IL_0ab4
  IL_0378:  br         IL_124c
  IL_037d:  ldloc.1
  IL_037e:  ldc.i4     0x93ab4b4f
  IL_0383:  bgt.un.s   IL_03ab
  IL_0385:  ldloc.1
  IL_0386:  ldc.i4     0x8548d5fd
  IL_038b:  beq        IL_0ac9
  IL_0390:  ldloc.1
  IL_0391:  ldc.i4     0x92ab49bc
  IL_0396:  beq        IL_0757
  IL_039b:  ldloc.1
  IL_039c:  ldc.i4     0x93ab4b4f
  IL_03a1:  beq        IL_076c
  IL_03a6:  br         IL_124c
  IL_03ab:  ldloc.1
  IL_03ac:  ldc.i4     0x94ab4ce2
  IL_03b1:  beq        IL_0781
  IL_03b6:  ldloc.1
  IL_03b7:  ldc.i4     0x94af9293
  IL_03bc:  beq        IL_0da8
  IL_03c1:  ldloc.1
  IL_03c2:  ldc.i4     0x95ab4e75
  IL_03c7:  beq        IL_0796
  IL_03cc:  br         IL_124c
  IL_03d1:  ldloc.1
  IL_03d2:  ldc.i4     0xa401384d
  IL_03d7:  bgt.un.s   IL_042d
  IL_03d9:  ldloc.1
  IL_03da:  ldc.i4     0x9e012edb
  IL_03df:  bgt.un.s   IL_0407
  IL_03e1:  ldloc.1
  IL_03e2:  ldc.i4     0x9a7462aa
  IL_03e7:  beq        IL_0c2e
  IL_03ec:  ldloc.1
  IL_03ed:  ldc.i4     0x9cbea9b5
  IL_03f2:  beq        IL_0dbd
  IL_03f7:  ldloc.1
  IL_03f8:  ldc.i4     0x9e012edb
  IL_03fd:  beq        IL_0925
  IL_0402:  br         IL_124c
  IL_0407:  ldloc.1
  IL_0408:  ldc.i4     0xa2882d34
  IL_040d:  beq        IL_0a8a
  IL_0412:  ldloc.1
  IL_0413:  ldc.i4     0xa3882ec7
  IL_0418:  beq        IL_0a9f
  IL_041d:  ldloc.1
  IL_041e:  ldc.i4     0xa401384d
  IL_0423:  beq        IL_093a
  IL_0428:  br         IL_124c
  IL_042d:  ldloc.1
  IL_042e:  ldc.i4     0xace04c23
  IL_0433:  bgt.un.s   IL_045b
  IL_0435:  ldloc.1
  IL_0436:  ldc.i4     0xabd2b2a0
  IL_043b:  beq        IL_09cd
  IL_0440:  ldloc.1
  IL_0441:  ldc.i4     0xabe04a90
  IL_0446:  beq        IL_08a7
  IL_044b:  ldloc.1
  IL_044c:  ldc.i4     0xace04c23
  IL_0451:  beq        IL_0892
  IL_0456:  br         IL_124c
  IL_045b:  ldloc.1
  IL_045c:  ldc.i4     0xaed2b759
  IL_0461:  bgt.un.s   IL_047e
  IL_0463:  ldloc.1
  IL_0464:  ldc.i4     0xade04db6
  IL_0469:  beq        IL_08d1
  IL_046e:  ldloc.1
  IL_046f:  ldc.i4     0xaed2b759
  IL_0474:  beq        IL_09b8
  IL_0479:  br         IL_124c
  IL_047e:  ldloc.1
  IL_047f:  ldc.i4     0xaee04f49
  IL_0484:  beq        IL_08bc
  IL_0489:  ldloc.1
  IL_048a:  ldc.i4     0xafe050dc
  IL_048f:  beq        IL_0868
  IL_0494:  br         IL_124c
  IL_0499:  ldloc.1
  IL_049a:  ldc.i4     0xdc91c9af
  IL_049f:  bgt.un     IL_0554
  IL_04a4:  ldloc.1
  IL_04a5:  ldc.i4     0xd007f6d9
  IL_04aa:  bgt.un.s   IL_0500
  IL_04ac:  ldloc.1
  IL_04ad:  ldc.i4     0xc61f5d38
  IL_04b2:  bgt.un.s   IL_04da
  IL_04b4:  ldloc.1
  IL_04b5:  ldc.i4     0xb0d2ba7f
  IL_04ba:  beq        IL_09a3
  IL_04bf:  ldloc.1
  IL_04c0:  ldc.i4     0xb1e05402
  IL_04c5:  beq        IL_087d
  IL_04ca:  ldloc.1
  IL_04cb:  ldc.i4     0xc61f5d38
  IL_04d0:  beq        IL_08e6
  IL_04d5:  br         IL_124c
  IL_04da:  ldloc.1
  IL_04db:  ldc.i4     0xc71f5ecb
  IL_04e0:  beq        IL_08fb
  IL_04e5:  ldloc.1
  IL_04e6:  ldc.i4     0xcf05b6af
  IL_04eb:  beq        IL_0d7e
  IL_04f0:  ldloc.1
  IL_04f1:  ldc.i4     0xd007f6d9
  IL_04f6:  beq        IL_0d69
  IL_04fb:  br         IL_124c
  IL_0500:  ldloc.1
  IL_0501:  ldc.i4     0xd30a3a29
  IL_0506:  bgt.un.s   IL_052e
  IL_0508:  ldloc.1
  IL_0509:  ldc.i4     0xd10a3703
  IL_050e:  beq        IL_0d3f
  IL_0513:  ldloc.1
  IL_0514:  ldc.i4     0xd20a3896
  IL_0519:  beq        IL_0d15
  IL_051e:  ldloc.1
  IL_051f:  ldc.i4     0xd30a3a29
  IL_0524:  beq        IL_0d2a
  IL_0529:  br         IL_124c
  IL_052e:  ldloc.1
  IL_052f:  ldc.i4     0xd53b501e
  IL_0534:  beq        IL_07ab
  IL_0539:  ldloc.1
  IL_053a:  ldc.i4     0xdc6685e0
  IL_053f:  beq        IL_098e
  IL_0544:  ldloc.1
  IL_0545:  ldc.i4     0xdc91c9af
  IL_054a:  beq        IL_0dd2
  IL_054f:  br         IL_124c
  IL_0554:  ldloc.1
  IL_0555:  ldc.i4     0xe0252800
  IL_055a:  bgt.un.s   IL_05b0
  IL_055c:  ldloc.1
  IL_055d:  ldc.i4     0xde91ccd5
  IL_0562:  bgt.un.s   IL_058a
  IL_0564:  ldloc.1
  IL_0565:  ldc.i4     0xdd91cb42
  IL_056a:  beq        IL_0de7
  IL_056f:  ldloc.1
  IL_0570:  ldc.i4     0xde178c28
  IL_0575:  beq        IL_09e2
  IL_057a:  ldloc.1
  IL_057b:  ldc.i4     0xde91ccd5
  IL_0580:  beq        IL_0dfc
  IL_0585:  br         IL_124c
  IL_058a:  ldloc.1
  IL_058b:  ldc.i4     0xdf0a4d0d
  IL_0590:  beq        IL_0d54
  IL_0595:  ldloc.1
  IL_0596:  ldc.i4     0xe0178f4e
  IL_059b:  beq        IL_0a0c
  IL_05a0:  ldloc.1
  IL_05a1:  ldc.i4     0xe0252800
  IL_05a6:  beq        IL_0a4b
  IL_05ab:  br         IL_124c
  IL_05b0:  ldloc.1
  IL_05b1:  ldc.i4     0xe2252b26
  IL_05b6:  bgt.un.s   IL_05de
  IL_05b8:  ldloc.1
  IL_05b9:  ldc.i4     0xe11790e1
  IL_05be:  beq        IL_09f7
  IL_05c3:  ldloc.1
  IL_05c4:  ldc.i4     0xe2179274
  IL_05c9:  beq        IL_0a21
  IL_05ce:  ldloc.1
  IL_05cf:  ldc.i4     0xe2252b26
  IL_05d4:  beq        IL_0a60
  IL_05d9:  br         IL_124c
  IL_05de:  ldloc.1
  IL_05df:  ldc.i4     0xec253ae4
  IL_05e4:  bgt.un.s   IL_0601
  IL_05e6:  ldloc.1
  IL_05e7:  ldc.i4     0xe517972d
  IL_05ec:  beq        IL_0a36
  IL_05f1:  ldloc.1
  IL_05f2:  ldc.i4     0xec253ae4
  IL_05f7:  beq        IL_0a75
  IL_05fc:  br         IL_124c
  IL_0601:  ldloc.1
  IL_0602:  ldc.i4     0xf9eeda43
  IL_0607:  beq        IL_0ade
  IL_060c:  ldloc.1
  IL_060d:  ldc.i4     0xfcfcc7c0
  IL_0612:  beq        IL_0cc1
  IL_0617:  br         IL_124c
  IL_061c:  ldarg.0
  IL_061d:  ldstr      "1.2.840.10040.4.1"
  IL_0622:  call       "bool string.op_Equality(string, string)"
  IL_0627:  brtrue     IL_0e3b
  IL_062c:  br         IL_124c
  IL_0631:  ldarg.0
  IL_0632:  ldstr      "1.2.840.10040.4.3"
  IL_0637:  call       "bool string.op_Equality(string, string)"
  IL_063c:  brtrue     IL_0e46
  IL_0641:  br         IL_124c
  IL_0646:  ldarg.0
  IL_0647:  ldstr      "1.2.840.10045.2.1"
  IL_064c:  call       "bool string.op_Equality(string, string)"
  IL_0651:  brtrue     IL_0e51
  IL_0656:  br         IL_124c
  IL_065b:  ldarg.0
  IL_065c:  ldstr      "1.2.840.10045.1.1"
  IL_0661:  call       "bool string.op_Equality(string, string)"
  IL_0666:  brtrue     IL_0e5c
  IL_066b:  br         IL_124c
  IL_0670:  ldarg.0
  IL_0671:  ldstr      "1.2.840.10045.1.2"
  IL_0676:  call       "bool string.op_Equality(string, string)"
  IL_067b:  brtrue     IL_0e67
  IL_0680:  br         IL_124c
  IL_0685:  ldarg.0
  IL_0686:  ldstr      "1.2.840.10045.3.1.7"
  IL_068b:  call       "bool string.op_Equality(string, string)"
  IL_0690:  brtrue     IL_0e72
  IL_0695:  br         IL_124c
  IL_069a:  ldarg.0
  IL_069b:  ldstr      "1.2.840.10045.4.1"
  IL_06a0:  call       "bool string.op_Equality(string, string)"
  IL_06a5:  brtrue     IL_0e7d
  IL_06aa:  br         IL_124c
  IL_06af:  ldarg.0
  IL_06b0:  ldstr      "1.2.840.10045.4.3.2"
  IL_06b5:  call       "bool string.op_Equality(string, string)"
  IL_06ba:  brtrue     IL_0e88
  IL_06bf:  br         IL_124c
  IL_06c4:  ldarg.0
  IL_06c5:  ldstr      "1.2.840.10045.4.3.3"
  IL_06ca:  call       "bool string.op_Equality(string, string)"
  IL_06cf:  brtrue     IL_0e93
  IL_06d4:  br         IL_124c
  IL_06d9:  ldarg.0
  IL_06da:  ldstr      "1.2.840.10045.4.3.4"
  IL_06df:  call       "bool string.op_Equality(string, string)"
  IL_06e4:  brtrue     IL_0e9e
  IL_06e9:  br         IL_124c
  IL_06ee:  ldarg.0
  IL_06ef:  ldstr      "1.2.840.113549.1.1.1"
  IL_06f4:  call       "bool string.op_Equality(string, string)"
  IL_06f9:  brtrue     IL_0ea9
  IL_06fe:  br         IL_124c
  IL_0703:  ldarg.0
  IL_0704:  ldstr      "1.2.840.113549.1.1.5"
  IL_0709:  call       "bool string.op_Equality(string, string)"
  IL_070e:  brtrue     IL_0eb4
  IL_0713:  br         IL_124c
  IL_0718:  ldarg.0
  IL_0719:  ldstr      "1.2.840.113549.1.1.7"
  IL_071e:  call       "bool string.op_Equality(string, string)"
  IL_0723:  brtrue     IL_0ebf
  IL_0728:  br         IL_124c
  IL_072d:  ldarg.0
  IL_072e:  ldstr      "1.2.840.113549.1.1.8"
  IL_0733:  call       "bool string.op_Equality(string, string)"
  IL_0738:  brtrue     IL_0eca
  IL_073d:  br         IL_124c
  IL_0742:  ldarg.0
  IL_0743:  ldstr      "1.2.840.113549.1.1.9"
  IL_0748:  call       "bool string.op_Equality(string, string)"
  IL_074d:  brtrue     IL_0ed5
  IL_0752:  br         IL_124c
  IL_0757:  ldarg.0
  IL_0758:  ldstr      "1.2.840.113549.1.1.10"
  IL_075d:  call       "bool string.op_Equality(string, string)"
  IL_0762:  brtrue     IL_0ee0
  IL_0767:  br         IL_124c
  IL_076c:  ldarg.0
  IL_076d:  ldstr      "1.2.840.113549.1.1.11"
  IL_0772:  call       "bool string.op_Equality(string, string)"
  IL_0777:  brtrue     IL_0eeb
  IL_077c:  br         IL_124c
  IL_0781:  ldarg.0
  IL_0782:  ldstr      "1.2.840.113549.1.1.12"
  IL_0787:  call       "bool string.op_Equality(string, string)"
  IL_078c:  brtrue     IL_0ef6
  IL_0791:  br         IL_124c
  IL_0796:  ldarg.0
  IL_0797:  ldstr      "1.2.840.113549.1.1.13"
  IL_079c:  call       "bool string.op_Equality(string, string)"
  IL_07a1:  brtrue     IL_0f01
  IL_07a6:  br         IL_124c
  IL_07ab:  ldarg.0
  IL_07ac:  ldstr      "1.2.840.113549.1.5.3"
  IL_07b1:  call       "bool string.op_Equality(string, string)"
  IL_07b6:  brtrue     IL_0f0c
  IL_07bb:  br         IL_124c
  IL_07c0:  ldarg.0
  IL_07c1:  ldstr      "1.2.840.113549.1.5.10"
  IL_07c6:  call       "bool string.op_Equality(string, string)"
  IL_07cb:  brtrue     IL_0f17
  IL_07d0:  br         IL_124c
  IL_07d5:  ldarg.0
  IL_07d6:  ldstr      "1.2.840.113549.1.5.11"
  IL_07db:  call       "bool string.op_Equality(string, string)"
  IL_07e0:  brtrue     IL_0f22
  IL_07e5:  br         IL_124c
  IL_07ea:  ldarg.0
  IL_07eb:  ldstr      "1.2.840.113549.1.5.12"
  IL_07f0:  call       "bool string.op_Equality(string, string)"
  IL_07f5:  brtrue     IL_0f2d
  IL_07fa:  br         IL_124c
  IL_07ff:  ldarg.0
  IL_0800:  ldstr      "1.2.840.113549.1.5.13"
  IL_0805:  call       "bool string.op_Equality(string, string)"
  IL_080a:  brtrue     IL_0f38
  IL_080f:  br         IL_124c
  IL_0814:  ldarg.0
  IL_0815:  ldstr      "1.2.840.113549.1.7.1"
  IL_081a:  call       "bool string.op_Equality(string, string)"
  IL_081f:  brtrue     IL_0f43
  IL_0824:  br         IL_124c
  IL_0829:  ldarg.0
  IL_082a:  ldstr      "1.2.840.113549.1.7.2"
  IL_082f:  call       "bool string.op_Equality(string, string)"
  IL_0834:  brtrue     IL_0f4e
  IL_0839:  br         IL_124c
  IL_083e:  ldarg.0
  IL_083f:  ldstr      "1.2.840.113549.1.7.3"
  IL_0844:  call       "bool string.op_Equality(string, string)"
  IL_0849:  brtrue     IL_0f59
  IL_084e:  br         IL_124c
  IL_0853:  ldarg.0
  IL_0854:  ldstr      "1.2.840.113549.1.7.6"
  IL_0859:  call       "bool string.op_Equality(string, string)"
  IL_085e:  brtrue     IL_0f64
  IL_0863:  br         IL_124c
  IL_0868:  ldarg.0
  IL_0869:  ldstr      "1.2.840.113549.1.9.1"
  IL_086e:  call       "bool string.op_Equality(string, string)"
  IL_0873:  brtrue     IL_0f6f
  IL_0878:  br         IL_124c
  IL_087d:  ldarg.0
  IL_087e:  ldstr      "1.2.840.113549.1.9.3"
  IL_0883:  call       "bool string.op_Equality(string, string)"
  IL_0888:  brtrue     IL_0f7a
  IL_088d:  br         IL_124c
  IL_0892:  ldarg.0
  IL_0893:  ldstr      "1.2.840.113549.1.9.4"
  IL_0898:  call       "bool string.op_Equality(string, string)"
  IL_089d:  brtrue     IL_0f85
  IL_08a2:  br         IL_124c
  IL_08a7:  ldarg.0
  IL_08a8:  ldstr      "1.2.840.113549.1.9.5"
  IL_08ad:  call       "bool string.op_Equality(string, string)"
  IL_08b2:  brtrue     IL_0f90
  IL_08b7:  br         IL_124c
  IL_08bc:  ldarg.0
  IL_08bd:  ldstr      "1.2.840.113549.1.9.6"
  IL_08c2:  call       "bool string.op_Equality(string, string)"
  IL_08c7:  brtrue     IL_0f9b
  IL_08cc:  br         IL_124c
  IL_08d1:  ldarg.0
  IL_08d2:  ldstr      "1.2.840.113549.1.9.7"
  IL_08d7:  call       "bool string.op_Equality(string, string)"
  IL_08dc:  brtrue     IL_0fa6
  IL_08e1:  br         IL_124c
  IL_08e6:  ldarg.0
  IL_08e7:  ldstr      "1.2.840.113549.1.9.14"
  IL_08ec:  call       "bool string.op_Equality(string, string)"
  IL_08f1:  brtrue     IL_0fb1
  IL_08f6:  br         IL_124c
  IL_08fb:  ldarg.0
  IL_08fc:  ldstr      "1.2.840.113549.1.9.15"
  IL_0901:  call       "bool string.op_Equality(string, string)"
  IL_0906:  brtrue     IL_0fbc
  IL_090b:  br         IL_124c
  IL_0910:  ldarg.0
  IL_0911:  ldstr      "1.2.840.113549.1.9.16.1.4"
  IL_0916:  call       "bool string.op_Equality(string, string)"
  IL_091b:  brtrue     IL_0fc7
  IL_0920:  br         IL_124c
  IL_0925:  ldarg.0
  IL_0926:  ldstr      "1.2.840.113549.1.9.16.2.12"
  IL_092b:  call       "bool string.op_Equality(string, string)"
  IL_0930:  brtrue     IL_0fd2
  IL_0935:  br         IL_124c
  IL_093a:  ldarg.0
  IL_093b:  ldstr      "1.2.840.113549.1.9.16.2.14"
  IL_0940:  call       "bool string.op_Equality(string, string)"
  IL_0945:  brtrue     IL_0fdd
  IL_094a:  br         IL_124c
  IL_094f:  ldarg.0
  IL_0950:  ldstr      "1.2.840.113549.1.9.16.2.47"
  IL_0955:  call       "bool string.op_Equality(string, string)"
  IL_095a:  brtrue     IL_0fe8
  IL_095f:  br         IL_124c
  IL_0964:  ldarg.0
  IL_0965:  ldstr      "1.2.840.113549.1.9.20"
  IL_096a:  call       "bool string.op_Equality(string, string)"
  IL_096f:  brtrue     IL_0ff3
  IL_0974:  br         IL_124c
  IL_0979:  ldarg.0
  IL_097a:  ldstr      "1.2.840.113549.1.9.21"
  IL_097f:  call       "bool string.op_Equality(string, string)"
  IL_0984:  brtrue     IL_0ffe
  IL_0989:  br         IL_124c
  IL_098e:  ldarg.0
  IL_098f:  ldstr      "1.2.840.113549.1.9.22.1"
  IL_0994:  call       "bool string.op_Equality(string, string)"
  IL_0999:  brtrue     IL_1009
  IL_099e:  br         IL_124c
  IL_09a3:  ldarg.0
  IL_09a4:  ldstr      "1.2.840.113549.1.12.1.3"
  IL_09a9:  call       "bool string.op_Equality(string, string)"
  IL_09ae:  brtrue     IL_1014
  IL_09b3:  br         IL_124c
  IL_09b8:  ldarg.0
  IL_09b9:  ldstr      "1.2.840.113549.1.12.1.5"
  IL_09be:  call       "bool string.op_Equality(string, string)"
  IL_09c3:  brtrue     IL_101f
  IL_09c8:  br         IL_124c
  IL_09cd:  ldarg.0
  IL_09ce:  ldstr      "1.2.840.113549.1.12.1.6"
  IL_09d3:  call       "bool string.op_Equality(string, string)"
  IL_09d8:  brtrue     IL_102a
  IL_09dd:  br         IL_124c
  IL_09e2:  ldarg.0
  IL_09e3:  ldstr      "1.2.840.113549.1.12.10.1.1"
  IL_09e8:  call       "bool string.op_Equality(string, string)"
  IL_09ed:  brtrue     IL_1035
  IL_09f2:  br         IL_124c
  IL_09f7:  ldarg.0
  IL_09f8:  ldstr      "1.2.840.113549.1.12.10.1.2"
  IL_09fd:  call       "bool string.op_Equality(string, string)"
  IL_0a02:  brtrue     IL_1040
  IL_0a07:  br         IL_124c
  IL_0a0c:  ldarg.0
  IL_0a0d:  ldstr      "1.2.840.113549.1.12.10.1.3"
  IL_0a12:  call       "bool string.op_Equality(string, string)"
  IL_0a17:  brtrue     IL_104b
  IL_0a1c:  br         IL_124c
  IL_0a21:  ldarg.0
  IL_0a22:  ldstr      "1.2.840.113549.1.12.10.1.5"
  IL_0a27:  call       "bool string.op_Equality(string, string)"
  IL_0a2c:  brtrue     IL_1056
  IL_0a31:  br         IL_124c
  IL_0a36:  ldarg.0
  IL_0a37:  ldstr      "1.2.840.113549.1.12.10.1.6"
  IL_0a3c:  call       "bool string.op_Equality(string, string)"
  IL_0a41:  brtrue     IL_1061
  IL_0a46:  br         IL_124c
  IL_0a4b:  ldarg.0
  IL_0a4c:  ldstr      "1.2.840.113549.2.5"
  IL_0a51:  call       "bool string.op_Equality(string, string)"
  IL_0a56:  brtrue     IL_106c
  IL_0a5b:  br         IL_124c
  IL_0a60:  ldarg.0
  IL_0a61:  ldstr      "1.2.840.113549.2.7"
  IL_0a66:  call       "bool string.op_Equality(string, string)"
  IL_0a6b:  brtrue     IL_1077
  IL_0a70:  br         IL_124c
  IL_0a75:  ldarg.0
  IL_0a76:  ldstr      "1.2.840.113549.2.9"
  IL_0a7b:  call       "bool string.op_Equality(string, string)"
  IL_0a80:  brtrue     IL_1082
  IL_0a85:  br         IL_124c
  IL_0a8a:  ldarg.0
  IL_0a8b:  ldstr      "1.2.840.113549.2.10"
  IL_0a90:  call       "bool string.op_Equality(string, string)"
  IL_0a95:  brtrue     IL_108d
  IL_0a9a:  br         IL_124c
  IL_0a9f:  ldarg.0
  IL_0aa0:  ldstr      "1.2.840.113549.2.11"
  IL_0aa5:  call       "bool string.op_Equality(string, string)"
  IL_0aaa:  brtrue     IL_1098
  IL_0aaf:  br         IL_124c
  IL_0ab4:  ldarg.0
  IL_0ab5:  ldstr      "1.2.840.113549.3.2"
  IL_0aba:  call       "bool string.op_Equality(string, string)"
  IL_0abf:  brtrue     IL_10a3
  IL_0ac4:  br         IL_124c
  IL_0ac9:  ldarg.0
  IL_0aca:  ldstr      "1.2.840.113549.3.7"
  IL_0acf:  call       "bool string.op_Equality(string, string)"
  IL_0ad4:  brtrue     IL_10ae
  IL_0ad9:  br         IL_124c
  IL_0ade:  ldarg.0
  IL_0adf:  ldstr      "1.3.6.1.4.1.311.17.1"
  IL_0ae4:  call       "bool string.op_Equality(string, string)"
  IL_0ae9:  brtrue     IL_10b9
  IL_0aee:  br         IL_124c
  IL_0af3:  ldarg.0
  IL_0af4:  ldstr      "1.3.6.1.4.1.311.17.3.20"
  IL_0af9:  call       "bool string.op_Equality(string, string)"
  IL_0afe:  brtrue     IL_10c4
  IL_0b03:  br         IL_124c
  IL_0b08:  ldarg.0
  IL_0b09:  ldstr      "1.3.6.1.4.1.311.20.2.3"
  IL_0b0e:  call       "bool string.op_Equality(string, string)"
  IL_0b13:  brtrue     IL_10cf
  IL_0b18:  br         IL_124c
  IL_0b1d:  ldarg.0
  IL_0b1e:  ldstr      "1.3.6.1.4.1.311.88.2.1"
  IL_0b23:  call       "bool string.op_Equality(string, string)"
  IL_0b28:  brtrue     IL_10da
  IL_0b2d:  br         IL_124c
  IL_0b32:  ldarg.0
  IL_0b33:  ldstr      "1.3.6.1.4.1.311.88.2.2"
  IL_0b38:  call       "bool string.op_Equality(string, string)"
  IL_0b3d:  brtrue     IL_10e5
  IL_0b42:  br         IL_124c
  IL_0b47:  ldarg.0
  IL_0b48:  ldstr      "1.3.6.1.5.5.7.3.1"
  IL_0b4d:  call       "bool string.op_Equality(string, string)"
  IL_0b52:  brtrue     IL_10f0
  IL_0b57:  br         IL_124c
  IL_0b5c:  ldarg.0
  IL_0b5d:  ldstr      "1.3.6.1.5.5.7.3.2"
  IL_0b62:  call       "bool string.op_Equality(string, string)"
  IL_0b67:  brtrue     IL_10fb
  IL_0b6c:  br         IL_124c
  IL_0b71:  ldarg.0
  IL_0b72:  ldstr      "1.3.6.1.5.5.7.3.3"
  IL_0b77:  call       "bool string.op_Equality(string, string)"
  IL_0b7c:  brtrue     IL_1106
  IL_0b81:  br         IL_124c
  IL_0b86:  ldarg.0
  IL_0b87:  ldstr      "1.3.6.1.5.5.7.3.4"
  IL_0b8c:  call       "bool string.op_Equality(string, string)"
  IL_0b91:  brtrue     IL_1111
  IL_0b96:  br         IL_124c
  IL_0b9b:  ldarg.0
  IL_0b9c:  ldstr      "1.3.6.1.5.5.7.3.8"
  IL_0ba1:  call       "bool string.op_Equality(string, string)"
  IL_0ba6:  brtrue     IL_111c
  IL_0bab:  br         IL_124c
  IL_0bb0:  ldarg.0
  IL_0bb1:  ldstr      "1.3.6.1.5.5.7.3.9"
  IL_0bb6:  call       "bool string.op_Equality(string, string)"
  IL_0bbb:  brtrue     IL_1127
  IL_0bc0:  br         IL_124c
  IL_0bc5:  ldarg.0
  IL_0bc6:  ldstr      "1.3.6.1.5.5.7.6.2"
  IL_0bcb:  call       "bool string.op_Equality(string, string)"
  IL_0bd0:  brtrue     IL_1132
  IL_0bd5:  br         IL_124c
  IL_0bda:  ldarg.0
  IL_0bdb:  ldstr      "1.3.6.1.5.5.7.48.1"
  IL_0be0:  call       "bool string.op_Equality(string, string)"
  IL_0be5:  brtrue     IL_113d
  IL_0bea:  br         IL_124c
  IL_0bef:  ldarg.0
  IL_0bf0:  ldstr      "1.3.6.1.5.5.7.48.1.2"
  IL_0bf5:  call       "bool string.op_Equality(string, string)"
  IL_0bfa:  brtrue     IL_1148
  IL_0bff:  br         IL_124c
  IL_0c04:  ldarg.0
  IL_0c05:  ldstr      "1.3.6.1.5.5.7.48.2"
  IL_0c0a:  call       "bool string.op_Equality(string, string)"
  IL_0c0f:  brtrue     IL_1153
  IL_0c14:  br         IL_124c
  IL_0c19:  ldarg.0
  IL_0c1a:  ldstr      "1.3.14.3.2.26"
  IL_0c1f:  call       "bool string.op_Equality(string, string)"
  IL_0c24:  brtrue     IL_115e
  IL_0c29:  br         IL_124c
  IL_0c2e:  ldarg.0
  IL_0c2f:  ldstr      "1.3.14.3.2.7"
  IL_0c34:  call       "bool string.op_Equality(string, string)"
  IL_0c39:  brtrue     IL_1169
  IL_0c3e:  br         IL_124c
  IL_0c43:  ldarg.0
  IL_0c44:  ldstr      "1.3.132.0.34"
  IL_0c49:  call       "bool string.op_Equality(string, string)"
  IL_0c4e:  brtrue     IL_1174
  IL_0c53:  br         IL_124c
  IL_0c58:  ldarg.0
  IL_0c59:  ldstr      "1.3.132.0.35"
  IL_0c5e:  call       "bool string.op_Equality(string, string)"
  IL_0c63:  brtrue     IL_117f
  IL_0c68:  br         IL_124c
  IL_0c6d:  ldarg.0
  IL_0c6e:  ldstr      "2.5.4.3"
  IL_0c73:  call       "bool string.op_Equality(string, string)"
  IL_0c78:  brtrue     IL_118a
  IL_0c7d:  br         IL_124c
  IL_0c82:  ldarg.0
  IL_0c83:  ldstr      "2.5.4.5"
  IL_0c88:  call       "bool string.op_Equality(string, string)"
  IL_0c8d:  brtrue     IL_1195
  IL_0c92:  br         IL_124c
  IL_0c97:  ldarg.0
  IL_0c98:  ldstr      "2.5.4.6"
  IL_0c9d:  call       "bool string.op_Equality(string, string)"
  IL_0ca2:  brtrue     IL_11a0
  IL_0ca7:  br         IL_124c
  IL_0cac:  ldarg.0
  IL_0cad:  ldstr      "2.5.4.7"
  IL_0cb2:  call       "bool string.op_Equality(string, string)"
  IL_0cb7:  brtrue     IL_11ab
  IL_0cbc:  br         IL_124c
  IL_0cc1:  ldarg.0
  IL_0cc2:  ldstr      "2.5.4.8"
  IL_0cc7:  call       "bool string.op_Equality(string, string)"
  IL_0ccc:  brtrue     IL_11b6
  IL_0cd1:  br         IL_124c
  IL_0cd6:  ldarg.0
  IL_0cd7:  ldstr      "2.5.4.10"
  IL_0cdc:  call       "bool string.op_Equality(string, string)"
  IL_0ce1:  brtrue     IL_11c1
  IL_0ce6:  br         IL_124c
  IL_0ceb:  ldarg.0
  IL_0cec:  ldstr      "2.5.4.11"
  IL_0cf1:  call       "bool string.op_Equality(string, string)"
  IL_0cf6:  brtrue     IL_11cc
  IL_0cfb:  br         IL_124c
  IL_0d00:  ldarg.0
  IL_0d01:  ldstr      "2.5.4.97"
  IL_0d06:  call       "bool string.op_Equality(string, string)"
  IL_0d0b:  brtrue     IL_11d4
  IL_0d10:  br         IL_124c
  IL_0d15:  ldarg.0
  IL_0d16:  ldstr      "2.5.29.14"
  IL_0d1b:  call       "bool string.op_Equality(string, string)"
  IL_0d20:  brtrue     IL_11dc
  IL_0d25:  br         IL_124c
  IL_0d2a:  ldarg.0
  IL_0d2b:  ldstr      "2.5.29.15"
  IL_0d30:  call       "bool string.op_Equality(string, string)"
  IL_0d35:  brtrue     IL_11e4
  IL_0d3a:  br         IL_124c
  IL_0d3f:  ldarg.0
  IL_0d40:  ldstr      "2.5.29.17"
  IL_0d45:  call       "bool string.op_Equality(string, string)"
  IL_0d4a:  brtrue     IL_11ec
  IL_0d4f:  br         IL_124c
  IL_0d54:  ldarg.0
  IL_0d55:  ldstr      "2.5.29.19"
  IL_0d5a:  call       "bool string.op_Equality(string, string)"
  IL_0d5f:  brtrue     IL_11f4
  IL_0d64:  br         IL_124c
  IL_0d69:  ldarg.0
  IL_0d6a:  ldstr      "2.5.29.20"
  IL_0d6f:  call       "bool string.op_Equality(string, string)"
  IL_0d74:  brtrue     IL_11fc
  IL_0d79:  br         IL_124c
  IL_0d7e:  ldarg.0
  IL_0d7f:  ldstr      "2.5.29.35"
  IL_0d84:  call       "bool string.op_Equality(string, string)"
  IL_0d89:  brtrue     IL_1204
  IL_0d8e:  br         IL_124c
  IL_0d93:  ldarg.0
  IL_0d94:  ldstr      "2.16.840.1.101.3.4.1.2"
  IL_0d99:  call       "bool string.op_Equality(string, string)"
  IL_0d9e:  brtrue     IL_120c
  IL_0da3:  br         IL_124c
  IL_0da8:  ldarg.0
  IL_0da9:  ldstr      "2.16.840.1.101.3.4.1.22"
  IL_0dae:  call       "bool string.op_Equality(string, string)"
  IL_0db3:  brtrue     IL_1214
  IL_0db8:  br         IL_124c
  IL_0dbd:  ldarg.0
  IL_0dbe:  ldstr      "2.16.840.1.101.3.4.1.42"
  IL_0dc3:  call       "bool string.op_Equality(string, string)"
  IL_0dc8:  brtrue     IL_121c
  IL_0dcd:  br         IL_124c
  IL_0dd2:  ldarg.0
  IL_0dd3:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_0dd8:  call       "bool string.op_Equality(string, string)"
  IL_0ddd:  brtrue     IL_1224
  IL_0de2:  br         IL_124c
  IL_0de7:  ldarg.0
  IL_0de8:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_0ded:  call       "bool string.op_Equality(string, string)"
  IL_0df2:  brtrue     IL_122c
  IL_0df7:  br         IL_124c
  IL_0dfc:  ldarg.0
  IL_0dfd:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_0e02:  call       "bool string.op_Equality(string, string)"
  IL_0e07:  brtrue     IL_1234
  IL_0e0c:  br         IL_124c
  IL_0e11:  ldarg.0
  IL_0e12:  ldstr      "2.23.140.1.2.1"
  IL_0e17:  call       "bool string.op_Equality(string, string)"
  IL_0e1c:  brtrue     IL_123c
  IL_0e21:  br         IL_124c
  IL_0e26:  ldarg.0
  IL_0e27:  ldstr      "2.23.140.1.2.2"
  IL_0e2c:  call       "bool string.op_Equality(string, string)"
  IL_0e31:  brtrue     IL_1244
  IL_0e36:  br         IL_124c
  IL_0e3b:  ldstr      "1.2.840.10040.4.1"
  IL_0e40:  stloc.0
  IL_0e41:  br         IL_1252
  IL_0e46:  ldstr      "1.2.840.10040.4.3"
  IL_0e4b:  stloc.0
  IL_0e4c:  br         IL_1252
  IL_0e51:  ldstr      "1.2.840.10045.2.1"
  IL_0e56:  stloc.0
  IL_0e57:  br         IL_1252
  IL_0e5c:  ldstr      "1.2.840.10045.1.1"
  IL_0e61:  stloc.0
  IL_0e62:  br         IL_1252
  IL_0e67:  ldstr      "1.2.840.10045.1.2"
  IL_0e6c:  stloc.0
  IL_0e6d:  br         IL_1252
  IL_0e72:  ldstr      "1.2.840.10045.3.1.7"
  IL_0e77:  stloc.0
  IL_0e78:  br         IL_1252
  IL_0e7d:  ldstr      "1.2.840.10045.4.1"
  IL_0e82:  stloc.0
  IL_0e83:  br         IL_1252
  IL_0e88:  ldstr      "1.2.840.10045.4.3.2"
  IL_0e8d:  stloc.0
  IL_0e8e:  br         IL_1252
  IL_0e93:  ldstr      "1.2.840.10045.4.3.3"
  IL_0e98:  stloc.0
  IL_0e99:  br         IL_1252
  IL_0e9e:  ldstr      "1.2.840.10045.4.3.4"
  IL_0ea3:  stloc.0
  IL_0ea4:  br         IL_1252
  IL_0ea9:  ldstr      "1.2.840.113549.1.1.1"
  IL_0eae:  stloc.0
  IL_0eaf:  br         IL_1252
  IL_0eb4:  ldstr      "1.2.840.113549.1.1.5"
  IL_0eb9:  stloc.0
  IL_0eba:  br         IL_1252
  IL_0ebf:  ldstr      "1.2.840.113549.1.1.7"
  IL_0ec4:  stloc.0
  IL_0ec5:  br         IL_1252
  IL_0eca:  ldstr      "1.2.840.113549.1.1.8"
  IL_0ecf:  stloc.0
  IL_0ed0:  br         IL_1252
  IL_0ed5:  ldstr      "1.2.840.113549.1.1.9"
  IL_0eda:  stloc.0
  IL_0edb:  br         IL_1252
  IL_0ee0:  ldstr      "1.2.840.113549.1.1.10"
  IL_0ee5:  stloc.0
  IL_0ee6:  br         IL_1252
  IL_0eeb:  ldstr      "1.2.840.113549.1.1.11"
  IL_0ef0:  stloc.0
  IL_0ef1:  br         IL_1252
  IL_0ef6:  ldstr      "1.2.840.113549.1.1.12"
  IL_0efb:  stloc.0
  IL_0efc:  br         IL_1252
  IL_0f01:  ldstr      "1.2.840.113549.1.1.13"
  IL_0f06:  stloc.0
  IL_0f07:  br         IL_1252
  IL_0f0c:  ldstr      "1.2.840.113549.1.5.3"
  IL_0f11:  stloc.0
  IL_0f12:  br         IL_1252
  IL_0f17:  ldstr      "1.2.840.113549.1.5.10"
  IL_0f1c:  stloc.0
  IL_0f1d:  br         IL_1252
  IL_0f22:  ldstr      "1.2.840.113549.1.5.11"
  IL_0f27:  stloc.0
  IL_0f28:  br         IL_1252
  IL_0f2d:  ldstr      "1.2.840.113549.1.5.12"
  IL_0f32:  stloc.0
  IL_0f33:  br         IL_1252
  IL_0f38:  ldstr      "1.2.840.113549.1.5.13"
  IL_0f3d:  stloc.0
  IL_0f3e:  br         IL_1252
  IL_0f43:  ldstr      "1.2.840.113549.1.7.1"
  IL_0f48:  stloc.0
  IL_0f49:  br         IL_1252
  IL_0f4e:  ldstr      "1.2.840.113549.1.7.2"
  IL_0f53:  stloc.0
  IL_0f54:  br         IL_1252
  IL_0f59:  ldstr      "1.2.840.113549.1.7.3"
  IL_0f5e:  stloc.0
  IL_0f5f:  br         IL_1252
  IL_0f64:  ldstr      "1.2.840.113549.1.7.6"
  IL_0f69:  stloc.0
  IL_0f6a:  br         IL_1252
  IL_0f6f:  ldstr      "1.2.840.113549.1.9.1"
  IL_0f74:  stloc.0
  IL_0f75:  br         IL_1252
  IL_0f7a:  ldstr      "1.2.840.113549.1.9.3"
  IL_0f7f:  stloc.0
  IL_0f80:  br         IL_1252
  IL_0f85:  ldstr      "1.2.840.113549.1.9.4"
  IL_0f8a:  stloc.0
  IL_0f8b:  br         IL_1252
  IL_0f90:  ldstr      "1.2.840.113549.1.9.5"
  IL_0f95:  stloc.0
  IL_0f96:  br         IL_1252
  IL_0f9b:  ldstr      "1.2.840.113549.1.9.6"
  IL_0fa0:  stloc.0
  IL_0fa1:  br         IL_1252
  IL_0fa6:  ldstr      "1.2.840.113549.1.9.7"
  IL_0fab:  stloc.0
  IL_0fac:  br         IL_1252
  IL_0fb1:  ldstr      "1.2.840.113549.1.9.14"
  IL_0fb6:  stloc.0
  IL_0fb7:  br         IL_1252
  IL_0fbc:  ldstr      "1.2.840.113549.1.9.15"
  IL_0fc1:  stloc.0
  IL_0fc2:  br         IL_1252
  IL_0fc7:  ldstr      "1.2.840.113549.1.9.16.1.4"
  IL_0fcc:  stloc.0
  IL_0fcd:  br         IL_1252
  IL_0fd2:  ldstr      "1.2.840.113549.1.9.16.2.12"
  IL_0fd7:  stloc.0
  IL_0fd8:  br         IL_1252
  IL_0fdd:  ldstr      "1.2.840.113549.1.9.16.2.14"
  IL_0fe2:  stloc.0
  IL_0fe3:  br         IL_1252
  IL_0fe8:  ldstr      "1.2.840.113549.1.9.16.2.47"
  IL_0fed:  stloc.0
  IL_0fee:  br         IL_1252
  IL_0ff3:  ldstr      "1.2.840.113549.1.9.20"
  IL_0ff8:  stloc.0
  IL_0ff9:  br         IL_1252
  IL_0ffe:  ldstr      "1.2.840.113549.1.9.21"
  IL_1003:  stloc.0
  IL_1004:  br         IL_1252
  IL_1009:  ldstr      "1.2.840.113549.1.9.22.1"
  IL_100e:  stloc.0
  IL_100f:  br         IL_1252
  IL_1014:  ldstr      "1.2.840.113549.1.12.1.3"
  IL_1019:  stloc.0
  IL_101a:  br         IL_1252
  IL_101f:  ldstr      "1.2.840.113549.1.12.1.5"
  IL_1024:  stloc.0
  IL_1025:  br         IL_1252
  IL_102a:  ldstr      "1.2.840.113549.1.12.1.6"
  IL_102f:  stloc.0
  IL_1030:  br         IL_1252
  IL_1035:  ldstr      "1.2.840.113549.1.12.10.1.1"
  IL_103a:  stloc.0
  IL_103b:  br         IL_1252
  IL_1040:  ldstr      "1.2.840.113549.1.12.10.1.2"
  IL_1045:  stloc.0
  IL_1046:  br         IL_1252
  IL_104b:  ldstr      "1.2.840.113549.1.12.10.1.3"
  IL_1050:  stloc.0
  IL_1051:  br         IL_1252
  IL_1056:  ldstr      "1.2.840.113549.1.12.10.1.5"
  IL_105b:  stloc.0
  IL_105c:  br         IL_1252
  IL_1061:  ldstr      "1.2.840.113549.1.12.10.1.6"
  IL_1066:  stloc.0
  IL_1067:  br         IL_1252
  IL_106c:  ldstr      "1.2.840.113549.2.5"
  IL_1071:  stloc.0
  IL_1072:  br         IL_1252
  IL_1077:  ldstr      "1.2.840.113549.2.7"
  IL_107c:  stloc.0
  IL_107d:  br         IL_1252
  IL_1082:  ldstr      "1.2.840.113549.2.9"
  IL_1087:  stloc.0
  IL_1088:  br         IL_1252
  IL_108d:  ldstr      "1.2.840.113549.2.10"
  IL_1092:  stloc.0
  IL_1093:  br         IL_1252
  IL_1098:  ldstr      "1.2.840.113549.2.11"
  IL_109d:  stloc.0
  IL_109e:  br         IL_1252
  IL_10a3:  ldstr      "1.2.840.113549.3.2"
  IL_10a8:  stloc.0
  IL_10a9:  br         IL_1252
  IL_10ae:  ldstr      "1.2.840.113549.3.7"
  IL_10b3:  stloc.0
  IL_10b4:  br         IL_1252
  IL_10b9:  ldstr      "1.3.6.1.4.1.311.17.1"
  IL_10be:  stloc.0
  IL_10bf:  br         IL_1252
  IL_10c4:  ldstr      "1.3.6.1.4.1.311.17.3.20"
  IL_10c9:  stloc.0
  IL_10ca:  br         IL_1252
  IL_10cf:  ldstr      "1.3.6.1.4.1.311.20.2.3"
  IL_10d4:  stloc.0
  IL_10d5:  br         IL_1252
  IL_10da:  ldstr      "1.3.6.1.4.1.311.88.2.1"
  IL_10df:  stloc.0
  IL_10e0:  br         IL_1252
  IL_10e5:  ldstr      "1.3.6.1.4.1.311.88.2.2"
  IL_10ea:  stloc.0
  IL_10eb:  br         IL_1252
  IL_10f0:  ldstr      "1.3.6.1.5.5.7.3.1"
  IL_10f5:  stloc.0
  IL_10f6:  br         IL_1252
  IL_10fb:  ldstr      "1.3.6.1.5.5.7.3.2"
  IL_1100:  stloc.0
  IL_1101:  br         IL_1252
  IL_1106:  ldstr      "1.3.6.1.5.5.7.3.3"
  IL_110b:  stloc.0
  IL_110c:  br         IL_1252
  IL_1111:  ldstr      "1.3.6.1.5.5.7.3.4"
  IL_1116:  stloc.0
  IL_1117:  br         IL_1252
  IL_111c:  ldstr      "1.3.6.1.5.5.7.3.8"
  IL_1121:  stloc.0
  IL_1122:  br         IL_1252
  IL_1127:  ldstr      "1.3.6.1.5.5.7.3.9"
  IL_112c:  stloc.0
  IL_112d:  br         IL_1252
  IL_1132:  ldstr      "1.3.6.1.5.5.7.6.2"
  IL_1137:  stloc.0
  IL_1138:  br         IL_1252
  IL_113d:  ldstr      "1.3.6.1.5.5.7.48.1"
  IL_1142:  stloc.0
  IL_1143:  br         IL_1252
  IL_1148:  ldstr      "1.3.6.1.5.5.7.48.1.2"
  IL_114d:  stloc.0
  IL_114e:  br         IL_1252
  IL_1153:  ldstr      "1.3.6.1.5.5.7.48.2"
  IL_1158:  stloc.0
  IL_1159:  br         IL_1252
  IL_115e:  ldstr      "1.3.14.3.2.26"
  IL_1163:  stloc.0
  IL_1164:  br         IL_1252
  IL_1169:  ldstr      "1.3.14.3.2.7"
  IL_116e:  stloc.0
  IL_116f:  br         IL_1252
  IL_1174:  ldstr      "1.3.132.0.34"
  IL_1179:  stloc.0
  IL_117a:  br         IL_1252
  IL_117f:  ldstr      "1.3.132.0.35"
  IL_1184:  stloc.0
  IL_1185:  br         IL_1252
  IL_118a:  ldstr      "2.5.4.3"
  IL_118f:  stloc.0
  IL_1190:  br         IL_1252
  IL_1195:  ldstr      "2.5.4.5"
  IL_119a:  stloc.0
  IL_119b:  br         IL_1252
  IL_11a0:  ldstr      "2.5.4.6"
  IL_11a5:  stloc.0
  IL_11a6:  br         IL_1252
  IL_11ab:  ldstr      "2.5.4.7"
  IL_11b0:  stloc.0
  IL_11b1:  br         IL_1252
  IL_11b6:  ldstr      "2.5.4.8"
  IL_11bb:  stloc.0
  IL_11bc:  br         IL_1252
  IL_11c1:  ldstr      "2.5.4.10"
  IL_11c6:  stloc.0
  IL_11c7:  br         IL_1252
  IL_11cc:  ldstr      "2.5.4.11"
  IL_11d1:  stloc.0
  IL_11d2:  br.s       IL_1252
  IL_11d4:  ldstr      "2.5.4.97"
  IL_11d9:  stloc.0
  IL_11da:  br.s       IL_1252
  IL_11dc:  ldstr      "2.5.29.14"
  IL_11e1:  stloc.0
  IL_11e2:  br.s       IL_1252
  IL_11e4:  ldstr      "2.5.29.15"
  IL_11e9:  stloc.0
  IL_11ea:  br.s       IL_1252
  IL_11ec:  ldstr      "2.5.29.17"
  IL_11f1:  stloc.0
  IL_11f2:  br.s       IL_1252
  IL_11f4:  ldstr      "2.5.29.19"
  IL_11f9:  stloc.0
  IL_11fa:  br.s       IL_1252
  IL_11fc:  ldstr      "2.5.29.20"
  IL_1201:  stloc.0
  IL_1202:  br.s       IL_1252
  IL_1204:  ldstr      "2.5.29.35"
  IL_1209:  stloc.0
  IL_120a:  br.s       IL_1252
  IL_120c:  ldstr      "2.16.840.1.101.3.4.1.2"
  IL_1211:  stloc.0
  IL_1212:  br.s       IL_1252
  IL_1214:  ldstr      "2.16.840.1.101.3.4.1.22"
  IL_1219:  stloc.0
  IL_121a:  br.s       IL_1252
  IL_121c:  ldstr      "2.16.840.1.101.3.4.1.42"
  IL_1221:  stloc.0
  IL_1222:  br.s       IL_1252
  IL_1224:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_1229:  stloc.0
  IL_122a:  br.s       IL_1252
  IL_122c:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_1231:  stloc.0
  IL_1232:  br.s       IL_1252
  IL_1234:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_1239:  stloc.0
  IL_123a:  br.s       IL_1252
  IL_123c:  ldstr      "2.23.140.1.2.1"
  IL_1241:  stloc.0
  IL_1242:  br.s       IL_1252
  IL_1244:  ldstr      "2.23.140.1.2.2"
  IL_1249:  stloc.0
  IL_124a:  br.s       IL_1252
  IL_124c:  ldstr      "default"
  IL_1251:  stloc.0
  IL_1252:  ldloc.0
  IL_1253:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void TryParseStatusFile()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/Common/src/Interop/Linux/procfs/Interop.ProcFsStat.TryReadStatusFile.cs#L66-L102
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("Pid");
assert("VmHWM");
assert("VmRSS");
assert("VmData");
assert("VmSwap");
assert("VmSize");
assert("VmPeak");
assert("VmStk");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "Pid" => "Pid",
            "VmHWM" => "VmHWM",
            "VmRSS" => "VmRSS",
            "VmData" => "VmData",
            "VmSwap" => "VmSwap",
            "VmSize" => "VmSize",
            "VmPeak" => "VmPeak",
            "VmStk" => "VmStk",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      317 (0x13d)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0135
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.3
  IL_000f:  sub
  IL_0010:  switch    (
        IL_0074,
        IL_0135,
        IL_002a,
        IL_0046)
  IL_0025:  br         IL_0135
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.2
  IL_002c:  call       "char string.this[int].get"
  IL_0031:  stloc.2
  IL_0032:  ldloc.2
  IL_0033:  ldc.i4.s   72
  IL_0035:  beq.s      IL_0086
  IL_0037:  ldloc.2
  IL_0038:  ldc.i4.s   82
  IL_003a:  beq.s      IL_0098
  IL_003c:  ldloc.2
  IL_003d:  ldc.i4.s   83
  IL_003f:  beq.s      IL_00aa
  IL_0041:  br         IL_0135
  IL_0046:  ldarg.0
  IL_0047:  ldc.i4.3
  IL_0048:  call       "char string.this[int].get"
  IL_004d:  stloc.2
  IL_004e:  ldloc.2
  IL_004f:  ldc.i4.s   101
  IL_0051:  bgt.un.s   IL_0065
  IL_0053:  ldloc.2
  IL_0054:  ldc.i4.s   97
  IL_0056:  beq.s      IL_00b9
  IL_0058:  ldloc.2
  IL_0059:  ldc.i4.s   101
  IL_005b:  beq        IL_00e6
  IL_0060:  br         IL_0135
  IL_0065:  ldloc.2
  IL_0066:  ldc.i4.s   105
  IL_0068:  beq.s      IL_00d7
  IL_006a:  ldloc.2
  IL_006b:  ldc.i4.s   119
  IL_006d:  beq.s      IL_00c8
  IL_006f:  br         IL_0135
  IL_0074:  ldarg.0
  IL_0075:  ldstr      "Pid"
  IL_007a:  call       "bool string.op_Equality(string, string)"
  IL_007f:  brtrue.s   IL_00f5
  IL_0081:  br         IL_0135
  IL_0086:  ldarg.0
  IL_0087:  ldstr      "VmHWM"
  IL_008c:  call       "bool string.op_Equality(string, string)"
  IL_0091:  brtrue.s   IL_00fd
  IL_0093:  br         IL_0135
  IL_0098:  ldarg.0
  IL_0099:  ldstr      "VmRSS"
  IL_009e:  call       "bool string.op_Equality(string, string)"
  IL_00a3:  brtrue.s   IL_0105
  IL_00a5:  br         IL_0135
  IL_00aa:  ldarg.0
  IL_00ab:  ldstr      "VmStk"
  IL_00b0:  call       "bool string.op_Equality(string, string)"
  IL_00b5:  brtrue.s   IL_012d
  IL_00b7:  br.s       IL_0135
  IL_00b9:  ldarg.0
  IL_00ba:  ldstr      "VmData"
  IL_00bf:  call       "bool string.op_Equality(string, string)"
  IL_00c4:  brtrue.s   IL_010d
  IL_00c6:  br.s       IL_0135
  IL_00c8:  ldarg.0
  IL_00c9:  ldstr      "VmSwap"
  IL_00ce:  call       "bool string.op_Equality(string, string)"
  IL_00d3:  brtrue.s   IL_0115
  IL_00d5:  br.s       IL_0135
  IL_00d7:  ldarg.0
  IL_00d8:  ldstr      "VmSize"
  IL_00dd:  call       "bool string.op_Equality(string, string)"
  IL_00e2:  brtrue.s   IL_011d
  IL_00e4:  br.s       IL_0135
  IL_00e6:  ldarg.0
  IL_00e7:  ldstr      "VmPeak"
  IL_00ec:  call       "bool string.op_Equality(string, string)"
  IL_00f1:  brtrue.s   IL_0125
  IL_00f3:  br.s       IL_0135
  IL_00f5:  ldstr      "Pid"
  IL_00fa:  stloc.0
  IL_00fb:  br.s       IL_013b
  IL_00fd:  ldstr      "VmHWM"
  IL_0102:  stloc.0
  IL_0103:  br.s       IL_013b
  IL_0105:  ldstr      "VmRSS"
  IL_010a:  stloc.0
  IL_010b:  br.s       IL_013b
  IL_010d:  ldstr      "VmData"
  IL_0112:  stloc.0
  IL_0113:  br.s       IL_013b
  IL_0115:  ldstr      "VmSwap"
  IL_011a:  stloc.0
  IL_011b:  br.s       IL_013b
  IL_011d:  ldstr      "VmSize"
  IL_0122:  stloc.0
  IL_0123:  br.s       IL_013b
  IL_0125:  ldstr      "VmPeak"
  IL_012a:  stloc.0
  IL_012b:  br.s       IL_013b
  IL_012d:  ldstr      "VmStk"
  IL_0132:  stloc.0
  IL_0133:  br.s       IL_013b
  IL_0135:  ldstr      "default"
  IL_013a:  stloc.0
  IL_013b:  ldloc.0
  IL_013c:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      322 (0x142)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x8979e177
  IL_000d:  bgt.un.s   IL_0044
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4     0x1defdbc8
  IL_0015:  bgt.un.s   IL_002f
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4     0xd5b8205
  IL_001d:  beq        IL_00cd
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x1defdbc8
  IL_0028:  beq.s      IL_009d
  IL_002a:  br         IL_013a
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4     0x364be934
  IL_0035:  beq.s      IL_00af
  IL_0037:  ldloc.1
  IL_0038:  ldc.i4     0x8979e177
  IL_003d:  beq.s      IL_00be
  IL_003f:  br         IL_013a
  IL_0044:  ldloc.1
  IL_0045:  ldc.i4     0xce4790c2
  IL_004a:  bgt.un.s   IL_0064
  IL_004c:  ldloc.1
  IL_004d:  ldc.i4     0xb230f6f0
  IL_0052:  beq        IL_00eb
  IL_0057:  ldloc.1
  IL_0058:  ldc.i4     0xce4790c2
  IL_005d:  beq.s      IL_0079
  IL_005f:  br         IL_013a
  IL_0064:  ldloc.1
  IL_0065:  ldc.i4     0xe840f3b7
  IL_006a:  beq.s      IL_00dc
  IL_006c:  ldloc.1
  IL_006d:  ldc.i4     0xf7948524
  IL_0072:  beq.s      IL_008b
  IL_0074:  br         IL_013a
  IL_0079:  ldarg.0
  IL_007a:  ldstr      "Pid"
  IL_007f:  call       "bool string.op_Equality(string, string)"
  IL_0084:  brtrue.s   IL_00fa
  IL_0086:  br         IL_013a
  IL_008b:  ldarg.0
  IL_008c:  ldstr      "VmHWM"
  IL_0091:  call       "bool string.op_Equality(string, string)"
  IL_0096:  brtrue.s   IL_0102
  IL_0098:  br         IL_013a
  IL_009d:  ldarg.0
  IL_009e:  ldstr      "VmRSS"
  IL_00a3:  call       "bool string.op_Equality(string, string)"
  IL_00a8:  brtrue.s   IL_010a
  IL_00aa:  br         IL_013a
  IL_00af:  ldarg.0
  IL_00b0:  ldstr      "VmData"
  IL_00b5:  call       "bool string.op_Equality(string, string)"
  IL_00ba:  brtrue.s   IL_0112
  IL_00bc:  br.s       IL_013a
  IL_00be:  ldarg.0
  IL_00bf:  ldstr      "VmSwap"
  IL_00c4:  call       "bool string.op_Equality(string, string)"
  IL_00c9:  brtrue.s   IL_011a
  IL_00cb:  br.s       IL_013a
  IL_00cd:  ldarg.0
  IL_00ce:  ldstr      "VmSize"
  IL_00d3:  call       "bool string.op_Equality(string, string)"
  IL_00d8:  brtrue.s   IL_0122
  IL_00da:  br.s       IL_013a
  IL_00dc:  ldarg.0
  IL_00dd:  ldstr      "VmPeak"
  IL_00e2:  call       "bool string.op_Equality(string, string)"
  IL_00e7:  brtrue.s   IL_012a
  IL_00e9:  br.s       IL_013a
  IL_00eb:  ldarg.0
  IL_00ec:  ldstr      "VmStk"
  IL_00f1:  call       "bool string.op_Equality(string, string)"
  IL_00f6:  brtrue.s   IL_0132
  IL_00f8:  br.s       IL_013a
  IL_00fa:  ldstr      "Pid"
  IL_00ff:  stloc.0
  IL_0100:  br.s       IL_0140
  IL_0102:  ldstr      "VmHWM"
  IL_0107:  stloc.0
  IL_0108:  br.s       IL_0140
  IL_010a:  ldstr      "VmRSS"
  IL_010f:  stloc.0
  IL_0110:  br.s       IL_0140
  IL_0112:  ldstr      "VmData"
  IL_0117:  stloc.0
  IL_0118:  br.s       IL_0140
  IL_011a:  ldstr      "VmSwap"
  IL_011f:  stloc.0
  IL_0120:  br.s       IL_0140
  IL_0122:  ldstr      "VmSize"
  IL_0127:  stloc.0
  IL_0128:  br.s       IL_0140
  IL_012a:  ldstr      "VmPeak"
  IL_012f:  stloc.0
  IL_0130:  br.s       IL_0140
  IL_0132:  ldstr      "VmStk"
  IL_0137:  stloc.0
  IL_0138:  br.s       IL_0140
  IL_013a:  ldstr      "default"
  IL_013f:  stloc.0
  IL_0140:  ldloc.0
  IL_0141:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void GetHashForChannelBinding()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Net.Security/src/System/Net/Security/Pal.Managed/EndpointChannelBindingToken.cs#L31-L57
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("1.2.840.113549.2.5");
assert("1.2.840.113549.1.1.4");
assert("1.3.14.3.2.26");
assert("1.2.840.10040.4.3");
assert("1.2.840.10045.4.1");
assert("1.2.840.113549.1.1.5");
assert("2.16.840.1.101.3.4.2.1");
assert("1.2.840.10045.4.3.2");
assert("1.2.840.113549.1.1.11");
assert("2.16.840.1.101.3.4.2.2");
assert("1.2.840.10045.4.3.3");
assert("1.2.840.113549.1.1.12");
assert("2.16.840.1.101.3.4.2.3");
assert("1.2.840.10045.4.3.4");
assert("1.2.840.113549.1.1.13");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "1.2.840.113549.2.5" => "1.2.840.113549.2.5",
            "1.2.840.113549.1.1.4" => "1.2.840.113549.1.1.4",
            "1.3.14.3.2.26" => "1.3.14.3.2.26",
            "1.2.840.10040.4.3" => "1.2.840.10040.4.3",
            "1.2.840.10045.4.1" => "1.2.840.10045.4.1",
            "1.2.840.113549.1.1.5" => "1.2.840.113549.1.1.5",
            "2.16.840.1.101.3.4.2.1" => "2.16.840.1.101.3.4.2.1",
            "1.2.840.10045.4.3.2" => "1.2.840.10045.4.3.2",
            "1.2.840.113549.1.1.11" => "1.2.840.113549.1.1.11",
            "2.16.840.1.101.3.4.2.2" => "2.16.840.1.101.3.4.2.2",
            "1.2.840.10045.4.3.3" => "1.2.840.10045.4.3.3",
            "1.2.840.113549.1.1.12" => "1.2.840.113549.1.1.12",
            "2.16.840.1.101.3.4.2.3" => "2.16.840.1.101.3.4.2.3",
            "1.2.840.10045.4.3.4" => "1.2.840.10045.4.3.4",
            "1.2.840.113549.1.1.13" => "1.2.840.113549.1.1.13",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      663 (0x297)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_028f
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.s   13
  IL_0010:  sub
  IL_0011:  switch    (
        IL_0127,
        IL_028f,
        IL_028f,
        IL_028f,
        IL_0061,
        IL_00e8,
        IL_00a2,
        IL_0043,
        IL_00c5,
        IL_007f)
  IL_003e:  br         IL_028f
  IL_0043:  ldarg.0
  IL_0044:  ldc.i4.s   19
  IL_0046:  call       "char string.this[int].get"
  IL_004b:  stloc.2
  IL_004c:  ldloc.2
  IL_004d:  ldc.i4.s   52
  IL_004f:  beq        IL_00fd
  IL_0054:  ldloc.2
  IL_0055:  ldc.i4.s   53
  IL_0057:  beq        IL_0112
  IL_005c:  br         IL_028f
  IL_0061:  ldarg.0
  IL_0062:  ldc.i4.s   12
  IL_0064:  call       "char string.this[int].get"
  IL_0069:  stloc.2
  IL_006a:  ldloc.2
  IL_006b:  ldc.i4.s   48
  IL_006d:  beq        IL_013c
  IL_0072:  ldloc.2
  IL_0073:  ldc.i4.s   53
  IL_0075:  beq        IL_0151
  IL_007a:  br         IL_028f
  IL_007f:  ldarg.0
  IL_0080:  ldc.i4.s   21
  IL_0082:  call       "char string.this[int].get"
  IL_0087:  stloc.2
  IL_0088:  ldloc.2
  IL_0089:  ldc.i4.s   49
  IL_008b:  sub
  IL_008c:  switch    (
        IL_0166,
        IL_017b,
        IL_0190)
  IL_009d:  br         IL_028f
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   18
  IL_00a5:  call       "char string.this[int].get"
  IL_00aa:  stloc.2
  IL_00ab:  ldloc.2
  IL_00ac:  ldc.i4.s   50
  IL_00ae:  sub
  IL_00af:  switch    (
        IL_01a5,
        IL_01ba,
        IL_01cf)
  IL_00c0:  br         IL_028f
  IL_00c5:  ldarg.0
  IL_00c6:  ldc.i4.s   20
  IL_00c8:  call       "char string.this[int].get"
  IL_00cd:  stloc.2
  IL_00ce:  ldloc.2
  IL_00cf:  ldc.i4.s   49
  IL_00d1:  sub
  IL_00d2:  switch    (
        IL_01e4,
        IL_01f6,
        IL_0208)
  IL_00e3:  br         IL_028f
  IL_00e8:  ldarg.0
  IL_00e9:  ldstr      "1.2.840.113549.2.5"
  IL_00ee:  call       "bool string.op_Equality(string, string)"
  IL_00f3:  brtrue     IL_0217
  IL_00f8:  br         IL_028f
  IL_00fd:  ldarg.0
  IL_00fe:  ldstr      "1.2.840.113549.1.1.4"
  IL_0103:  call       "bool string.op_Equality(string, string)"
  IL_0108:  brtrue     IL_021f
  IL_010d:  br         IL_028f
  IL_0112:  ldarg.0
  IL_0113:  ldstr      "1.2.840.113549.1.1.5"
  IL_0118:  call       "bool string.op_Equality(string, string)"
  IL_011d:  brtrue     IL_023f
  IL_0122:  br         IL_028f
  IL_0127:  ldarg.0
  IL_0128:  ldstr      "1.3.14.3.2.26"
  IL_012d:  call       "bool string.op_Equality(string, string)"
  IL_0132:  brtrue     IL_0227
  IL_0137:  br         IL_028f
  IL_013c:  ldarg.0
  IL_013d:  ldstr      "1.2.840.10040.4.3"
  IL_0142:  call       "bool string.op_Equality(string, string)"
  IL_0147:  brtrue     IL_022f
  IL_014c:  br         IL_028f
  IL_0151:  ldarg.0
  IL_0152:  ldstr      "1.2.840.10045.4.1"
  IL_0157:  call       "bool string.op_Equality(string, string)"
  IL_015c:  brtrue     IL_0237
  IL_0161:  br         IL_028f
  IL_0166:  ldarg.0
  IL_0167:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_016c:  call       "bool string.op_Equality(string, string)"
  IL_0171:  brtrue     IL_0247
  IL_0176:  br         IL_028f
  IL_017b:  ldarg.0
  IL_017c:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_0181:  call       "bool string.op_Equality(string, string)"
  IL_0186:  brtrue     IL_025f
  IL_018b:  br         IL_028f
  IL_0190:  ldarg.0
  IL_0191:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_0196:  call       "bool string.op_Equality(string, string)"
  IL_019b:  brtrue     IL_0277
  IL_01a0:  br         IL_028f
  IL_01a5:  ldarg.0
  IL_01a6:  ldstr      "1.2.840.10045.4.3.2"
  IL_01ab:  call       "bool string.op_Equality(string, string)"
  IL_01b0:  brtrue     IL_024f
  IL_01b5:  br         IL_028f
  IL_01ba:  ldarg.0
  IL_01bb:  ldstr      "1.2.840.10045.4.3.3"
  IL_01c0:  call       "bool string.op_Equality(string, string)"
  IL_01c5:  brtrue     IL_0267
  IL_01ca:  br         IL_028f
  IL_01cf:  ldarg.0
  IL_01d0:  ldstr      "1.2.840.10045.4.3.4"
  IL_01d5:  call       "bool string.op_Equality(string, string)"
  IL_01da:  brtrue     IL_027f
  IL_01df:  br         IL_028f
  IL_01e4:  ldarg.0
  IL_01e5:  ldstr      "1.2.840.113549.1.1.11"
  IL_01ea:  call       "bool string.op_Equality(string, string)"
  IL_01ef:  brtrue.s   IL_0257
  IL_01f1:  br         IL_028f
  IL_01f6:  ldarg.0
  IL_01f7:  ldstr      "1.2.840.113549.1.1.12"
  IL_01fc:  call       "bool string.op_Equality(string, string)"
  IL_0201:  brtrue.s   IL_026f
  IL_0203:  br         IL_028f
  IL_0208:  ldarg.0
  IL_0209:  ldstr      "1.2.840.113549.1.1.13"
  IL_020e:  call       "bool string.op_Equality(string, string)"
  IL_0213:  brtrue.s   IL_0287
  IL_0215:  br.s       IL_028f
  IL_0217:  ldstr      "1.2.840.113549.2.5"
  IL_021c:  stloc.0
  IL_021d:  br.s       IL_0295
  IL_021f:  ldstr      "1.2.840.113549.1.1.4"
  IL_0224:  stloc.0
  IL_0225:  br.s       IL_0295
  IL_0227:  ldstr      "1.3.14.3.2.26"
  IL_022c:  stloc.0
  IL_022d:  br.s       IL_0295
  IL_022f:  ldstr      "1.2.840.10040.4.3"
  IL_0234:  stloc.0
  IL_0235:  br.s       IL_0295
  IL_0237:  ldstr      "1.2.840.10045.4.1"
  IL_023c:  stloc.0
  IL_023d:  br.s       IL_0295
  IL_023f:  ldstr      "1.2.840.113549.1.1.5"
  IL_0244:  stloc.0
  IL_0245:  br.s       IL_0295
  IL_0247:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_024c:  stloc.0
  IL_024d:  br.s       IL_0295
  IL_024f:  ldstr      "1.2.840.10045.4.3.2"
  IL_0254:  stloc.0
  IL_0255:  br.s       IL_0295
  IL_0257:  ldstr      "1.2.840.113549.1.1.11"
  IL_025c:  stloc.0
  IL_025d:  br.s       IL_0295
  IL_025f:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_0264:  stloc.0
  IL_0265:  br.s       IL_0295
  IL_0267:  ldstr      "1.2.840.10045.4.3.3"
  IL_026c:  stloc.0
  IL_026d:  br.s       IL_0295
  IL_026f:  ldstr      "1.2.840.113549.1.1.12"
  IL_0274:  stloc.0
  IL_0275:  br.s       IL_0295
  IL_0277:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_027c:  stloc.0
  IL_027d:  br.s       IL_0295
  IL_027f:  ldstr      "1.2.840.10045.4.3.4"
  IL_0284:  stloc.0
  IL_0285:  br.s       IL_0295
  IL_0287:  ldstr      "1.2.840.113549.1.1.13"
  IL_028c:  stloc.0
  IL_028d:  br.s       IL_0295
  IL_028f:  ldstr      "default"
  IL_0294:  stloc.0
  IL_0295:  ldloc.0
  IL_0296:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      684 (0x2ac)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x2607050f
  IL_000d:  bgt.un.s   IL_007b
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4     0x23070056
  IL_0015:  bgt.un.s   IL_003d
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4     0x1d3018af
  IL_001d:  beq        IL_014e
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x1e6ebeee
  IL_0028:  beq        IL_0139
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0x23070056
  IL_0033:  beq        IL_01cc
  IL_0038:  br         IL_02a4
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4     0x240701e9
  IL_0043:  bgt.un.s   IL_0060
  IL_0045:  ldloc.1
  IL_0046:  ldc.i4     0x23db8e98
  IL_004b:  beq        IL_0163
  IL_0050:  ldloc.1
  IL_0051:  ldc.i4     0x240701e9
  IL_0056:  beq        IL_018d
  IL_005b:  br         IL_02a4
  IL_0060:  ldloc.1
  IL_0061:  ldc.i4     0x24db902b
  IL_0066:  beq        IL_010f
  IL_006b:  ldloc.1
  IL_006c:  ldc.i4     0x2607050f
  IL_0071:  beq        IL_020b
  IL_0076:  br         IL_02a4
  IL_007b:  ldloc.1
  IL_007c:  ldc.i4     0x95ab4e75
  IL_0081:  bgt.un.s   IL_00c1
  IL_0083:  ldloc.1
  IL_0084:  ldc.i4     0x93ab4b4f
  IL_0089:  bgt.un.s   IL_00a6
  IL_008b:  ldloc.1
  IL_008c:  ldc.i4     0x332fa045
  IL_0091:  beq        IL_0124
  IL_0096:  ldloc.1
  IL_0097:  ldc.i4     0x93ab4b4f
  IL_009c:  beq        IL_01a2
  IL_00a1:  br         IL_02a4
  IL_00a6:  ldloc.1
  IL_00a7:  ldc.i4     0x94ab4ce2
  IL_00ac:  beq        IL_01e1
  IL_00b1:  ldloc.1
  IL_00b2:  ldc.i4     0x95ab4e75
  IL_00b7:  beq        IL_021d
  IL_00bc:  br         IL_02a4
  IL_00c1:  ldloc.1
  IL_00c2:  ldc.i4     0xdd91cb42
  IL_00c7:  bgt.un.s   IL_00e4
  IL_00c9:  ldloc.1
  IL_00ca:  ldc.i4     0xdc91c9af
  IL_00cf:  beq        IL_0178
  IL_00d4:  ldloc.1
  IL_00d5:  ldc.i4     0xdd91cb42
  IL_00da:  beq        IL_01b7
  IL_00df:  br         IL_02a4
  IL_00e4:  ldloc.1
  IL_00e5:  ldc.i4     0xde91ccd5
  IL_00ea:  beq        IL_01f6
  IL_00ef:  ldloc.1
  IL_00f0:  ldc.i4     0xe0252800
  IL_00f5:  bne.un     IL_02a4
  IL_00fa:  ldarg.0
  IL_00fb:  ldstr      "1.2.840.113549.2.5"
  IL_0100:  call       "bool string.op_Equality(string, string)"
  IL_0105:  brtrue     IL_022c
  IL_010a:  br         IL_02a4
  IL_010f:  ldarg.0
  IL_0110:  ldstr      "1.2.840.113549.1.1.4"
  IL_0115:  call       "bool string.op_Equality(string, string)"
  IL_011a:  brtrue     IL_0234
  IL_011f:  br         IL_02a4
  IL_0124:  ldarg.0
  IL_0125:  ldstr      "1.3.14.3.2.26"
  IL_012a:  call       "bool string.op_Equality(string, string)"
  IL_012f:  brtrue     IL_023c
  IL_0134:  br         IL_02a4
  IL_0139:  ldarg.0
  IL_013a:  ldstr      "1.2.840.10040.4.3"
  IL_013f:  call       "bool string.op_Equality(string, string)"
  IL_0144:  brtrue     IL_0244
  IL_0149:  br         IL_02a4
  IL_014e:  ldarg.0
  IL_014f:  ldstr      "1.2.840.10045.4.1"
  IL_0154:  call       "bool string.op_Equality(string, string)"
  IL_0159:  brtrue     IL_024c
  IL_015e:  br         IL_02a4
  IL_0163:  ldarg.0
  IL_0164:  ldstr      "1.2.840.113549.1.1.5"
  IL_0169:  call       "bool string.op_Equality(string, string)"
  IL_016e:  brtrue     IL_0254
  IL_0173:  br         IL_02a4
  IL_0178:  ldarg.0
  IL_0179:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_017e:  call       "bool string.op_Equality(string, string)"
  IL_0183:  brtrue     IL_025c
  IL_0188:  br         IL_02a4
  IL_018d:  ldarg.0
  IL_018e:  ldstr      "1.2.840.10045.4.3.2"
  IL_0193:  call       "bool string.op_Equality(string, string)"
  IL_0198:  brtrue     IL_0264
  IL_019d:  br         IL_02a4
  IL_01a2:  ldarg.0
  IL_01a3:  ldstr      "1.2.840.113549.1.1.11"
  IL_01a8:  call       "bool string.op_Equality(string, string)"
  IL_01ad:  brtrue     IL_026c
  IL_01b2:  br         IL_02a4
  IL_01b7:  ldarg.0
  IL_01b8:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_01bd:  call       "bool string.op_Equality(string, string)"
  IL_01c2:  brtrue     IL_0274
  IL_01c7:  br         IL_02a4
  IL_01cc:  ldarg.0
  IL_01cd:  ldstr      "1.2.840.10045.4.3.3"
  IL_01d2:  call       "bool string.op_Equality(string, string)"
  IL_01d7:  brtrue     IL_027c
  IL_01dc:  br         IL_02a4
  IL_01e1:  ldarg.0
  IL_01e2:  ldstr      "1.2.840.113549.1.1.12"
  IL_01e7:  call       "bool string.op_Equality(string, string)"
  IL_01ec:  brtrue     IL_0284
  IL_01f1:  br         IL_02a4
  IL_01f6:  ldarg.0
  IL_01f7:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_01fc:  call       "bool string.op_Equality(string, string)"
  IL_0201:  brtrue     IL_028c
  IL_0206:  br         IL_02a4
  IL_020b:  ldarg.0
  IL_020c:  ldstr      "1.2.840.10045.4.3.4"
  IL_0211:  call       "bool string.op_Equality(string, string)"
  IL_0216:  brtrue.s   IL_0294
  IL_0218:  br         IL_02a4
  IL_021d:  ldarg.0
  IL_021e:  ldstr      "1.2.840.113549.1.1.13"
  IL_0223:  call       "bool string.op_Equality(string, string)"
  IL_0228:  brtrue.s   IL_029c
  IL_022a:  br.s       IL_02a4
  IL_022c:  ldstr      "1.2.840.113549.2.5"
  IL_0231:  stloc.0
  IL_0232:  br.s       IL_02aa
  IL_0234:  ldstr      "1.2.840.113549.1.1.4"
  IL_0239:  stloc.0
  IL_023a:  br.s       IL_02aa
  IL_023c:  ldstr      "1.3.14.3.2.26"
  IL_0241:  stloc.0
  IL_0242:  br.s       IL_02aa
  IL_0244:  ldstr      "1.2.840.10040.4.3"
  IL_0249:  stloc.0
  IL_024a:  br.s       IL_02aa
  IL_024c:  ldstr      "1.2.840.10045.4.1"
  IL_0251:  stloc.0
  IL_0252:  br.s       IL_02aa
  IL_0254:  ldstr      "1.2.840.113549.1.1.5"
  IL_0259:  stloc.0
  IL_025a:  br.s       IL_02aa
  IL_025c:  ldstr      "2.16.840.1.101.3.4.2.1"
  IL_0261:  stloc.0
  IL_0262:  br.s       IL_02aa
  IL_0264:  ldstr      "1.2.840.10045.4.3.2"
  IL_0269:  stloc.0
  IL_026a:  br.s       IL_02aa
  IL_026c:  ldstr      "1.2.840.113549.1.1.11"
  IL_0271:  stloc.0
  IL_0272:  br.s       IL_02aa
  IL_0274:  ldstr      "2.16.840.1.101.3.4.2.2"
  IL_0279:  stloc.0
  IL_027a:  br.s       IL_02aa
  IL_027c:  ldstr      "1.2.840.10045.4.3.3"
  IL_0281:  stloc.0
  IL_0282:  br.s       IL_02aa
  IL_0284:  ldstr      "1.2.840.113549.1.1.12"
  IL_0289:  stloc.0
  IL_028a:  br.s       IL_02aa
  IL_028c:  ldstr      "2.16.840.1.101.3.4.2.3"
  IL_0291:  stloc.0
  IL_0292:  br.s       IL_02aa
  IL_0294:  ldstr      "1.2.840.10045.4.3.4"
  IL_0299:  stloc.0
  IL_029a:  br.s       IL_02aa
  IL_029c:  ldstr      "1.2.840.113549.1.1.13"
  IL_02a1:  stloc.0
  IL_02a2:  br.s       IL_02aa
  IL_02a4:  ldstr      "default"
  IL_02a9:  stloc.0
  IL_02aa:  ldloc.0
  IL_02ab:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void WriteEntityRef()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Private.Xml.Linq/src/System/Xml/Linq/XNodeBuilder.cs#L115-L134
        var source = """
assert(null, "default");
assert("", "default");
assert("apos");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "amp" => "amp",
            "apos" => "apos",
            "gt" => "gt",
            "lt" => "lt",
            "quot" => "quot",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      115 (0x73)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "amp"
  IL_0006:  call       "bool string.op_Equality(string, string)"
  IL_000b:  brtrue.s   IL_0043
  IL_000d:  ldarg.0
  IL_000e:  ldstr      "apos"
  IL_0013:  call       "bool string.op_Equality(string, string)"
  IL_0018:  brtrue.s   IL_004b
  IL_001a:  ldarg.0
  IL_001b:  ldstr      "gt"
  IL_0020:  call       "bool string.op_Equality(string, string)"
  IL_0025:  brtrue.s   IL_0053
  IL_0027:  ldarg.0
  IL_0028:  ldstr      "lt"
  IL_002d:  call       "bool string.op_Equality(string, string)"
  IL_0032:  brtrue.s   IL_005b
  IL_0034:  ldarg.0
  IL_0035:  ldstr      "quot"
  IL_003a:  call       "bool string.op_Equality(string, string)"
  IL_003f:  brtrue.s   IL_0063
  IL_0041:  br.s       IL_006b
  IL_0043:  ldstr      "amp"
  IL_0048:  stloc.0
  IL_0049:  br.s       IL_0071
  IL_004b:  ldstr      "apos"
  IL_0050:  stloc.0
  IL_0051:  br.s       IL_0071
  IL_0053:  ldstr      "gt"
  IL_0058:  stloc.0
  IL_0059:  br.s       IL_0071
  IL_005b:  ldstr      "lt"
  IL_0060:  stloc.0
  IL_0061:  br.s       IL_0071
  IL_0063:  ldstr      "quot"
  IL_0068:  stloc.0
  IL_0069:  br.s       IL_0071
  IL_006b:  ldstr      "default"
  IL_0070:  stloc.0
  IL_0071:  ldloc.0
  IL_0072:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void FunctionAvailable()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Private.Xml/src/System/Xml/Xsl/XsltOld/XsltCompileContext.cs#L451-L485
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("last");
assert("position");
assert("name");
assert("namespace-uri");
assert("local-name");
assert("count");
assert("id");
assert("string");
assert("concat");
assert("starts-with");
assert("contains");
assert("substring-before");
assert("substring-after");
assert("substring");
assert("string-length");
assert("normalize-space");
assert("translate");
assert("boolean");
assert("not");
assert("true");
assert("false");
assert("lang");
assert("number");
assert("sum");
assert("floor");
assert("ceiling");
assert("round");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "last" => "last",
            "position" => "position",
            "name" => "name",
            "namespace-uri" => "namespace-uri",
            "local-name" => "local-name",
            "count" => "count",
            "id" => "id",
            "string" => "string",
            "concat" => "concat",
            "starts-with" => "starts-with",
            "contains" => "contains",
            "substring-before" => "substring-before",
            "substring-after" => "substring-after",
            "substring" => "substring",
            "string-length" => "string-length",
            "normalize-space" => "normalize-space",
            "translate" => "translate",
            "boolean" => "boolean",
            "not" => "not",
            "true" => "true",
            "false" => "false",
            "lang" => "lang",
            "number" => "number",
            "sum" => "sum",
            "floor" => "floor",
            "ceiling" => "ceiling",
            "round" => "round",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     1231 (0x4cf)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_04c7
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.2
  IL_000f:  sub
  IL_0010:  switch    (
        IL_02a8,
        IL_017a,
        IL_0056,
        IL_00c7,
        IL_00fe,
        IL_015d,
        IL_008d,
        IL_0140,
        IL_023f,
        IL_02fc,
        IL_04c7,
        IL_00aa,
        IL_04c7,
        IL_0123,
        IL_0311)
  IL_0051:  br         IL_04c7
  IL_0056:  ldarg.0
  IL_0057:  ldc.i4.2
  IL_0058:  call       "char string.this[int].get"
  IL_005d:  stloc.2
  IL_005e:  ldloc.2
  IL_005f:  ldc.i4.s   110
  IL_0061:  bgt.un.s   IL_0078
  IL_0063:  ldloc.2
  IL_0064:  ldc.i4.s   109
  IL_0066:  beq        IL_01ac
  IL_006b:  ldloc.2
  IL_006c:  ldc.i4.s   110
  IL_006e:  beq        IL_01d6
  IL_0073:  br         IL_04c7
  IL_0078:  ldloc.2
  IL_0079:  ldc.i4.s   115
  IL_007b:  beq        IL_0197
  IL_0080:  ldloc.2
  IL_0081:  ldc.i4.s   117
  IL_0083:  beq        IL_01c1
  IL_0088:  br         IL_04c7
  IL_008d:  ldarg.0
  IL_008e:  ldc.i4.0
  IL_008f:  call       "char string.this[int].get"
  IL_0094:  stloc.2
  IL_0095:  ldloc.2
  IL_0096:  ldc.i4.s   99
  IL_0098:  beq        IL_0200
  IL_009d:  ldloc.2
  IL_009e:  ldc.i4.s   112
  IL_00a0:  beq        IL_01eb
  IL_00a5:  br         IL_04c7
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.0
  IL_00ac:  call       "char string.this[int].get"
  IL_00b1:  stloc.2
  IL_00b2:  ldloc.2
  IL_00b3:  ldc.i4.s   110
  IL_00b5:  beq        IL_0215
  IL_00ba:  ldloc.2
  IL_00bb:  ldc.i4.s   115
  IL_00bd:  beq        IL_022a
  IL_00c2:  br         IL_04c7
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.4
  IL_00c9:  call       "char string.this[int].get"
  IL_00ce:  stloc.2
  IL_00cf:  ldloc.2
  IL_00d0:  ldc.i4.s   101
  IL_00d2:  bgt.un.s   IL_00e9
  IL_00d4:  ldloc.2
  IL_00d5:  ldc.i4.s   100
  IL_00d7:  beq        IL_0293
  IL_00dc:  ldloc.2
  IL_00dd:  ldc.i4.s   101
  IL_00df:  beq        IL_0269
  IL_00e4:  br         IL_04c7
  IL_00e9:  ldloc.2
  IL_00ea:  ldc.i4.s   114
  IL_00ec:  beq        IL_027e
  IL_00f1:  ldloc.2
  IL_00f2:  ldc.i4.s   116
  IL_00f4:  beq        IL_0254
  IL_00f9:  br         IL_04c7
  IL_00fe:  ldarg.0
  IL_00ff:  ldc.i4.0
  IL_0100:  call       "char string.this[int].get"
  IL_0105:  stloc.2
  IL_0106:  ldloc.2
  IL_0107:  ldc.i4.s   99
  IL_0109:  beq        IL_02d2
  IL_010e:  ldloc.2
  IL_010f:  ldc.i4.s   110
  IL_0111:  beq        IL_02e7
  IL_0116:  ldloc.2
  IL_0117:  ldc.i4.s   115
  IL_0119:  beq        IL_02bd
  IL_011e:  br         IL_04c7
  IL_0123:  ldarg.0
  IL_0124:  ldc.i4.0
  IL_0125:  call       "char string.this[int].get"
  IL_012a:  stloc.2
  IL_012b:  ldloc.2
  IL_012c:  ldc.i4.s   110
  IL_012e:  beq        IL_033b
  IL_0133:  ldloc.2
  IL_0134:  ldc.i4.s   115
  IL_0136:  beq        IL_0326
  IL_013b:  br         IL_04c7
  IL_0140:  ldarg.0
  IL_0141:  ldc.i4.0
  IL_0142:  call       "char string.this[int].get"
  IL_0147:  stloc.2
  IL_0148:  ldloc.2
  IL_0149:  ldc.i4.s   115
  IL_014b:  beq        IL_0350
  IL_0150:  ldloc.2
  IL_0151:  ldc.i4.s   116
  IL_0153:  beq        IL_0365
  IL_0158:  br         IL_04c7
  IL_015d:  ldarg.0
  IL_015e:  ldc.i4.0
  IL_015f:  call       "char string.this[int].get"
  IL_0164:  stloc.2
  IL_0165:  ldloc.2
  IL_0166:  ldc.i4.s   98
  IL_0168:  beq        IL_037a
  IL_016d:  ldloc.2
  IL_016e:  ldc.i4.s   99
  IL_0170:  beq        IL_038f
  IL_0175:  br         IL_04c7
  IL_017a:  ldarg.0
  IL_017b:  ldc.i4.0
  IL_017c:  call       "char string.this[int].get"
  IL_0181:  stloc.2
  IL_0182:  ldloc.2
  IL_0183:  ldc.i4.s   110
  IL_0185:  beq        IL_03a4
  IL_018a:  ldloc.2
  IL_018b:  ldc.i4.s   115
  IL_018d:  beq        IL_03b9
  IL_0192:  br         IL_04c7
  IL_0197:  ldarg.0
  IL_0198:  ldstr      "last"
  IL_019d:  call       "bool string.op_Equality(string, string)"
  IL_01a2:  brtrue     IL_03ce
  IL_01a7:  br         IL_04c7
  IL_01ac:  ldarg.0
  IL_01ad:  ldstr      "name"
  IL_01b2:  call       "bool string.op_Equality(string, string)"
  IL_01b7:  brtrue     IL_03e4
  IL_01bc:  br         IL_04c7
  IL_01c1:  ldarg.0
  IL_01c2:  ldstr      "true"
  IL_01c7:  call       "bool string.op_Equality(string, string)"
  IL_01cc:  brtrue     IL_0487
  IL_01d1:  br         IL_04c7
  IL_01d6:  ldarg.0
  IL_01d7:  ldstr      "lang"
  IL_01dc:  call       "bool string.op_Equality(string, string)"
  IL_01e1:  brtrue     IL_0497
  IL_01e6:  br         IL_04c7
  IL_01eb:  ldarg.0
  IL_01ec:  ldstr      "position"
  IL_01f1:  call       "bool string.op_Equality(string, string)"
  IL_01f6:  brtrue     IL_03d9
  IL_01fb:  br         IL_04c7
  IL_0200:  ldarg.0
  IL_0201:  ldstr      "contains"
  IL_0206:  call       "bool string.op_Equality(string, string)"
  IL_020b:  brtrue     IL_043c
  IL_0210:  br         IL_04c7
  IL_0215:  ldarg.0
  IL_0216:  ldstr      "namespace-uri"
  IL_021b:  call       "bool string.op_Equality(string, string)"
  IL_0220:  brtrue     IL_03ef
  IL_0225:  br         IL_04c7
  IL_022a:  ldarg.0
  IL_022b:  ldstr      "string-length"
  IL_0230:  call       "bool string.op_Equality(string, string)"
  IL_0235:  brtrue     IL_045f
  IL_023a:  br         IL_04c7
  IL_023f:  ldarg.0
  IL_0240:  ldstr      "local-name"
  IL_0245:  call       "bool string.op_Equality(string, string)"
  IL_024a:  brtrue     IL_03fa
  IL_024f:  br         IL_04c7
  IL_0254:  ldarg.0
  IL_0255:  ldstr      "count"
  IL_025a:  call       "bool string.op_Equality(string, string)"
  IL_025f:  brtrue     IL_0405
  IL_0264:  br         IL_04c7
  IL_0269:  ldarg.0
  IL_026a:  ldstr      "false"
  IL_026f:  call       "bool string.op_Equality(string, string)"
  IL_0274:  brtrue     IL_048f
  IL_0279:  br         IL_04c7
  IL_027e:  ldarg.0
  IL_027f:  ldstr      "floor"
  IL_0284:  call       "bool string.op_Equality(string, string)"
  IL_0289:  brtrue     IL_04af
  IL_028e:  br         IL_04c7
  IL_0293:  ldarg.0
  IL_0294:  ldstr      "round"
  IL_0299:  call       "bool string.op_Equality(string, string)"
  IL_029e:  brtrue     IL_04bf
  IL_02a3:  br         IL_04c7
  IL_02a8:  ldarg.0
  IL_02a9:  ldstr      "id"
  IL_02ae:  call       "bool string.op_Equality(string, string)"
  IL_02b3:  brtrue     IL_0410
  IL_02b8:  br         IL_04c7
  IL_02bd:  ldarg.0
  IL_02be:  ldstr      "string"
  IL_02c3:  call       "bool string.op_Equality(string, string)"
  IL_02c8:  brtrue     IL_041b
  IL_02cd:  br         IL_04c7
  IL_02d2:  ldarg.0
  IL_02d3:  ldstr      "concat"
  IL_02d8:  call       "bool string.op_Equality(string, string)"
  IL_02dd:  brtrue     IL_0426
  IL_02e2:  br         IL_04c7
  IL_02e7:  ldarg.0
  IL_02e8:  ldstr      "number"
  IL_02ed:  call       "bool string.op_Equality(string, string)"
  IL_02f2:  brtrue     IL_049f
  IL_02f7:  br         IL_04c7
  IL_02fc:  ldarg.0
  IL_02fd:  ldstr      "starts-with"
  IL_0302:  call       "bool string.op_Equality(string, string)"
  IL_0307:  brtrue     IL_0431
  IL_030c:  br         IL_04c7
  IL_0311:  ldarg.0
  IL_0312:  ldstr      "substring-before"
  IL_0317:  call       "bool string.op_Equality(string, string)"
  IL_031c:  brtrue     IL_0447
  IL_0321:  br         IL_04c7
  IL_0326:  ldarg.0
  IL_0327:  ldstr      "substring-after"
  IL_032c:  call       "bool string.op_Equality(string, string)"
  IL_0331:  brtrue     IL_044f
  IL_0336:  br         IL_04c7
  IL_033b:  ldarg.0
  IL_033c:  ldstr      "normalize-space"
  IL_0341:  call       "bool string.op_Equality(string, string)"
  IL_0346:  brtrue     IL_0467
  IL_034b:  br         IL_04c7
  IL_0350:  ldarg.0
  IL_0351:  ldstr      "substring"
  IL_0356:  call       "bool string.op_Equality(string, string)"
  IL_035b:  brtrue     IL_0457
  IL_0360:  br         IL_04c7
  IL_0365:  ldarg.0
  IL_0366:  ldstr      "translate"
  IL_036b:  call       "bool string.op_Equality(string, string)"
  IL_0370:  brtrue     IL_046f
  IL_0375:  br         IL_04c7
  IL_037a:  ldarg.0
  IL_037b:  ldstr      "boolean"
  IL_0380:  call       "bool string.op_Equality(string, string)"
  IL_0385:  brtrue     IL_0477
  IL_038a:  br         IL_04c7
  IL_038f:  ldarg.0
  IL_0390:  ldstr      "ceiling"
  IL_0395:  call       "bool string.op_Equality(string, string)"
  IL_039a:  brtrue     IL_04b7
  IL_039f:  br         IL_04c7
  IL_03a4:  ldarg.0
  IL_03a5:  ldstr      "not"
  IL_03aa:  call       "bool string.op_Equality(string, string)"
  IL_03af:  brtrue     IL_047f
  IL_03b4:  br         IL_04c7
  IL_03b9:  ldarg.0
  IL_03ba:  ldstr      "sum"
  IL_03bf:  call       "bool string.op_Equality(string, string)"
  IL_03c4:  brtrue     IL_04a7
  IL_03c9:  br         IL_04c7
  IL_03ce:  ldstr      "last"
  IL_03d3:  stloc.0
  IL_03d4:  br         IL_04cd
  IL_03d9:  ldstr      "position"
  IL_03de:  stloc.0
  IL_03df:  br         IL_04cd
  IL_03e4:  ldstr      "name"
  IL_03e9:  stloc.0
  IL_03ea:  br         IL_04cd
  IL_03ef:  ldstr      "namespace-uri"
  IL_03f4:  stloc.0
  IL_03f5:  br         IL_04cd
  IL_03fa:  ldstr      "local-name"
  IL_03ff:  stloc.0
  IL_0400:  br         IL_04cd
  IL_0405:  ldstr      "count"
  IL_040a:  stloc.0
  IL_040b:  br         IL_04cd
  IL_0410:  ldstr      "id"
  IL_0415:  stloc.0
  IL_0416:  br         IL_04cd
  IL_041b:  ldstr      "string"
  IL_0420:  stloc.0
  IL_0421:  br         IL_04cd
  IL_0426:  ldstr      "concat"
  IL_042b:  stloc.0
  IL_042c:  br         IL_04cd
  IL_0431:  ldstr      "starts-with"
  IL_0436:  stloc.0
  IL_0437:  br         IL_04cd
  IL_043c:  ldstr      "contains"
  IL_0441:  stloc.0
  IL_0442:  br         IL_04cd
  IL_0447:  ldstr      "substring-before"
  IL_044c:  stloc.0
  IL_044d:  br.s       IL_04cd
  IL_044f:  ldstr      "substring-after"
  IL_0454:  stloc.0
  IL_0455:  br.s       IL_04cd
  IL_0457:  ldstr      "substring"
  IL_045c:  stloc.0
  IL_045d:  br.s       IL_04cd
  IL_045f:  ldstr      "string-length"
  IL_0464:  stloc.0
  IL_0465:  br.s       IL_04cd
  IL_0467:  ldstr      "normalize-space"
  IL_046c:  stloc.0
  IL_046d:  br.s       IL_04cd
  IL_046f:  ldstr      "translate"
  IL_0474:  stloc.0
  IL_0475:  br.s       IL_04cd
  IL_0477:  ldstr      "boolean"
  IL_047c:  stloc.0
  IL_047d:  br.s       IL_04cd
  IL_047f:  ldstr      "not"
  IL_0484:  stloc.0
  IL_0485:  br.s       IL_04cd
  IL_0487:  ldstr      "true"
  IL_048c:  stloc.0
  IL_048d:  br.s       IL_04cd
  IL_048f:  ldstr      "false"
  IL_0494:  stloc.0
  IL_0495:  br.s       IL_04cd
  IL_0497:  ldstr      "lang"
  IL_049c:  stloc.0
  IL_049d:  br.s       IL_04cd
  IL_049f:  ldstr      "number"
  IL_04a4:  stloc.0
  IL_04a5:  br.s       IL_04cd
  IL_04a7:  ldstr      "sum"
  IL_04ac:  stloc.0
  IL_04ad:  br.s       IL_04cd
  IL_04af:  ldstr      "floor"
  IL_04b4:  stloc.0
  IL_04b5:  br.s       IL_04cd
  IL_04b7:  ldstr      "ceiling"
  IL_04bc:  stloc.0
  IL_04bd:  br.s       IL_04cd
  IL_04bf:  ldstr      "round"
  IL_04c4:  stloc.0
  IL_04c5:  br.s       IL_04cd
  IL_04c7:  ldstr      "default"
  IL_04cc:  stloc.0
  IL_04cd:  ldloc.0
  IL_04ce:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     1266 (0x4f2)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x6ccaf138
  IL_000d:  bgt.un     IL_00da
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x39b1ddf4
  IL_0018:  bgt.un.s   IL_006e
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4     0x1bd670a0
  IL_0020:  bgt.un.s   IL_0048
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0xb069958
  IL_0028:  beq        IL_035e
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0x17c16538
  IL_0033:  beq        IL_024d
  IL_0038:  ldloc.1
  IL_0039:  ldc.i4     0x1bd670a0
  IL_003e:  beq        IL_0388
  IL_0043:  br         IL_04ea
  IL_0048:  ldloc.1
  IL_0049:  ldc.i4     0x29b19c8a
  IL_004e:  beq        IL_0334
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4     0x37386ae0
  IL_0059:  beq        IL_0238
  IL_005e:  ldloc.1
  IL_005f:  ldc.i4     0x39b1ddf4
  IL_0064:  beq        IL_0223
  IL_0069:  br         IL_04ea
  IL_006e:  ldloc.1
  IL_006f:  ldc.i4     0x4f0be23b
  IL_0074:  bgt.un.s   IL_009c
  IL_0076:  ldloc.1
  IL_0077:  ldc.i4     0x3b8c1a3c
  IL_007c:  beq        IL_020e
  IL_0081:  ldloc.1
  IL_0082:  ldc.i4     0x4db211e5
  IL_0087:  beq        IL_0349
  IL_008c:  ldloc.1
  IL_008d:  ldc.i4     0x4f0be23b
  IL_0092:  beq        IL_03dc
  IL_0097:  br         IL_04ea
  IL_009c:  ldloc.1
  IL_009d:  ldc.i4     0x63e1d819
  IL_00a2:  bgt.un.s   IL_00bf
  IL_00a4:  ldloc.1
  IL_00a5:  ldc.i4     0x57b8a51f
  IL_00aa:  beq        IL_02f5
  IL_00af:  ldloc.1
  IL_00b0:  ldc.i4     0x63e1d819
  IL_00b5:  beq        IL_01ba
  IL_00ba:  br         IL_04ea
  IL_00bf:  ldloc.1
  IL_00c0:  ldc.i4     0x65f46ebf
  IL_00c5:  beq        IL_031f
  IL_00ca:  ldloc.1
  IL_00cb:  ldc.i4     0x6ccaf138
  IL_00d0:  beq        IL_028c
  IL_00d5:  br         IL_04ea
  IL_00da:  ldloc.1
  IL_00db:  ldc.i4     0x9ba9b528
  IL_00e0:  bgt.un.s   IL_014e
  IL_00e2:  ldloc.1
  IL_00e3:  ldc.i4     0x89116dd3
  IL_00e8:  bgt.un.s   IL_0110
  IL_00ea:  ldloc.1
  IL_00eb:  ldc.i4     0x70ea3b55
  IL_00f0:  beq        IL_0373
  IL_00f5:  ldloc.1
  IL_00f6:  ldc.i4     0x7a1b637f
  IL_00fb:  beq        IL_02e0
  IL_0100:  ldloc.1
  IL_0101:  ldc.i4     0x89116dd3
  IL_0106:  beq        IL_0277
  IL_010b:  br         IL_04ea
  IL_0110:  ldloc.1
  IL_0111:  ldc.i4     0x934f4e0a
  IL_0116:  bgt.un.s   IL_0133
  IL_0118:  ldloc.1
  IL_0119:  ldc.i4     0x8d39bde6
  IL_011e:  beq        IL_01e4
  IL_0123:  ldloc.1
  IL_0124:  ldc.i4     0x934f4e0a
  IL_0129:  beq        IL_01cf
  IL_012e:  br         IL_04ea
  IL_0133:  ldloc.1
  IL_0134:  ldc.i4     0x961d0b4f
  IL_0139:  beq        IL_01f9
  IL_013e:  ldloc.1
  IL_013f:  ldc.i4     0x9ba9b528
  IL_0144:  beq        IL_02cb
  IL_0149:  br         IL_04ea
  IL_014e:  ldloc.1
  IL_014f:  ldc.i4     0xb9e59880
  IL_0154:  bgt.un.s   IL_017c
  IL_0156:  ldloc.1
  IL_0157:  ldc.i4     0xad0ecfd5
  IL_015c:  beq        IL_030a
  IL_0161:  ldloc.1
  IL_0162:  ldc.i4     0xb8e70c1d
  IL_0167:  beq        IL_03b2
  IL_016c:  ldloc.1
  IL_016d:  ldc.i4     0xb9e59880
  IL_0172:  beq        IL_03c7
  IL_0177:  br         IL_04ea
  IL_017c:  ldloc.1
  IL_017d:  ldc.i4     0xcf622408
  IL_0182:  bgt.un.s   IL_019f
  IL_0184:  ldloc.1
  IL_0185:  ldc.i4     0xc04f5db7
  IL_018a:  beq        IL_02b6
  IL_018f:  ldloc.1
  IL_0190:  ldc.i4     0xcf622408
  IL_0195:  beq        IL_02a1
  IL_019a:  br         IL_04ea
  IL_019f:  ldloc.1
  IL_01a0:  ldc.i4     0xdd4e3aa8
  IL_01a5:  beq        IL_039d
  IL_01aa:  ldloc.1
  IL_01ab:  ldc.i4     0xf5cf8c7d
  IL_01b0:  beq        IL_0262
  IL_01b5:  br         IL_04ea
  IL_01ba:  ldarg.0
  IL_01bb:  ldstr      "last"
  IL_01c0:  call       "bool string.op_Equality(string, string)"
  IL_01c5:  brtrue     IL_03f1
  IL_01ca:  br         IL_04ea
  IL_01cf:  ldarg.0
  IL_01d0:  ldstr      "position"
  IL_01d5:  call       "bool string.op_Equality(string, string)"
  IL_01da:  brtrue     IL_03fc
  IL_01df:  br         IL_04ea
  IL_01e4:  ldarg.0
  IL_01e5:  ldstr      "name"
  IL_01ea:  call       "bool string.op_Equality(string, string)"
  IL_01ef:  brtrue     IL_0407
  IL_01f4:  br         IL_04ea
  IL_01f9:  ldarg.0
  IL_01fa:  ldstr      "namespace-uri"
  IL_01ff:  call       "bool string.op_Equality(string, string)"
  IL_0204:  brtrue     IL_0412
  IL_0209:  br         IL_04ea
  IL_020e:  ldarg.0
  IL_020f:  ldstr      "local-name"
  IL_0214:  call       "bool string.op_Equality(string, string)"
  IL_0219:  brtrue     IL_041d
  IL_021e:  br         IL_04ea
  IL_0223:  ldarg.0
  IL_0224:  ldstr      "count"
  IL_0229:  call       "bool string.op_Equality(string, string)"
  IL_022e:  brtrue     IL_0428
  IL_0233:  br         IL_04ea
  IL_0238:  ldarg.0
  IL_0239:  ldstr      "id"
  IL_023e:  call       "bool string.op_Equality(string, string)"
  IL_0243:  brtrue     IL_0433
  IL_0248:  br         IL_04ea
  IL_024d:  ldarg.0
  IL_024e:  ldstr      "string"
  IL_0253:  call       "bool string.op_Equality(string, string)"
  IL_0258:  brtrue     IL_043e
  IL_025d:  br         IL_04ea
  IL_0262:  ldarg.0
  IL_0263:  ldstr      "concat"
  IL_0268:  call       "bool string.op_Equality(string, string)"
  IL_026d:  brtrue     IL_0449
  IL_0272:  br         IL_04ea
  IL_0277:  ldarg.0
  IL_0278:  ldstr      "starts-with"
  IL_027d:  call       "bool string.op_Equality(string, string)"
  IL_0282:  brtrue     IL_0454
  IL_0287:  br         IL_04ea
  IL_028c:  ldarg.0
  IL_028d:  ldstr      "contains"
  IL_0292:  call       "bool string.op_Equality(string, string)"
  IL_0297:  brtrue     IL_045f
  IL_029c:  br         IL_04ea
  IL_02a1:  ldarg.0
  IL_02a2:  ldstr      "substring-before"
  IL_02a7:  call       "bool string.op_Equality(string, string)"
  IL_02ac:  brtrue     IL_046a
  IL_02b1:  br         IL_04ea
  IL_02b6:  ldarg.0
  IL_02b7:  ldstr      "substring-after"
  IL_02bc:  call       "bool string.op_Equality(string, string)"
  IL_02c1:  brtrue     IL_0472
  IL_02c6:  br         IL_04ea
  IL_02cb:  ldarg.0
  IL_02cc:  ldstr      "substring"
  IL_02d1:  call       "bool string.op_Equality(string, string)"
  IL_02d6:  brtrue     IL_047a
  IL_02db:  br         IL_04ea
  IL_02e0:  ldarg.0
  IL_02e1:  ldstr      "string-length"
  IL_02e6:  call       "bool string.op_Equality(string, string)"
  IL_02eb:  brtrue     IL_0482
  IL_02f0:  br         IL_04ea
  IL_02f5:  ldarg.0
  IL_02f6:  ldstr      "normalize-space"
  IL_02fb:  call       "bool string.op_Equality(string, string)"
  IL_0300:  brtrue     IL_048a
  IL_0305:  br         IL_04ea
  IL_030a:  ldarg.0
  IL_030b:  ldstr      "translate"
  IL_0310:  call       "bool string.op_Equality(string, string)"
  IL_0315:  brtrue     IL_0492
  IL_031a:  br         IL_04ea
  IL_031f:  ldarg.0
  IL_0320:  ldstr      "boolean"
  IL_0325:  call       "bool string.op_Equality(string, string)"
  IL_032a:  brtrue     IL_049a
  IL_032f:  br         IL_04ea
  IL_0334:  ldarg.0
  IL_0335:  ldstr      "not"
  IL_033a:  call       "bool string.op_Equality(string, string)"
  IL_033f:  brtrue     IL_04a2
  IL_0344:  br         IL_04ea
  IL_0349:  ldarg.0
  IL_034a:  ldstr      "true"
  IL_034f:  call       "bool string.op_Equality(string, string)"
  IL_0354:  brtrue     IL_04aa
  IL_0359:  br         IL_04ea
  IL_035e:  ldarg.0
  IL_035f:  ldstr      "false"
  IL_0364:  call       "bool string.op_Equality(string, string)"
  IL_0369:  brtrue     IL_04b2
  IL_036e:  br         IL_04ea
  IL_0373:  ldarg.0
  IL_0374:  ldstr      "lang"
  IL_0379:  call       "bool string.op_Equality(string, string)"
  IL_037e:  brtrue     IL_04ba
  IL_0383:  br         IL_04ea
  IL_0388:  ldarg.0
  IL_0389:  ldstr      "number"
  IL_038e:  call       "bool string.op_Equality(string, string)"
  IL_0393:  brtrue     IL_04c2
  IL_0398:  br         IL_04ea
  IL_039d:  ldarg.0
  IL_039e:  ldstr      "sum"
  IL_03a3:  call       "bool string.op_Equality(string, string)"
  IL_03a8:  brtrue     IL_04ca
  IL_03ad:  br         IL_04ea
  IL_03b2:  ldarg.0
  IL_03b3:  ldstr      "floor"
  IL_03b8:  call       "bool string.op_Equality(string, string)"
  IL_03bd:  brtrue     IL_04d2
  IL_03c2:  br         IL_04ea
  IL_03c7:  ldarg.0
  IL_03c8:  ldstr      "ceiling"
  IL_03cd:  call       "bool string.op_Equality(string, string)"
  IL_03d2:  brtrue     IL_04da
  IL_03d7:  br         IL_04ea
  IL_03dc:  ldarg.0
  IL_03dd:  ldstr      "round"
  IL_03e2:  call       "bool string.op_Equality(string, string)"
  IL_03e7:  brtrue     IL_04e2
  IL_03ec:  br         IL_04ea
  IL_03f1:  ldstr      "last"
  IL_03f6:  stloc.0
  IL_03f7:  br         IL_04f0
  IL_03fc:  ldstr      "position"
  IL_0401:  stloc.0
  IL_0402:  br         IL_04f0
  IL_0407:  ldstr      "name"
  IL_040c:  stloc.0
  IL_040d:  br         IL_04f0
  IL_0412:  ldstr      "namespace-uri"
  IL_0417:  stloc.0
  IL_0418:  br         IL_04f0
  IL_041d:  ldstr      "local-name"
  IL_0422:  stloc.0
  IL_0423:  br         IL_04f0
  IL_0428:  ldstr      "count"
  IL_042d:  stloc.0
  IL_042e:  br         IL_04f0
  IL_0433:  ldstr      "id"
  IL_0438:  stloc.0
  IL_0439:  br         IL_04f0
  IL_043e:  ldstr      "string"
  IL_0443:  stloc.0
  IL_0444:  br         IL_04f0
  IL_0449:  ldstr      "concat"
  IL_044e:  stloc.0
  IL_044f:  br         IL_04f0
  IL_0454:  ldstr      "starts-with"
  IL_0459:  stloc.0
  IL_045a:  br         IL_04f0
  IL_045f:  ldstr      "contains"
  IL_0464:  stloc.0
  IL_0465:  br         IL_04f0
  IL_046a:  ldstr      "substring-before"
  IL_046f:  stloc.0
  IL_0470:  br.s       IL_04f0
  IL_0472:  ldstr      "substring-after"
  IL_0477:  stloc.0
  IL_0478:  br.s       IL_04f0
  IL_047a:  ldstr      "substring"
  IL_047f:  stloc.0
  IL_0480:  br.s       IL_04f0
  IL_0482:  ldstr      "string-length"
  IL_0487:  stloc.0
  IL_0488:  br.s       IL_04f0
  IL_048a:  ldstr      "normalize-space"
  IL_048f:  stloc.0
  IL_0490:  br.s       IL_04f0
  IL_0492:  ldstr      "translate"
  IL_0497:  stloc.0
  IL_0498:  br.s       IL_04f0
  IL_049a:  ldstr      "boolean"
  IL_049f:  stloc.0
  IL_04a0:  br.s       IL_04f0
  IL_04a2:  ldstr      "not"
  IL_04a7:  stloc.0
  IL_04a8:  br.s       IL_04f0
  IL_04aa:  ldstr      "true"
  IL_04af:  stloc.0
  IL_04b0:  br.s       IL_04f0
  IL_04b2:  ldstr      "false"
  IL_04b7:  stloc.0
  IL_04b8:  br.s       IL_04f0
  IL_04ba:  ldstr      "lang"
  IL_04bf:  stloc.0
  IL_04c0:  br.s       IL_04f0
  IL_04c2:  ldstr      "number"
  IL_04c7:  stloc.0
  IL_04c8:  br.s       IL_04f0
  IL_04ca:  ldstr      "sum"
  IL_04cf:  stloc.0
  IL_04d0:  br.s       IL_04f0
  IL_04d2:  ldstr      "floor"
  IL_04d7:  stloc.0
  IL_04d8:  br.s       IL_04f0
  IL_04da:  ldstr      "ceiling"
  IL_04df:  stloc.0
  IL_04e0:  br.s       IL_04f0
  IL_04e2:  ldstr      "round"
  IL_04e7:  stloc.0
  IL_04e8:  br.s       IL_04f0
  IL_04ea:  ldstr      "default"
  IL_04ef:  stloc.0
  IL_04f0:  ldloc.0
  IL_04f1:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void NormalizeTimeZone()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.ServiceModel.Syndication/src/System/ServiceModel/Syndication/DateTimeHelper.cs#L146-L212
        // Buckets: 1, 1, 2, 2, 2, 2
        var source = """
assert("UT");
assert("Z");
assert("GMT");
assert("A");
assert("B");
assert("C");
assert("D");
assert("EDT");
assert("E");
assert("EST");
assert("CDT");
assert("F");
assert("CST");
assert("MDT");
assert("G");
assert("MST");
assert("PDT");
assert("H");
assert("PST");
assert("I");
assert("K");
assert("L");
assert("M");
assert("N");
assert("O");
assert("P");
assert("Q");
assert("R");
assert("S");
assert("T");
assert("U");
assert("V");
assert("W");
assert("X");
assert("Y");

assert(null, "default");
assert("", "default");
assert("other", "default");
assert("3", "default");
assert("AMT", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "UT" => "UT",
            "Z" => "Z",
            "GMT" => "GMT",
            "A" => "A",
            "B" => "B",
            "C" => "C",
            "D" => "D",
            "EDT" => "EDT",
            "E" => "E",
            "EST" => "EST",
            "CDT" => "CDT",
            "F" => "F",
            "CST" => "CST",
            "MDT" => "MDT",
            "G" => "G",
            "MST" => "MST",
            "PDT" => "PDT",
            "H" => "H",
            "PST" => "PST",
            "I" => "I",
            "K" => "K",
            "L" => "L",
            "M" => "M",
            "N" => "N",
            "O" => "O",
            "P" => "P",
            "Q" => "Q",
            "R" => "R",
            "S" => "S",
            "T" => "T",
            "U" => "U",
            "V" => "V",
            "W" => "W",
            "X" => "X",
            "Y" => "Y",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      757 (0x2f5)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_02ed
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.1
  IL_000f:  sub
  IL_0010:  switch    (
        IL_0026,
        IL_00de,
        IL_00a4)
  IL_0021:  br         IL_02ed
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.0
  IL_0028:  call       "char string.this[int].get"
  IL_002d:  stloc.2
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.s   65
  IL_0031:  sub
  IL_0032:  switch    (
        IL_01bd,
        IL_01c8,
        IL_01d3,
        IL_01de,
        IL_01f4,
        IL_0215,
        IL_0236,
        IL_0257,
        IL_026d,
        IL_02ed,
        IL_0275,
        IL_027d,
        IL_0285,
        IL_028d,
        IL_0295,
        IL_029d,
        IL_02a5,
        IL_02ad,
        IL_02b5,
        IL_02bd,
        IL_02c5,
        IL_02cd,
        IL_02d5,
        IL_02dd,
        IL_02e5,
        IL_01a7)
  IL_009f:  br         IL_02ed
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.0
  IL_00a6:  call       "char string.this[int].get"
  IL_00ab:  stloc.2
  IL_00ac:  ldloc.2
  IL_00ad:  ldc.i4.s   67
  IL_00af:  sub
  IL_00b0:  switch    (
        IL_012d,
        IL_02ed,
        IL_0108,
        IL_02ed,
        IL_00f3)
  IL_00c9:  ldloc.2
  IL_00ca:  ldc.i4.s   77
  IL_00cc:  beq        IL_0152
  IL_00d1:  ldloc.2
  IL_00d2:  ldc.i4.s   80
  IL_00d4:  beq        IL_0177
  IL_00d9:  br         IL_02ed
  IL_00de:  ldarg.0
  IL_00df:  ldstr      "UT"
  IL_00e4:  call       "bool string.op_Equality(string, string)"
  IL_00e9:  brtrue     IL_019c
  IL_00ee:  br         IL_02ed
  IL_00f3:  ldarg.0
  IL_00f4:  ldstr      "GMT"
  IL_00f9:  call       "bool string.op_Equality(string, string)"
  IL_00fe:  brtrue     IL_01b2
  IL_0103:  br         IL_02ed
  IL_0108:  ldarg.0
  IL_0109:  ldstr      "EDT"
  IL_010e:  call       "bool string.op_Equality(string, string)"
  IL_0113:  brtrue     IL_01e9
  IL_0118:  ldarg.0
  IL_0119:  ldstr      "EST"
  IL_011e:  call       "bool string.op_Equality(string, string)"
  IL_0123:  brtrue     IL_01ff
  IL_0128:  br         IL_02ed
  IL_012d:  ldarg.0
  IL_012e:  ldstr      "CDT"
  IL_0133:  call       "bool string.op_Equality(string, string)"
  IL_0138:  brtrue     IL_020a
  IL_013d:  ldarg.0
  IL_013e:  ldstr      "CST"
  IL_0143:  call       "bool string.op_Equality(string, string)"
  IL_0148:  brtrue     IL_0220
  IL_014d:  br         IL_02ed
  IL_0152:  ldarg.0
  IL_0153:  ldstr      "MDT"
  IL_0158:  call       "bool string.op_Equality(string, string)"
  IL_015d:  brtrue     IL_022b
  IL_0162:  ldarg.0
  IL_0163:  ldstr      "MST"
  IL_0168:  call       "bool string.op_Equality(string, string)"
  IL_016d:  brtrue     IL_0241
  IL_0172:  br         IL_02ed
  IL_0177:  ldarg.0
  IL_0178:  ldstr      "PDT"
  IL_017d:  call       "bool string.op_Equality(string, string)"
  IL_0182:  brtrue     IL_024c
  IL_0187:  ldarg.0
  IL_0188:  ldstr      "PST"
  IL_018d:  call       "bool string.op_Equality(string, string)"
  IL_0192:  brtrue     IL_0262
  IL_0197:  br         IL_02ed
  IL_019c:  ldstr      "UT"
  IL_01a1:  stloc.0
  IL_01a2:  br         IL_02f3
  IL_01a7:  ldstr      "Z"
  IL_01ac:  stloc.0
  IL_01ad:  br         IL_02f3
  IL_01b2:  ldstr      "GMT"
  IL_01b7:  stloc.0
  IL_01b8:  br         IL_02f3
  IL_01bd:  ldstr      "A"
  IL_01c2:  stloc.0
  IL_01c3:  br         IL_02f3
  IL_01c8:  ldstr      "B"
  IL_01cd:  stloc.0
  IL_01ce:  br         IL_02f3
  IL_01d3:  ldstr      "C"
  IL_01d8:  stloc.0
  IL_01d9:  br         IL_02f3
  IL_01de:  ldstr      "D"
  IL_01e3:  stloc.0
  IL_01e4:  br         IL_02f3
  IL_01e9:  ldstr      "EDT"
  IL_01ee:  stloc.0
  IL_01ef:  br         IL_02f3
  IL_01f4:  ldstr      "E"
  IL_01f9:  stloc.0
  IL_01fa:  br         IL_02f3
  IL_01ff:  ldstr      "EST"
  IL_0204:  stloc.0
  IL_0205:  br         IL_02f3
  IL_020a:  ldstr      "CDT"
  IL_020f:  stloc.0
  IL_0210:  br         IL_02f3
  IL_0215:  ldstr      "F"
  IL_021a:  stloc.0
  IL_021b:  br         IL_02f3
  IL_0220:  ldstr      "CST"
  IL_0225:  stloc.0
  IL_0226:  br         IL_02f3
  IL_022b:  ldstr      "MDT"
  IL_0230:  stloc.0
  IL_0231:  br         IL_02f3
  IL_0236:  ldstr      "G"
  IL_023b:  stloc.0
  IL_023c:  br         IL_02f3
  IL_0241:  ldstr      "MST"
  IL_0246:  stloc.0
  IL_0247:  br         IL_02f3
  IL_024c:  ldstr      "PDT"
  IL_0251:  stloc.0
  IL_0252:  br         IL_02f3
  IL_0257:  ldstr      "H"
  IL_025c:  stloc.0
  IL_025d:  br         IL_02f3
  IL_0262:  ldstr      "PST"
  IL_0267:  stloc.0
  IL_0268:  br         IL_02f3
  IL_026d:  ldstr      "I"
  IL_0272:  stloc.0
  IL_0273:  br.s       IL_02f3
  IL_0275:  ldstr      "K"
  IL_027a:  stloc.0
  IL_027b:  br.s       IL_02f3
  IL_027d:  ldstr      "L"
  IL_0282:  stloc.0
  IL_0283:  br.s       IL_02f3
  IL_0285:  ldstr      "M"
  IL_028a:  stloc.0
  IL_028b:  br.s       IL_02f3
  IL_028d:  ldstr      "N"
  IL_0292:  stloc.0
  IL_0293:  br.s       IL_02f3
  IL_0295:  ldstr      "O"
  IL_029a:  stloc.0
  IL_029b:  br.s       IL_02f3
  IL_029d:  ldstr      "P"
  IL_02a2:  stloc.0
  IL_02a3:  br.s       IL_02f3
  IL_02a5:  ldstr      "Q"
  IL_02aa:  stloc.0
  IL_02ab:  br.s       IL_02f3
  IL_02ad:  ldstr      "R"
  IL_02b2:  stloc.0
  IL_02b3:  br.s       IL_02f3
  IL_02b5:  ldstr      "S"
  IL_02ba:  stloc.0
  IL_02bb:  br.s       IL_02f3
  IL_02bd:  ldstr      "T"
  IL_02c2:  stloc.0
  IL_02c3:  br.s       IL_02f3
  IL_02c5:  ldstr      "U"
  IL_02ca:  stloc.0
  IL_02cb:  br.s       IL_02f3
  IL_02cd:  ldstr      "V"
  IL_02d2:  stloc.0
  IL_02d3:  br.s       IL_02f3
  IL_02d5:  ldstr      "W"
  IL_02da:  stloc.0
  IL_02db:  br.s       IL_02f3
  IL_02dd:  ldstr      "X"
  IL_02e2:  stloc.0
  IL_02e3:  br.s       IL_02f3
  IL_02e5:  ldstr      "Y"
  IL_02ea:  stloc.0
  IL_02eb:  br.s       IL_02f3
  IL_02ed:  ldstr      "default"
  IL_02f2:  stloc.0
  IL_02f3:  ldloc.0
  IL_02f4:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void EmitMatchCharacterClass()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCompiler.cs#L5810-L5879
        // Buckets: 3, 3, 2
        var source = """
assert("\0\0\0\u03ff\ufffe\u07ff\ufffe\u07ff");
assert("\0\0\0\u03FF\0\0\0\0");
assert("\0\0\0\0\ufffe\u07FF\ufffe\u07ff");
assert("\0\0\0\0\0\0\ufffe\u07ff");
assert("\0\0\0\0\ufffe\u07FF\0\0");
assert("\0\0\0\u03FF\u007E\0\u007E\0");
assert("\0\0\0\u03FF\u007E\0\0\0");
assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "\0\0\0\u03ff\ufffe\u07ff\ufffe\u07ff" => "\0\0\0\u03ff\ufffe\u07ff\ufffe\u07ff",
            "\0\0\0\u03FF\0\0\0\0" => "\0\0\0\u03FF\0\0\0\0",
            "\0\0\0\0\ufffe\u07FF\ufffe\u07ff" => "\0\0\0\0\ufffe\u07FF\ufffe\u07ff",
            "\0\0\0\0\0\0\ufffe\u07ff" => "\0\0\0\0\0\0\ufffe\u07ff",
            "\0\0\0\0\ufffe\u07FF\0\0" => "\0\0\0\0\ufffe\u07FF\0\0",
            "\0\0\0\u03FF\u007E\0\u007E\0" => "\0\0\0\u03FF\u007E\0\u007E\0",
            "\0\0\0\u03FF\0\0\u007E\0" => "\0\0\0\u03FF\0\0\u007E\0",
            "\0\0\0\u03FF\u007E\0\0\0" => "\0\0\0\u03FF\u007E\0\0\0",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "RAN");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void EmitMatchCharacterClass_2()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCompiler.cs#L5810-L5879
        var source = """
assert("00100000");
assert("00100001");
assert("00010000");
assert("00010001");
assert("00001000");
assert("00001001");
assert("00000100");
assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "00100000" => "00100000",
            "00100001" => "00100001",
            "00010000" => "00010000",
            "00010001" => "00010001",
            "00001000" => "00001000",
            "00001001" => "00001001",
            "00000100" => "00000100",
            "00000101" => "00000101",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      223 (0xdf)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_00d7
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.8
  IL_000f:  bne.un     IL_00d7
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.7
  IL_0016:  call       "char string.this[int].get"
  IL_001b:  stloc.2
  IL_001c:  ldloc.2
  IL_001d:  ldc.i4.s   48
  IL_001f:  beq.s      IL_002b
  IL_0021:  ldloc.2
  IL_0022:  ldc.i4.s   49
  IL_0024:  beq.s      IL_0061
  IL_0026:  br         IL_00d7
  IL_002b:  ldarg.0
  IL_002c:  ldstr      "00100000"
  IL_0031:  call       "bool string.op_Equality(string, string)"
  IL_0036:  brtrue.s   IL_0097
  IL_0038:  ldarg.0
  IL_0039:  ldstr      "00010000"
  IL_003e:  call       "bool string.op_Equality(string, string)"
  IL_0043:  brtrue.s   IL_00a7
  IL_0045:  ldarg.0
  IL_0046:  ldstr      "00001000"
  IL_004b:  call       "bool string.op_Equality(string, string)"
  IL_0050:  brtrue.s   IL_00b7
  IL_0052:  ldarg.0
  IL_0053:  ldstr      "00000100"
  IL_0058:  call       "bool string.op_Equality(string, string)"
  IL_005d:  brtrue.s   IL_00c7
  IL_005f:  br.s       IL_00d7
  IL_0061:  ldarg.0
  IL_0062:  ldstr      "00100001"
  IL_0067:  call       "bool string.op_Equality(string, string)"
  IL_006c:  brtrue.s   IL_009f
  IL_006e:  ldarg.0
  IL_006f:  ldstr      "00010001"
  IL_0074:  call       "bool string.op_Equality(string, string)"
  IL_0079:  brtrue.s   IL_00af
  IL_007b:  ldarg.0
  IL_007c:  ldstr      "00001001"
  IL_0081:  call       "bool string.op_Equality(string, string)"
  IL_0086:  brtrue.s   IL_00bf
  IL_0088:  ldarg.0
  IL_0089:  ldstr      "00000101"
  IL_008e:  call       "bool string.op_Equality(string, string)"
  IL_0093:  brtrue.s   IL_00cf
  IL_0095:  br.s       IL_00d7
  IL_0097:  ldstr      "00100000"
  IL_009c:  stloc.0
  IL_009d:  br.s       IL_00dd
  IL_009f:  ldstr      "00100001"
  IL_00a4:  stloc.0
  IL_00a5:  br.s       IL_00dd
  IL_00a7:  ldstr      "00010000"
  IL_00ac:  stloc.0
  IL_00ad:  br.s       IL_00dd
  IL_00af:  ldstr      "00010001"
  IL_00b4:  stloc.0
  IL_00b5:  br.s       IL_00dd
  IL_00b7:  ldstr      "00001000"
  IL_00bc:  stloc.0
  IL_00bd:  br.s       IL_00dd
  IL_00bf:  ldstr      "00001001"
  IL_00c4:  stloc.0
  IL_00c5:  br.s       IL_00dd
  IL_00c7:  ldstr      "00000100"
  IL_00cc:  stloc.0
  IL_00cd:  br.s       IL_00dd
  IL_00cf:  ldstr      "00000101"
  IL_00d4:  stloc.0
  IL_00d5:  br.s       IL_00dd
  IL_00d7:  ldstr      "default"
  IL_00dc:  stloc.0
  IL_00dd:  ldloc.0
  IL_00de:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void AcceptCommand()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/mono/wasm/debugger/BrowserDebugProxy/MonoProxy.cs#L274-L572
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert(null, "default");
assert("", "default");
assert("DotnetDebugger.setDebuggerProperty", "19");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        switch (value)
        {
            case "Target.attachToTarget":
                return "1";

            case "Debugger.enable":
                return "2";

            case "Debugger.getScriptSource":
                return "3";

            case "Runtime.compileScript":
                return "4";

            case "Debugger.getPossibleBreakpoints":
                return "5";

            case "Debugger.setBreakpoint":
                return "6";

            case "Debugger.setBreakpointByUrl":
                return "7";

            case "Debugger.removeBreakpoint":
                return "8";

            case "Debugger.resume":
                return "9";

            case "Debugger.stepInto":
                return "10";

            case "Debugger.setVariableValue":
                return "11";

            case "Debugger.stepOut":
                return "12";

            case "Debugger.stepOver":
                return "13";

            case "Runtime.evaluate":
                return "14";

            case "Debugger.evaluateOnCallFrame":
                return "15";

            case "Runtime.getProperties":
                return "16";

            case "Runtime.releaseObject":
                return "17";

            case "Debugger.setPauseOnExceptions":
                return "18";

            case "DotnetDebugger.setDebuggerProperty":
                return "19";

            case "DotnetDebugger.setNextIP":
                return "20";

            case "DotnetDebugger.applyUpdates":
                return "21";

            case "DotnetDebugger.addSymbolServerUrl":
                return "22";

            case "DotnetDebugger.getMethodLocation":
                return "23";

            case "Runtime.callFunctionOn":
                return "24";

            default:
                return "default";
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     1022 (0x3fe)
  .maxstack  2
  .locals init (int V_0,
                char V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_03f8
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.s   15
  IL_0010:  sub
  IL_0011:  switch    (
        IL_00a2,
        IL_0153,
        IL_0135,
        IL_03f8,
        IL_03f8,
        IL_03f8,
        IL_006b,
        IL_00dd,
        IL_03f8,
        IL_00c0,
        IL_0117,
        IL_03f8,
        IL_00fa,
        IL_02ff,
        IL_0314,
        IL_03f8,
        IL_0218,
        IL_0353,
        IL_033e,
        IL_0329)
  IL_0066:  br         IL_03f8
  IL_006b:  ldarg.0
  IL_006c:  ldc.i4.8
  IL_006d:  call       "char string.this[int].get"
  IL_0072:  stloc.1
  IL_0073:  ldloc.1
  IL_0074:  ldc.i4.s   103
  IL_0076:  bgt.un.s   IL_008d
  IL_0078:  ldloc.1
  IL_0079:  ldc.i4.s   99
  IL_007b:  beq        IL_0185
  IL_0080:  ldloc.1
  IL_0081:  ldc.i4.s   103
  IL_0083:  beq        IL_019a
  IL_0088:  br         IL_03f8
  IL_008d:  ldloc.1
  IL_008e:  ldc.i4.s   114
  IL_0090:  beq        IL_01af
  IL_0095:  ldloc.1
  IL_0096:  ldc.i4.s   116
  IL_0098:  beq        IL_0170
  IL_009d:  br         IL_03f8
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   9
  IL_00a5:  call       "char string.this[int].get"
  IL_00aa:  stloc.1
  IL_00ab:  ldloc.1
  IL_00ac:  ldc.i4.s   101
  IL_00ae:  beq        IL_01c4
  IL_00b3:  ldloc.1
  IL_00b4:  ldc.i4.s   114
  IL_00b6:  beq        IL_01d9
  IL_00bb:  br         IL_03f8
  IL_00c0:  ldarg.0
  IL_00c1:  ldc.i4.1
  IL_00c2:  call       "char string.this[int].get"
  IL_00c7:  stloc.1
  IL_00c8:  ldloc.1
  IL_00c9:  ldc.i4.s   101
  IL_00cb:  beq        IL_01ee
  IL_00d0:  ldloc.1
  IL_00d1:  ldc.i4.s   111
  IL_00d3:  beq        IL_0203
  IL_00d8:  br         IL_03f8
  IL_00dd:  ldarg.0
  IL_00de:  ldc.i4.0
  IL_00df:  call       "char string.this[int].get"
  IL_00e4:  stloc.1
  IL_00e5:  ldloc.1
  IL_00e6:  ldc.i4.s   68
  IL_00e8:  beq        IL_022d
  IL_00ed:  ldloc.1
  IL_00ee:  ldc.i4.s   82
  IL_00f0:  beq        IL_0242
  IL_00f5:  br         IL_03f8
  IL_00fa:  ldarg.0
  IL_00fb:  ldc.i4.1
  IL_00fc:  call       "char string.this[int].get"
  IL_0101:  stloc.1
  IL_0102:  ldloc.1
  IL_0103:  ldc.i4.s   101
  IL_0105:  beq        IL_0257
  IL_010a:  ldloc.1
  IL_010b:  ldc.i4.s   111
  IL_010d:  beq        IL_026c
  IL_0112:  br         IL_03f8
  IL_0117:  ldarg.0
  IL_0118:  ldc.i4.s   9
  IL_011a:  call       "char string.this[int].get"
  IL_011f:  stloc.1
  IL_0120:  ldloc.1
  IL_0121:  ldc.i4.s   114
  IL_0123:  beq        IL_0281
  IL_0128:  ldloc.1
  IL_0129:  ldc.i4.s   115
  IL_012b:  beq        IL_0296
  IL_0130:  br         IL_03f8
  IL_0135:  ldarg.0
  IL_0136:  ldc.i4.s   13
  IL_0138:  call       "char string.this[int].get"
  IL_013d:  stloc.1
  IL_013e:  ldloc.1
  IL_013f:  ldc.i4.s   73
  IL_0141:  beq        IL_02ab
  IL_0146:  ldloc.1
  IL_0147:  ldc.i4.s   79
  IL_0149:  beq        IL_02c0
  IL_014e:  br         IL_03f8
  IL_0153:  ldarg.0
  IL_0154:  ldc.i4.0
  IL_0155:  call       "char string.this[int].get"
  IL_015a:  stloc.1
  IL_015b:  ldloc.1
  IL_015c:  ldc.i4.s   68
  IL_015e:  beq        IL_02d5
  IL_0163:  ldloc.1
  IL_0164:  ldc.i4.s   82
  IL_0166:  beq        IL_02ea
  IL_016b:  br         IL_03f8
  IL_0170:  ldarg.0
  IL_0171:  ldstr      "Target.attachToTarget"
  IL_0176:  call       "bool string.op_Equality(string, string)"
  IL_017b:  brtrue     IL_0368
  IL_0180:  br         IL_03f8
  IL_0185:  ldarg.0
  IL_0186:  ldstr      "Runtime.compileScript"
  IL_018b:  call       "bool string.op_Equality(string, string)"
  IL_0190:  brtrue     IL_037a
  IL_0195:  br         IL_03f8
  IL_019a:  ldarg.0
  IL_019b:  ldstr      "Runtime.getProperties"
  IL_01a0:  call       "bool string.op_Equality(string, string)"
  IL_01a5:  brtrue     IL_03c2
  IL_01aa:  br         IL_03f8
  IL_01af:  ldarg.0
  IL_01b0:  ldstr      "Runtime.releaseObject"
  IL_01b5:  call       "bool string.op_Equality(string, string)"
  IL_01ba:  brtrue     IL_03c8
  IL_01bf:  br         IL_03f8
  IL_01c4:  ldarg.0
  IL_01c5:  ldstr      "Debugger.enable"
  IL_01ca:  call       "bool string.op_Equality(string, string)"
  IL_01cf:  brtrue     IL_036e
  IL_01d4:  br         IL_03f8
  IL_01d9:  ldarg.0
  IL_01da:  ldstr      "Debugger.resume"
  IL_01df:  call       "bool string.op_Equality(string, string)"
  IL_01e4:  brtrue     IL_0398
  IL_01e9:  br         IL_03f8
  IL_01ee:  ldarg.0
  IL_01ef:  ldstr      "Debugger.getScriptSource"
  IL_01f4:  call       "bool string.op_Equality(string, string)"
  IL_01f9:  brtrue     IL_0374
  IL_01fe:  br         IL_03f8
  IL_0203:  ldarg.0
  IL_0204:  ldstr      "DotnetDebugger.setNextIP"
  IL_0209:  call       "bool string.op_Equality(string, string)"
  IL_020e:  brtrue     IL_03da
  IL_0213:  br         IL_03f8
  IL_0218:  ldarg.0
  IL_0219:  ldstr      "Debugger.getPossibleBreakpoints"
  IL_021e:  call       "bool string.op_Equality(string, string)"
  IL_0223:  brtrue     IL_0380
  IL_0228:  br         IL_03f8
  IL_022d:  ldarg.0
  IL_022e:  ldstr      "Debugger.setBreakpoint"
  IL_0233:  call       "bool string.op_Equality(string, string)"
  IL_0238:  brtrue     IL_0386
  IL_023d:  br         IL_03f8
  IL_0242:  ldarg.0
  IL_0243:  ldstr      "Runtime.callFunctionOn"
  IL_0248:  call       "bool string.op_Equality(string, string)"
  IL_024d:  brtrue     IL_03f2
  IL_0252:  br         IL_03f8
  IL_0257:  ldarg.0
  IL_0258:  ldstr      "Debugger.setBreakpointByUrl"
  IL_025d:  call       "bool string.op_Equality(string, string)"
  IL_0262:  brtrue     IL_038c
  IL_0267:  br         IL_03f8
  IL_026c:  ldarg.0
  IL_026d:  ldstr      "DotnetDebugger.applyUpdates"
  IL_0272:  call       "bool string.op_Equality(string, string)"
  IL_0277:  brtrue     IL_03e0
  IL_027c:  br         IL_03f8
  IL_0281:  ldarg.0
  IL_0282:  ldstr      "Debugger.removeBreakpoint"
  IL_0287:  call       "bool string.op_Equality(string, string)"
  IL_028c:  brtrue     IL_0392
  IL_0291:  br         IL_03f8
  IL_0296:  ldarg.0
  IL_0297:  ldstr      "Debugger.setVariableValue"
  IL_029c:  call       "bool string.op_Equality(string, string)"
  IL_02a1:  brtrue     IL_03a4
  IL_02a6:  br         IL_03f8
  IL_02ab:  ldarg.0
  IL_02ac:  ldstr      "Debugger.stepInto"
  IL_02b1:  call       "bool string.op_Equality(string, string)"
  IL_02b6:  brtrue     IL_039e
  IL_02bb:  br         IL_03f8
  IL_02c0:  ldarg.0
  IL_02c1:  ldstr      "Debugger.stepOver"
  IL_02c6:  call       "bool string.op_Equality(string, string)"
  IL_02cb:  brtrue     IL_03b0
  IL_02d0:  br         IL_03f8
  IL_02d5:  ldarg.0
  IL_02d6:  ldstr      "Debugger.stepOut"
  IL_02db:  call       "bool string.op_Equality(string, string)"
  IL_02e0:  brtrue     IL_03aa
  IL_02e5:  br         IL_03f8
  IL_02ea:  ldarg.0
  IL_02eb:  ldstr      "Runtime.evaluate"
  IL_02f0:  call       "bool string.op_Equality(string, string)"
  IL_02f5:  brtrue     IL_03b6
  IL_02fa:  br         IL_03f8
  IL_02ff:  ldarg.0
  IL_0300:  ldstr      "Debugger.evaluateOnCallFrame"
  IL_0305:  call       "bool string.op_Equality(string, string)"
  IL_030a:  brtrue     IL_03bc
  IL_030f:  br         IL_03f8
  IL_0314:  ldarg.0
  IL_0315:  ldstr      "Debugger.setPauseOnExceptions"
  IL_031a:  call       "bool string.op_Equality(string, string)"
  IL_031f:  brtrue     IL_03ce
  IL_0324:  br         IL_03f8
  IL_0329:  ldarg.0
  IL_032a:  ldstr      "DotnetDebugger.setDebuggerProperty"
  IL_032f:  call       "bool string.op_Equality(string, string)"
  IL_0334:  brtrue     IL_03d4
  IL_0339:  br         IL_03f8
  IL_033e:  ldarg.0
  IL_033f:  ldstr      "DotnetDebugger.addSymbolServerUrl"
  IL_0344:  call       "bool string.op_Equality(string, string)"
  IL_0349:  brtrue     IL_03e6
  IL_034e:  br         IL_03f8
  IL_0353:  ldarg.0
  IL_0354:  ldstr      "DotnetDebugger.getMethodLocation"
  IL_0359:  call       "bool string.op_Equality(string, string)"
  IL_035e:  brtrue     IL_03ec
  IL_0363:  br         IL_03f8
  IL_0368:  ldstr      "1"
  IL_036d:  ret
  IL_036e:  ldstr      "2"
  IL_0373:  ret
  IL_0374:  ldstr      "3"
  IL_0379:  ret
  IL_037a:  ldstr      "4"
  IL_037f:  ret
  IL_0380:  ldstr      "5"
  IL_0385:  ret
  IL_0386:  ldstr      "6"
  IL_038b:  ret
  IL_038c:  ldstr      "7"
  IL_0391:  ret
  IL_0392:  ldstr      "8"
  IL_0397:  ret
  IL_0398:  ldstr      "9"
  IL_039d:  ret
  IL_039e:  ldstr      "10"
  IL_03a3:  ret
  IL_03a4:  ldstr      "11"
  IL_03a9:  ret
  IL_03aa:  ldstr      "12"
  IL_03af:  ret
  IL_03b0:  ldstr      "13"
  IL_03b5:  ret
  IL_03b6:  ldstr      "14"
  IL_03bb:  ret
  IL_03bc:  ldstr      "15"
  IL_03c1:  ret
  IL_03c2:  ldstr      "16"
  IL_03c7:  ret
  IL_03c8:  ldstr      "17"
  IL_03cd:  ret
  IL_03ce:  ldstr      "18"
  IL_03d3:  ret
  IL_03d4:  ldstr      "19"
  IL_03d9:  ret
  IL_03da:  ldstr      "20"
  IL_03df:  ret
  IL_03e0:  ldstr      "21"
  IL_03e5:  ret
  IL_03e6:  ldstr      "22"
  IL_03eb:  ret
  IL_03ec:  ldstr      "23"
  IL_03f1:  ret
  IL_03f2:  ldstr      "24"
  IL_03f7:  ret
  IL_03f8:  ldstr      "default"
  IL_03fd:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     1018 (0x3fa)
  .maxstack  2
  .locals init (uint V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0xb508d68f
  IL_000d:  bgt.un     IL_00c2
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x4b092f03
  IL_0018:  bgt.un.s   IL_006e
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4     0x1e768f83
  IL_0020:  bgt.un.s   IL_0048
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4     0x15a7290
  IL_0028:  beq        IL_033a
  IL_002d:  ldloc.0
  IL_002e:  ldc.i4     0x35576d8
  IL_0033:  beq        IL_02e6
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4     0x1e768f83
  IL_003e:  beq        IL_0214
  IL_0043:  br         IL_03f4
  IL_0048:  ldloc.0
  IL_0049:  ldc.i4     0x36ca30ef
  IL_004e:  beq        IL_0181
  IL_0053:  ldloc.0
  IL_0054:  ldc.i4     0x48313e0c
  IL_0059:  beq        IL_0229
  IL_005e:  ldloc.0
  IL_005f:  ldc.i4     0x4b092f03
  IL_0064:  beq        IL_0292
  IL_0069:  br         IL_03f4
  IL_006e:  ldloc.0
  IL_006f:  ldc.i4     0x7de76a02
  IL_0074:  bgt.un.s   IL_009c
  IL_0076:  ldloc.0
  IL_0077:  ldc.i4     0x4be2fe50
  IL_007c:  beq        IL_027d
  IL_0081:  ldloc.0
  IL_0082:  ldc.i4     0x6f7079ab
  IL_0087:  beq        IL_01ea
  IL_008c:  ldloc.0
  IL_008d:  ldc.i4     0x7de76a02
  IL_0092:  beq        IL_02fb
  IL_0097:  br         IL_03f4
  IL_009c:  ldloc.0
  IL_009d:  ldc.i4     0x925e89c3
  IL_00a2:  beq        IL_02bc
  IL_00a7:  ldloc.0
  IL_00a8:  ldc.i4     0xa0ca5e2a
  IL_00ad:  beq        IL_034f
  IL_00b2:  ldloc.0
  IL_00b3:  ldc.i4     0xb508d68f
  IL_00b8:  beq        IL_0325
  IL_00bd:  br         IL_03f4
  IL_00c2:  ldloc.0
  IL_00c3:  ldc.i4     0xd3193fe0
  IL_00c8:  bgt.un.s   IL_011b
  IL_00ca:  ldloc.0
  IL_00cb:  ldc.i4     0xc16fb451
  IL_00d0:  bgt.un.s   IL_00f8
  IL_00d2:  ldloc.0
  IL_00d3:  ldc.i4     0xbc145742
  IL_00d8:  beq        IL_0196
  IL_00dd:  ldloc.0
  IL_00de:  ldc.i4     0xbe721584
  IL_00e3:  beq        IL_0310
  IL_00e8:  ldloc.0
  IL_00e9:  ldc.i4     0xc16fb451
  IL_00ee:  beq        IL_01c0
  IL_00f3:  br         IL_03f4
  IL_00f8:  ldloc.0
  IL_00f9:  ldc.i4     0xc49a4297
  IL_00fe:  beq.s      IL_016c
  IL_0100:  ldloc.0
  IL_0101:  ldc.i4     0xd07d7ccf
  IL_0106:  beq        IL_01ab
  IL_010b:  ldloc.0
  IL_010c:  ldc.i4     0xd3193fe0
  IL_0111:  beq        IL_0268
  IL_0116:  br         IL_03f4
  IL_011b:  ldloc.0
  IL_011c:  ldc.i4     0xdee508e0
  IL_0121:  bgt.un.s   IL_0149
  IL_0123:  ldloc.0
  IL_0124:  ldc.i4     0xd8f74285
  IL_0129:  beq        IL_02d1
  IL_012e:  ldloc.0
  IL_012f:  ldc.i4     0xde4a6def
  IL_0134:  beq        IL_01ff
  IL_0139:  ldloc.0
  IL_013a:  ldc.i4     0xdee508e0
  IL_013f:  beq        IL_02a7
  IL_0144:  br         IL_03f4
  IL_0149:  ldloc.0
  IL_014a:  ldc.i4     0xe0c38306
  IL_014f:  beq        IL_0253
  IL_0154:  ldloc.0
  IL_0155:  ldc.i4     0xfbe9f177
  IL_015a:  beq.s      IL_01d5
  IL_015c:  ldloc.0
  IL_015d:  ldc.i4     0xfc282ff1
  IL_0162:  beq        IL_023e
  IL_0167:  br         IL_03f4
  IL_016c:  ldarg.0
  IL_016d:  ldstr      "Target.attachToTarget"
  IL_0172:  call       "bool string.op_Equality(string, string)"
  IL_0177:  brtrue     IL_0364
  IL_017c:  br         IL_03f4
  IL_0181:  ldarg.0
  IL_0182:  ldstr      "Debugger.enable"
  IL_0187:  call       "bool string.op_Equality(string, string)"
  IL_018c:  brtrue     IL_036a
  IL_0191:  br         IL_03f4
  IL_0196:  ldarg.0
  IL_0197:  ldstr      "Debugger.getScriptSource"
  IL_019c:  call       "bool string.op_Equality(string, string)"
  IL_01a1:  brtrue     IL_0370
  IL_01a6:  br         IL_03f4
  IL_01ab:  ldarg.0
  IL_01ac:  ldstr      "Runtime.compileScript"
  IL_01b1:  call       "bool string.op_Equality(string, string)"
  IL_01b6:  brtrue     IL_0376
  IL_01bb:  br         IL_03f4
  IL_01c0:  ldarg.0
  IL_01c1:  ldstr      "Debugger.getPossibleBreakpoints"
  IL_01c6:  call       "bool string.op_Equality(string, string)"
  IL_01cb:  brtrue     IL_037c
  IL_01d0:  br         IL_03f4
  IL_01d5:  ldarg.0
  IL_01d6:  ldstr      "Debugger.setBreakpoint"
  IL_01db:  call       "bool string.op_Equality(string, string)"
  IL_01e0:  brtrue     IL_0382
  IL_01e5:  br         IL_03f4
  IL_01ea:  ldarg.0
  IL_01eb:  ldstr      "Debugger.setBreakpointByUrl"
  IL_01f0:  call       "bool string.op_Equality(string, string)"
  IL_01f5:  brtrue     IL_0388
  IL_01fa:  br         IL_03f4
  IL_01ff:  ldarg.0
  IL_0200:  ldstr      "Debugger.removeBreakpoint"
  IL_0205:  call       "bool string.op_Equality(string, string)"
  IL_020a:  brtrue     IL_038e
  IL_020f:  br         IL_03f4
  IL_0214:  ldarg.0
  IL_0215:  ldstr      "Debugger.resume"
  IL_021a:  call       "bool string.op_Equality(string, string)"
  IL_021f:  brtrue     IL_0394
  IL_0224:  br         IL_03f4
  IL_0229:  ldarg.0
  IL_022a:  ldstr      "Debugger.stepInto"
  IL_022f:  call       "bool string.op_Equality(string, string)"
  IL_0234:  brtrue     IL_039a
  IL_0239:  br         IL_03f4
  IL_023e:  ldarg.0
  IL_023f:  ldstr      "Debugger.setVariableValue"
  IL_0244:  call       "bool string.op_Equality(string, string)"
  IL_0249:  brtrue     IL_03a0
  IL_024e:  br         IL_03f4
  IL_0253:  ldarg.0
  IL_0254:  ldstr      "Debugger.stepOut"
  IL_0259:  call       "bool string.op_Equality(string, string)"
  IL_025e:  brtrue     IL_03a6
  IL_0263:  br         IL_03f4
  IL_0268:  ldarg.0
  IL_0269:  ldstr      "Debugger.stepOver"
  IL_026e:  call       "bool string.op_Equality(string, string)"
  IL_0273:  brtrue     IL_03ac
  IL_0278:  br         IL_03f4
  IL_027d:  ldarg.0
  IL_027e:  ldstr      "Runtime.evaluate"
  IL_0283:  call       "bool string.op_Equality(string, string)"
  IL_0288:  brtrue     IL_03b2
  IL_028d:  br         IL_03f4
  IL_0292:  ldarg.0
  IL_0293:  ldstr      "Debugger.evaluateOnCallFrame"
  IL_0298:  call       "bool string.op_Equality(string, string)"
  IL_029d:  brtrue     IL_03b8
  IL_02a2:  br         IL_03f4
  IL_02a7:  ldarg.0
  IL_02a8:  ldstr      "Runtime.getProperties"
  IL_02ad:  call       "bool string.op_Equality(string, string)"
  IL_02b2:  brtrue     IL_03be
  IL_02b7:  br         IL_03f4
  IL_02bc:  ldarg.0
  IL_02bd:  ldstr      "Runtime.releaseObject"
  IL_02c2:  call       "bool string.op_Equality(string, string)"
  IL_02c7:  brtrue     IL_03c4
  IL_02cc:  br         IL_03f4
  IL_02d1:  ldarg.0
  IL_02d2:  ldstr      "Debugger.setPauseOnExceptions"
  IL_02d7:  call       "bool string.op_Equality(string, string)"
  IL_02dc:  brtrue     IL_03ca
  IL_02e1:  br         IL_03f4
  IL_02e6:  ldarg.0
  IL_02e7:  ldstr      "DotnetDebugger.setDebuggerProperty"
  IL_02ec:  call       "bool string.op_Equality(string, string)"
  IL_02f1:  brtrue     IL_03d0
  IL_02f6:  br         IL_03f4
  IL_02fb:  ldarg.0
  IL_02fc:  ldstr      "DotnetDebugger.setNextIP"
  IL_0301:  call       "bool string.op_Equality(string, string)"
  IL_0306:  brtrue     IL_03d6
  IL_030b:  br         IL_03f4
  IL_0310:  ldarg.0
  IL_0311:  ldstr      "DotnetDebugger.applyUpdates"
  IL_0316:  call       "bool string.op_Equality(string, string)"
  IL_031b:  brtrue     IL_03dc
  IL_0320:  br         IL_03f4
  IL_0325:  ldarg.0
  IL_0326:  ldstr      "DotnetDebugger.addSymbolServerUrl"
  IL_032b:  call       "bool string.op_Equality(string, string)"
  IL_0330:  brtrue     IL_03e2
  IL_0335:  br         IL_03f4
  IL_033a:  ldarg.0
  IL_033b:  ldstr      "DotnetDebugger.getMethodLocation"
  IL_0340:  call       "bool string.op_Equality(string, string)"
  IL_0345:  brtrue     IL_03e8
  IL_034a:  br         IL_03f4
  IL_034f:  ldarg.0
  IL_0350:  ldstr      "Runtime.callFunctionOn"
  IL_0355:  call       "bool string.op_Equality(string, string)"
  IL_035a:  brtrue     IL_03ee
  IL_035f:  br         IL_03f4
  IL_0364:  ldstr      "1"
  IL_0369:  ret
  IL_036a:  ldstr      "2"
  IL_036f:  ret
  IL_0370:  ldstr      "3"
  IL_0375:  ret
  IL_0376:  ldstr      "4"
  IL_037b:  ret
  IL_037c:  ldstr      "5"
  IL_0381:  ret
  IL_0382:  ldstr      "6"
  IL_0387:  ret
  IL_0388:  ldstr      "7"
  IL_038d:  ret
  IL_038e:  ldstr      "8"
  IL_0393:  ret
  IL_0394:  ldstr      "9"
  IL_0399:  ret
  IL_039a:  ldstr      "10"
  IL_039f:  ret
  IL_03a0:  ldstr      "11"
  IL_03a5:  ret
  IL_03a6:  ldstr      "12"
  IL_03ab:  ret
  IL_03ac:  ldstr      "13"
  IL_03b1:  ret
  IL_03b2:  ldstr      "14"
  IL_03b7:  ret
  IL_03b8:  ldstr      "15"
  IL_03bd:  ret
  IL_03be:  ldstr      "16"
  IL_03c3:  ret
  IL_03c4:  ldstr      "17"
  IL_03c9:  ret
  IL_03ca:  ldstr      "18"
  IL_03cf:  ret
  IL_03d0:  ldstr      "19"
  IL_03d5:  ret
  IL_03d6:  ldstr      "20"
  IL_03db:  ret
  IL_03dc:  ldstr      "21"
  IL_03e1:  ret
  IL_03e2:  ldstr      "22"
  IL_03e7:  ret
  IL_03e8:  ldstr      "23"
  IL_03ed:  ret
  IL_03ee:  ldstr      "24"
  IL_03f3:  ret
  IL_03f4:  ldstr      "default"
  IL_03f9:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void IsOperator()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/tools/illink/src/linker/Linker.Steps/DiscoverCustomOperatorsHandler.cs#L156-L221
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert(null, "default");
assert("", "default");
assert("GreaterThanOrEqual", "2");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        switch (value)
        {
            case "UnaryPlus":
            case "UnaryNegation":
            case "LogicalNot":
            case "OnesComplement":
            case "Increment":
            case "Decrement":
            case "True":
            case "False":
                return "1";
            case "Addition":
            case "Subtraction":
            case "Multiply":
            case "Division":
            case "Modulus":
            case "BitwiseAnd":
            case "BitwiseOr":
            case "ExclusiveOr":
            case "LeftShift":
            case "RightShift":
            case "Equality":
            case "Inequality":
            case "LessThan":
            case "GreaterThan":
            case "LessThanOrEqual":
            case "GreaterThanOrEqual":
                return "2";
            case "Implicit":
            case "Explicit":
                return "3";
            default:
                return "default";
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      852 (0x354)
  .maxstack  2
  .locals init (int V_0,
                char V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_034e
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.4
  IL_000f:  sub
  IL_0010:  switch    (
        IL_0231,
        IL_0246,
        IL_034e,
        IL_030f,
        IL_00cc,
        IL_0056,
        IL_0095,
        IL_0125,
        IL_034e,
        IL_01b3,
        IL_021c,
        IL_031e,
        IL_034e,
        IL_034e,
        IL_032d)
  IL_0051:  br         IL_034e
  IL_0056:  ldarg.0
  IL_0057:  ldc.i4.0
  IL_0058:  call       "char string.this[int].get"
  IL_005d:  stloc.1
  IL_005e:  ldloc.1
  IL_005f:  ldc.i4.s   68
  IL_0061:  bgt.un.s   IL_0078
  IL_0063:  ldloc.1
  IL_0064:  ldc.i4.s   66
  IL_0066:  beq        IL_0189
  IL_006b:  ldloc.1
  IL_006c:  ldc.i4.s   68
  IL_006e:  beq        IL_0174
  IL_0073:  br         IL_034e
  IL_0078:  ldloc.1
  IL_0079:  ldc.i4.s   73
  IL_007b:  beq        IL_015f
  IL_0080:  ldloc.1
  IL_0081:  ldc.i4.s   76
  IL_0083:  beq        IL_019e
  IL_0088:  ldloc.1
  IL_0089:  ldc.i4.s   85
  IL_008b:  beq        IL_014a
  IL_0090:  br         IL_034e
  IL_0095:  ldarg.0
  IL_0096:  ldc.i4.0
  IL_0097:  call       "char string.this[int].get"
  IL_009c:  stloc.1
  IL_009d:  ldloc.1
  IL_009e:  ldc.i4.s   73
  IL_00a0:  bgt.un.s   IL_00b7
  IL_00a2:  ldloc.1
  IL_00a3:  ldc.i4.s   66
  IL_00a5:  beq        IL_01dd
  IL_00aa:  ldloc.1
  IL_00ab:  ldc.i4.s   73
  IL_00ad:  beq        IL_0207
  IL_00b2:  br         IL_034e
  IL_00b7:  ldloc.1
  IL_00b8:  ldc.i4.s   76
  IL_00ba:  beq        IL_01c8
  IL_00bf:  ldloc.1
  IL_00c0:  ldc.i4.s   82
  IL_00c2:  beq        IL_01f2
  IL_00c7:  br         IL_034e
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.1
  IL_00ce:  call       "char string.this[int].get"
  IL_00d3:  stloc.1
  IL_00d4:  ldloc.1
  IL_00d5:  ldc.i4.s   105
  IL_00d7:  bgt.un.s   IL_00f6
  IL_00d9:  ldloc.1
  IL_00da:  ldc.i4.s   100
  IL_00dc:  beq        IL_025b
  IL_00e1:  ldloc.1
  IL_00e2:  ldc.i4.s   101
  IL_00e4:  beq        IL_02af
  IL_00e9:  ldloc.1
  IL_00ea:  ldc.i4.s   105
  IL_00ec:  beq        IL_0285
  IL_00f1:  br         IL_034e
  IL_00f6:  ldloc.1
  IL_00f7:  ldc.i4.s   113
  IL_00f9:  bgt.un.s   IL_0110
  IL_00fb:  ldloc.1
  IL_00fc:  ldc.i4.s   109
  IL_00fe:  beq        IL_02c4
  IL_0103:  ldloc.1
  IL_0104:  ldc.i4.s   113
  IL_0106:  beq        IL_029a
  IL_010b:  br         IL_034e
  IL_0110:  ldloc.1
  IL_0111:  ldc.i4.s   117
  IL_0113:  beq        IL_0270
  IL_0118:  ldloc.1
  IL_0119:  ldc.i4.s   120
  IL_011b:  beq        IL_02d3
  IL_0120:  br         IL_034e
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.0
  IL_0127:  call       "char string.this[int].get"
  IL_012c:  stloc.1
  IL_012d:  ldloc.1
  IL_012e:  ldc.i4.s   69
  IL_0130:  beq        IL_02f1
  IL_0135:  ldloc.1
  IL_0136:  ldc.i4.s   71
  IL_0138:  beq        IL_0300
  IL_013d:  ldloc.1
  IL_013e:  ldc.i4.s   83
  IL_0140:  beq        IL_02e2
  IL_0145:  br         IL_034e
  IL_014a:  ldarg.0
  IL_014b:  ldstr      "UnaryPlus"
  IL_0150:  call       "bool string.op_Equality(string, string)"
  IL_0155:  brtrue     IL_033c
  IL_015a:  br         IL_034e
  IL_015f:  ldarg.0
  IL_0160:  ldstr      "Increment"
  IL_0165:  call       "bool string.op_Equality(string, string)"
  IL_016a:  brtrue     IL_033c
  IL_016f:  br         IL_034e
  IL_0174:  ldarg.0
  IL_0175:  ldstr      "Decrement"
  IL_017a:  call       "bool string.op_Equality(string, string)"
  IL_017f:  brtrue     IL_033c
  IL_0184:  br         IL_034e
  IL_0189:  ldarg.0
  IL_018a:  ldstr      "BitwiseOr"
  IL_018f:  call       "bool string.op_Equality(string, string)"
  IL_0194:  brtrue     IL_0342
  IL_0199:  br         IL_034e
  IL_019e:  ldarg.0
  IL_019f:  ldstr      "LeftShift"
  IL_01a4:  call       "bool string.op_Equality(string, string)"
  IL_01a9:  brtrue     IL_0342
  IL_01ae:  br         IL_034e
  IL_01b3:  ldarg.0
  IL_01b4:  ldstr      "UnaryNegation"
  IL_01b9:  call       "bool string.op_Equality(string, string)"
  IL_01be:  brtrue     IL_033c
  IL_01c3:  br         IL_034e
  IL_01c8:  ldarg.0
  IL_01c9:  ldstr      "LogicalNot"
  IL_01ce:  call       "bool string.op_Equality(string, string)"
  IL_01d3:  brtrue     IL_033c
  IL_01d8:  br         IL_034e
  IL_01dd:  ldarg.0
  IL_01de:  ldstr      "BitwiseAnd"
  IL_01e3:  call       "bool string.op_Equality(string, string)"
  IL_01e8:  brtrue     IL_0342
  IL_01ed:  br         IL_034e
  IL_01f2:  ldarg.0
  IL_01f3:  ldstr      "RightShift"
  IL_01f8:  call       "bool string.op_Equality(string, string)"
  IL_01fd:  brtrue     IL_0342
  IL_0202:  br         IL_034e
  IL_0207:  ldarg.0
  IL_0208:  ldstr      "Inequality"
  IL_020d:  call       "bool string.op_Equality(string, string)"
  IL_0212:  brtrue     IL_0342
  IL_0217:  br         IL_034e
  IL_021c:  ldarg.0
  IL_021d:  ldstr      "OnesComplement"
  IL_0222:  call       "bool string.op_Equality(string, string)"
  IL_0227:  brtrue     IL_033c
  IL_022c:  br         IL_034e
  IL_0231:  ldarg.0
  IL_0232:  ldstr      "True"
  IL_0237:  call       "bool string.op_Equality(string, string)"
  IL_023c:  brtrue     IL_033c
  IL_0241:  br         IL_034e
  IL_0246:  ldarg.0
  IL_0247:  ldstr      "False"
  IL_024c:  call       "bool string.op_Equality(string, string)"
  IL_0251:  brtrue     IL_033c
  IL_0256:  br         IL_034e
  IL_025b:  ldarg.0
  IL_025c:  ldstr      "Addition"
  IL_0261:  call       "bool string.op_Equality(string, string)"
  IL_0266:  brtrue     IL_0342
  IL_026b:  br         IL_034e
  IL_0270:  ldarg.0
  IL_0271:  ldstr      "Multiply"
  IL_0276:  call       "bool string.op_Equality(string, string)"
  IL_027b:  brtrue     IL_0342
  IL_0280:  br         IL_034e
  IL_0285:  ldarg.0
  IL_0286:  ldstr      "Division"
  IL_028b:  call       "bool string.op_Equality(string, string)"
  IL_0290:  brtrue     IL_0342
  IL_0295:  br         IL_034e
  IL_029a:  ldarg.0
  IL_029b:  ldstr      "Equality"
  IL_02a0:  call       "bool string.op_Equality(string, string)"
  IL_02a5:  brtrue     IL_0342
  IL_02aa:  br         IL_034e
  IL_02af:  ldarg.0
  IL_02b0:  ldstr      "LessThan"
  IL_02b5:  call       "bool string.op_Equality(string, string)"
  IL_02ba:  brtrue     IL_0342
  IL_02bf:  br         IL_034e
  IL_02c4:  ldarg.0
  IL_02c5:  ldstr      "Implicit"
  IL_02ca:  call       "bool string.op_Equality(string, string)"
  IL_02cf:  brtrue.s   IL_0348
  IL_02d1:  br.s       IL_034e
  IL_02d3:  ldarg.0
  IL_02d4:  ldstr      "Explicit"
  IL_02d9:  call       "bool string.op_Equality(string, string)"
  IL_02de:  brtrue.s   IL_0348
  IL_02e0:  br.s       IL_034e
  IL_02e2:  ldarg.0
  IL_02e3:  ldstr      "Subtraction"
  IL_02e8:  call       "bool string.op_Equality(string, string)"
  IL_02ed:  brtrue.s   IL_0342
  IL_02ef:  br.s       IL_034e
  IL_02f1:  ldarg.0
  IL_02f2:  ldstr      "ExclusiveOr"
  IL_02f7:  call       "bool string.op_Equality(string, string)"
  IL_02fc:  brtrue.s   IL_0342
  IL_02fe:  br.s       IL_034e
  IL_0300:  ldarg.0
  IL_0301:  ldstr      "GreaterThan"
  IL_0306:  call       "bool string.op_Equality(string, string)"
  IL_030b:  brtrue.s   IL_0342
  IL_030d:  br.s       IL_034e
  IL_030f:  ldarg.0
  IL_0310:  ldstr      "Modulus"
  IL_0315:  call       "bool string.op_Equality(string, string)"
  IL_031a:  brtrue.s   IL_0342
  IL_031c:  br.s       IL_034e
  IL_031e:  ldarg.0
  IL_031f:  ldstr      "LessThanOrEqual"
  IL_0324:  call       "bool string.op_Equality(string, string)"
  IL_0329:  brtrue.s   IL_0342
  IL_032b:  br.s       IL_034e
  IL_032d:  ldarg.0
  IL_032e:  ldstr      "GreaterThanOrEqual"
  IL_0333:  call       "bool string.op_Equality(string, string)"
  IL_0338:  brtrue.s   IL_0342
  IL_033a:  br.s       IL_034e
  IL_033c:  ldstr      "1"
  IL_0341:  ret
  IL_0342:  ldstr      "2"
  IL_0347:  ret
  IL_0348:  ldstr      "3"
  IL_034d:  ret
  IL_034e:  ldstr      "default"
  IL_0353:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void EmitIL()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/coreclr/tools/Common/TypeSystem/IL/Stubs/UnsafeIntrinsics.cs#L21-L94
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("AsPointer");
assert("As");
assert("AsRef");
assert("Add");
assert("AddByteOffset");
assert("Copy");
assert("CopyBlock");
assert("CopyBlockUnaligned");
assert("InitBlock");
assert("InitBlockUnaligned");
assert("Read");
assert("Write");
assert("ReadUnaligned");
assert("WriteUnaligned");
assert("AreSame");
assert("ByteOffset");
assert("NullRef");
assert("IsNullRef");
assert("SkipInit");
assert("Subtract");
assert("SubtractByteOffset");
assert("Unbox");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "AsPointer" => "AsPointer",
            "As" => "As",
            "AsRef" => "AsRef",
            "Add" => "Add",
            "AddByteOffset" => "AddByteOffset",
            "Copy" => "Copy",
            "CopyBlock" => "CopyBlock",
            "CopyBlockUnaligned" => "CopyBlockUnaligned",
            "InitBlock" => "InitBlock",
            "InitBlockUnaligned" => "InitBlockUnaligned",
            "Read" => "Read",
            "Write" => "Write",
            "ReadUnaligned" => "ReadUnaligned",
            "WriteUnaligned" => "WriteUnaligned",
            "AreSame" => "AreSame",
            "ByteOffset" => "ByteOffset",
            "NullRef" => "NullRef",
            "IsNullRef" => "IsNullRef",
            "SkipInit" => "SkipInit",
            "Subtract" => "Subtract",
            "SubtractByteOffset" => "SubtractByteOffset",
            "Unbox" => "Unbox",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     1003 (0x3eb)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_03e3
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.2
  IL_000f:  sub
  IL_0010:  switch    (
        IL_01a7,
        IL_01fb,
        IL_00d7,
        IL_0095,
        IL_03e3,
        IL_0119,
        IL_0136,
        IL_005e,
        IL_02e2,
        IL_03e3,
        IL_03e3,
        IL_00ba,
        IL_02a3,
        IL_03e3,
        IL_03e3,
        IL_03e3,
        IL_00f4)
  IL_0059:  br         IL_03e3
  IL_005e:  ldarg.0
  IL_005f:  ldc.i4.2
  IL_0060:  call       "char string.this[int].get"
  IL_0065:  stloc.2
  IL_0066:  ldloc.2
  IL_0067:  ldc.i4.s   80
  IL_0069:  bgt.un.s   IL_0080
  IL_006b:  ldloc.2
  IL_006c:  ldc.i4.s   78
  IL_006e:  beq        IL_0192
  IL_0073:  ldloc.2
  IL_0074:  ldc.i4.s   80
  IL_0076:  beq        IL_0153
  IL_007b:  br         IL_03e3
  IL_0080:  ldloc.2
  IL_0081:  ldc.i4.s   105
  IL_0083:  beq        IL_017d
  IL_0088:  ldloc.2
  IL_0089:  ldc.i4.s   112
  IL_008b:  beq        IL_0168
  IL_0090:  br         IL_03e3
  IL_0095:  ldarg.0
  IL_0096:  ldc.i4.0
  IL_0097:  call       "char string.this[int].get"
  IL_009c:  stloc.2
  IL_009d:  ldloc.2
  IL_009e:  ldc.i4.s   65
  IL_00a0:  beq        IL_01bc
  IL_00a5:  ldloc.2
  IL_00a6:  ldc.i4.s   85
  IL_00a8:  beq        IL_01e6
  IL_00ad:  ldloc.2
  IL_00ae:  ldc.i4.s   87
  IL_00b0:  beq        IL_01d1
  IL_00b5:  br         IL_03e3
  IL_00ba:  ldarg.0
  IL_00bb:  ldc.i4.0
  IL_00bc:  call       "char string.this[int].get"
  IL_00c1:  stloc.2
  IL_00c2:  ldloc.2
  IL_00c3:  ldc.i4.s   65
  IL_00c5:  beq        IL_0210
  IL_00ca:  ldloc.2
  IL_00cb:  ldc.i4.s   82
  IL_00cd:  beq        IL_0225
  IL_00d2:  br         IL_03e3
  IL_00d7:  ldarg.0
  IL_00d8:  ldc.i4.0
  IL_00d9:  call       "char string.this[int].get"
  IL_00de:  stloc.2
  IL_00df:  ldloc.2
  IL_00e0:  ldc.i4.s   67
  IL_00e2:  beq        IL_023a
  IL_00e7:  ldloc.2
  IL_00e8:  ldc.i4.s   82
  IL_00ea:  beq        IL_024f
  IL_00ef:  br         IL_03e3
  IL_00f4:  ldarg.0
  IL_00f5:  ldc.i4.0
  IL_00f6:  call       "char string.this[int].get"
  IL_00fb:  stloc.2
  IL_00fc:  ldloc.2
  IL_00fd:  ldc.i4.s   67
  IL_00ff:  beq        IL_0264
  IL_0104:  ldloc.2
  IL_0105:  ldc.i4.s   73
  IL_0107:  beq        IL_0279
  IL_010c:  ldloc.2
  IL_010d:  ldc.i4.s   83
  IL_010f:  beq        IL_028e
  IL_0114:  br         IL_03e3
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.0
  IL_011b:  call       "char string.this[int].get"
  IL_0120:  stloc.2
  IL_0121:  ldloc.2
  IL_0122:  ldc.i4.s   65
  IL_0124:  beq        IL_02b8
  IL_0129:  ldloc.2
  IL_012a:  ldc.i4.s   78
  IL_012c:  beq        IL_02cd
  IL_0131:  br         IL_03e3
  IL_0136:  ldarg.0
  IL_0137:  ldc.i4.1
  IL_0138:  call       "char string.this[int].get"
  IL_013d:  stloc.2
  IL_013e:  ldloc.2
  IL_013f:  ldc.i4.s   107
  IL_0141:  beq        IL_02f7
  IL_0146:  ldloc.2
  IL_0147:  ldc.i4.s   117
  IL_0149:  beq        IL_030c
  IL_014e:  br         IL_03e3
  IL_0153:  ldarg.0
  IL_0154:  ldstr      "AsPointer"
  IL_0159:  call       "bool string.op_Equality(string, string)"
  IL_015e:  brtrue     IL_0321
  IL_0163:  br         IL_03e3
  IL_0168:  ldarg.0
  IL_0169:  ldstr      "CopyBlock"
  IL_016e:  call       "bool string.op_Equality(string, string)"
  IL_0173:  brtrue     IL_0363
  IL_0178:  br         IL_03e3
  IL_017d:  ldarg.0
  IL_017e:  ldstr      "InitBlock"
  IL_0183:  call       "bool string.op_Equality(string, string)"
  IL_0188:  brtrue     IL_0373
  IL_018d:  br         IL_03e3
  IL_0192:  ldarg.0
  IL_0193:  ldstr      "IsNullRef"
  IL_0198:  call       "bool string.op_Equality(string, string)"
  IL_019d:  brtrue     IL_03bb
  IL_01a2:  br         IL_03e3
  IL_01a7:  ldarg.0
  IL_01a8:  ldstr      "As"
  IL_01ad:  call       "bool string.op_Equality(string, string)"
  IL_01b2:  brtrue     IL_032c
  IL_01b7:  br         IL_03e3
  IL_01bc:  ldarg.0
  IL_01bd:  ldstr      "AsRef"
  IL_01c2:  call       "bool string.op_Equality(string, string)"
  IL_01c7:  brtrue     IL_0337
  IL_01cc:  br         IL_03e3
  IL_01d1:  ldarg.0
  IL_01d2:  ldstr      "Write"
  IL_01d7:  call       "bool string.op_Equality(string, string)"
  IL_01dc:  brtrue     IL_038b
  IL_01e1:  br         IL_03e3
  IL_01e6:  ldarg.0
  IL_01e7:  ldstr      "Unbox"
  IL_01ec:  call       "bool string.op_Equality(string, string)"
  IL_01f1:  brtrue     IL_03db
  IL_01f6:  br         IL_03e3
  IL_01fb:  ldarg.0
  IL_01fc:  ldstr      "Add"
  IL_0201:  call       "bool string.op_Equality(string, string)"
  IL_0206:  brtrue     IL_0342
  IL_020b:  br         IL_03e3
  IL_0210:  ldarg.0
  IL_0211:  ldstr      "AddByteOffset"
  IL_0216:  call       "bool string.op_Equality(string, string)"
  IL_021b:  brtrue     IL_034d
  IL_0220:  br         IL_03e3
  IL_0225:  ldarg.0
  IL_0226:  ldstr      "ReadUnaligned"
  IL_022b:  call       "bool string.op_Equality(string, string)"
  IL_0230:  brtrue     IL_0393
  IL_0235:  br         IL_03e3
  IL_023a:  ldarg.0
  IL_023b:  ldstr      "Copy"
  IL_0240:  call       "bool string.op_Equality(string, string)"
  IL_0245:  brtrue     IL_0358
  IL_024a:  br         IL_03e3
  IL_024f:  ldarg.0
  IL_0250:  ldstr      "Read"
  IL_0255:  call       "bool string.op_Equality(string, string)"
  IL_025a:  brtrue     IL_0383
  IL_025f:  br         IL_03e3
  IL_0264:  ldarg.0
  IL_0265:  ldstr      "CopyBlockUnaligned"
  IL_026a:  call       "bool string.op_Equality(string, string)"
  IL_026f:  brtrue     IL_036b
  IL_0274:  br         IL_03e3
  IL_0279:  ldarg.0
  IL_027a:  ldstr      "InitBlockUnaligned"
  IL_027f:  call       "bool string.op_Equality(string, string)"
  IL_0284:  brtrue     IL_037b
  IL_0289:  br         IL_03e3
  IL_028e:  ldarg.0
  IL_028f:  ldstr      "SubtractByteOffset"
  IL_0294:  call       "bool string.op_Equality(string, string)"
  IL_0299:  brtrue     IL_03d3
  IL_029e:  br         IL_03e3
  IL_02a3:  ldarg.0
  IL_02a4:  ldstr      "WriteUnaligned"
  IL_02a9:  call       "bool string.op_Equality(string, string)"
  IL_02ae:  brtrue     IL_039b
  IL_02b3:  br         IL_03e3
  IL_02b8:  ldarg.0
  IL_02b9:  ldstr      "AreSame"
  IL_02be:  call       "bool string.op_Equality(string, string)"
  IL_02c3:  brtrue     IL_03a3
  IL_02c8:  br         IL_03e3
  IL_02cd:  ldarg.0
  IL_02ce:  ldstr      "NullRef"
  IL_02d3:  call       "bool string.op_Equality(string, string)"
  IL_02d8:  brtrue     IL_03b3
  IL_02dd:  br         IL_03e3
  IL_02e2:  ldarg.0
  IL_02e3:  ldstr      "ByteOffset"
  IL_02e8:  call       "bool string.op_Equality(string, string)"
  IL_02ed:  brtrue     IL_03ab
  IL_02f2:  br         IL_03e3
  IL_02f7:  ldarg.0
  IL_02f8:  ldstr      "SkipInit"
  IL_02fd:  call       "bool string.op_Equality(string, string)"
  IL_0302:  brtrue     IL_03c3
  IL_0307:  br         IL_03e3
  IL_030c:  ldarg.0
  IL_030d:  ldstr      "Subtract"
  IL_0312:  call       "bool string.op_Equality(string, string)"
  IL_0317:  brtrue     IL_03cb
  IL_031c:  br         IL_03e3
  IL_0321:  ldstr      "AsPointer"
  IL_0326:  stloc.0
  IL_0327:  br         IL_03e9
  IL_032c:  ldstr      "As"
  IL_0331:  stloc.0
  IL_0332:  br         IL_03e9
  IL_0337:  ldstr      "AsRef"
  IL_033c:  stloc.0
  IL_033d:  br         IL_03e9
  IL_0342:  ldstr      "Add"
  IL_0347:  stloc.0
  IL_0348:  br         IL_03e9
  IL_034d:  ldstr      "AddByteOffset"
  IL_0352:  stloc.0
  IL_0353:  br         IL_03e9
  IL_0358:  ldstr      "Copy"
  IL_035d:  stloc.0
  IL_035e:  br         IL_03e9
  IL_0363:  ldstr      "CopyBlock"
  IL_0368:  stloc.0
  IL_0369:  br.s       IL_03e9
  IL_036b:  ldstr      "CopyBlockUnaligned"
  IL_0370:  stloc.0
  IL_0371:  br.s       IL_03e9
  IL_0373:  ldstr      "InitBlock"
  IL_0378:  stloc.0
  IL_0379:  br.s       IL_03e9
  IL_037b:  ldstr      "InitBlockUnaligned"
  IL_0380:  stloc.0
  IL_0381:  br.s       IL_03e9
  IL_0383:  ldstr      "Read"
  IL_0388:  stloc.0
  IL_0389:  br.s       IL_03e9
  IL_038b:  ldstr      "Write"
  IL_0390:  stloc.0
  IL_0391:  br.s       IL_03e9
  IL_0393:  ldstr      "ReadUnaligned"
  IL_0398:  stloc.0
  IL_0399:  br.s       IL_03e9
  IL_039b:  ldstr      "WriteUnaligned"
  IL_03a0:  stloc.0
  IL_03a1:  br.s       IL_03e9
  IL_03a3:  ldstr      "AreSame"
  IL_03a8:  stloc.0
  IL_03a9:  br.s       IL_03e9
  IL_03ab:  ldstr      "ByteOffset"
  IL_03b0:  stloc.0
  IL_03b1:  br.s       IL_03e9
  IL_03b3:  ldstr      "NullRef"
  IL_03b8:  stloc.0
  IL_03b9:  br.s       IL_03e9
  IL_03bb:  ldstr      "IsNullRef"
  IL_03c0:  stloc.0
  IL_03c1:  br.s       IL_03e9
  IL_03c3:  ldstr      "SkipInit"
  IL_03c8:  stloc.0
  IL_03c9:  br.s       IL_03e9
  IL_03cb:  ldstr      "Subtract"
  IL_03d0:  stloc.0
  IL_03d1:  br.s       IL_03e9
  IL_03d3:  ldstr      "SubtractByteOffset"
  IL_03d8:  stloc.0
  IL_03d9:  br.s       IL_03e9
  IL_03db:  ldstr      "Unbox"
  IL_03e0:  stloc.0
  IL_03e1:  br.s       IL_03e9
  IL_03e3:  ldstr      "default"
  IL_03e8:  stloc.0
  IL_03e9:  ldloc.0
  IL_03ea:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size     1003 (0x3eb)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x76692b6a
  IL_000d:  bgt.un     IL_00b7
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x5cb9b8dd
  IL_0018:  bgt.un.s   IL_0063
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4     0x1e73dfe4
  IL_0020:  bgt.un.s   IL_003d
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x10bc73b2
  IL_0028:  beq        IL_0153
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0x1e73dfe4
  IL_0033:  beq        IL_028e
  IL_0038:  br         IL_03e3
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4     0x3c92fd81
  IL_0043:  beq        IL_02e2
  IL_0048:  ldloc.1
  IL_0049:  ldc.i4     0x4f0befe5
  IL_004e:  beq        IL_0225
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4     0x5cb9b8dd
  IL_0059:  beq        IL_01d1
  IL_005e:  br         IL_03e3
  IL_0063:  ldloc.1
  IL_0064:  ldc.i4     0x65710578
  IL_0069:  bgt.un.s   IL_0091
  IL_006b:  ldloc.1
  IL_006c:  ldc.i4     0x5dd56ead
  IL_0071:  beq        IL_0168
  IL_0076:  ldloc.1
  IL_0077:  ldc.i4     0x603ac699
  IL_007c:  beq        IL_02a3
  IL_0081:  ldloc.1
  IL_0082:  ldc.i4     0x65710578
  IL_0087:  beq        IL_02f7
  IL_008c:  br         IL_03e3
  IL_0091:  ldloc.1
  IL_0092:  ldc.i4     0x658f3664
  IL_0097:  beq        IL_01bc
  IL_009c:  ldloc.1
  IL_009d:  ldc.i4     0x665c79fc
  IL_00a2:  beq        IL_023a
  IL_00a7:  ldloc.1
  IL_00a8:  ldc.i4     0x76692b6a
  IL_00ad:  beq        IL_024f
  IL_00b2:  br         IL_03e3
  IL_00b7:  ldloc.1
  IL_00b8:  ldc.i4     0x9c4426da
  IL_00bd:  bgt.un.s   IL_0105
  IL_00bf:  ldloc.1
  IL_00c0:  ldc.i4     0x8e475485
  IL_00c5:  bgt.un.s   IL_00e2
  IL_00c7:  ldloc.1
  IL_00c8:  ldc.i4     0x79d7c769
  IL_00cd:  beq        IL_02b8
  IL_00d2:  ldloc.1
  IL_00d3:  ldc.i4     0x8e475485
  IL_00d8:  beq        IL_0210
  IL_00dd:  br         IL_03e3
  IL_00e2:  ldloc.1
  IL_00e3:  ldc.i4     0x96bf6e27
  IL_00e8:  beq        IL_030c
  IL_00ed:  ldloc.1
  IL_00ee:  ldc.i4     0x98df118c
  IL_00f3:  beq        IL_01fb
  IL_00f8:  ldloc.1
  IL_00f9:  ldc.i4     0x9c4426da
  IL_00fe:  beq.s      IL_017d
  IL_0100:  br         IL_03e3
  IL_0105:  ldloc.1
  IL_0106:  ldc.i4     0xac7eb953
  IL_010b:  bgt.un.s   IL_0130
  IL_010d:  ldloc.1
  IL_010e:  ldc.i4     0x9dc3aa14
  IL_0113:  beq.s      IL_0192
  IL_0115:  ldloc.1
  IL_0116:  ldc.i4     0xa5c174f5
  IL_011b:  beq        IL_0264
  IL_0120:  ldloc.1
  IL_0121:  ldc.i4     0xac7eb953
  IL_0126:  beq        IL_0279
  IL_012b:  br         IL_03e3
  IL_0130:  ldloc.1
  IL_0131:  ldc.i4     0xb5f98d32
  IL_0136:  beq        IL_01e6
  IL_013b:  ldloc.1
  IL_013c:  ldc.i4     0xdae6a448
  IL_0141:  beq        IL_02cd
  IL_0146:  ldloc.1
  IL_0147:  ldc.i4     0xf8b02149
  IL_014c:  beq.s      IL_01a7
  IL_014e:  br         IL_03e3
  IL_0153:  ldarg.0
  IL_0154:  ldstr      "AsPointer"
  IL_0159:  call       "bool string.op_Equality(string, string)"
  IL_015e:  brtrue     IL_0321
  IL_0163:  br         IL_03e3
  IL_0168:  ldarg.0
  IL_0169:  ldstr      "As"
  IL_016e:  call       "bool string.op_Equality(string, string)"
  IL_0173:  brtrue     IL_032c
  IL_0178:  br         IL_03e3
  IL_017d:  ldarg.0
  IL_017e:  ldstr      "AsRef"
  IL_0183:  call       "bool string.op_Equality(string, string)"
  IL_0188:  brtrue     IL_0337
  IL_018d:  br         IL_03e3
  IL_0192:  ldarg.0
  IL_0193:  ldstr      "Add"
  IL_0198:  call       "bool string.op_Equality(string, string)"
  IL_019d:  brtrue     IL_0342
  IL_01a2:  br         IL_03e3
  IL_01a7:  ldarg.0
  IL_01a8:  ldstr      "AddByteOffset"
  IL_01ad:  call       "bool string.op_Equality(string, string)"
  IL_01b2:  brtrue     IL_034d
  IL_01b7:  br         IL_03e3
  IL_01bc:  ldarg.0
  IL_01bd:  ldstr      "Copy"
  IL_01c2:  call       "bool string.op_Equality(string, string)"
  IL_01c7:  brtrue     IL_0358
  IL_01cc:  br         IL_03e3
  IL_01d1:  ldarg.0
  IL_01d2:  ldstr      "CopyBlock"
  IL_01d7:  call       "bool string.op_Equality(string, string)"
  IL_01dc:  brtrue     IL_0363
  IL_01e1:  br         IL_03e3
  IL_01e6:  ldarg.0
  IL_01e7:  ldstr      "CopyBlockUnaligned"
  IL_01ec:  call       "bool string.op_Equality(string, string)"
  IL_01f1:  brtrue     IL_036b
  IL_01f6:  br         IL_03e3
  IL_01fb:  ldarg.0
  IL_01fc:  ldstr      "InitBlock"
  IL_0201:  call       "bool string.op_Equality(string, string)"
  IL_0206:  brtrue     IL_0373
  IL_020b:  br         IL_03e3
  IL_0210:  ldarg.0
  IL_0211:  ldstr      "InitBlockUnaligned"
  IL_0216:  call       "bool string.op_Equality(string, string)"
  IL_021b:  brtrue     IL_037b
  IL_0220:  br         IL_03e3
  IL_0225:  ldarg.0
  IL_0226:  ldstr      "Read"
  IL_022b:  call       "bool string.op_Equality(string, string)"
  IL_0230:  brtrue     IL_0383
  IL_0235:  br         IL_03e3
  IL_023a:  ldarg.0
  IL_023b:  ldstr      "Write"
  IL_0240:  call       "bool string.op_Equality(string, string)"
  IL_0245:  brtrue     IL_038b
  IL_024a:  br         IL_03e3
  IL_024f:  ldarg.0
  IL_0250:  ldstr      "ReadUnaligned"
  IL_0255:  call       "bool string.op_Equality(string, string)"
  IL_025a:  brtrue     IL_0393
  IL_025f:  br         IL_03e3
  IL_0264:  ldarg.0
  IL_0265:  ldstr      "WriteUnaligned"
  IL_026a:  call       "bool string.op_Equality(string, string)"
  IL_026f:  brtrue     IL_039b
  IL_0274:  br         IL_03e3
  IL_0279:  ldarg.0
  IL_027a:  ldstr      "AreSame"
  IL_027f:  call       "bool string.op_Equality(string, string)"
  IL_0284:  brtrue     IL_03a3
  IL_0289:  br         IL_03e3
  IL_028e:  ldarg.0
  IL_028f:  ldstr      "ByteOffset"
  IL_0294:  call       "bool string.op_Equality(string, string)"
  IL_0299:  brtrue     IL_03ab
  IL_029e:  br         IL_03e3
  IL_02a3:  ldarg.0
  IL_02a4:  ldstr      "NullRef"
  IL_02a9:  call       "bool string.op_Equality(string, string)"
  IL_02ae:  brtrue     IL_03b3
  IL_02b3:  br         IL_03e3
  IL_02b8:  ldarg.0
  IL_02b9:  ldstr      "IsNullRef"
  IL_02be:  call       "bool string.op_Equality(string, string)"
  IL_02c3:  brtrue     IL_03bb
  IL_02c8:  br         IL_03e3
  IL_02cd:  ldarg.0
  IL_02ce:  ldstr      "SkipInit"
  IL_02d3:  call       "bool string.op_Equality(string, string)"
  IL_02d8:  brtrue     IL_03c3
  IL_02dd:  br         IL_03e3
  IL_02e2:  ldarg.0
  IL_02e3:  ldstr      "Subtract"
  IL_02e8:  call       "bool string.op_Equality(string, string)"
  IL_02ed:  brtrue     IL_03cb
  IL_02f2:  br         IL_03e3
  IL_02f7:  ldarg.0
  IL_02f8:  ldstr      "SubtractByteOffset"
  IL_02fd:  call       "bool string.op_Equality(string, string)"
  IL_0302:  brtrue     IL_03d3
  IL_0307:  br         IL_03e3
  IL_030c:  ldarg.0
  IL_030d:  ldstr      "Unbox"
  IL_0312:  call       "bool string.op_Equality(string, string)"
  IL_0317:  brtrue     IL_03db
  IL_031c:  br         IL_03e3
  IL_0321:  ldstr      "AsPointer"
  IL_0326:  stloc.0
  IL_0327:  br         IL_03e9
  IL_032c:  ldstr      "As"
  IL_0331:  stloc.0
  IL_0332:  br         IL_03e9
  IL_0337:  ldstr      "AsRef"
  IL_033c:  stloc.0
  IL_033d:  br         IL_03e9
  IL_0342:  ldstr      "Add"
  IL_0347:  stloc.0
  IL_0348:  br         IL_03e9
  IL_034d:  ldstr      "AddByteOffset"
  IL_0352:  stloc.0
  IL_0353:  br         IL_03e9
  IL_0358:  ldstr      "Copy"
  IL_035d:  stloc.0
  IL_035e:  br         IL_03e9
  IL_0363:  ldstr      "CopyBlock"
  IL_0368:  stloc.0
  IL_0369:  br.s       IL_03e9
  IL_036b:  ldstr      "CopyBlockUnaligned"
  IL_0370:  stloc.0
  IL_0371:  br.s       IL_03e9
  IL_0373:  ldstr      "InitBlock"
  IL_0378:  stloc.0
  IL_0379:  br.s       IL_03e9
  IL_037b:  ldstr      "InitBlockUnaligned"
  IL_0380:  stloc.0
  IL_0381:  br.s       IL_03e9
  IL_0383:  ldstr      "Read"
  IL_0388:  stloc.0
  IL_0389:  br.s       IL_03e9
  IL_038b:  ldstr      "Write"
  IL_0390:  stloc.0
  IL_0391:  br.s       IL_03e9
  IL_0393:  ldstr      "ReadUnaligned"
  IL_0398:  stloc.0
  IL_0399:  br.s       IL_03e9
  IL_039b:  ldstr      "WriteUnaligned"
  IL_03a0:  stloc.0
  IL_03a1:  br.s       IL_03e9
  IL_03a3:  ldstr      "AreSame"
  IL_03a8:  stloc.0
  IL_03a9:  br.s       IL_03e9
  IL_03ab:  ldstr      "ByteOffset"
  IL_03b0:  stloc.0
  IL_03b1:  br.s       IL_03e9
  IL_03b3:  ldstr      "NullRef"
  IL_03b8:  stloc.0
  IL_03b9:  br.s       IL_03e9
  IL_03bb:  ldstr      "IsNullRef"
  IL_03c0:  stloc.0
  IL_03c1:  br.s       IL_03e9
  IL_03c3:  ldstr      "SkipInit"
  IL_03c8:  stloc.0
  IL_03c9:  br.s       IL_03e9
  IL_03cb:  ldstr      "Subtract"
  IL_03d0:  stloc.0
  IL_03d1:  br.s       IL_03e9
  IL_03d3:  ldstr      "SubtractByteOffset"
  IL_03d8:  stloc.0
  IL_03d9:  br.s       IL_03e9
  IL_03db:  ldstr      "Unbox"
  IL_03e0:  stloc.0
  IL_03e1:  br.s       IL_03e9
  IL_03e3:  ldstr      "default"
  IL_03e8:  stloc.0
  IL_03e9:  ldloc.0
  IL_03ea:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void GetWellKnownType()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/coreclr/tools/aot/ILLink.Shared/TypeSystemProxy/WellKnownType.cs#L48-L58
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("String");
assert("Nullable`1");
assert("Type");
assert("Array");
assert("Attribute");
assert("Object");
assert("NotSupportedException");
assert("Void");

assert("other", "default");
assert(null, "default");
assert("", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "String" => "String",
            "Nullable`1" => "Nullable`1",
            "Type" => "Type",
            "Array" => "Array",
            "Attribute" => "Attribute",
            "Object" => "Object",
            "NotSupportedException" => "NotSupportedException",
            "Void" => "Void",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      312 (0x138)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0130
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.4
  IL_000f:  sub
  IL_0010:  switch    (
        IL_0055,
        IL_00c3,
        IL_003e,
        IL_0130,
        IL_0130,
        IL_00d2,
        IL_0093)
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.s   21
  IL_0034:  beq        IL_00e1
  IL_0039:  br         IL_0130
  IL_003e:  ldarg.0
  IL_003f:  ldc.i4.0
  IL_0040:  call       "char string.this[int].get"
  IL_0045:  stloc.2
  IL_0046:  ldloc.2
  IL_0047:  ldc.i4.s   79
  IL_0049:  beq.s      IL_007e
  IL_004b:  ldloc.2
  IL_004c:  ldc.i4.s   83
  IL_004e:  beq.s      IL_006c
  IL_0050:  br         IL_0130
  IL_0055:  ldarg.0
  IL_0056:  ldc.i4.0
  IL_0057:  call       "char string.this[int].get"
  IL_005c:  stloc.2
  IL_005d:  ldloc.2
  IL_005e:  ldc.i4.s   84
  IL_0060:  beq.s      IL_00a5
  IL_0062:  ldloc.2
  IL_0063:  ldc.i4.s   86
  IL_0065:  beq.s      IL_00b4
  IL_0067:  br         IL_0130
  IL_006c:  ldarg.0
  IL_006d:  ldstr      "String"
  IL_0072:  call       "bool string.op_Equality(string, string)"
  IL_0077:  brtrue.s   IL_00f0
  IL_0079:  br         IL_0130
  IL_007e:  ldarg.0
  IL_007f:  ldstr      "Object"
  IL_0084:  call       "bool string.op_Equality(string, string)"
  IL_0089:  brtrue     IL_0118
  IL_008e:  br         IL_0130
  IL_0093:  ldarg.0
  IL_0094:  ldstr      "Nullable`1"
  IL_0099:  call       "bool string.op_Equality(string, string)"
  IL_009e:  brtrue.s   IL_00f8
  IL_00a0:  br         IL_0130
  IL_00a5:  ldarg.0
  IL_00a6:  ldstr      "Type"
  IL_00ab:  call       "bool string.op_Equality(string, string)"
  IL_00b0:  brtrue.s   IL_0100
  IL_00b2:  br.s       IL_0130
  IL_00b4:  ldarg.0
  IL_00b5:  ldstr      "Void"
  IL_00ba:  call       "bool string.op_Equality(string, string)"
  IL_00bf:  brtrue.s   IL_0128
  IL_00c1:  br.s       IL_0130
  IL_00c3:  ldarg.0
  IL_00c4:  ldstr      "Array"
  IL_00c9:  call       "bool string.op_Equality(string, string)"
  IL_00ce:  brtrue.s   IL_0108
  IL_00d0:  br.s       IL_0130
  IL_00d2:  ldarg.0
  IL_00d3:  ldstr      "Attribute"
  IL_00d8:  call       "bool string.op_Equality(string, string)"
  IL_00dd:  brtrue.s   IL_0110
  IL_00df:  br.s       IL_0130
  IL_00e1:  ldarg.0
  IL_00e2:  ldstr      "NotSupportedException"
  IL_00e7:  call       "bool string.op_Equality(string, string)"
  IL_00ec:  brtrue.s   IL_0120
  IL_00ee:  br.s       IL_0130
  IL_00f0:  ldstr      "String"
  IL_00f5:  stloc.0
  IL_00f6:  br.s       IL_0136
  IL_00f8:  ldstr      "Nullable`1"
  IL_00fd:  stloc.0
  IL_00fe:  br.s       IL_0136
  IL_0100:  ldstr      "Type"
  IL_0105:  stloc.0
  IL_0106:  br.s       IL_0136
  IL_0108:  ldstr      "Array"
  IL_010d:  stloc.0
  IL_010e:  br.s       IL_0136
  IL_0110:  ldstr      "Attribute"
  IL_0115:  stloc.0
  IL_0116:  br.s       IL_0136
  IL_0118:  ldstr      "Object"
  IL_011d:  stloc.0
  IL_011e:  br.s       IL_0136
  IL_0120:  ldstr      "NotSupportedException"
  IL_0125:  stloc.0
  IL_0126:  br.s       IL_0136
  IL_0128:  ldstr      "Void"
  IL_012d:  stloc.0
  IL_012e:  br.s       IL_0136
  IL_0130:  ldstr      "default"
  IL_0135:  stloc.0
  IL_0136:  ldloc.0
  IL_0137:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      325 (0x145)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x62fa988f
  IL_000d:  bgt.un.s   IL_0047
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4     0x326d11d1
  IL_0015:  bgt.un.s   IL_0032
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4     0x16c8fcc6
  IL_001d:  beq        IL_00b2
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x326d11d1
  IL_0028:  beq        IL_00df
  IL_002d:  br         IL_013d
  IL_0032:  ldloc.1
  IL_0033:  ldc.i4     0x604f4858
  IL_0038:  beq.s      IL_007c
  IL_003a:  ldloc.1
  IL_003b:  ldc.i4     0x62fa988f
  IL_0040:  beq.s      IL_00c1
  IL_0042:  br         IL_013d
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4     0xc8e3517f
  IL_004d:  bgt.un.s   IL_0067
  IL_004f:  ldloc.1
  IL_0050:  ldc.i4     0x66898a9f
  IL_0055:  beq.s      IL_008e
  IL_0057:  ldloc.1
  IL_0058:  ldc.i4     0xc8e3517f
  IL_005d:  beq        IL_00ee
  IL_0062:  br         IL_013d
  IL_0067:  ldloc.1
  IL_0068:  ldc.i4     0xd155d06d
  IL_006d:  beq.s      IL_00a0
  IL_006f:  ldloc.1
  IL_0070:  ldc.i4     0xe58e64da
  IL_0075:  beq.s      IL_00d0
  IL_0077:  br         IL_013d
  IL_007c:  ldarg.0
  IL_007d:  ldstr      "String"
  IL_0082:  call       "bool string.op_Equality(string, string)"
  IL_0087:  brtrue.s   IL_00fd
  IL_0089:  br         IL_013d
  IL_008e:  ldarg.0
  IL_008f:  ldstr      "Nullable`1"
  IL_0094:  call       "bool string.op_Equality(string, string)"
  IL_0099:  brtrue.s   IL_0105
  IL_009b:  br         IL_013d
  IL_00a0:  ldarg.0
  IL_00a1:  ldstr      "Type"
  IL_00a6:  call       "bool string.op_Equality(string, string)"
  IL_00ab:  brtrue.s   IL_010d
  IL_00ad:  br         IL_013d
  IL_00b2:  ldarg.0
  IL_00b3:  ldstr      "Array"
  IL_00b8:  call       "bool string.op_Equality(string, string)"
  IL_00bd:  brtrue.s   IL_0115
  IL_00bf:  br.s       IL_013d
  IL_00c1:  ldarg.0
  IL_00c2:  ldstr      "Attribute"
  IL_00c7:  call       "bool string.op_Equality(string, string)"
  IL_00cc:  brtrue.s   IL_011d
  IL_00ce:  br.s       IL_013d
  IL_00d0:  ldarg.0
  IL_00d1:  ldstr      "Object"
  IL_00d6:  call       "bool string.op_Equality(string, string)"
  IL_00db:  brtrue.s   IL_0125
  IL_00dd:  br.s       IL_013d
  IL_00df:  ldarg.0
  IL_00e0:  ldstr      "NotSupportedException"
  IL_00e5:  call       "bool string.op_Equality(string, string)"
  IL_00ea:  brtrue.s   IL_012d
  IL_00ec:  br.s       IL_013d
  IL_00ee:  ldarg.0
  IL_00ef:  ldstr      "Void"
  IL_00f4:  call       "bool string.op_Equality(string, string)"
  IL_00f9:  brtrue.s   IL_0135
  IL_00fb:  br.s       IL_013d
  IL_00fd:  ldstr      "String"
  IL_0102:  stloc.0
  IL_0103:  br.s       IL_0143
  IL_0105:  ldstr      "Nullable`1"
  IL_010a:  stloc.0
  IL_010b:  br.s       IL_0143
  IL_010d:  ldstr      "Type"
  IL_0112:  stloc.0
  IL_0113:  br.s       IL_0143
  IL_0115:  ldstr      "Array"
  IL_011a:  stloc.0
  IL_011b:  br.s       IL_0143
  IL_011d:  ldstr      "Attribute"
  IL_0122:  stloc.0
  IL_0123:  br.s       IL_0143
  IL_0125:  ldstr      "Object"
  IL_012a:  stloc.0
  IL_012b:  br.s       IL_0143
  IL_012d:  ldstr      "NotSupportedException"
  IL_0132:  stloc.0
  IL_0133:  br.s       IL_0143
  IL_0135:  ldstr      "Void"
  IL_013a:  stloc.0
  IL_013b:  br.s       IL_0143
  IL_013d:  ldstr      "default"
  IL_0142:  stloc.0
  IL_0143:  ldloc.0
  IL_0144:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void GetLocalizedString()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.ComponentModel.Primitives/src/System/ComponentModel/CategoryAttribute.cs#L202-L226
        // Buckets: 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("Action");
assert("Appearance");
assert("Asynchronous");
assert("Behavior");
assert("Config");
assert("Data");
assert("DDE");
assert("Default");
assert("Design");
assert("DragDrop");
assert("Focus");
assert("Font");
assert("Format");
assert("Key");
assert("Layout");
assert("List");
assert("Mouse");
assert("Position");
assert("Scale");
assert("Text");
assert("WindowStyle");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "Action" => "Action",
            "Appearance" => "Appearance",
            "Asynchronous" => "Asynchronous",
            "Behavior" => "Behavior",
            "Config" => "Config",
            "Data" => "Data",
            "DDE" => "DDE",
            "Default" => "Default",
            "Design" => "Design",
            "DragDrop" => "DragDrop",
            "Focus" => "Focus",
            "Font" => "Font",
            "Format" => "Format",
            "Key" => "Key",
            "Layout" => "Layout",
            "List" => "List",
            "Mouse" => "Mouse",
            "Position" => "Position",
            "Scale" => "Scale",
            "Text" => "Text",
            "WindowStyle" => "WindowStyle",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      910 (0x38e)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0386
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.3
  IL_000f:  sub
  IL_0010:  switch    (
        IL_00d4,
        IL_009d,
        IL_00f1,
        IL_0042,
        IL_0266,
        IL_0078,
        IL_0386,
        IL_017f,
        IL_02ba,
        IL_0194)
  IL_003d:  br         IL_0386
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.0
  IL_0044:  call       "char string.this[int].get"
  IL_0049:  stloc.2
  IL_004a:  ldloc.2
  IL_004b:  ldc.i4.s   65
  IL_004d:  sub
  IL_004e:  switch    (
        IL_0116,
        IL_0386,
        IL_012b,
        IL_0140,
        IL_0386,
        IL_0155)
  IL_006b:  ldloc.2
  IL_006c:  ldc.i4.s   76
  IL_006e:  beq        IL_016a
  IL_0073:  br         IL_0386
  IL_0078:  ldarg.0
  IL_0079:  ldc.i4.0
  IL_007a:  call       "char string.this[int].get"
  IL_007f:  stloc.2
  IL_0080:  ldloc.2
  IL_0081:  ldc.i4.s   66
  IL_0083:  beq        IL_01a9
  IL_0088:  ldloc.2
  IL_0089:  ldc.i4.s   68
  IL_008b:  beq        IL_01be
  IL_0090:  ldloc.2
  IL_0091:  ldc.i4.s   80
  IL_0093:  beq        IL_01d3
  IL_0098:  br         IL_0386
  IL_009d:  ldarg.0
  IL_009e:  ldc.i4.0
  IL_009f:  call       "char string.this[int].get"
  IL_00a4:  stloc.2
  IL_00a5:  ldloc.2
  IL_00a6:  ldc.i4.s   70
  IL_00a8:  bgt.un.s   IL_00bf
  IL_00aa:  ldloc.2
  IL_00ab:  ldc.i4.s   68
  IL_00ad:  beq        IL_01e8
  IL_00b2:  ldloc.2
  IL_00b3:  ldc.i4.s   70
  IL_00b5:  beq        IL_01fd
  IL_00ba:  br         IL_0386
  IL_00bf:  ldloc.2
  IL_00c0:  ldc.i4.s   76
  IL_00c2:  beq        IL_0212
  IL_00c7:  ldloc.2
  IL_00c8:  ldc.i4.s   84
  IL_00ca:  beq        IL_0227
  IL_00cf:  br         IL_0386
  IL_00d4:  ldarg.0
  IL_00d5:  ldc.i4.0
  IL_00d6:  call       "char string.this[int].get"
  IL_00db:  stloc.2
  IL_00dc:  ldloc.2
  IL_00dd:  ldc.i4.s   68
  IL_00df:  beq        IL_023c
  IL_00e4:  ldloc.2
  IL_00e5:  ldc.i4.s   75
  IL_00e7:  beq        IL_0251
  IL_00ec:  br         IL_0386
  IL_00f1:  ldarg.0
  IL_00f2:  ldc.i4.0
  IL_00f3:  call       "char string.this[int].get"
  IL_00f8:  stloc.2
  IL_00f9:  ldloc.2
  IL_00fa:  ldc.i4.s   70
  IL_00fc:  beq        IL_027b
  IL_0101:  ldloc.2
  IL_0102:  ldc.i4.s   77
  IL_0104:  beq        IL_0290
  IL_0109:  ldloc.2
  IL_010a:  ldc.i4.s   83
  IL_010c:  beq        IL_02a5
  IL_0111:  br         IL_0386
  IL_0116:  ldarg.0
  IL_0117:  ldstr      "Action"
  IL_011c:  call       "bool string.op_Equality(string, string)"
  IL_0121:  brtrue     IL_02cf
  IL_0126:  br         IL_0386
  IL_012b:  ldarg.0
  IL_012c:  ldstr      "Config"
  IL_0131:  call       "bool string.op_Equality(string, string)"
  IL_0136:  brtrue     IL_02fb
  IL_013b:  br         IL_0386
  IL_0140:  ldarg.0
  IL_0141:  ldstr      "Design"
  IL_0146:  call       "bool string.op_Equality(string, string)"
  IL_014b:  brtrue     IL_031e
  IL_0150:  br         IL_0386
  IL_0155:  ldarg.0
  IL_0156:  ldstr      "Format"
  IL_015b:  call       "bool string.op_Equality(string, string)"
  IL_0160:  brtrue     IL_033e
  IL_0165:  br         IL_0386
  IL_016a:  ldarg.0
  IL_016b:  ldstr      "Layout"
  IL_0170:  call       "bool string.op_Equality(string, string)"
  IL_0175:  brtrue     IL_034e
  IL_017a:  br         IL_0386
  IL_017f:  ldarg.0
  IL_0180:  ldstr      "Appearance"
  IL_0185:  call       "bool string.op_Equality(string, string)"
  IL_018a:  brtrue     IL_02da
  IL_018f:  br         IL_0386
  IL_0194:  ldarg.0
  IL_0195:  ldstr      "Asynchronous"
  IL_019a:  call       "bool string.op_Equality(string, string)"
  IL_019f:  brtrue     IL_02e5
  IL_01a4:  br         IL_0386
  IL_01a9:  ldarg.0
  IL_01aa:  ldstr      "Behavior"
  IL_01af:  call       "bool string.op_Equality(string, string)"
  IL_01b4:  brtrue     IL_02f0
  IL_01b9:  br         IL_0386
  IL_01be:  ldarg.0
  IL_01bf:  ldstr      "DragDrop"
  IL_01c4:  call       "bool string.op_Equality(string, string)"
  IL_01c9:  brtrue     IL_0326
  IL_01ce:  br         IL_0386
  IL_01d3:  ldarg.0
  IL_01d4:  ldstr      "Position"
  IL_01d9:  call       "bool string.op_Equality(string, string)"
  IL_01de:  brtrue     IL_0366
  IL_01e3:  br         IL_0386
  IL_01e8:  ldarg.0
  IL_01e9:  ldstr      "Data"
  IL_01ee:  call       "bool string.op_Equality(string, string)"
  IL_01f3:  brtrue     IL_0306
  IL_01f8:  br         IL_0386
  IL_01fd:  ldarg.0
  IL_01fe:  ldstr      "Font"
  IL_0203:  call       "bool string.op_Equality(string, string)"
  IL_0208:  brtrue     IL_0336
  IL_020d:  br         IL_0386
  IL_0212:  ldarg.0
  IL_0213:  ldstr      "List"
  IL_0218:  call       "bool string.op_Equality(string, string)"
  IL_021d:  brtrue     IL_0356
  IL_0222:  br         IL_0386
  IL_0227:  ldarg.0
  IL_0228:  ldstr      "Text"
  IL_022d:  call       "bool string.op_Equality(string, string)"
  IL_0232:  brtrue     IL_0376
  IL_0237:  br         IL_0386
  IL_023c:  ldarg.0
  IL_023d:  ldstr      "DDE"
  IL_0242:  call       "bool string.op_Equality(string, string)"
  IL_0247:  brtrue     IL_030e
  IL_024c:  br         IL_0386
  IL_0251:  ldarg.0
  IL_0252:  ldstr      "Key"
  IL_0257:  call       "bool string.op_Equality(string, string)"
  IL_025c:  brtrue     IL_0346
  IL_0261:  br         IL_0386
  IL_0266:  ldarg.0
  IL_0267:  ldstr      "Default"
  IL_026c:  call       "bool string.op_Equality(string, string)"
  IL_0271:  brtrue     IL_0316
  IL_0276:  br         IL_0386
  IL_027b:  ldarg.0
  IL_027c:  ldstr      "Focus"
  IL_0281:  call       "bool string.op_Equality(string, string)"
  IL_0286:  brtrue     IL_032e
  IL_028b:  br         IL_0386
  IL_0290:  ldarg.0
  IL_0291:  ldstr      "Mouse"
  IL_0296:  call       "bool string.op_Equality(string, string)"
  IL_029b:  brtrue     IL_035e
  IL_02a0:  br         IL_0386
  IL_02a5:  ldarg.0
  IL_02a6:  ldstr      "Scale"
  IL_02ab:  call       "bool string.op_Equality(string, string)"
  IL_02b0:  brtrue     IL_036e
  IL_02b5:  br         IL_0386
  IL_02ba:  ldarg.0
  IL_02bb:  ldstr      "WindowStyle"
  IL_02c0:  call       "bool string.op_Equality(string, string)"
  IL_02c5:  brtrue     IL_037e
  IL_02ca:  br         IL_0386
  IL_02cf:  ldstr      "Action"
  IL_02d4:  stloc.0
  IL_02d5:  br         IL_038c
  IL_02da:  ldstr      "Appearance"
  IL_02df:  stloc.0
  IL_02e0:  br         IL_038c
  IL_02e5:  ldstr      "Asynchronous"
  IL_02ea:  stloc.0
  IL_02eb:  br         IL_038c
  IL_02f0:  ldstr      "Behavior"
  IL_02f5:  stloc.0
  IL_02f6:  br         IL_038c
  IL_02fb:  ldstr      "Config"
  IL_0300:  stloc.0
  IL_0301:  br         IL_038c
  IL_0306:  ldstr      "Data"
  IL_030b:  stloc.0
  IL_030c:  br.s       IL_038c
  IL_030e:  ldstr      "DDE"
  IL_0313:  stloc.0
  IL_0314:  br.s       IL_038c
  IL_0316:  ldstr      "Default"
  IL_031b:  stloc.0
  IL_031c:  br.s       IL_038c
  IL_031e:  ldstr      "Design"
  IL_0323:  stloc.0
  IL_0324:  br.s       IL_038c
  IL_0326:  ldstr      "DragDrop"
  IL_032b:  stloc.0
  IL_032c:  br.s       IL_038c
  IL_032e:  ldstr      "Focus"
  IL_0333:  stloc.0
  IL_0334:  br.s       IL_038c
  IL_0336:  ldstr      "Font"
  IL_033b:  stloc.0
  IL_033c:  br.s       IL_038c
  IL_033e:  ldstr      "Format"
  IL_0343:  stloc.0
  IL_0344:  br.s       IL_038c
  IL_0346:  ldstr      "Key"
  IL_034b:  stloc.0
  IL_034c:  br.s       IL_038c
  IL_034e:  ldstr      "Layout"
  IL_0353:  stloc.0
  IL_0354:  br.s       IL_038c
  IL_0356:  ldstr      "List"
  IL_035b:  stloc.0
  IL_035c:  br.s       IL_038c
  IL_035e:  ldstr      "Mouse"
  IL_0363:  stloc.0
  IL_0364:  br.s       IL_038c
  IL_0366:  ldstr      "Position"
  IL_036b:  stloc.0
  IL_036c:  br.s       IL_038c
  IL_036e:  ldstr      "Scale"
  IL_0373:  stloc.0
  IL_0374:  br.s       IL_038c
  IL_0376:  ldstr      "Text"
  IL_037b:  stloc.0
  IL_037c:  br.s       IL_038c
  IL_037e:  ldstr      "WindowStyle"
  IL_0383:  stloc.0
  IL_0384:  br.s       IL_038c
  IL_0386:  ldstr      "default"
  IL_038b:  stloc.0
  IL_038c:  ldloc.0
  IL_038d:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      969 (0x3c9)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x5ef81c51
  IL_000d:  bgt.un     IL_00ac
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4     0x28528e11
  IL_0018:  bgt.un.s   IL_0063
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4     0x19dc307e
  IL_0020:  bgt.un.s   IL_003d
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0xa77a91f
  IL_0028:  beq        IL_0151
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4     0x19dc307e
  IL_0033:  beq        IL_01e4
  IL_0038:  br         IL_03c1
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4     0x1f19a447
  IL_0043:  beq        IL_0190
  IL_0048:  ldloc.1
  IL_0049:  ldc.i4     0x25989e7a
  IL_004e:  beq        IL_020e
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4     0x28528e11
  IL_0059:  beq        IL_02cb
  IL_005e:  br         IL_03c1
  IL_0063:  ldloc.1
  IL_0064:  ldc.i4     0x31018b9f
  IL_0069:  bgt.un.s   IL_0086
  IL_006b:  ldloc.1
  IL_006c:  ldc.i4     0x2b1d9b64
  IL_0071:  beq        IL_02a1
  IL_0076:  ldloc.1
  IL_0077:  ldc.i4     0x31018b9f
  IL_007c:  beq        IL_0166
  IL_0081:  br         IL_03c1
  IL_0086:  ldloc.1
  IL_0087:  ldc.i4     0x3e142d5e
  IL_008c:  beq        IL_02e0
  IL_0091:  ldloc.1
  IL_0092:  ldc.i4     0x3f5279c5
  IL_0097:  beq        IL_01ba
  IL_009c:  ldloc.1
  IL_009d:  ldc.i4     0x5ef81c51
  IL_00a2:  beq        IL_017b
  IL_00a7:  br         IL_03c1
  IL_00ac:  ldloc.1
  IL_00ad:  ldc.i4     0xa77a5eb0
  IL_00b2:  bgt.un.s   IL_00fd
  IL_00b4:  ldloc.1
  IL_00b5:  ldc.i4     0x6a12e0e7
  IL_00ba:  bgt.un.s   IL_00d7
  IL_00bc:  ldloc.1
  IL_00bd:  ldc.i4     0x69116f10
  IL_00c2:  beq        IL_01cf
  IL_00c7:  ldloc.1
  IL_00c8:  ldc.i4     0x6a12e0e7
  IL_00cd:  beq        IL_01a5
  IL_00d2:  br         IL_03c1
  IL_00d7:  ldloc.1
  IL_00d8:  ldc.i4     0x7a836c5e
  IL_00dd:  beq        IL_02f5
  IL_00e2:  ldloc.1
  IL_00e3:  ldc.i4     0x8d2937a1
  IL_00e8:  beq        IL_028c
  IL_00ed:  ldloc.1
  IL_00ee:  ldc.i4     0xa77a5eb0
  IL_00f3:  beq        IL_0238
  IL_00f8:  br         IL_03c1
  IL_00fd:  ldloc.1
  IL_00fe:  ldc.i4     0xe27f342a
  IL_0103:  bgt.un.s   IL_012b
  IL_0105:  ldloc.1
  IL_0106:  ldc.i4     0xbc57b1b3
  IL_010b:  beq        IL_0223
  IL_0110:  ldloc.1
  IL_0111:  ldc.i4     0xcd1ac90c
  IL_0116:  beq        IL_0262
  IL_011b:  ldloc.1
  IL_011c:  ldc.i4     0xe27f342a
  IL_0121:  beq        IL_02b6
  IL_0126:  br         IL_03c1
  IL_012b:  ldloc.1
  IL_012c:  ldc.i4     0xe88d02ef
  IL_0131:  beq        IL_01f9
  IL_0136:  ldloc.1
  IL_0137:  ldc.i4     0xf788421f
  IL_013c:  beq        IL_0277
  IL_0141:  ldloc.1
  IL_0142:  ldc.i4     0xffb0ff72
  IL_0147:  beq        IL_024d
  IL_014c:  br         IL_03c1
  IL_0151:  ldarg.0
  IL_0152:  ldstr      "Action"
  IL_0157:  call       "bool string.op_Equality(string, string)"
  IL_015c:  brtrue     IL_030a
  IL_0161:  br         IL_03c1
  IL_0166:  ldarg.0
  IL_0167:  ldstr      "Appearance"
  IL_016c:  call       "bool string.op_Equality(string, string)"
  IL_0171:  brtrue     IL_0315
  IL_0176:  br         IL_03c1
  IL_017b:  ldarg.0
  IL_017c:  ldstr      "Asynchronous"
  IL_0181:  call       "bool string.op_Equality(string, string)"
  IL_0186:  brtrue     IL_0320
  IL_018b:  br         IL_03c1
  IL_0190:  ldarg.0
  IL_0191:  ldstr      "Behavior"
  IL_0196:  call       "bool string.op_Equality(string, string)"
  IL_019b:  brtrue     IL_032b
  IL_01a0:  br         IL_03c1
  IL_01a5:  ldarg.0
  IL_01a6:  ldstr      "Config"
  IL_01ab:  call       "bool string.op_Equality(string, string)"
  IL_01b0:  brtrue     IL_0336
  IL_01b5:  br         IL_03c1
  IL_01ba:  ldarg.0
  IL_01bb:  ldstr      "Data"
  IL_01c0:  call       "bool string.op_Equality(string, string)"
  IL_01c5:  brtrue     IL_0341
  IL_01ca:  br         IL_03c1
  IL_01cf:  ldarg.0
  IL_01d0:  ldstr      "DDE"
  IL_01d5:  call       "bool string.op_Equality(string, string)"
  IL_01da:  brtrue     IL_0349
  IL_01df:  br         IL_03c1
  IL_01e4:  ldarg.0
  IL_01e5:  ldstr      "Default"
  IL_01ea:  call       "bool string.op_Equality(string, string)"
  IL_01ef:  brtrue     IL_0351
  IL_01f4:  br         IL_03c1
  IL_01f9:  ldarg.0
  IL_01fa:  ldstr      "Design"
  IL_01ff:  call       "bool string.op_Equality(string, string)"
  IL_0204:  brtrue     IL_0359
  IL_0209:  br         IL_03c1
  IL_020e:  ldarg.0
  IL_020f:  ldstr      "DragDrop"
  IL_0214:  call       "bool string.op_Equality(string, string)"
  IL_0219:  brtrue     IL_0361
  IL_021e:  br         IL_03c1
  IL_0223:  ldarg.0
  IL_0224:  ldstr      "Focus"
  IL_0229:  call       "bool string.op_Equality(string, string)"
  IL_022e:  brtrue     IL_0369
  IL_0233:  br         IL_03c1
  IL_0238:  ldarg.0
  IL_0239:  ldstr      "Font"
  IL_023e:  call       "bool string.op_Equality(string, string)"
  IL_0243:  brtrue     IL_0371
  IL_0248:  br         IL_03c1
  IL_024d:  ldarg.0
  IL_024e:  ldstr      "Format"
  IL_0253:  call       "bool string.op_Equality(string, string)"
  IL_0258:  brtrue     IL_0379
  IL_025d:  br         IL_03c1
  IL_0262:  ldarg.0
  IL_0263:  ldstr      "Key"
  IL_0268:  call       "bool string.op_Equality(string, string)"
  IL_026d:  brtrue     IL_0381
  IL_0272:  br         IL_03c1
  IL_0277:  ldarg.0
  IL_0278:  ldstr      "Layout"
  IL_027d:  call       "bool string.op_Equality(string, string)"
  IL_0282:  brtrue     IL_0389
  IL_0287:  br         IL_03c1
  IL_028c:  ldarg.0
  IL_028d:  ldstr      "List"
  IL_0292:  call       "bool string.op_Equality(string, string)"
  IL_0297:  brtrue     IL_0391
  IL_029c:  br         IL_03c1
  IL_02a1:  ldarg.0
  IL_02a2:  ldstr      "Mouse"
  IL_02a7:  call       "bool string.op_Equality(string, string)"
  IL_02ac:  brtrue     IL_0399
  IL_02b1:  br         IL_03c1
  IL_02b6:  ldarg.0
  IL_02b7:  ldstr      "Position"
  IL_02bc:  call       "bool string.op_Equality(string, string)"
  IL_02c1:  brtrue     IL_03a1
  IL_02c6:  br         IL_03c1
  IL_02cb:  ldarg.0
  IL_02cc:  ldstr      "Scale"
  IL_02d1:  call       "bool string.op_Equality(string, string)"
  IL_02d6:  brtrue     IL_03a9
  IL_02db:  br         IL_03c1
  IL_02e0:  ldarg.0
  IL_02e1:  ldstr      "Text"
  IL_02e6:  call       "bool string.op_Equality(string, string)"
  IL_02eb:  brtrue     IL_03b1
  IL_02f0:  br         IL_03c1
  IL_02f5:  ldarg.0
  IL_02f6:  ldstr      "WindowStyle"
  IL_02fb:  call       "bool string.op_Equality(string, string)"
  IL_0300:  brtrue     IL_03b9
  IL_0305:  br         IL_03c1
  IL_030a:  ldstr      "Action"
  IL_030f:  stloc.0
  IL_0310:  br         IL_03c7
  IL_0315:  ldstr      "Appearance"
  IL_031a:  stloc.0
  IL_031b:  br         IL_03c7
  IL_0320:  ldstr      "Asynchronous"
  IL_0325:  stloc.0
  IL_0326:  br         IL_03c7
  IL_032b:  ldstr      "Behavior"
  IL_0330:  stloc.0
  IL_0331:  br         IL_03c7
  IL_0336:  ldstr      "Config"
  IL_033b:  stloc.0
  IL_033c:  br         IL_03c7
  IL_0341:  ldstr      "Data"
  IL_0346:  stloc.0
  IL_0347:  br.s       IL_03c7
  IL_0349:  ldstr      "DDE"
  IL_034e:  stloc.0
  IL_034f:  br.s       IL_03c7
  IL_0351:  ldstr      "Default"
  IL_0356:  stloc.0
  IL_0357:  br.s       IL_03c7
  IL_0359:  ldstr      "Design"
  IL_035e:  stloc.0
  IL_035f:  br.s       IL_03c7
  IL_0361:  ldstr      "DragDrop"
  IL_0366:  stloc.0
  IL_0367:  br.s       IL_03c7
  IL_0369:  ldstr      "Focus"
  IL_036e:  stloc.0
  IL_036f:  br.s       IL_03c7
  IL_0371:  ldstr      "Font"
  IL_0376:  stloc.0
  IL_0377:  br.s       IL_03c7
  IL_0379:  ldstr      "Format"
  IL_037e:  stloc.0
  IL_037f:  br.s       IL_03c7
  IL_0381:  ldstr      "Key"
  IL_0386:  stloc.0
  IL_0387:  br.s       IL_03c7
  IL_0389:  ldstr      "Layout"
  IL_038e:  stloc.0
  IL_038f:  br.s       IL_03c7
  IL_0391:  ldstr      "List"
  IL_0396:  stloc.0
  IL_0397:  br.s       IL_03c7
  IL_0399:  ldstr      "Mouse"
  IL_039e:  stloc.0
  IL_039f:  br.s       IL_03c7
  IL_03a1:  ldstr      "Position"
  IL_03a6:  stloc.0
  IL_03a7:  br.s       IL_03c7
  IL_03a9:  ldstr      "Scale"
  IL_03ae:  stloc.0
  IL_03af:  br.s       IL_03c7
  IL_03b1:  ldstr      "Text"
  IL_03b6:  stloc.0
  IL_03b7:  br.s       IL_03c7
  IL_03b9:  ldstr      "WindowStyle"
  IL_03be:  stloc.0
  IL_03bf:  br.s       IL_03c7
  IL_03c1:  ldstr      "default"
  IL_03c6:  stloc.0
  IL_03c7:  ldloc.0
  IL_03c8:  ret
}
""");
    }

    [Fact, WorkItem(56374, "https://github.com/dotnet/roslyn/issues/56374")]
    public void ParseGraphicsUnits()
    {
        // Based on https://github.com/dotnet/runtime/blob/a30de6d40f69ef612b514344a5ec83fffd10b957/src/libraries/System.Drawing.Common/src/System/Drawing/FontConverter.cs#L255-L265
        // Buckets: 1, 1, 1, 1, 1, 1, 1
        var source = """
assert("display");
assert("doc");
assert("pt");
assert("in");
assert("mm");
assert("px");
assert("world");

assert(null, "default");
assert("", "default");
assert("other", "default");
System.Console.Write("RAN");

void assert(string input, string expected = null)
{
    if (C.M(input) != (expected ?? input))
    {
        throw new System.Exception($"{input} produced {C.M(input)}");
    }
}

public class C
{
    public static string M(string value)
    {
        return value switch
        {
            "display" => "display",
            "doc" => "doc",
            "pt" => "pt",
            "in" => "in",
            "mm" => "mm",
            "px" => "px",
            "world" => "world",
            _ => "default"
        };
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      268 (0x10c)
  .maxstack  2
  .locals init (string V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0104
  IL_0006:  ldarg.0
  IL_0007:  call       "int string.Length.get"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.2
  IL_000f:  sub
  IL_0010:  switch    (
        IL_0032,
        IL_006f,
        IL_0104,
        IL_00bd,
        IL_0104,
        IL_005d)
  IL_002d:  br         IL_0104
  IL_0032:  ldarg.0
  IL_0033:  ldc.i4.1
  IL_0034:  call       "char string.this[int].get"
  IL_0039:  stloc.2
  IL_003a:  ldloc.2
  IL_003b:  ldc.i4.s   110
  IL_003d:  bgt.un.s   IL_004e
  IL_003f:  ldloc.2
  IL_0040:  ldc.i4.s   109
  IL_0042:  beq.s      IL_009f
  IL_0044:  ldloc.2
  IL_0045:  ldc.i4.s   110
  IL_0047:  beq.s      IL_0090
  IL_0049:  br         IL_0104
  IL_004e:  ldloc.2
  IL_004f:  ldc.i4.s   116
  IL_0051:  beq.s      IL_0081
  IL_0053:  ldloc.2
  IL_0054:  ldc.i4.s   120
  IL_0056:  beq.s      IL_00ae
  IL_0058:  br         IL_0104
  IL_005d:  ldarg.0
  IL_005e:  ldstr      "display"
  IL_0063:  call       "bool string.op_Equality(string, string)"
  IL_0068:  brtrue.s   IL_00cc
  IL_006a:  br         IL_0104
  IL_006f:  ldarg.0
  IL_0070:  ldstr      "doc"
  IL_0075:  call       "bool string.op_Equality(string, string)"
  IL_007a:  brtrue.s   IL_00d4
  IL_007c:  br         IL_0104
  IL_0081:  ldarg.0
  IL_0082:  ldstr      "pt"
  IL_0087:  call       "bool string.op_Equality(string, string)"
  IL_008c:  brtrue.s   IL_00dc
  IL_008e:  br.s       IL_0104
  IL_0090:  ldarg.0
  IL_0091:  ldstr      "in"
  IL_0096:  call       "bool string.op_Equality(string, string)"
  IL_009b:  brtrue.s   IL_00e4
  IL_009d:  br.s       IL_0104
  IL_009f:  ldarg.0
  IL_00a0:  ldstr      "mm"
  IL_00a5:  call       "bool string.op_Equality(string, string)"
  IL_00aa:  brtrue.s   IL_00ec
  IL_00ac:  br.s       IL_0104
  IL_00ae:  ldarg.0
  IL_00af:  ldstr      "px"
  IL_00b4:  call       "bool string.op_Equality(string, string)"
  IL_00b9:  brtrue.s   IL_00f4
  IL_00bb:  br.s       IL_0104
  IL_00bd:  ldarg.0
  IL_00be:  ldstr      "world"
  IL_00c3:  call       "bool string.op_Equality(string, string)"
  IL_00c8:  brtrue.s   IL_00fc
  IL_00ca:  br.s       IL_0104
  IL_00cc:  ldstr      "display"
  IL_00d1:  stloc.0
  IL_00d2:  br.s       IL_010a
  IL_00d4:  ldstr      "doc"
  IL_00d9:  stloc.0
  IL_00da:  br.s       IL_010a
  IL_00dc:  ldstr      "pt"
  IL_00e1:  stloc.0
  IL_00e2:  br.s       IL_010a
  IL_00e4:  ldstr      "in"
  IL_00e9:  stloc.0
  IL_00ea:  br.s       IL_010a
  IL_00ec:  ldstr      "mm"
  IL_00f1:  stloc.0
  IL_00f2:  br.s       IL_010a
  IL_00f4:  ldstr      "px"
  IL_00f9:  stloc.0
  IL_00fa:  br.s       IL_010a
  IL_00fc:  ldstr      "world"
  IL_0101:  stloc.0
  IL_0102:  br.s       IL_010a
  IL_0104:  ldstr      "default"
  IL_0109:  stloc.0
  IL_010a:  ldloc.0
  IL_010b:  ret
}
""");

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
        comp.VerifyDiagnostics();
        verifier = CompileAndVerify(comp, expectedOutput: "RAN");
        verifier.VerifyIL("C.M", """
{
  // Code size      272 (0x110)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "uint <PrivateImplementationDetails>.ComputeStringHash(string)"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x4569f715
  IL_000d:  bgt.un.s   IL_002f
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4     0x37a3e893
  IL_0015:  beq        IL_00c1
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4     0x41387a9e
  IL_0020:  beq.s      IL_0094
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x4569f715
  IL_0028:  beq.s      IL_0061
  IL_002a:  br         IL_0108
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4     0x5d4e6d01
  IL_0035:  bgt.un.s   IL_004c
  IL_0037:  ldloc.1
  IL_0038:  ldc.i4     0x594e66b5
  IL_003d:  beq.s      IL_00b2
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4     0x5d4e6d01
  IL_0045:  beq.s      IL_0085
  IL_0047:  br         IL_0108
  IL_004c:  ldloc.1
  IL_004d:  ldc.i4     0x602e1e0f
  IL_0052:  beq.s      IL_00a3
  IL_0054:  ldloc.1
  IL_0055:  ldc.i4     0xea68c355
  IL_005a:  beq.s      IL_0073
  IL_005c:  br         IL_0108
  IL_0061:  ldarg.0
  IL_0062:  ldstr      "display"
  IL_0067:  call       "bool string.op_Equality(string, string)"
  IL_006c:  brtrue.s   IL_00d0
  IL_006e:  br         IL_0108
  IL_0073:  ldarg.0
  IL_0074:  ldstr      "doc"
  IL_0079:  call       "bool string.op_Equality(string, string)"
  IL_007e:  brtrue.s   IL_00d8
  IL_0080:  br         IL_0108
  IL_0085:  ldarg.0
  IL_0086:  ldstr      "pt"
  IL_008b:  call       "bool string.op_Equality(string, string)"
  IL_0090:  brtrue.s   IL_00e0
  IL_0092:  br.s       IL_0108
  IL_0094:  ldarg.0
  IL_0095:  ldstr      "in"
  IL_009a:  call       "bool string.op_Equality(string, string)"
  IL_009f:  brtrue.s   IL_00e8
  IL_00a1:  br.s       IL_0108
  IL_00a3:  ldarg.0
  IL_00a4:  ldstr      "mm"
  IL_00a9:  call       "bool string.op_Equality(string, string)"
  IL_00ae:  brtrue.s   IL_00f0
  IL_00b0:  br.s       IL_0108
  IL_00b2:  ldarg.0
  IL_00b3:  ldstr      "px"
  IL_00b8:  call       "bool string.op_Equality(string, string)"
  IL_00bd:  brtrue.s   IL_00f8
  IL_00bf:  br.s       IL_0108
  IL_00c1:  ldarg.0
  IL_00c2:  ldstr      "world"
  IL_00c7:  call       "bool string.op_Equality(string, string)"
  IL_00cc:  brtrue.s   IL_0100
  IL_00ce:  br.s       IL_0108
  IL_00d0:  ldstr      "display"
  IL_00d5:  stloc.0
  IL_00d6:  br.s       IL_010e
  IL_00d8:  ldstr      "doc"
  IL_00dd:  stloc.0
  IL_00de:  br.s       IL_010e
  IL_00e0:  ldstr      "pt"
  IL_00e5:  stloc.0
  IL_00e6:  br.s       IL_010e
  IL_00e8:  ldstr      "in"
  IL_00ed:  stloc.0
  IL_00ee:  br.s       IL_010e
  IL_00f0:  ldstr      "mm"
  IL_00f5:  stloc.0
  IL_00f6:  br.s       IL_010e
  IL_00f8:  ldstr      "px"
  IL_00fd:  stloc.0
  IL_00fe:  br.s       IL_010e
  IL_0100:  ldstr      "world"
  IL_0105:  stloc.0
  IL_0106:  br.s       IL_010e
  IL_0108:  ldstr      "default"
  IL_010d:  stloc.0
  IL_010e:  ldloc.0
  IL_010f:  ret
}
""");
    }
}
