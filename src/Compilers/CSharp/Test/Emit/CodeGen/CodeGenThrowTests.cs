// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenThrowTests : CSharpTestBase
    {
        [ConditionalFact(typeof(DesktopOnly))]
        public void TestThrowNewExpression()
        {
            var source = @"
class C
{
    static void Main()
    {
        throw new System.Exception(""TestThrowNewExpression"");
    }
}";
            var compilation = CompileAndVerifyException<Exception>(source, "TestThrowNewExpression");

            compilation.VerifyIL("C.Main", @"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""TestThrowNewExpression""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  throw     
}
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void TestThrowLocalExpression()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Exception e = new System.Exception(""TestThrowLocalExpression"");
        throw e;
    }
}";
            var compilation = CompileAndVerifyException<Exception>(source, "TestThrowLocalExpression");

            compilation.VerifyIL("C.Main", @"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""TestThrowLocalExpression""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  throw
}
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void TestThrowNull()
        {
            var source = @"
class C
{
    static void Main()
    {
        throw null;
    }
}";
            var compilation = CompileAndVerifyException<NullReferenceException>(source);

            compilation.VerifyIL("C.Main", @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull    
  IL_0001:  throw     
}
");
        }

        [Fact]
        public void TestRethrowImplicit()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
            System.Console.WriteLine();
        }
        catch
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.Main", @"{
  // Code size       11 (0xb)
  .maxstack  1
  .try
  {
    IL_0000:  call       ""void System.Console.WriteLine()""
    IL_0005:  leave.s    IL_000a
  }
  catch object
  {
    IL_0007:  pop
    IL_0008:  rethrow
  }
  IL_000a:  ret
}
");
        }

        [Fact]
        public void TestRethrowTyped()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.Main", @"{
 // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
");
        }

        [Fact]
        public void TestRethrowTyped2()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
            System.Console.WriteLine();
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.Main", @"{
  // Code size       11 (0xb)
  .maxstack  1
  .try
  {
    IL_0000:  call       ""void System.Console.WriteLine()""
    IL_0005:  leave.s    IL_000a
  }
  catch System.Exception
  {
    IL_0007:  pop
    IL_0008:  rethrow
  }
  IL_000a:  ret
}
");
        }

        [Fact]
        public void TestRethrowNamed()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (System.Exception e)
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.Main", @"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
");
        }

        [Fact]
        public void TestRethrowNamed2()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
            System.Console.WriteLine();
        }
        catch (System.Exception e)
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.Main", @"{
  // Code size       11 (0xb)
  .maxstack  1
  .try
  {
    IL_0000:  call       ""void System.Console.WriteLine()""
    IL_0005:  leave.s    IL_000a
  }
  catch System.Exception
  {
    IL_0007:  pop
    IL_0008:  rethrow
  }
  IL_000a:  ret
}
");
        }

        [Fact]
        public void TestRethrowModified()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        try
        {
            try
            {
                Exception e = new Exception();
                e.Source = ""A"";
                throw e;
            }
            catch (Exception e)
            {
                // This modifies the exception that will be rethrown.
                e.Source = ""B"";
                throw;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Source);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "B");
        }

        [Fact]
        public void TestRethrowOverwritten()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        try
        {
            try
            {
                throw new Exception(""A"");
            }
            catch (Exception e)
            {
                // This modifies the local - the original exception is rethrown.
                e = null;
                throw;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "A");
        }
    }
}
