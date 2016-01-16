// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class ForLoopsTests : EmitMetadataTestBase
    {
        [Fact]
        public void ForLoop()
        {
            string source =
@"class C
{
    static void Main()
    {
        int x = 3;
        for (int i = 0; i < 3; i = i + 1)
        {
            x = x * 3;
        }
        System.Console.Write(""{0}"", x);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "81");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (int V_0, //x
           int V_1) //i
  IL_0000:  ldc.i4.3  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.0  
  IL_0003:  stloc.1   
  IL_0004:  br.s       IL_000e
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.3  
  IL_0008:  mul       
  IL_0009:  stloc.0   
  IL_000a:  ldloc.1   
  IL_000b:  ldc.i4.1  
  IL_000c:  add       
  IL_000d:  stloc.1   
  IL_000e:  ldloc.1   
  IL_000f:  ldc.i4.3  
  IL_0010:  blt.s      IL_0006
  IL_0012:  ldstr      ""{0}""
  IL_0017:  ldloc.0   
  IL_0018:  box        ""int""
  IL_001d:  call       ""void System.Console.Write(string, object)""
  IL_0022:  ret       
}
");
        }

        [Fact]
        public void ForLoopExecuteOnce()
        {
            string source =
@"class C
{
    static void Main()
    {
        int i = 0;
        int j;
        bool loop = true;
        for (j = 0; loop; j = j + 1)
        {
            loop = false;
            i = i + 1;
        }
        System.Console.Write(""{0}, {1}"", i, j);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "1, 1");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (int V_0, //i
           int V_1, //j
           bool V_2) //loop
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.1  
  IL_0003:  stloc.2   
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_0012
  IL_0008:  ldc.i4.0  
  IL_0009:  stloc.2   
  IL_000a:  ldloc.0   
  IL_000b:  ldc.i4.1  
  IL_000c:  add       
  IL_000d:  stloc.0   
  IL_000e:  ldloc.1   
  IL_000f:  ldc.i4.1  
  IL_0010:  add       
  IL_0011:  stloc.1   
  IL_0012:  ldloc.2   
  IL_0013:  brtrue.s   IL_0008
  IL_0015:  ldstr      ""{0}, {1}""
  IL_001a:  ldloc.0   
  IL_001b:  box        ""int""
  IL_0020:  ldloc.1   
  IL_0021:  box        ""int""
  IL_0026:  call       ""void System.Console.Write(string, object, object)""
  IL_002b:  ret       
}
");
        }

        [Fact]
        public void ForLoopTrue()
        {
            string source =
@"class C
{
    static void Main()
    {
        int i = 0;
        int j;
        for (j = 0; true; j = j + 1)
        {
            i = i + 1;
            break;
        }
        System.Console.Write(""{0}, {1}"", i, j);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "1, 0");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.0  
  IL_0003:  stloc.1   
  IL_0004:  ldloc.0   
  IL_0005:  ldc.i4.1  
  IL_0006:  add       
  IL_0007:  stloc.0   
  IL_0008:  ldstr      ""{0}, {1}""
  IL_000d:  ldloc.0   
  IL_000e:  box        ""int""
  IL_0013:  ldloc.1   
  IL_0014:  box        ""int""
  IL_0019:  call       ""void System.Console.Write(string, object, object)""
  IL_001e:  ret       
}
");
        }

        [Fact]
        public void ForLoopFalse()
        {
            string source =
@"class C
{
    static void Main()
    {
        int i = 0;
        int j;
        for (j = 0; false; j = j + 1)
        {
            i = i + 1;
        }
        System.Console.Write(""{0}, {1}"", i, j);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "0, 0");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.0  
  IL_0003:  stloc.1   
  IL_0004:  ldstr      ""{0}, {1}""
  IL_0009:  ldloc.0   
  IL_000a:  box        ""int""
  IL_000f:  ldloc.1   
  IL_0010:  box        ""int""
  IL_0015:  call       ""void System.Console.Write(string, object, object)""
  IL_001a:  ret       
}
");
        }

        [Fact]
        public void ForLoopBreakContinue()
        {
            string source =
@"class C
{
    static void Main()
    {
        int i;
        int j;
        for (i = 0, j = 0; i < 5; i = i + 1)
        {
            if (i > 2) continue;
            j = j + 1;
        }
        System.Console.Write(""{0}, {1}, "", i, j);
        for (i = 0, j = 0; i < 5; i = i + 1)
        {
            if (i > 3) break;
            j = j + 1;
        }
        System.Console.Write(""{0}, {1}"", i, j);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "5, 3, 4, 4");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       89 (0x59)
  .maxstack  3
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.0  
  IL_0003:  stloc.1   
  IL_0004:  br.s       IL_0012
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.2  
  IL_0008:  bgt.s      IL_000e
  IL_000a:  ldloc.1   
  IL_000b:  ldc.i4.1  
  IL_000c:  add       
  IL_000d:  stloc.1   
  IL_000e:  ldloc.0   
  IL_000f:  ldc.i4.1  
  IL_0010:  add       
  IL_0011:  stloc.0   
  IL_0012:  ldloc.0   
  IL_0013:  ldc.i4.5  
  IL_0014:  blt.s      IL_0006
  IL_0016:  ldstr      ""{0}, {1}, ""
  IL_001b:  ldloc.0   
  IL_001c:  box        ""int""
  IL_0021:  ldloc.1   
  IL_0022:  box        ""int""
  IL_0027:  call       ""void System.Console.Write(string, object, object)""
  IL_002c:  ldc.i4.0  
  IL_002d:  stloc.0   
  IL_002e:  ldc.i4.0  
  IL_002f:  stloc.1   
  IL_0030:  br.s       IL_003e
  IL_0032:  ldloc.0   
  IL_0033:  ldc.i4.3  
  IL_0034:  bgt.s      IL_0042
  IL_0036:  ldloc.1   
  IL_0037:  ldc.i4.1  
  IL_0038:  add       
  IL_0039:  stloc.1   
  IL_003a:  ldloc.0   
  IL_003b:  ldc.i4.1  
  IL_003c:  add       
  IL_003d:  stloc.0   
  IL_003e:  ldloc.0   
  IL_003f:  ldc.i4.5  
  IL_0040:  blt.s      IL_0032
  IL_0042:  ldstr      ""{0}, {1}""
  IL_0047:  ldloc.0   
  IL_0048:  box        ""int""
  IL_004d:  ldloc.1   
  IL_004e:  box        ""int""
  IL_0053:  call       ""void System.Console.Write(string, object, object)""
  IL_0058:  ret       
}
");
        }

        /// <summary>
        /// No statements in for loop parts.
        /// </summary>
        [Fact]
        public void ForLoopNoStatements()
        {
            string source =
@"class C
{
    static void Main()
    {
        int i = 0;
        for (; ; )
        {
            if (i > 4) break;
            i = i + 2;
        }
        System.Console.Write(""{0}"", i);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "6");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldloc.0   
  IL_0003:  ldc.i4.4  
  IL_0004:  bgt.s      IL_000c
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.2  
  IL_0008:  add       
  IL_0009:  stloc.0   
  IL_000a:  br.s       IL_0002
  IL_000c:  ldstr      ""{0}""
  IL_0011:  ldloc.0   
  IL_0012:  box        ""int""
  IL_0017:  call       ""void System.Console.Write(string, object)""
  IL_001c:  ret       
}
");
        }

        /// <summary>
        /// Statement expression list in initializer.
        /// </summary>
        [Fact]
        public void ForLoopStatementExpressionListInitializer()
        {
            string source =
@"class C
{
    static void Main()
    {
        int i = 0;
        int j = 0;
        for (i = i + 1, i = i + 1; j < 2; i = i + 2, j = j + 1)
        {
        }
        System.Console.Write(""{0}"", i);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "6");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.0  
  IL_0003:  stloc.1   
  IL_0004:  ldloc.0   
  IL_0005:  ldc.i4.1  
  IL_0006:  add       
  IL_0007:  stloc.0   
  IL_0008:  ldloc.0   
  IL_0009:  ldc.i4.1  
  IL_000a:  add       
  IL_000b:  stloc.0   
  IL_000c:  br.s       IL_0016
  IL_000e:  ldloc.0   
  IL_000f:  ldc.i4.2  
  IL_0010:  add       
  IL_0011:  stloc.0   
  IL_0012:  ldloc.1   
  IL_0013:  ldc.i4.1  
  IL_0014:  add       
  IL_0015:  stloc.1   
  IL_0016:  ldloc.1   
  IL_0017:  ldc.i4.2  
  IL_0018:  blt.s      IL_000e
  IL_001a:  ldstr      ""{0}""
  IL_001f:  ldloc.0   
  IL_0020:  box        ""int""
  IL_0025:  call       ""void System.Console.Write(string, object)""
  IL_002a:  ret       
}
");
        }

        // The initializer list is a comma separated list of expressions
        [Fact]
        public void InitializerList()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        int i = 1;
        for (; i < 10; i = i + 1)
        {
        }
    }
}
";
            string expectedIL = @"{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.1  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0008
  IL_0004:  ldloc.0   
  IL_0005:  ldc.i4.1  
  IL_0006:  add       
  IL_0007:  stloc.0   
  IL_0008:  ldloc.0   
  IL_0009:  ldc.i4.s   10
  IL_000b:  blt.s      IL_0004
  IL_000d:  ret       
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // The value of  Iterator expression could be decreasing
        [Fact]
        public void DecreasingForIterator()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int k = 200 ; k > 100; k = k - 1)
        {
        }
    }
}
";
            string expectedIL = @"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (int V_0) //k
  IL_0000:  ldc.i4     0xc8
  IL_0005:  stloc.0   
  IL_0006:  br.s       IL_000c
  IL_0008:  ldloc.0   
  IL_0009:  ldc.i4.1  
  IL_000a:  sub       
  IL_000b:  stloc.0   
  IL_000c:  ldloc.0   
  IL_000d:  ldc.i4.s   100
  IL_000f:  bgt.s      IL_0008
  IL_0011:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Method calls work fine in all for loop areas
        [Fact]
        public void MethodCall()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (Initializer(); Conditional(); Iterator())
        {
        }
    }
    public static int Initializer() { return 1; }
    public static bool Conditional()
    { return true; }
    public static int Iterator() { return 1; }
}
";
            string expectedIL = @"{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  call       ""int C.Initializer()""
  IL_0005:  pop       
  IL_0006:  br.s       IL_000e
  IL_0008:  call       ""int C.Iterator()""
  IL_000d:  pop       
  IL_000e:  call       ""bool C.Conditional()""
  IL_0013:  brtrue.s   IL_0008
  IL_0015:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        [Fact]
        public void EmptyForBody()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 10; i < 100; i = i + 1) ;
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.s   10
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0009
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.s   100
  IL_000c:  blt.s      IL_0005
  IL_000e:  ret       
}
");
        }

        // Nested for Loops 
        [Fact]
        public void NestedForLoop()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 100; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0015
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_000c
  IL_0008:  ldloc.1   
  IL_0009:  ldc.i4.1  
  IL_000a:  add       
  IL_000b:  stloc.1   
  IL_000c:  ldloc.1   
  IL_000d:  ldc.i4.s   10
  IL_000f:  blt.s      IL_0008
  IL_0011:  ldloc.0   
  IL_0012:  ldc.i4.1  
  IL_0013:  add       
  IL_0014:  stloc.0   
  IL_0015:  ldloc.0   
  IL_0016:  ldc.i4.s   100
  IL_0018:  blt.s      IL_0004
  IL_001a:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Change outer variable in inner for Loops
        [Fact]
        public void ChangeOuterInInnerForLoop()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                i = 1;
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0017
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_000e
  IL_0008:  ldc.i4.1  
  IL_0009:  stloc.0   
  IL_000a:  ldloc.1   
  IL_000b:  ldc.i4.1  
  IL_000c:  add       
  IL_000d:  stloc.1   
  IL_000e:  ldloc.1   
  IL_000f:  ldc.i4.s   10
  IL_0011:  blt.s      IL_0008
  IL_0013:  ldloc.0   
  IL_0014:  ldc.i4.1  
  IL_0015:  add       
  IL_0016:  stloc.0   
  IL_0017:  ldloc.0   
  IL_0018:  ldc.i4.s   10
  IL_001a:  blt.s      IL_0004
  IL_001c:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Inner for loop referencing the outer for loop iteration variable
        [Fact]
        public void InnerRefOuterIteration()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = i + 1; i < j; j = j - 1)
            {
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0016
  IL_0004:  ldloc.0   
  IL_0005:  ldc.i4.1  
  IL_0006:  add       
  IL_0007:  stloc.1   
  IL_0008:  br.s       IL_000e
  IL_000a:  ldloc.1   
  IL_000b:  ldc.i4.1  
  IL_000c:  sub       
  IL_000d:  stloc.1   
  IL_000e:  ldloc.0   
  IL_000f:  ldloc.1   
  IL_0010:  blt.s      IL_000a
  IL_0012:  ldloc.0   
  IL_0013:  ldc.i4.1  
  IL_0014:  add       
  IL_0015:  stloc.0   
  IL_0016:  ldloc.0   
  IL_0017:  ldc.i4.5  
  IL_0018:  blt.s      IL_0004
  IL_001a:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Breaking from nested Loops
        [Fact, WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        public void BreakFromNestedLoop()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                if (j == 5)
                    break;
            }
        }
    }
}
";
            var c = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            c.VerifyIL("C.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int V_0, //i
  int V_1) //j
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0019
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0010
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.5
  IL_000a:  beq.s      IL_0015
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.s   10
  IL_0013:  blt.s      IL_0008
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.1
  IL_0017:  add
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  ldc.i4.5
  IL_001b:  blt.s      IL_0004
  IL_001d:  ret
}
");
        }

        [WorkItem(539555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539555")]
        // Continuing for nested Loops
        [Fact]
        public void ContinueForNestedLoop()
        {
            var text =
@"
    class C
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 5; i = i + 1)
            {
                for (int j = 1; j < 10; j = j + 1)
                {
                    if ((j % 2) != 0)
                        continue;
                    i = i + 1;
                    System.Console.Write(i);
                }
            }
        }
    }
";
            string expectedIL = @"{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0024
  IL_0004:  ldc.i4.1  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_001b
  IL_0008:  ldloc.1   
  IL_0009:  ldc.i4.2  
  IL_000a:  rem       
  IL_000b:  brtrue.s   IL_0017
  IL_000d:  ldloc.0   
  IL_000e:  ldc.i4.1  
  IL_000f:  add       
  IL_0010:  stloc.0   
  IL_0011:  ldloc.0   
  IL_0012:  call       ""void System.Console.Write(int)""
  IL_0017:  ldloc.1   
  IL_0018:  ldc.i4.1  
  IL_0019:  add       
  IL_001a:  stloc.1   
  IL_001b:  ldloc.1   
  IL_001c:  ldc.i4.s   10
  IL_001e:  blt.s      IL_0008
  IL_0020:  ldloc.0   
  IL_0021:  ldc.i4.1  
  IL_0022:  add       
  IL_0023:  stloc.0   
  IL_0024:  ldloc.0   
  IL_0025:  ldc.i4.5  
  IL_0026:  blt.s      IL_0004
  IL_0028:  ret       
}
";
            CompileAndVerify(text, expectedOutput: "1234").
                 VerifyIL("C.Main", expectedIL);
        }

        // Goto in for Loops
        [Fact]
        public void GotoForNestedLoop_1()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                goto stop;
            stop:
                j = j + 1;
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int V_0, //i
  int V_1) //j
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0019
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0010
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.1
  IL_000a:  add
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.s   10
  IL_0013:  blt.s      IL_0008
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.1
  IL_0017:  add
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  ldc.i4.5
  IL_001b:  blt.s      IL_0004
  IL_001d:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Goto in for Loops
        [Fact, WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        public void GotoForNestedLoop_2()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                goto stop;
            }
            stop:
                i = i + 1;
        }
    }
}
";
            var c = CompileAndVerify(source, options: TestOptions.ReleaseDll);
            c.VerifyIL("C.Main", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (int V_0, //i
  int V_1) //j
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0013
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldc.i4.s   10
  IL_0009:  pop
  IL_000a:  pop
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  add
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.5
  IL_0015:  blt.s      IL_0004
  IL_0017:  ret
}
");
        }

        // Goto in for Loops
        [Fact, WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        public void GotoForNestedLoop_3()
        {
            var source =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                goto outerLoop;
            }
        outerLoop:
            i = i + 2;
        }
    }
}
";
            var c = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            c.VerifyIL("C.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (int V_0, //i
  int V_1) //j
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0013
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldc.i4.s   10
  IL_0009:  pop
  IL_000a:  pop
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.2
  IL_000d:  add
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.s   10
  IL_0016:  blt.s      IL_0004
  IL_0018:  ret
}
");
        }

        // Throw exception in for
        [Fact]
        public void ThrowException()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10;)
            {
                throw new System.Exception();
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0017
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_000e
  IL_0008:  newobj     ""System.Exception..ctor()""
  IL_000d:  throw     
  IL_000e:  ldloc.1   
  IL_000f:  ldc.i4.s   10
  IL_0011:  blt.s      IL_0008
  IL_0013:  ldloc.0   
  IL_0014:  ldc.i4.1  
  IL_0015:  add       
  IL_0016:  stloc.0   
  IL_0017:  ldloc.0   
  IL_0018:  ldc.i4.s   10
  IL_001a:  blt.s      IL_0004
  IL_001c:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Return in for
        [Fact]
        public void ReturnInFor()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; )
        {
            for (int j = 0; j < 5; )
            {
                return;
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_000d
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_0009
  IL_0008:  ret       
  IL_0009:  ldloc.1   
  IL_000a:  ldc.i4.5  
  IL_000b:  blt.s      IL_0008
  IL_000d:  ldloc.0   
  IL_000e:  ldc.i4.s   10
  IL_0010:  blt.s      IL_0004
  IL_0012:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // The value of initializer expressions
        [Fact]
        public void ValueOfInit()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0, j = 1; i < 5; i = i + 1)
        {
            j = 2;
        }
    }
}
";
            string expectedIL = @"{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0008
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.5
  IL_000a:  blt.s      IL_0004
  IL_000c:  ret
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // The value of  condition expression 
        [Fact]
        public void ValueOfCondition()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        int c = 0, x = 0;
        for (int i = 0; i < 50 - x; i = i + 1)
        {
            x = x + 1;
            c = c + 1;
        }
    }
}
";
            string expectedIL = @"{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (int V_0, //c
           int V_1, //x
           int V_2) //i
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.0  
  IL_0003:  stloc.1   
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.2   
  IL_0006:  br.s       IL_0014
  IL_0008:  ldloc.1   
  IL_0009:  ldc.i4.1  
  IL_000a:  add       
  IL_000b:  stloc.1   
  IL_000c:  ldloc.0   
  IL_000d:  ldc.i4.1  
  IL_000e:  add       
  IL_000f:  stloc.0   
  IL_0010:  ldloc.2   
  IL_0011:  ldc.i4.1  
  IL_0012:  add       
  IL_0013:  stloc.2   
  IL_0014:  ldloc.2   
  IL_0015:  ldc.i4.s   50
  IL_0017:  ldloc.1   
  IL_0018:  sub       
  IL_0019:  blt.s      IL_0008
  IL_001b:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // The evaluation of condition expression take place before execution of loop
        [Fact]
        public void ValueOfConditionBeforeLoop()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 0; i = i + 1)
        {
            i = i + 1;
        }
    }
}
";
            string expectedIL = @"{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_000c
  IL_0004:  ldloc.0   
  IL_0005:  ldc.i4.1  
  IL_0006:  add       
  IL_0007:  stloc.0   
  IL_0008:  ldloc.0   
  IL_0009:  ldc.i4.1  
  IL_000a:  add       
  IL_000b:  stloc.0   
  IL_000c:  ldloc.0   
  IL_000d:  ldc.i4.0  
  IL_000e:  blt.s      IL_0004
  IL_0010:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Unreachable code
        [Fact]
        public void CS0162WRN_UnreachableCode_1()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        for (; false; )
        {
            System.Console.WriteLine(""hello"");        //unreachable
        }
    }
}
";
            string expectedIL = @"{
  // Code size        1 (0x1)
  .maxstack  1
  IL_0000:  ret       
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "System"));
        }

        [Fact, WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        public void CS0162WRN_UnreachableCode_2()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        for (; ; ) { }
        System.Console.WriteLine(""hello"");           //unreachable
    }
}
";
            var c = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            c.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "System"));
            c.VerifyIL("C.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  br.s       IL_0000
}
");
        }

        [Fact, WorkItem(528275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528275")]
        public void CS0162WRN_UnreachableCode_3()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        for (uint i = 0; true || f(); i = i + 1) // f() is unreachable expression 
        {
        }
    }
    static bool f()
    {
        return true;
    }
}
";
            string expectedIL = @"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (uint V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_0002
}
";
            // Roslyn no unreachable warning CS0429 (for expr)
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Unreachable code
        [Fact]
        public void CS0162WRN_UnreachableCode_4()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                goto stop;
                goto outerLoop;
            }
        outerLoop:
            return;
        }
    stop:
        return;
    }
}
";
            string expectedIL = @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (int V_0, //i
  int V_1) //j
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_000c
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldc.i4.s   10
  IL_0009:  pop
  IL_000a:  pop
  IL_000b:  ret
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.s   10
  IL_000f:  blt.s      IL_0004
  IL_0011:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "goto"),
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "i")
                );
        }

        // Unreachable code
        [Fact]
        public void CS0162WRN_UnreachableCode_5()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1) // j++ is unreachable
            {
                throw new System.Exception();
            }
        }
    }
}
";
            string expectedIL = @"{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0, //i
           int V_1) //j
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0017
  IL_0004:  ldc.i4.0  
  IL_0005:  stloc.1   
  IL_0006:  br.s       IL_000e
  IL_0008:  newobj     ""System.Exception..ctor()""
  IL_000d:  throw     
  IL_000e:  ldloc.1   
  IL_000f:  ldc.i4.s   10
  IL_0011:  blt.s      IL_0008
  IL_0013:  ldloc.0   
  IL_0014:  ldc.i4.1  
  IL_0015:  add       
  IL_0016:  stloc.0   
  IL_0017:  ldloc.0   
  IL_0018:  ldc.i4.s   10
  IL_001a:  blt.s      IL_0004
  IL_001c:  ret       
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "j"));
        }

        // Unreachable code
        [Fact]
        public void CS0162WRN_UnreachableCode_6()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            return;
        }
    }
}
";
            string expectedIL = @"
{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.s   10
  IL_0005:  pop
  IL_0006:  pop
  IL_0007:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "i"));
        }

        // Object initializer as iteration variable of for loop
        [Fact]
        public void ObjectInitAsInitializer()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        for (Foo f = new Foo { i = 0, s = ""abc"" }; f.i < 5; f.i = f.i + 1)
        {
        }
    }
}
public class Foo
{
    public int i;
    public string s;
}
";
            string expectedIL = @"
{
  // Code size       50 (0x32)
  .maxstack  3
  .locals init (Foo V_0) //f
  IL_0000:  newobj     ""Foo..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  stfld      ""int Foo.i""
  IL_000c:  dup
  IL_000d:  ldstr      ""abc""
  IL_0012:  stfld      ""string Foo.s""
  IL_0017:  stloc.0
  IL_0018:  br.s       IL_0028
  IL_001a:  ldloc.0
  IL_001b:  ldloc.0
  IL_001c:  ldfld      ""int Foo.i""
  IL_0021:  ldc.i4.1
  IL_0022:  add
  IL_0023:  stfld      ""int Foo.i""
  IL_0028:  ldloc.0
  IL_0029:  ldfld      ""int Foo.i""
  IL_002e:  ldc.i4.5
  IL_002f:  blt.s      IL_001a
  IL_0031:  ret
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Dynamic works in for loop
        [Fact]
        public void DynamicInFor()
        {
            var text = @"class C
{
    static void Main(string[] args)
    {
        dynamic d = new myFor();
        for (d.Initialize(5); d.Done; d.Next())
        {
        }
    }
}

public class myFor
{
    int index;
    int max;
    public void Initialize(int max)
    {
        index = 0;
        this.max = max;
        System.Console.WriteLine(""Initialize"");
    }
    public bool Done
    {
        get
        {
            System.Console.WriteLine(""Done"");
            return index < max;
        }
    }
    public void Next()
    {
        index = index + 1;
        System.Console.WriteLine(""Next"");
    }
}
";
            CompileAndVerify(text, additionalRefs: new MetadataReference[] { CSharpRef, SystemCoreRef }, expectedOutput: @"Initialize
Done
Next
Done
Next
Done
Next
Done
Next
Done
Next
Done");
        }

        // Var works in for loop
        [Fact]
        public void VarInFor()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (var i = 1; i < 5; i = i + 1) ;
    }
}
";
            string expectedIL = @"{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0008
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.5
  IL_000a:  blt.s      IL_0004
  IL_000c:  ret       
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // Query expression in initializer list
        [Fact]
        public void QueryInInit()
        {
            var text =
@"
using System.Linq;
using System.Collections.Generic;
class C
{
    static void Main(string[] args)
    {
        for (IEnumerable<string> str = from x in ""123""
                                       let z = x.ToString()
                                       select z into w
                                       select w; ; )
        {
            foreach (var item in str)
            {
                System.Console.WriteLine(item);
            }
            return;
        }
    }
}
";
            var comp = CompileAndVerify(text, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: @"1
2
3");
        }

        // Query expression in for body
        [Fact]
        public void QueryInBody()
        {
            var text =
@"
using System.Linq;
using System.Collections.Generic;
class C
{
    static void Main(string[] args)
    {
        foreach (var item in fun())
        {
            System.Console.WriteLine(item);
        }
    }

    private static IEnumerable<string> fun()
    {
        for (int i = 0; i < 5; )
        {
            return from x in ""123""
                   let z = x.ToString()
                   select z into w
                   select w;
        }
        return null;
    }
}
";
            var comp = CompileAndVerify(text, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: @"1
2
3");
        }

        // Expression tree in for initializer of for statement
        [Fact]
        public void ExpressiontreeInInit()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        System.Linq.Expressions.Expression<System.Func<int, int>> e = x => x % 6;
        int i = 1;
        for (e = x => x * x; i < 5; i++)
        {
            var lambda = e.Compile();
            System.Console.WriteLine(lambda(i));
        }
    }
}
";
            CompileAndVerify(text, additionalRefs: new[] { SystemCoreRef }, expectedOutput: @"1
4
9
16");
        }

        // Expression tree in for iterator of for statement
        [Fact]
        public void ExpressiontreeInIterator()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        System.Linq.Expressions.Expression<System.Func<int, int>> e = x => x % 6;
        for (int i = 1; i < 5; e = x => x * x, i = i + 1)
        {
            var lambda = e.Compile();
            System.Console.WriteLine(lambda(i));
        }
    }
}
";

            var comp = CompileAndVerify(text, additionalRefs: new[] { SystemCoreRef }, expectedOutput: @"1
4
9
16");
        }

        // Custom type in for loop
        [Fact]
        public void CustomerTypeInFor()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (C1 i = new C1(); i == null; i++) { }
    }
}
public class C1
{
    public static C1 operator ++(C1 obj)
    {
        return obj;
    }
}
";

            string expectedIL = @"{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (C1 V_0) //i
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_000f
  IL_0008:  ldloc.0
  IL_0009:  call       ""C1 C1.op_Increment(C1)""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  brfalse.s  IL_0008
  IL_0012:  ret
}
";
            CompileAndVerify(text).
                 VerifyIL("C.Main", expectedIL);
        }

        // PostFix Increment In For
        [Fact, WorkItem(539759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539759")]
        public void PostFixIncrementInFor()
        {
            var text =
@"
class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        for (int j = i++; j < 5; ++j)
        {
            System.Console.WriteLine(j);
        }
    }
}
";

            string expectedIL = @"{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0) //j
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_000e
  IL_0004:  ldloc.0
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  add
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.5
  IL_0010:  blt.s      IL_0004
  IL_0012:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("Program.Main", expectedIL);
        }

        // PreFix Increment In For
        [Fact]
        public void PreFixIncrementInFor()
        {
            var text =
@"
class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        for (int j = ++i; j < 5; ++j)
        {
            System.Console.WriteLine(j);
        }
    }
}
";
            var comp = CompileAndVerify(text, expectedOutput: @"1
2
3
4");
        }

        // PreFix Increment In Condition
        [Fact]
        public void PreFixIncrementInCondition()
        {
            var text =
@"
class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; ++i < 5; )
        {
            System.Console.WriteLine(i);
        }
    }
}
";
            var comp = CompileAndVerify(text, expectedOutput: @"1
2
3
4");
        }

        // PostFix Increment In Condition
        [Fact]
        public void PostFixDecrementInCondition()
        {
            var text =
@"
class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; foo(i--) > -5;)
        {
            System.Console.WriteLine(i);
        }
    }
    static int foo(int x)
    {
        return x;
    }
}
";
            var comp = CompileAndVerify(text, expectedOutput: @"-1
-2
-3
-4
-5");
        }

        [Fact, WorkItem(992882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992882")]
        public void InfiniteLoopVerify()
        {
            var text =
@"
class Program
{
    static void Main(string[] args)
    {
        for (;true;)
        {
            System.Console.WriteLine(""z"");
        }
    }
}
";

            string expectedIL = @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""z""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  br.s       IL_0000
}";
            CompileAndVerify(text).
                VerifyIL("Program.Main", expectedIL);
        }

        [Fact, WorkItem(992882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992882")]
        public void InfiniteLoopVerify01()
        {
            var text =
@"
class Program
{
    static void Main(string[] args)
    {
        for (;true;)
        {
            System.Console.WriteLine(""z"");
        }
    }
}
";

            var c = CompileAndVerify(text, options: TestOptions.DebugExe);

            c.VerifyIL("Program.Main", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  br.s       IL_0010
  IL_0003:  nop
  IL_0004:  ldstr      ""z""
  IL_0009:  call       ""void System.Console.WriteLine(string)""
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  ldc.i4.1
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_0003
}");
        }
    }
}
