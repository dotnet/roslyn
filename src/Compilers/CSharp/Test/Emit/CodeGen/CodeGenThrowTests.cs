// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

        [Fact, WorkItem(14965, "https://github.com/dotnet/roslyn/issues/14965")]
        public void TestThrowConvertedValue()
        {
            var source = @"
class C
{
    static void M(X x)
    {
        throw x;
    }
}
class X
{
    public static implicit operator System.NullReferenceException(X x) => new System.NullReferenceException();
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         throw x;
                Diagnostic(ErrorCode.ERR_BadExceptionType, "x").WithLocation(6, 15)
                );
            var compilation = CompileAndVerify(source);
            compilation.VerifyDiagnostics();

            compilation.VerifyIL("C.M",
@"{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       ""System.NullReferenceException X.op_Implicit(X)""
    IL_0006:  throw
}");
        }

        [Fact]
        public void TestThrowSwitchedValue()
        {
            var source = @"
class C
{
    static void M(bool c)
    {
        throw c switch { true => new System.NullReferenceException(), false => new System.ArgumentException() };
    }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyDiagnostics();

            compilation.VerifyIL("C.M",
@"
{
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (System.Exception V_0)
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_000b
    IL_0003:  newobj     ""System.NullReferenceException..ctor()""
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011
    IL_000b:  newobj     ""System.ArgumentException..ctor()""
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  throw
}
");
        }

        [Fact]
        public void TestThrowTypeParameter()
        {
            var source = @"
class C
{
    static void M<T>(T t)
    {
        throw t;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0029: Cannot implicitly convert type 'T' to 'System.Exception'
                //         throw t;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "t").WithArguments("T", "System.Exception").WithLocation(6, 15)
                );
        }
    }
}
