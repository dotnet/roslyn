// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenMethodGroupConversionTests : CSharpTestBase
    {
        #region Not caching delegate creations

        [Fact]
        public void NotCachingDelegateCreations_01()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        Invoke(new D(Target), new D(Target));
    }

    static void Target() { }
    static void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "PASS");
        }

        [Fact]
        public void NotCachingDelegateCreations_02()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(c.Target), new D(c.Target));
    }

    void Target() { }
    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "PASS");
        }

        #endregion

        #region Not caching explicit conversions

        [Fact]
        public void NotCachingExplicitConversions_01()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        Invoke((D)Target, (D)Target);
    }

    static void Target() { }
    static void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "PASS");
        }

        [Fact]
        public void NotCachingExplicitConversions_02()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke((D)c.Target, (D)c.Target);
    }

    void Target() { }
    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "PASS");
        }

        #endregion

        #region Changed behavior(and IL of course) after cache introduced

        [Fact]
        public void LockDelegate()
        {
            var text =
@"
delegate void D(int p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1 = PM)
        {
        }
    }
    static partial void PM(int p2);
    static partial void PM(int p2)
    {
    }
}
";
            CompileAndVerify(text);
/*            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (D V_0,
                bool V_1)
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Test.PM(int)""
  IL_0007:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  .try
  {
    IL_000f:  ldloc.0
    IL_0010:  ldloca.s   V_1
    IL_0012:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0017:  leave.s    IL_0023
  }
  finally
  {
    IL_0019:  ldloc.1
    IL_001a:  brfalse.s  IL_0022
    IL_001c:  ldloc.0
    IL_001d:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0022:  endfinally
  }
  IL_0023:  ret
}");*/
        }

        [Fact]
        public void TestConditionalOperatorMethodGroup()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Func<int> f = null;
        System.Console.WriteLine(f);
        System.Func<int> g1 = b ? f : M;
        System.Console.WriteLine(g1);
        System.Func<int> g2 = b ? M : f;
        System.Console.WriteLine(g2);
    }

    static int M()
    {
        return 0;
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
/*            comp.VerifyIL("C.Main", @"
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (System.Func<int> V_0) //f
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void System.Console.WriteLine(object)""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_001a
  IL_000c:  ldnull
  IL_000d:  ldftn      ""int C.M()""
  IL_0013:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0018:  br.s       IL_001b
  IL_001a:  ldloc.0
  IL_001b:  call       ""void System.Console.WriteLine(object)""
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldloc.0
  IL_0023:  br.s       IL_0031
  IL_0025:  ldnull
  IL_0026:  ldftn      ""int C.M()""
  IL_002c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0031:  call       ""void System.Console.WriteLine(object)""
  IL_0036:  ret
}
");*/
        }

        #endregion

    }
}
