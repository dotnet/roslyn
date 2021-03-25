// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests5 : PatternMatchingTestBase
    {
        [Fact]
        public void ExtendedPropertyPatterns_01()
        {
            var program = @"
using System;
class C
{
    public C Prop1 { get; set; }
    public C Prop2 { get; set; }
    public C Prop3 { get; set; }
    
    static bool Test1(C o) => o is { Prop1.Prop2.Prop3: null };
    static bool Test2(S o) => o is { Prop1.Prop2.Prop3: null };

    public static void Main(string[] args)
    {        
        Console.Write(Test1(new() { Prop1 = new() { Prop2 = new() { Prop3 = null }}}));
        Console.Write(Test2(new() { Prop1 = new() { Prop2 = new() { Prop3 = null }}}));
    }
}
struct S { public A? Prop1;    }
struct A { public B? Prop2;    }
struct B { public int? Prop3;  }

";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithPatternCombinators, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation, expectedOutput: "TrueTrue");
            verifier.VerifyIL("C.Test1", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (C V_0,
                C V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0021
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""C C.Prop1.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0021
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""C C.Prop2.get""
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_0021
  IL_0017:  ldloc.1
  IL_0018:  callvirt   ""C C.Prop3.get""
  IL_001d:  ldnull
  IL_001e:  ceq
  IL_0020:  ret
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}");
            verifier.VerifyIL("C.Test2", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (A? V_0,
                B? V_1,
                int? V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""A? S.Prop1""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool A?.HasValue.get""
  IL_000e:  brfalse.s  IL_003e
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""A A?.GetValueOrDefault()""
  IL_0017:  ldfld      ""B? A.Prop2""
  IL_001c:  stloc.1
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       ""bool B?.HasValue.get""
  IL_0024:  brfalse.s  IL_003e
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       ""B B?.GetValueOrDefault()""
  IL_002d:  ldfld      ""int? B.Prop3""
  IL_0032:  stloc.2
  IL_0033:  ldloca.s   V_2
  IL_0035:  call       ""bool int?.HasValue.get""
  IL_003a:  ldc.i4.0
  IL_003b:  ceq
  IL_003d:  ret
  IL_003e:  ldc.i4.0
  IL_003f:  ret
}");
        }

        [Fact]
        public void ExtendedPropertyPatterns_02()
        {
            var program = @"
class C
{
    public C Prop1 { get; set; }
    public C Prop2 { get; set; }

    public static void Main(string[] args)
    {        
        _ = new C() is { Prop1: null } and { Prop1.Prop2: null };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithPatternCombinators, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void ExtendedPropertyPatterns_03()
        {
            var program = @"
using System;
class C
{
    public C Prop1 { get; set; }
    public C Prop2 { get; set; }

    public static void Main(string[] args)
    {        
        var result = new C() switch
        {
            {Prop1: null} => 1,
            {Prop1.Prop2: null} => 2,
            _ => -1,
        };
        Console.Write(result);
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithPatternCombinators, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("C.Main", @"
{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (int V_0,
                C V_1,
                C V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  brfalse.s  IL_0025
  IL_0009:  ldloc.1
  IL_000a:  callvirt   ""C C.Prop1.get""
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  brfalse.s  IL_001d
  IL_0013:  ldloc.2
  IL_0014:  callvirt   ""C C.Prop2.get""
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  br.s       IL_0025
  IL_001d:  ldc.i4.1
  IL_001e:  stloc.0
  IL_001f:  br.s       IL_0027
  IL_0021:  ldc.i4.2
  IL_0022:  stloc.0
  IL_0023:  br.s       IL_0027
  IL_0025:  ldc.i4.m1
  IL_0026:  stloc.0
  IL_0027:  ldloc.0
  IL_0028:  call       ""void System.Console.Write(int)""
  IL_002d:  ret
}
");
        }
    }
}
