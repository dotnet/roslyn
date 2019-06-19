// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenNullCheckedParameterTests : CSharpTestBase
    {
        [Fact]
        public void TestIsNullChecked()
        {
            var source = @"
using System;
public class C
{
    public static void Main() { }
    public void M(string input!) { }
}
";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
      // Code size       10 (0xa)
      .maxstack  1
     ~IL_0000:  ldarg.1
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.Exception..ctor()""
      IL_0008:  throw
     -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
{
      // Code size       11 (0xb)
      .maxstack  1
     ~IL_0000:  ldarg.1
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.Exception..ctor()""
      IL_0008:  throw
     -IL_0009:  nop
     -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestManyParamsOneNullChecked()
        {
            var source = @"
using System;
public class C
{
    public static void Main() { }
    public void M(string x, string y!) { }
}
";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
  // Code size       10 (0xa)
  .maxstack  1
 ~IL_0000:  ldarg.2
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.Exception..ctor()""
  IL_0008:  throw
 -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
{
      // Code size       11 (0xb)
      .maxstack  1
     ~IL_0000:  ldarg.2
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.Exception..ctor()""
      IL_0008:  throw
     -IL_0009:  nop
     -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalNullParameter()
        {
            // PROTOTYPE : Should give warning that the default value is null on a null-checked parameter.
            var source = @"
class C
{
    public static void Main() { }
    void M(string name! = null) { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
    // Code size       10 (0xa)
    .maxstack  1
   ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
   -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
 {
    // Code size       11 (0xb)
    .maxstack  1
   ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
   -IL_0009:  nop
   -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalStringParameter()
        {
            var source = @"
class C
{
    public static void Main() { }
    void M(string name! = ""rose"") { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
  // Code size       10 (0xa)
  .maxstack  1
 ~IL_0000:  ldarg.1
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.Exception..ctor()""
  IL_0008:  throw
 -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
 {
    // Code size       11 (0xb)
    .maxstack  1
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    -IL_0009:  nop
    -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedOperator()
        {
            var source = @"
class Box 
{
    public static void Main() { }
    public static int operator+ (Box b!, Box c)  
    { 
        return 2;
    }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("int Box.op_Addition(Box, Box)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
   ~IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
   -IL_0009:  ldc.i4.2
    IL_000a:  ret
}", sequencePoints: "Box.op_Addition");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("int Box.op_Addition(Box, Box)", @"
{
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int V_0)
   ~IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
   -IL_0009:  nop
   -IL_000a:  ldc.i4.2
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e
   -IL_000e:  ldloc.0
    IL_000f:  ret
}", sequencePoints: "Box.op_Addition");
        }

        [Fact(Skip = "PROTOTYPE")]
        public void TestNullCheckedArgListImplementation()
        {
            // PROTOTYPE : Will address later - issues with post-fix & binding?
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1!, 'M'));
    }
    void M2(__arglist)
    {
    }
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
    // Code size       10 (0xa)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.s   77
    IL_0004:  call       ""void C.M2(__arglist) with __arglist( int, char)""
    IL_0009:  ret
}");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
{
    // Code size       12 (0xc)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  ldc.i4.s   77
    IL_0005:  call       ""void C.M2(__arglist) with __arglist( int, char)""
    IL_000a:  nop
    IL_000b:  ret
}");
        }

        [Fact]
        public void TestManyNullCheckedArgs()
        {
            var source = @"
class C
{
    public void M(int x!, int y!) { }
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M(int, int)", @"
{
  // Code size       19 (0x13)
  .maxstack  1
 ~IL_0000:  ldarg.1
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.Exception..ctor()""
  IL_0008:  throw
 ~IL_0009:  ldarg.2
  IL_000a:  brtrue.s   IL_0012
  IL_000c:  newobj     ""System.Exception..ctor()""
  IL_0011:  throw
 -IL_0012:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M(int, int)", @"
  {
  // Code size       20 (0x14)
  .maxstack  1
 ~IL_0000:  ldarg.1
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.Exception..ctor()""
 IL_0008:  throw
 ~IL_0009:  ldarg.2
  IL_000a:  brtrue.s   IL_0012
  IL_000c:  newobj     ""System.Exception..ctor()""
  IL_0011:  throw
 -IL_0012:  nop
 -IL_0013:  ret
  }", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[int index!] => null;
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.this[int].get", @"
{
  // Code size       11 (0xb)
  .maxstack  1
 ~IL_0000:  ldarg.1
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.Exception..ctor()""
  IL_0008:  throw
 -IL_0009:  ldnull
  IL_000a:  ret
}", sequencePoints: "C.get_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.this[int].get", @"
{
   // Code size       11 (0xb)
   .maxstack  1
  ~IL_0000:  ldarg.1
   IL_0001:  brtrue.s   IL_0009
   IL_0003:  newobj     ""System.Exception..ctor()""
   IL_0008:  throw
  -IL_0009:  ldnull
   IL_000a:  ret
}", sequencePoints: "C.get_Item");
        }

        [Fact]
        public void TestNullCheckedIndexedGetterSetter()
        {
            var source = @"
class C
{
    private string[] names;
    public string this[int index!]
    {
        get
        {
            return names[index];
        }
        set
        {
            names[index] = value;
        }
    }
    public static void Main() { }
}";
            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.this[int].set", @"
{
    // Code size       19 (0x13)
    .maxstack  3
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    -IL_0009:  ldarg.0
    IL_000a:  ldfld      ""string[] C.names""
    IL_000f:  ldarg.1
    IL_0010:  ldarg.2
    IL_0011:  stelem.ref
    -IL_0012:  ret
}", sequencePoints: "C.set_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.this[int].set", @"
{
    // Code size       20 (0x14)
    .maxstack  3
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    -IL_0009:  nop
    -IL_000a:  ldarg.0
    IL_000b:  ldfld      ""string[] C.names""
    IL_0010:  ldarg.1
    IL_0011:  ldarg.2
    IL_0012:  stelem.ref
    -IL_0013:  ret
}", sequencePoints: "C.set_Item");
        }

        [Fact]
        public void TestNullCheckedIndexedSetter()
        {
            var source = @"
class C
{
    private string[] names;
    public string this[int index!]
    {
        set
        {
            names[index] = value;
        }
    }
    public static void Main() { }
}";
            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.this[int].set", @"
{
    // Code size       19 (0x13)
    .maxstack  3
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    -IL_0009:  ldarg.0
    IL_000a:  ldfld      ""string[] C.names""
    IL_000f:  ldarg.1
    IL_0010:  ldarg.2
    IL_0011:  stelem.ref
    -IL_0012:  ret
}", sequencePoints: "C.set_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.this[int].set", @"
{
    // Code size       20 (0x14)
    .maxstack  3
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    -IL_0009:  nop
    -IL_000a:  ldarg.0
    IL_000b:  ldfld      ""string[] C.names""
    IL_0010:  ldarg.1
    IL_0011:  ldarg.2
    IL_0012:  stelem.ref
    -IL_0013:  ret
}", sequencePoints: "C.set_Item");
        }
    }
}
