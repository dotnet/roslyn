// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CodeGenRefExtensionMethods : CompilingTestBase
    {
        [Fact]
        public void RefExtensionMethods()
        {
            var code = @"
public static class Extensions
{
    public static int IncrementAndGet(ref this int x)
    {
        return x++;
    }
}
public class Test
{
    public static void Main()
    {
        int value = 0;
        int other = value.IncrementAndGet();
        System.Console.Write(value);
        System.Console.Write(other);
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "10");

            verifier.VerifyIL("Test.Main", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int V_0) //value
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""int Extensions.IncrementAndGet(ref int)""
  IL_0009:  ldloc.0
  IL_000a:  call       ""void System.Console.Write(int)""
  IL_000f:  call       ""void System.Console.Write(int)""
  IL_0014:  ret
}");

            verifier.VerifyIL("Extensions.IncrementAndGet", @"
{
  // Code size       10 (0xa)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i4
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  stind.i4
  IL_0008:  ldloc.0
  IL_0009:  ret
}");
        }

        [Fact]
        public void RefReadOnlyExtensionMethods()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref readonly this int x)
    {
        System.Console.Write(x);
    }
}
public class Test
{
    public static void Main()
    {
        int value = 0;
        value.Print();
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "0");

            verifier.VerifyIL("Test.Main", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0) //value
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""void Extensions.Print(in int)""
  IL_0009:  ret
}");

            verifier.VerifyIL("Extensions.Print", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldind.i4
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  ret
}");
        }
    }
}
