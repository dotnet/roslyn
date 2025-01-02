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
    public class CodeGenTryFinallyTests : CSharpTestBase
    {
        [Fact]
        public void EmptyTryFinally()
        {
            var source =
@"class C
{
    static void EmptyTryFinally()
    {
        try { }
        finally { }
    }
    static void EmptyTryFinallyInTry()
    {
        try
        {
            try { }
            finally { }
        }
        finally { }
    }
    static void EmptyTryFinallyInFinally()
    {
        try { }
        finally
        {
            try { }
            finally { }
        }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.EmptyTryFinally",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            compilation.VerifyIL("C.EmptyTryFinallyInTry",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            compilation.VerifyIL("C.EmptyTryFinallyInFinally",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [Theory, WorkItem(4729, "https://github.com/dotnet/roslyn/issues/4729")]
        [InlineData("")]
        [InlineData(";")]
        public void NopInTryCatchFinally(string doNothingStatements)
        {
            var source =
$@"class C
{{
    static void M1()
    {{
        try {{ {doNothingStatements} }}
        catch (System.Exception) {{ {doNothingStatements} }}
        finally {{ {doNothingStatements} }}
    }}
    static void M2()
    {{
        try {{
            try {{ {doNothingStatements} }}
            catch (System.Exception) {{ {doNothingStatements} }}
            finally {{ {doNothingStatements} }}
        }}
        catch (System.Exception) {{
            try {{ {doNothingStatements} }}
            catch (System.Exception) {{ {doNothingStatements} }}
            finally {{ {doNothingStatements} }}
        }}
        finally {{
            try {{ {doNothingStatements} }}
            catch (System.Exception) {{ {doNothingStatements} }}
            finally {{ {doNothingStatements} }}
        }}
    }}
    static void M3()
    {{
        try {{ System.Console.WriteLine(1); }}
        catch (System.Exception) {{ {doNothingStatements} }}
        finally {{ {doNothingStatements} }}
    }}
    static void M4()
    {{
        try {{ {doNothingStatements} }}
        catch (System.Exception) {{ System.Console.WriteLine(1); }}
        finally {{ {doNothingStatements} }}
    }}
    static void M5()
    {{
        try {{ {doNothingStatements} }}
        catch (System.Exception) {{ {doNothingStatements} }}
        finally {{ System.Console.WriteLine(1); }}
    }}
}}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.M1",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            compilation.VerifyIL("C.M2",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            compilation.VerifyIL("C.M3",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  .try
  {
    IL_0000:  ldc.i4.1
    IL_0001:  call       ""void System.Console.WriteLine(int)""
    IL_0006:  leave.s    IL_000b
  }
  catch System.Exception
  {
    IL_0008:  pop
    IL_0009:  leave.s    IL_000b
  }
  IL_000b:  ret
}");
            compilation.VerifyIL("C.M4",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            compilation.VerifyIL("C.M5",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  leave.s    IL_0009
  }
  finally
  {
    IL_0002:  ldc.i4.1
    IL_0003:  call       ""void System.Console.WriteLine(int)""
    IL_0008:  endfinally
  }
  IL_0009:  ret
}");
        }

        [Fact]
        public void TryFinally()
        {
            var source =
@"class C
{
    static void Main()
    {
        M(1);
        M(0);
    }
    static void M(int f)
    {
        try
        {
            System.Console.Write(""1, "");
            if (f > 0)
                goto End;
            System.Console.Write(""2, "");
            return;
        }
        finally
        {
            System.Console.Write(""3, "");
        }
    End:
        System.Console.Write(""4, "");
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "1, 3, 4, 1, 2, 3, ");
            compilation.VerifyIL("C.M",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .try
{
  IL_0000:  ldstr      ""1, ""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  ble.s      IL_0010
  IL_000e:  leave.s    IL_0027
  IL_0010:  ldstr      ""2, ""
  IL_0015:  call       ""void System.Console.Write(string)""
  IL_001a:  leave.s    IL_0031
}
  finally
{
  IL_001c:  ldstr      ""3, ""
  IL_0021:  call       ""void System.Console.Write(string)""
  IL_0026:  endfinally
}
  IL_0027:  ldstr      ""4, ""
  IL_002c:  call       ""void System.Console.Write(string)""
  IL_0031:  ret
}");
        }

        [Fact]
        public void TryCatch()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        M(1);
        M(0);
    }
    static void M(int f)
    {
        try
        {
            Console.Write(""before, "");
            N(f);
            Console.Write(""after, "");
        }
        catch (Exception)
        {
            Console.Write(""catch, "");
        }
    }
    static void N(int f)
    {
        if (f > 0)
            throw new Exception();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "before, catch, before, after,");
            compilation.VerifyIL("C.M",
@"{
  // Code size       42 (0x2a)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""before, ""
    IL_0005:  call       ""void System.Console.Write(string)""
    IL_000a:  ldarg.0   
    IL_000b:  call       ""void C.N(int)""
    IL_0010:  ldstr      ""after, ""
    IL_0015:  call       ""void System.Console.Write(string)""
    IL_001a:  leave.s    IL_0029
  }
  catch System.Exception
  {
    IL_001c:  pop       
    IL_001d:  ldstr      ""catch, ""
    IL_0022:  call       ""void System.Console.Write(string)""
    IL_0027:  leave.s    IL_0029
  }
  IL_0029:  ret       
}");
        }

        [Fact]
        public void TryCatch001()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
    }

    static void M()
    {
        try
        {
            System.Object o = null;
            o.ToString();//null Exception throw

        }
        catch (System.NullReferenceException NullException)//null Exception caught
        {
            try
            {
                throw new System.ApplicationException();//app Exception throw
            }
            catch (System.ApplicationException AppException)//app Exception caught
            {
                throw new System.DivideByZeroException();//Zero Exception throw
            }
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "");
            compilation.VerifyIL("C.M",
@"{
  // Code size       24 (0x18)
  .maxstack  1
  .try
  {
    IL_0000:  ldnull
    IL_0001:  callvirt   ""string object.ToString()""
    IL_0006:  pop
    IL_0007:  leave.s    IL_0017
  }
  catch System.NullReferenceException
  {
    IL_0009:  pop
    .try
    {
      IL_000a:  newobj     ""System.ApplicationException..ctor()""
      IL_000f:  throw
    }
    catch System.ApplicationException
    {
      IL_0010:  pop
      IL_0011:  newobj     ""System.DivideByZeroException..ctor()""
      IL_0016:  throw
    }
  }
  IL_0017:  ret
}");
        }

        [Fact]
        public void TryCatch002()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
    }

    static void M()
    {
        try
        {
            System.Object o = null;
            o.ToString();//null Exception throw
            goto l1;
        }
        catch (System.NullReferenceException NullException)//null Exception caught
        {
            try
            {
                throw new System.ApplicationException();//app Exception throw
            }
            catch (System.ApplicationException AppException)//app Exception caught
            {
                throw new System.DivideByZeroException();//Zero Exception throw
            }
        }

        l1:
        ;
        ;
        ;
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "");
            compilation.VerifyIL("C.M", @"
{
  // Code size       24 (0x18)
  .maxstack  1
  .try
  {
    IL_0000:  ldnull
    IL_0001:  callvirt   ""string object.ToString()""
    IL_0006:  pop
    IL_0007:  leave.s    IL_0017
  }
  catch System.NullReferenceException
  {
    IL_0009:  pop
    .try
    {
      IL_000a:  newobj     ""System.ApplicationException..ctor()""
      IL_000f:  throw
    }
    catch System.ApplicationException
    {
      IL_0010:  pop
      IL_0011:  newobj     ""System.DivideByZeroException..ctor()""
      IL_0016:  throw
    }
  }
  IL_0017:  ret
}");
        }

        [WorkItem(813428, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813428")]
        [Fact]
        public void TryCatchOptimized001()
        {
            var source =
@"

using System;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        try
        {
            throw new Exception(""debug only"");
        }
        catch (Exception ex)
        {
            Debug.Write(ex.Message);
        }

        try
        {
            throw new Exception(""hello"");
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }

        try
        {
            throw new Exception(""bye"");
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
            Console.Write(ex.Message);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "hellobyebye");
            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       74 (0x4a)
  .maxstack  2
  .try
{
  IL_0000:  ldstr      ""debug only""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  throw
}
  catch System.Exception
{
  IL_000b:  pop
  IL_000c:  leave.s    IL_000e
}
  IL_000e:  nop
  .try
{
  IL_000f:  ldstr      ""hello""
  IL_0014:  newobj     ""System.Exception..ctor(string)""
  IL_0019:  throw
}
  catch System.Exception
{
  IL_001a:  callvirt   ""string System.Exception.Message.get""
  IL_001f:  call       ""void System.Console.Write(string)""
  IL_0024:  leave.s    IL_0026
}
  IL_0026:  nop
  .try
{
  IL_0027:  ldstr      ""bye""
  IL_002c:  newobj     ""System.Exception..ctor(string)""
  IL_0031:  throw
}
  catch System.Exception
{
  IL_0032:  dup
  IL_0033:  callvirt   ""string System.Exception.Message.get""
  IL_0038:  call       ""void System.Console.Write(string)""
  IL_003d:  callvirt   ""string System.Exception.Message.get""
  IL_0042:  call       ""void System.Console.Write(string)""
  IL_0047:  leave.s    IL_0049
}
  IL_0049:  ret
}
");
        }

        [Fact]
        public void TryFilterOptimized001()
        {
            var source =
@"
using System;

class Program
{
    static void Main()
    {
        try
        {
            throw new Exception(""hello"");
        }
        catch (Exception ex1) when (ex1.Message == null)
        {
        }

        try
        {
            throw new Exception(""bye"");
        }
        catch (Exception ex2) when (F(ex2, ex2))
        {
        }
    }

    private static bool F(Exception e1, Exception e2) 
    {
        return false;
    }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Program.Main", @"
{
  // Code size       78 (0x4e)
  .maxstack  2
  .try
{
  IL_0000:  ldstr      ""hello""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  throw
}
  filter
{
  IL_000b:  isinst     ""System.Exception""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_0017
  IL_0013:  pop
  IL_0014:  ldc.i4.0
  IL_0015:  br.s       IL_0022
  IL_0017:  callvirt   ""string System.Exception.Message.get""
  IL_001c:  ldnull
  IL_001d:  ceq
  IL_001f:  ldc.i4.0
  IL_0020:  cgt.un
  IL_0022:  endfilter
}  // end filter
{  // handler
  IL_0024:  pop
  IL_0025:  leave.s    IL_0027
}
  IL_0027:  nop
  .try
{
  IL_0028:  ldstr      ""bye""
  IL_002d:  newobj     ""System.Exception..ctor(string)""
  IL_0032:  throw
}
  filter
{
  IL_0033:  isinst     ""System.Exception""
  IL_0038:  dup
  IL_0039:  brtrue.s   IL_003f
  IL_003b:  pop
  IL_003c:  ldc.i4.0
  IL_003d:  br.s       IL_0048
  IL_003f:  dup
  IL_0040:  call       ""bool Program.F(System.Exception, System.Exception)""
  IL_0045:  ldc.i4.0
  IL_0046:  cgt.un
  IL_0048:  endfilter
}  // end filter
{  // handler
  IL_004a:  pop
  IL_004b:  leave.s    IL_004d
}
  IL_004d:  ret
}
");
        }

        [Fact]
        public void TryFilterOptimized002()
        {
            var source =
@"
using System;

class Program
{
    static void Main()
    {
        try
        {
            throw new Exception(""bye"");
        }
        catch (Exception ex) when (F(ex, ex))
        {
            Console.WriteLine(ex);
        }
    }

    private static bool F(Exception e1, Exception e2) 
    {
        return false;
    }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Program.Main", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  .try
  {
    IL_0000:  ldstr      ""bye""
    IL_0005:  newobj     ""System.Exception..ctor(string)""
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     ""System.Exception""
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_0022
    IL_0017:  stloc.0
    IL_0018:  ldloc.0
    IL_0019:  ldloc.0
    IL_001a:  call       ""bool Program.F(System.Exception, System.Exception)""
    IL_001f:  ldc.i4.0
    IL_0020:  cgt.un
    IL_0022:  endfilter
  }  // end filter
  {  // handler
    IL_0024:  pop
    IL_0025:  ldloc.0
    IL_0026:  call       ""void System.Console.WriteLine(object)""
    IL_002b:  leave.s    IL_002d
  }
  IL_002d:  ret
}
");
        }

        [Fact]
        public void TryFilterOptimized003()
        {
            var source =
@"
using System;

class Program
{
    static void Main()
    {
        try
        {
            throw new Exception(""bye"");
        }
        catch (Exception ex) when (F(ref ex))
        {
            Console.WriteLine(ex);
        }
    }

    private static bool F(ref Exception e) 
    {
        e = null;
        return true;
    }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Program.Main", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  .try
{
  IL_0000:  ldstr      ""bye""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  throw
}
  filter
{
  IL_000b:  isinst     ""System.Exception""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_0017
  IL_0013:  pop
  IL_0014:  ldc.i4.0
  IL_0015:  br.s       IL_0022
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""bool Program.F(ref System.Exception)""
  IL_001f:  ldc.i4.0
  IL_0020:  cgt.un
  IL_0022:  endfilter
}  // end filter
{  // handler
  IL_0024:  pop
  IL_0025:  ldloc.0
  IL_0026:  call       ""void System.Console.WriteLine(object)""
  IL_002b:  leave.s    IL_002d
}
  IL_002d:  ret
}");
        }

        [Fact]
        public void TryFilterOptimized004()
        {
            var source =
@"
using System;

class Program
{
    static void F<T>() where T : Exception
    {
        try
        {
            throw new Exception(""bye"");
        }
        catch (T ex) when (F(ex, ex))
        {
            Console.WriteLine(ex);
        }
    }

    private static bool F<S>(S e1, S e2) 
    {
        return false;
    }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Program.F<T>", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (T V_0) //ex
  .try
  {
    IL_0000:  ldstr      ""bye""
    IL_0005:  newobj     ""System.Exception..ctor(string)""
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     ""T""
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_0027
    IL_0017:  unbox.any  ""T""
    IL_001c:  stloc.0
    IL_001d:  ldloc.0
    IL_001e:  ldloc.0
    IL_001f:  call       ""bool Program.F<T>(T, T)""
    IL_0024:  ldc.i4.0
    IL_0025:  cgt.un
    IL_0027:  endfilter
  }  // end filter
  {  // handler
    IL_0029:  pop
    IL_002a:  ldloc.0
    IL_002b:  box        ""T""
    IL_0030:  call       ""void System.Console.WriteLine(object)""
    IL_0035:  leave.s    IL_0037
  }
  IL_0037:  ret
}
");
        }

        [Fact, WorkItem(854935, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854935")]
        public void LiftedExceptionVariableInGenericIterator()
        {
            var source = @"
using System;
using System.IO;
using System.Collections.Generic;

class C 
{
	public IEnumerable<int> Iter2<T>() 
	{
		try
		{
			throw new IOException(""Hi"");
        }
		catch (Exception e)
		{
			( (Action) delegate { Console.WriteLine(e.Message); })();
		}
        yield return 1;
	}
 
	static void Main()
    {
        foreach (var x in new C().Iter2<object>()) { }
    }
}";
            CompileAndVerify(source, expectedOutput: "Hi");
        }

        [Fact, WorkItem(854935, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854935")]
        public void GenericLiftedExceptionVariableInGenericIterator()
        {
            var source = @"
using System;
using System.IO;
using System.Collections.Generic;

class C
{
	public IEnumerable<int> Iter2<T, E>() where E : Exception
	{
		try
		{
			throw new IOException(""Hi"");
        }
		catch (E e) when (new Func<bool>(() => e.Message != null)())
		{
			( (Action) delegate { Console.WriteLine(e.Message); })();
		}
        yield return 1;
	}
 
	static void Main()
    {
        foreach (var x in new C().Iter2<object, IOException>()) { }
    }
}";
            CompileAndVerify(source, expectedOutput: "Hi");
        }

        [WorkItem(579778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579778")]
        [Fact]
        public void Regression579778()
        {
            var source =
@"
using System;
using System.Security;

[assembly: SecurityTransparent()]

class C
{
    static void nop() {}
    static void Main()
    {
            try
            {
                throw null;
            }
            catch (Exception)
            {
            }
            finally
            {
                try { nop(); }
                catch { }
            }


    }
    
}";
            var compilation = CompileAndVerify(source, expectedOutput: "");
            compilation.VerifyIL("C.Main",
@"
{
  // Code size       18 (0x12)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw
    }
    catch System.Exception
    {
      IL_0002:  pop
      IL_0003:  leave.s    IL_0011
    }
  }
  finally
  {
    IL_0005:  nop
    .try
    {
      IL_0006:  call       ""void C.nop()""
      IL_000b:  leave.s    IL_0010
    }
    catch object
    {
      IL_000d:  pop
      IL_000e:  leave.s    IL_0010
    }
    IL_0010:  endfinally
  }
  IL_0011:  ret
}
");
        }

        [Fact]
        public void NestedExceptionHandlers()
        {
            var source =
@"using System;
class C
{
    static void F(int i)
    {
        if (i == 0)
        {
            throw new Exception(""i == 0"");
        }
        throw new InvalidOperationException(""i != 0"");
    }
    static void M(int i)
    {
        try
        {
            try
            {
                F(i);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(""InvalidOperationException: {0}"", e.Message);
                F(i + 1);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(""Exception: {0}"", e.Message);
        }
    }
    static void Main()
    {
        M(0);
        M(1);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"Exception: i == 0
InvalidOperationException: i != 0
Exception: i != 0");
            compilation.VerifyIL("C.M",
@"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (System.InvalidOperationException V_0, //e
                System.Exception V_1) //e
  .try
  {
    .try
    {
      IL_0000:  ldarg.0
      IL_0001:  call       ""void C.F(int)""
      IL_0006:  leave.s    IL_0023
    }
    catch System.InvalidOperationException
    {
      IL_0008:  stloc.0
      IL_0009:  ldstr      ""InvalidOperationException: {0}""
      IL_000e:  ldloc.0
      IL_000f:  callvirt   ""string System.Exception.Message.get""
      IL_0014:  call       ""void System.Console.WriteLine(string, object)""
      IL_0019:  ldarg.0
      IL_001a:  ldc.i4.1
      IL_001b:  add
      IL_001c:  call       ""void C.F(int)""
      IL_0021:  leave.s    IL_0023
    }
    IL_0023:  leave.s    IL_0038
  }
  catch System.Exception
  {
    IL_0025:  stloc.1
    IL_0026:  ldstr      ""Exception: {0}""
    IL_002b:  ldloc.1
    IL_002c:  callvirt   ""string System.Exception.Message.get""
    IL_0031:  call       ""void System.Console.WriteLine(string, object)""
    IL_0036:  leave.s    IL_0038
  }
  IL_0038:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void NestedExceptionHandlersThreadAbort01()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                s.Set();
                for (; ;) ;
            }
            catch (Exception ex)
            {
                Console.WriteLine(""catch1"");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(""catch2"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
catch1
catch2
");
            compilation.VerifyIL("Program.Test",
@"
{
  // Code size       41 (0x29)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
      IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
      IL_000a:  br.s       IL_000a
    }
    catch System.Exception
    {
      IL_000c:  pop
      IL_000d:  ldstr      ""catch1""
      IL_0012:  call       ""void System.Console.WriteLine(string)""
      IL_0017:  leave.s    IL_0019
    }
    IL_0019:  leave.s    IL_0028
  }
  catch System.Exception
  {
    IL_001b:  pop
    IL_001c:  ldstr      ""catch2""
    IL_0021:  call       ""void System.Console.WriteLine(string)""
    IL_0026:  leave.s    IL_0028
  }
  IL_0028:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void NestedExceptionHandlersThreadAbort02()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                try
                {
                    s.Set();
                    for (; ;) ;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(""catch1"");
                }
            }
            finally
            {
                Console.WriteLine(""finally"");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(""catch2"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
catch1
finally
catch2
");
            compilation.VerifyIL("Program.Test",
@"
{
  // Code size       54 (0x36)
  .maxstack  1
  .try
  {
    .try
    {
      .try
      {
        IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
        IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
        IL_000a:  br.s       IL_000a
      }
      catch System.Exception
      {
        IL_000c:  pop
        IL_000d:  ldstr      ""catch1""
        IL_0012:  call       ""void System.Console.WriteLine(string)""
        IL_0017:  leave.s    IL_0019
      }
      IL_0019:  leave.s    IL_0026
    }
    finally
    {
      IL_001b:  ldstr      ""finally""
      IL_0020:  call       ""void System.Console.WriteLine(string)""
      IL_0025:  endfinally
    }
    IL_0026:  leave.s    IL_0035
  }
  catch System.Exception
  {
    IL_0028:  pop
    IL_0029:  ldstr      ""catch2""
    IL_002e:  call       ""void System.Console.WriteLine(string)""
    IL_0033:  leave.s    IL_0035
  }
  IL_0035:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void NestedExceptionHandlersThreadAbort03()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                try
                {
                    try
                    {
                        s.Set();
                        while (s != null) {};
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(""catch1"");
                    }
    }
                finally
                {
                    Console.WriteLine(""finally1"");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(""catch2"");
            }
        }
        finally
        {
            Console.WriteLine(""finally2"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
catch1
finally1
catch2
finally2
");
            compilation.VerifyIL("Program.Test",
@"
{
  // Code size       72 (0x48)
  .maxstack  1
  .try
  {
    .try
    {
      .try
      {
        .try
        {
          IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
          IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
          IL_000a:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
          IL_000f:  brtrue.s   IL_000a
          IL_0011:  leave.s    IL_0020
        }
        catch System.Exception
        {
          IL_0013:  pop
          IL_0014:  ldstr      ""catch1""
          IL_0019:  call       ""void System.Console.WriteLine(string)""
          IL_001e:  leave.s    IL_0020
        }
        IL_0020:  leave.s    IL_002d
      }
      finally
      {
        IL_0022:  ldstr      ""finally1""
        IL_0027:  call       ""void System.Console.WriteLine(string)""
        IL_002c:  endfinally
      }
      IL_002d:  leave.s    IL_0047
    }
    catch System.Exception
    {
      IL_002f:  pop
      IL_0030:  ldstr      ""catch2""
      IL_0035:  call       ""void System.Console.WriteLine(string)""
      IL_003a:  leave.s    IL_0047
    }
  }
  finally
  {
    IL_003c:  ldstr      ""finally2""
    IL_0041:  call       ""void System.Console.WriteLine(string)""
    IL_0046:  endfinally
  }
  IL_0047:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void NestedExceptionHandlersThreadAbort04()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                try
                {
                    try
                    {
                        s.Set();
                        while (s != null) {};
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(""catch1"");
                    }
    }
                finally
                {
                    Console.WriteLine(""finally1"");
                }
            }
            catch (Exception ex) when (ex != null)
            {
                Console.WriteLine(""catch2"");
            }
        }
        finally
        {
            Console.WriteLine(""finally2"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
catch1
finally1
catch2
finally2
");
            compilation.VerifyIL("Program.Test",
@"
{
  // Code size       92 (0x5c)
  .maxstack  2
  .try
  {
    .try
    {
      .try
      {
        .try
        {
          IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
          IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
          IL_000a:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
          IL_000f:  brtrue.s   IL_000a
          IL_0011:  leave.s    IL_0020
        }
        catch System.Exception
        {
          IL_0013:  pop
          IL_0014:  ldstr      ""catch1""
          IL_0019:  call       ""void System.Console.WriteLine(string)""
          IL_001e:  leave.s    IL_0020
        }
        IL_0020:  leave.s    IL_002d
      }
      finally
      {
        IL_0022:  ldstr      ""finally1""
        IL_0027:  call       ""void System.Console.WriteLine(string)""
        IL_002c:  endfinally
      }
      IL_002d:  leave.s    IL_005b
    }
    filter
    {
      IL_002f:  isinst     ""System.Exception""
      IL_0034:  dup
      IL_0035:  brtrue.s   IL_003b
      IL_0037:  pop
      IL_0038:  ldc.i4.0
      IL_0039:  br.s       IL_0041
      IL_003b:  ldnull
      IL_003c:  cgt.un
      IL_003e:  ldc.i4.0
      IL_003f:  cgt.un
      IL_0041:  endfilter
    }  // end filter
    {  // handler
      IL_0043:  pop
      IL_0044:  ldstr      ""catch2""
      IL_0049:  call       ""void System.Console.WriteLine(string)""
      IL_004e:  leave.s    IL_005b
    }
  }
  finally
  {
    IL_0050:  ldstr      ""finally2""
    IL_0055:  call       ""void System.Console.WriteLine(string)""
    IL_005a:  endfinally
  }
  IL_005b:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void NestedExceptionHandlersThreadAbort05()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static void nop() { }
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                s.Set();
                while (s != null) {};
            }
            catch
            {
                try
                {
                    nop();
                }
                catch
                {
                    Console.WriteLine(""catch1"");
                }
            }
            finally
            {
                try
                {
                    Console.WriteLine(""try2"");
                }
                catch
                {
                    Console.WriteLine(""catch2"");
                }
            }
        }
        catch
        {
            Console.WriteLine(""catch3"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
try2
catch3
");
            compilation.VerifyIL("Program.Test",
@"{
  // Code size       87 (0x57)
  .maxstack  1
  .try
  {
    .try
    {
      .try
      {
        IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
        IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
        IL_000a:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
        IL_000f:  brtrue.s   IL_000a
        IL_0011:  leave.s    IL_002a
      }
      catch object
      {
        IL_0013:  pop
        .try
        {
          IL_0014:  call       ""void Program.nop()""
          IL_0019:  leave.s    IL_0028
        }
        catch object
        {
          IL_001b:  pop
          IL_001c:  ldstr      ""catch1""
          IL_0021:  call       ""void System.Console.WriteLine(string)""
          IL_0026:  leave.s    IL_0028
        }
        IL_0028:  leave.s    IL_002a
      }
      IL_002a:  leave.s    IL_0047
    }
    finally
    {
      IL_002c:  nop
      .try
      {
        IL_002d:  ldstr      ""try2""
        IL_0032:  call       ""void System.Console.WriteLine(string)""
        IL_0037:  leave.s    IL_0046
      }
      catch object
      {
        IL_0039:  pop
        IL_003a:  ldstr      ""catch2""
        IL_003f:  call       ""void System.Console.WriteLine(string)""
        IL_0044:  leave.s    IL_0046
      }
      IL_0046:  endfinally
    }
    IL_0047:  leave.s    IL_0056
  }
  catch object
  {
    IL_0049:  pop
    IL_004a:  ldstr      ""catch3""
    IL_004f:  call       ""void System.Console.WriteLine(string)""
    IL_0054:  leave.s    IL_0056
  }
  IL_0056:  ret
}");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void NestedExceptionHandlersThreadAbort06()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                s.Set();
                while (s != null) {};
            }
            catch
            {
                try
                {
                    int i = 0;
                    i = i / i;
                }
                catch
                {
                    Console.WriteLine(""catch1"");
                }
            }
            finally
            {
                try
                {
                    Console.WriteLine(""try2"");
                }
                catch
                {
                    Console.WriteLine(""catch2"");
                }
            }
        }
        catch
        {
            Console.WriteLine(""catch3"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
catch1
try2
catch3
");
            compilation.VerifyIL("Program.Test",
@"
{
  // Code size       86 (0x56)
  .maxstack  2
  .try
  {
    .try
    {
      .try
      {
        IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
        IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
        IL_000a:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
        IL_000f:  brtrue.s   IL_000a
        IL_0011:  leave.s    IL_0029
      }
      catch object
      {
        IL_0013:  pop
        .try
        {
          IL_0014:  ldc.i4.0
          IL_0015:  dup
          IL_0016:  div
          IL_0017:  pop
          IL_0018:  leave.s    IL_0027
        }
        catch object
        {
          IL_001a:  pop
          IL_001b:  ldstr      ""catch1""
          IL_0020:  call       ""void System.Console.WriteLine(string)""
          IL_0025:  leave.s    IL_0027
        }
        IL_0027:  leave.s    IL_0029
      }
      IL_0029:  leave.s    IL_0046
    }
    finally
    {
      IL_002b:  nop
      .try
      {
        IL_002c:  ldstr      ""try2""
        IL_0031:  call       ""void System.Console.WriteLine(string)""
        IL_0036:  leave.s    IL_0045
      }
      catch object
      {
        IL_0038:  pop
        IL_0039:  ldstr      ""catch2""
        IL_003e:  call       ""void System.Console.WriteLine(string)""
        IL_0043:  leave.s    IL_0045
      }
      IL_0045:  endfinally
    }
    IL_0046:  leave.s    IL_0055
  }
  catch object
  {
    IL_0048:  pop
    IL_0049:  ldstr      ""catch3""
    IL_004e:  call       ""void System.Console.WriteLine(string)""
    IL_0053:  leave.s    IL_0055
  }
  IL_0055:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void NestedExceptionHandlersThreadAbort07()
        {
            var source =
@"
using System;
using System.Threading;

class Program
{
    static ManualResetEventSlim s = new ManualResetEventSlim(false);

    static void Main(string[] args)
    {
        var ts = new ThreadStart(Test);
        var t = new Thread(ts);

        t.Start();
        s.Wait();
        t.Abort();
        t.Join();
    }

    public static void Test()
    {
        try
        {
            try
            {
                try
                {
                    try
                    {
                        s.Set();
                        while (s != null) {};
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(""catch1"");
                    }
    }
                finally
                {
                    Console.WriteLine(""finally1"");
                }
            }
            finally
            {
                Console.WriteLine(""finally2"");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(""catch2"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
catch1
finally1
finally2
catch2
");
            compilation.VerifyIL("Program.Test",
@"
{
  // Code size       74 (0x4a)
  .maxstack  1
  .try
  {
    .try
    {
      .try
      {
        .try
        {
          IL_0000:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
          IL_0005:  callvirt   ""void System.Threading.ManualResetEventSlim.Set()""
          IL_000a:  ldsfld     ""System.Threading.ManualResetEventSlim Program.s""
          IL_000f:  brtrue.s   IL_000a
          IL_0011:  leave.s    IL_0020
        }
        catch System.Exception
        {
          IL_0013:  pop
          IL_0014:  ldstr      ""catch1""
          IL_0019:  call       ""void System.Console.WriteLine(string)""
          IL_001e:  leave.s    IL_0020
        }
        IL_0020:  leave.s    IL_002d
      }
      finally
      {
        IL_0022:  ldstr      ""finally1""
        IL_0027:  call       ""void System.Console.WriteLine(string)""
        IL_002c:  endfinally
      }
      IL_002d:  leave.s    IL_003a
    }
    finally
    {
      IL_002f:  ldstr      ""finally2""
      IL_0034:  call       ""void System.Console.WriteLine(string)""
      IL_0039:  endfinally
    }
    IL_003a:  leave.s    IL_0049
  }
  catch System.Exception
  {
    IL_003c:  pop
    IL_003d:  ldstr      ""catch2""
    IL_0042:  call       ""void System.Console.WriteLine(string)""
    IL_0047:  leave.s    IL_0049
  }
  IL_0049:  ret
}
");
        }

        [Fact]
        public void ThrowInTry()
        {
            var source =
@"using System;
class C
{
    static void nop() { }
    static void ThrowInTry()
    {
        try { throw new Exception(); }
        finally { }
    }
    static void ThrowInTryInTry()
    {
        try
        {
            try { throw new Exception(); }
            finally { }
        }
        finally { }
    }
    static void ThrowInTryInFinally()
    {
        try { }
        finally
        {
            try { throw new Exception(); }
            finally { }
        }
    }
}
class D
{
    static void nop() { }
    static void ThrowInTry()
    {
        try { throw new Exception(); }
        catch { }
        finally { }
    }
    static void ThrowInTryInTry()
    {
        try
        {
            try { throw new Exception(); }
            catch { }
            finally { }
        }
        finally { }
    }

    static void ThrowInTryInFinally()
    {
        try { nop(); }
        catch { }
        finally
        {
            try { throw new Exception(); }
            catch { }
            finally { }
        }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.ThrowInTry",
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""System.Exception..ctor()""
  IL_0005:  throw
}");
            compilation.VerifyIL("C.ThrowInTryInTry",
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""System.Exception..ctor()""
  IL_0005:  throw
}");
            compilation.VerifyIL("C.ThrowInTryInFinally",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  leave.s    IL_0008
  }
  finally
  {
    IL_0002:  newobj     ""System.Exception..ctor()""
    IL_0007:  throw
  }
  IL_0008:  br.s       IL_0008
}");
            compilation.VerifyIL("D.ThrowInTry",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  newobj     ""System.Exception..ctor()""
    IL_0005:  throw
  }
  catch object
  {
    IL_0006:  pop
    IL_0007:  leave.s    IL_0009
  }
  IL_0009:  ret
}");
            compilation.VerifyIL("D.ThrowInTryInTry",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  newobj     ""System.Exception..ctor()""
    IL_0005:  throw
  }
  catch object
  {
    IL_0006:  pop
    IL_0007:  leave.s    IL_0009
  }
  IL_0009:  ret
}");
            compilation.VerifyIL("D.ThrowInTryInFinally",
@"{
  // Code size       22 (0x16)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  call       ""void D.nop()""
      IL_0005:  leave.s    IL_0015
    }
    catch object
    {
      IL_0007:  pop
      IL_0008:  leave.s    IL_0015
    }
  }
  finally
  {
    IL_000a:  nop
    .try
    {
      IL_000b:  newobj     ""System.Exception..ctor()""
      IL_0010:  throw
    }
    catch object
    {
      IL_0011:  pop
      IL_0012:  leave.s    IL_0014
    }
    IL_0014:  endfinally
  }
  IL_0015:  ret
}");
        }

        [WorkItem(540716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540716")]
        [Fact]
        public void ThrowInFinally()
        {
            var source =
@"using System;
class C
{
    static void nop() { }
    static void ThrowInFinally()
    {
        try { }
        finally { throw new Exception(); }
    }
    static void ThrowInFinallyInTry()
    {
        try
        {
            try { }
            finally { throw new Exception(); }
        }
        finally { nop(); }
    }
    static int ThrowInFinallyInFinally()
    {
        try { }
        finally
        {
            try { }
            finally { throw new Exception(); }
        }

        // unreachable and has no return
        System.Console.WriteLine();
    }
}
class D
{
    static void nop() { }
    static void ThrowInFinally()
    {
        try { nop(); }
        catch { }
        finally { throw new Exception(); }
    }
    static void ThrowInFinallyInTry()
    {
        try
        {
            try { nop(); }
            catch { }
            finally { throw new Exception(); }
        }
        catch { }
        finally { nop(); }
    }
    static void ThrowInFinallyInFinally()
    {
        try { nop(); }
        catch { }
        finally
        {
            try { nop(); }
            catch { }
            finally { throw new Exception(); }
        }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.ThrowInFinally",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  leave.s    IL_0008
  }
  finally
  {
    IL_0002:  newobj     ""System.Exception..ctor()""
    IL_0007:  throw
  }
  IL_0008:  br.s       IL_0008
}");
            compilation.VerifyIL("C.ThrowInFinallyInTry",
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  leave.s    IL_0008
    }
    finally
    {
      IL_0002:  newobj     ""System.Exception..ctor()""
      IL_0007:  throw
    }
    IL_0008:  br.s       IL_0008
  }
  finally
  {
    IL_000a:  call       ""void C.nop()""
    IL_000f:  endfinally
  }
}");
            // The nop below is to work around a verifier bug.
            // See DevDiv 563799.
            compilation.VerifyIL("C.ThrowInFinallyInFinally",
@"{
  // Code size       15 (0xf)
  .maxstack  1
  .try
  {
    IL_0000:  leave.s    IL_000d
  }
  finally
  {
    IL_0002:  nop
    .try
    {
      IL_0003:  leave.s    IL_000b
    }
    finally
    {
      IL_0005:  newobj     ""System.Exception..ctor()""
      IL_000a:  throw
    }
    IL_000b:  br.s       IL_000b
  }
  IL_000d:  br.s       IL_000d
}");
            compilation.VerifyIL("D.ThrowInFinally",
@"{
  // Code size       18 (0x12)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  call       ""void D.nop()""
      IL_0005:  leave.s    IL_0010
    }
    catch object
    {
      IL_0007:  pop
      IL_0008:  leave.s    IL_0010
    }
  }
  finally
  {
    IL_000a:  newobj     ""System.Exception..ctor()""
    IL_000f:  throw
  }
  IL_0010:  br.s       IL_0010
}");
            compilation.VerifyIL("D.ThrowInFinallyInTry",
@"{
  // Code size       30 (0x1e)
  .maxstack  1
  .try
  {
    .try
    {
      .try
      {
        .try
        {
          IL_0000:  call       ""void D.nop()""
          IL_0005:  leave.s    IL_000a
        }
        catch object
        {
          IL_0007:  pop
          IL_0008:  leave.s    IL_000a
        }
        IL_000a:  leave.s    IL_0012
      }
      finally
      {
        IL_000c:  newobj     ""System.Exception..ctor()""
        IL_0011:  throw
      }
      IL_0012:  br.s       IL_0012
    }
    catch object
    {
      IL_0014:  pop
      IL_0015:  leave.s    IL_001d
    }
  }
  finally
  {
    IL_0017:  call       ""void D.nop()""
    IL_001c:  endfinally
  }
  IL_001d:  ret
}");
            compilation.VerifyIL("D.ThrowInFinallyInFinally",
@"{
  // Code size       31 (0x1f)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  call       ""void D.nop()""
      IL_0005:  leave.s    IL_001d
    }
    catch object
    {
      IL_0007:  pop
      IL_0008:  leave.s    IL_001d
    }
  }
  finally
  {
    IL_000a:  nop
    .try
    {
      .try
      {
        IL_000b:  call       ""void D.nop()""
        IL_0010:  leave.s    IL_001b
      }
      catch object
      {
        IL_0012:  pop
        IL_0013:  leave.s    IL_001b
      }
    }
    finally
    {
      IL_0015:  newobj     ""System.Exception..ctor()""
      IL_001a:  throw
    }
    IL_001b:  br.s       IL_001b
  }
  IL_001d:  br.s       IL_001d
}");
        }

        [Fact]
        public void TryFilterSimple()
        {
            var src = @"
using System;
class C
{
    static bool Filter()
    {
        Console.Write(""Filter"");
        return true;
    }

    static void Main()
    {
        int x = 0;
        
        try
        {
            Console.Write(""Try"");
            x = x / x;
        }
        catch when (Filter())
        {
            Console.Write(""Catch"");
        }
        finally
        {
            Console.Write(""Finally"");
        }
    }
}";
            var comp = CompileAndVerify(src,
                expectedOutput: "TryFilterCatchFinally");
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      ""Try""
      IL_0007:  call       ""void System.Console.Write(string)""
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_0035
    }
    filter
    {
      IL_0012:  pop
      IL_0013:  call       ""bool C.Filter()""
      IL_0018:  ldc.i4.0
      IL_0019:  cgt.un
      IL_001b:  endfilter
    }  // end filter
    {  // handler
      IL_001d:  pop
      IL_001e:  ldstr      ""Catch""
      IL_0023:  call       ""void System.Console.Write(string)""
      IL_0028:  leave.s    IL_0035
    }
  }
  finally
  {
    IL_002a:  ldstr      ""Finally""
    IL_002f:  call       ""void System.Console.Write(string)""
    IL_0034:  endfinally
  }
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TryFilterUseException()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            Test();
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }

    static void Test()
    {
        int x = 0;

        try
        {
            Console.Write(""Try"");
            x = x / x;
        }
        catch (DivideByZeroException e)
              when (e.Message == null)
        {
            Console.Write(""Catch1"");
        }
        catch (DivideByZeroException e)
               when (e.Message != null)
        {
            Console.Write(""Catch2"" + e.Message.Length);
        }
        finally
        {
            Console.Write(""Finally"");
        }
    }
}";
            CompileAndVerify(src, expectedOutput: "TryCatch228Finally").
                VerifyIL("C.Test", @"
{
  // Code size      132 (0x84)
  .maxstack  2
  .locals init (int V_0, //x
                System.DivideByZeroException V_1, //e
                int V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      ""Try""
      IL_0007:  call       ""void System.Console.Write(string)""
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_0083
    }
    filter
    {
      IL_0012:  isinst     ""System.DivideByZeroException""
      IL_0017:  dup
      IL_0018:  brtrue.s   IL_001e
      IL_001a:  pop
      IL_001b:  ldc.i4.0
      IL_001c:  br.s       IL_0029
      IL_001e:  callvirt   ""string System.Exception.Message.get""
      IL_0023:  ldnull
      IL_0024:  ceq
      IL_0026:  ldc.i4.0
      IL_0027:  cgt.un
      IL_0029:  endfilter
    }  // end filter
    {  // handler
      IL_002b:  pop
      IL_002c:  ldstr      ""Catch1""
      IL_0031:  call       ""void System.Console.Write(string)""
      IL_0036:  leave.s    IL_0083
    }
    filter
    {
      IL_0038:  isinst     ""System.DivideByZeroException""
      IL_003d:  dup
      IL_003e:  brtrue.s   IL_0044
      IL_0040:  pop
      IL_0041:  ldc.i4.0
      IL_0042:  br.s       IL_0051
      IL_0044:  stloc.1
      IL_0045:  ldloc.1
      IL_0046:  callvirt   ""string System.Exception.Message.get""
      IL_004b:  ldnull
      IL_004c:  cgt.un
      IL_004e:  ldc.i4.0
      IL_004f:  cgt.un
      IL_0051:  endfilter
    }  // end filter
    {  // handler
      IL_0053:  pop
      IL_0054:  ldstr      ""Catch2""
      IL_0059:  ldloc.1
      IL_005a:  callvirt   ""string System.Exception.Message.get""
      IL_005f:  callvirt   ""int string.Length.get""
      IL_0064:  stloc.2
      IL_0065:  ldloca.s   V_2
      IL_0067:  call       ""string int.ToString()""
      IL_006c:  call       ""string string.Concat(string, string)""
      IL_0071:  call       ""void System.Console.Write(string)""
      IL_0076:  leave.s    IL_0083
    }
  }
  finally
  {
    IL_0078:  ldstr      ""Finally""
    IL_007d:  call       ""void System.Console.Write(string)""
    IL_0082:  endfinally
  }
  IL_0083:  ret
}");
        }

        [Fact]
        public void TryFilterScoping()
        {
            var src = @"
using System;
class C
{
    private static string str = ""S1"";

    static void Main()
    {
        int x = 0;

        try
        {
            Console.Write(""Try"");
            x = x / x;
        }
        catch when (new Func<bool>(() => str.Length == 2)())
        {
            Console.Write(""Catch"" + str);
        }
        finally
        {
            Console.Write(""Finally"");
        }
    }
}";
            var comp = CompileAndVerify(src, expectedOutput: "TryCatchS1Finally");
            comp.VerifyIL("C.Main", @"
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      ""Try""
      IL_0007:  call       ""void System.Console.Write(string)""
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_005e
    }
    filter
    {
      IL_0012:  pop
      IL_0013:  ldsfld     ""System.Func<bool> C.<>c.<>9__1_0""
      IL_0018:  dup
      IL_0019:  brtrue.s   IL_0032
      IL_001b:  pop
      IL_001c:  ldsfld     ""C.<>c C.<>c.<>9""
      IL_0021:  ldftn      ""bool C.<>c.<Main>b__1_0()""
      IL_0027:  newobj     ""System.Func<bool>..ctor(object, System.IntPtr)""
      IL_002c:  dup
      IL_002d:  stsfld     ""System.Func<bool> C.<>c.<>9__1_0""
      IL_0032:  callvirt   ""bool System.Func<bool>.Invoke()""
      IL_0037:  ldc.i4.0
      IL_0038:  cgt.un
      IL_003a:  endfilter
    }  // end filter
    {  // handler
      IL_003c:  pop
      IL_003d:  ldstr      ""Catch""
      IL_0042:  ldsfld     ""string C.str""
      IL_0047:  call       ""string string.Concat(string, string)""
      IL_004c:  call       ""void System.Console.Write(string)""
      IL_0051:  leave.s    IL_005e
    }
  }
  finally
  {
    IL_0053:  ldstr      ""Finally""
    IL_0058:  call       ""void System.Console.Write(string)""
    IL_005d:  endfinally
  }
  IL_005e:  ret
}
");
        }

        [WorkItem(18678, "https://github.com/dotnet/roslyn/issues/18678")]
        [Fact]
        public void TryCatchConstantFalseFilter1()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        try
        {
            throw new Exception();
        }
        catch (Exception) when (false)
        {
            Console.Write(""Catch"");
        }
    }
}";
            var comp = CompileAndVerify(src);
            comp.VerifyIL("C.Main", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""System.Exception..ctor()""
  IL_0005:  throw
}");
        }

        [WorkItem(18678, "https://github.com/dotnet/roslyn/issues/18678")]
        [Fact]
        public void TryCatchConstantFalseFilter2()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        try
        {
            throw new Exception();
        }
        catch (NullReferenceException) when (false)
        {
            Console.Write(""Catch1"");
        }
        catch (Exception) when (false)
        {
            Console.Write(""Catch2"");
        }
        catch when (false)
        {
            Console.Write(""Catch"");
        }
    }
}";
            var comp = CompileAndVerify(src);
            comp.VerifyIL("C.Main", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""System.Exception..ctor()""
  IL_0005:  throw
}");
        }

        [WorkItem(18678, "https://github.com/dotnet/roslyn/issues/18678")]
        [Fact]
        public void TryCatchConstantFalseFilter3()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        try
        {
            throw new Exception();
        }
        catch (NullReferenceException) when ((1+1) == 2)
        {
            Console.Write(""Catch1"");
        }
        catch (Exception) when (true == false)
        {
            Console.Write(""Catch2"");
        }
        catch when ((1+1) != 2)
        {
            Console.Write(""Catch"");
        }
    }
}";
            var comp = CompileAndVerify(src);
            comp.VerifyIL("C.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .try
  {
    IL_0000:  newobj     ""System.Exception..ctor()""
    IL_0005:  throw
  }
  filter
  {
    IL_0006:  isinst     ""System.NullReferenceException""
    IL_000b:  dup
    IL_000c:  brtrue.s   IL_0012
    IL_000e:  pop
    IL_000f:  ldc.i4.0
    IL_0010:  br.s       IL_0017
    IL_0012:  pop
    IL_0013:  ldc.i4.1
    IL_0014:  ldc.i4.0
    IL_0015:  cgt.un
    IL_0017:  endfilter
  }  // end filter
  {  // handler
    IL_0019:  pop
    IL_001a:  ldstr      ""Catch1""
    IL_001f:  call       ""void System.Console.Write(string)""
    IL_0024:  leave.s    IL_0026
  }
  IL_0026:  ret
}");
        }

        [WorkItem(18678, "https://github.com/dotnet/roslyn/issues/18678")]
        [Fact]
        public void TryCatchConstantFalseFilterCombined()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        var message = ""ExceptionMessage"";
        try
        {
            throw new Exception(message);
        }
        catch (NullReferenceException) when (false)
        {
            Console.Write(""NullReferenceCatch"");
        }
        catch (Exception e) when (e.Message == message)
        {
            Console.Write(""ExceptionFilter"");
        }
        catch (Exception)
        {
            Console.Write(""ExceptionCatch"");
        }
        catch when (false)
        {
            Console.Write(""Catch"");
        }
    }
}";
            var comp = CompileAndVerify(src, expectedOutput: "ExceptionFilter");
            comp.VerifyIL("C.Main", @"
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (string V_0) //message
  IL_0000:  ldstr      ""ExceptionMessage""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldloc.0
    IL_0007:  newobj     ""System.Exception..ctor(string)""
    IL_000c:  throw
  }
  filter
  {
    IL_000d:  isinst     ""System.Exception""
    IL_0012:  dup
    IL_0013:  brtrue.s   IL_0019
    IL_0015:  pop
    IL_0016:  ldc.i4.0
    IL_0017:  br.s       IL_0027
    IL_0019:  callvirt   ""string System.Exception.Message.get""
    IL_001e:  ldloc.0
    IL_001f:  call       ""bool string.op_Equality(string, string)""
    IL_0024:  ldc.i4.0
    IL_0025:  cgt.un
    IL_0027:  endfilter
  }  // end filter
  {  // handler
    IL_0029:  pop
    IL_002a:  ldstr      ""ExceptionFilter""
    IL_002f:  call       ""void System.Console.Write(string)""
    IL_0034:  leave.s    IL_0043
  }
  catch System.Exception
  {
    IL_0036:  pop
    IL_0037:  ldstr      ""ExceptionCatch""
    IL_003c:  call       ""void System.Console.Write(string)""
    IL_0041:  leave.s    IL_0043
  }
  IL_0043:  ret
}");
        }

        [WorkItem(18678, "https://github.com/dotnet/roslyn/issues/18678")]
        [Fact]
        public void TryCatchFinallyConstantFalseFilter()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        try
        {

            try
            {
                throw new Exception();
            }
            catch (NullReferenceException) when (false)
            {
                Console.Write(""Catch1"");
            }
            catch (Exception) when (false)
            {
                Console.Write(""Catch2"");
            }
            finally
            {
                Console.Write(""Finally"");
            }
        }
        catch
        {
            Console.Write(""OuterCatch"");
        }
    }
}";
            var comp = CompileAndVerify(src, expectedOutput: "FinallyOuterCatch");
            comp.VerifyIL("C.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .try
  {
    .try
    {
      IL_0000:  newobj     ""System.Exception..ctor()""
      IL_0005:  throw
    }
    finally
    {
      IL_0006:  ldstr      ""Finally""
      IL_000b:  call       ""void System.Console.Write(string)""
      IL_0010:  endfinally
    }
  }
  catch object
  {
    IL_0011:  pop
    IL_0012:  ldstr      ""OuterCatch""
    IL_0017:  call       ""void System.Console.Write(string)""
    IL_001c:  leave.s    IL_001e
  }
  IL_001e:  ret
}");
        }

        [Fact]
        public void TryCatchWithReturnValue()
        {
            var source =
@"using System;
class C
{
    static int F(int i)
    {
        if (i == 0)
        {
            throw new InvalidOperationException();
        }
        else if (i == 1)
        {
            throw new InvalidCastException();
        }
        return i;
    }
    static int M(int i)
    {
        try
        {
            return F(i) * 3;
        }
        catch (InvalidOperationException)
        {
            return (i - 1) * 4;
        }
        catch
        {
        }
        finally
        {
            i = i + 10;
        }
        return i;
    }
    static void Main()
    {
        Console.WriteLine(""M(0)={0}, M(1)={1}, M(2)={2}"", M(0), M(1), M(2));
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "M(0)=-4, M(1)=11, M(2)=6");
            compilation.VerifyIL("C.M",
@"{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (int V_0)
  .try
  {
    .try
    {
      IL_0000:  ldarg.0   
      IL_0001:  call       ""int C.F(int)""
      IL_0006:  ldc.i4.3  
      IL_0007:  mul       
      IL_0008:  stloc.0   
      IL_0009:  leave.s    IL_0020
    }
    catch System.InvalidOperationException
    {
      IL_000b:  pop       
      IL_000c:  ldarg.0   
      IL_000d:  ldc.i4.1  
      IL_000e:  sub       
      IL_000f:  ldc.i4.4  
      IL_0010:  mul       
      IL_0011:  stloc.0   
      IL_0012:  leave.s    IL_0020
    }
    catch object
    {
      IL_0014:  pop       
      IL_0015:  leave.s    IL_001e
    }
  }
  finally
  {
    IL_0017:  ldarg.0   
    IL_0018:  ldc.i4.s   10
    IL_001a:  add       
    IL_001b:  starg.s    V_0
    IL_001d:  endfinally
  }
  IL_001e:  ldarg.0   
  IL_001f:  ret       
  IL_0020:  ldloc.0   
  IL_0021:  ret       
}");
        }

        [Fact]
        public void Rethrow()
        {
            var source =
@"using System.IO;
class C
{
    static void nop() { }
    static int F = 0;
    static void M()
    {
        try
        {
            nop();
        }
        catch (FileNotFoundException e)
        {
            if (F > 0)
            {
                throw;
            }
            else
            {
                throw e;
            }
        }
        catch (IOException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.M",
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (System.IO.FileNotFoundException V_0) //e
  .try
  {
    IL_0000:  call       ""void C.nop()""
    IL_0005:  leave.s    IL_001a
  }
  catch System.IO.FileNotFoundException
  {
    IL_0007:  stloc.0
    IL_0008:  ldsfld     ""int C.F""
    IL_000d:  ldc.i4.0
    IL_000e:  ble.s      IL_0012
    IL_0010:  rethrow
    IL_0012:  ldloc.0
    IL_0013:  throw
  }
  catch System.IO.IOException
  {
    IL_0014:  pop
    IL_0015:  rethrow
  }
  catch object
  {
    IL_0017:  pop
    IL_0018:  rethrow
  }
  IL_001a:  ret
}");
        }

        [WorkItem(541494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541494")]
        [Fact]
        public void CatchT()
        {
            var source =
@"using System;
class C
{
    internal static void TryCatch<T>() where T : Exception
    {
        try
        {
            Throw();
        }
        catch (T t)
        {
            Catch(t);
        }
    }
    static void Throw() { throw new NotImplementedException(); }
    static void Catch(Exception e)
    {
        Console.WriteLine(""Handled"");
    }
    static void Main()
    {
        try
        {
            TryCatch<NotImplementedException>();
            TryCatch<InvalidOperationException>();
        }
        catch (Exception)
        {
            Console.WriteLine(""Unhandled"");
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"Handled
Unhandled");
            compilation.VerifyIL("C.TryCatch<T>()",
@"
{
  // Code size       25 (0x19)
  .maxstack  1
  .try
{
  IL_0000:  call       ""void C.Throw()""
  IL_0005:  leave.s    IL_0018
}
  catch T
{
  IL_0007:  unbox.any  ""T""
  IL_000c:  box        ""T""
  IL_0011:  call       ""void C.Catch(System.Exception)""
  IL_0016:  leave.s    IL_0018
}
  IL_0018:  ret
}
");
        }

        [WorkItem(540664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540664")]
        [Fact]
        public void ExceptionAlreadyCaught1()
        {
            var source =
@"class C
{
    static void M()
    {
        try { }
        catch (System.Exception) { }
        catch { }
    }
}";
            CompileAndVerify(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreachableGeneralCatch, "catch").WithLocation(7, 9));
        }

        [Fact]
        public void ExceptionAlreadyCaught2()
        {
            var text = @"
class Program
{
    static void M()
    {
        int a = 1;
        try { }
        catch (System.Exception) { }
        catch when (a == 1) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,9): warning CS1058: A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a System.Runtime.CompilerServices.RuntimeWrappedException.
                //         catch when (a == 1) { }
                Diagnostic(ErrorCode.WRN_UnreachableGeneralCatch, "catch").WithLocation(9, 9));
        }

        [Fact]
        public void ExceptionAlreadyCaught3()
        {
            var text = @"
class Program
{
    static void M()
    {
        int a = 1;
        try { }
        catch when (a == 2) { }
        catch (System.Exception) when (a == 3) { }
        catch { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(540666, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540666")]
        [Fact]
        public void EmptyTryFinally_Simple()
        {
            var source =
@"class C
{
    static void M()
    {
        try { }
        finally { }
    }
}";
            CompileAndVerify(source);
        }

        [WorkItem(542002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542002")]
        [Fact]
        public void ConditionInTry()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        int a = 10;
        try
        {
            if (a == 3)
            {
            }
        }
        catch (System.Exception)
        {
            
        }
    }
}";

            var compilation = CompileAndVerify(source, expectedOutput: "");
            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0) //a
  IL_0000:  ldc.i4.s   10
  IL_0002:  stloc.0
  .try
{
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.3
  IL_0005:  pop
  IL_0006:  pop
  IL_0007:  leave.s    IL_000c
}
  catch System.Exception
{
  IL_0009:  pop
  IL_000a:  leave.s    IL_000c
}
  IL_000c:  ret
}
");
        }

        [Fact(), WorkItem(544911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544911")]
        public void UnreachableAfterTryFinally()
        {
            var source = @"
using System;

class Program
{
    static int Result = 0;
    static void F()
    {
        Result = 1;
    }

    static void T1()
    {
        try
        {
            goto L;
        }
        finally
        {
            for (; ; ) { }
        }
    L:
        F();        //unreachable
    }

    static void Main()
    {
        Console.Write(Result);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0");
            compilation.VerifyIL("Program.T1",
@"
{
  // Code size        6 (0x6)
  .maxstack  0
  .try
{
  IL_0000:  leave.s    IL_0004
}
  finally
{
  IL_0002:  br.s       IL_0002
}
  IL_0004:  br.s       IL_0004
}
");
        }

        [Fact(), WorkItem(544911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544911")]
        public void ReachableAfterBlockingCatch()
        {
            var source =
@"using System;

    class Program
    {
        static void nop() { }
        static void F()
        {
            Console.WriteLine(""hello"");
        }

        static void T1()
        {
            try
            {
                nop();
            }
            catch 
            {
                for (; ; ) { }
            }

            F();        //reachable
        }

        static void Main()
        {
            T1();
        }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "hello");
            compilation.VerifyIL("Program.T1",
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .try
  {
    IL_0000:  call       ""void Program.nop()""
    IL_0005:  leave.s    IL_000a
  }
  catch object
  {
    IL_0007:  pop
    IL_0008:  br.s       IL_0008
  }
  IL_000a:  call       ""void Program.F()""
  IL_000f:  ret
}");
        }

        [Fact(), WorkItem(544911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544911")]
        public void UnreachableAfterTryFinallyConditional()
        {
            var source = @"
using System;

class Program
{
    static int Result = 0;
    static void F()
    {
        Result = 1;
    }

    static void T1()
    {
        try
        {
            if (Result == 0)
                goto L;
        }
        finally
        {
            for (; ; ) { }
        }
    L:
        F();        //unreachable
    }

    static void Main()
    {
        Console.Write(Result);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0");
            compilation.VerifyIL("Program.T1",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .try
{
  IL_0000:  ldsfld     ""int Program.Result""
  IL_0005:  pop
  IL_0006:  leave.s    IL_000a
}
  finally
{
  IL_0008:  br.s       IL_0008
}
  IL_000a:  br.s       IL_000a
}
");
        }

        [Fact()]
        public void ReachableAfterFinallyButNotFromTryConditional01()
        {
            var source = @"
using System;

class Program
{
    static int Result = 0;
    static void F()
    {
        Result = 1;
    }

    static void T1()
    {
        if (Result == 1)
            goto L;

        try
        {
            try
            {
                if (Result == 0)
                    goto L;

                goto L;
            }
            finally
            {
                for (; ; ) { }
            }

            if (Result == 0)
                goto L;
        }
        finally
        {
            for (; ; ) { }
        }

    L:
        F();        //reachable but not from the try
    }

    static void Main()
    {
        Console.Write(Result);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0");
            compilation.VerifyIL("Program.T1",
@"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""int Program.Result""
  IL_0005:  ldc.i4.1
  IL_0006:  beq.s      IL_0017
  IL_0008:  nop
  .try
{
  .try
{
  IL_0009:  ldsfld     ""int Program.Result""
  IL_000e:  pop
  IL_000f:  leave.s    IL_0013
}
  finally
{
  IL_0011:  br.s       IL_0011
}
  IL_0013:  br.s       IL_0013
}
  finally
{
  IL_0015:  br.s       IL_0015
}
  IL_0017:  call       ""void Program.F()""
  IL_001c:  ret
}
");
        }

        [Fact(), WorkItem(544911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544911")]
        public void ReachableAfterFinallyButNotFromTryConditional()
        {
            var source = @"
using System;

class Program
{
    static int Result = 0;
    static void F()
    {
        Result = 1;
    }

    static void T1()
    {
        if (Result == 1)
            goto L;

        try
        {
            try
            {
                if (Result == 0)
                    goto L;
            }
            finally
            {
                for (; ; ) { }
            }

            if (Result == 0)
                goto L;
        }
        finally
        {
            for (; ; ) { }
        }

    L:
        F();        //reachable but not from the try
    }

    static void Main()
    {
        Console.Write(Result);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0");
            compilation.VerifyIL("Program.T1",
@"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""int Program.Result""
  IL_0005:  ldc.i4.1
  IL_0006:  beq.s      IL_0017
  IL_0008:  nop
  .try
{
  .try
{
  IL_0009:  ldsfld     ""int Program.Result""
  IL_000e:  pop
  IL_000f:  leave.s    IL_0013
}
  finally
{
  IL_0011:  br.s       IL_0011
}
  IL_0013:  br.s       IL_0013
}
  finally
{
  IL_0015:  br.s       IL_0015
}
  IL_0017:  call       ""void Program.F()""
  IL_001c:  ret
}
");
        }

        [Fact(), WorkItem(713418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713418")]
        public void ConditionalUnconditionalBranches()
        {
            var source = @"
using System;

    class Program
    {
        private static int x = 123;

        static void Main(string[] args)
        {
            try
            {
                if (x == 10)
                {
                    goto l2;;
                }


                if (x != 0)
                {
                    Console.WriteLine(""2"");
                }
                goto l1;

l2:
                Console.WriteLine(""l2"");

l1:
                goto lOut;
            }
            finally
            {
                Console.WriteLine(""Finally"");
            }

lOut:
            Console.WriteLine(""Out"");
        }
    }
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
Finally
Out");
            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       62 (0x3e)
  .maxstack  2
  .try
{
  IL_0000:  ldsfld     ""int Program.x""
  IL_0005:  ldc.i4.s   10
  IL_0007:  beq.s      IL_001c
  IL_0009:  ldsfld     ""int Program.x""
  IL_000e:  brfalse.s  IL_0026
  IL_0010:  ldstr      ""2""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  leave.s    IL_0033
  IL_001c:  ldstr      ""l2""
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  leave.s    IL_0033
}
  finally
{
  IL_0028:  ldstr      ""Finally""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  endfinally
}
  IL_0033:  ldstr      ""Out""
  IL_0038:  call       ""void System.Console.WriteLine(string)""
  IL_003d:  ret
}
");
        }

        [Fact(), WorkItem(713418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713418")]
        public void ConditionalUnconditionalBranches001()
        {
            var source = @"
using System;

    class Program
    {
        private static int x = 123;

        static void Main(string[] args)
        {
            try
            {
                if (x == 10)
                {
                    goto l2;;
                }


                if (x != 0)
                {
                    Console.WriteLine(""2"");
                }
                goto l1;

l2:
                // Console.WriteLine(""l2"");

l1:
                goto lOut;
            }
            finally
            {
                Console.WriteLine(""Finally"");
            }

lOut:
            Console.WriteLine(""Out"");
        }
    }
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
Finally
Out");
            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       52 (0x34)
  .maxstack  2
  .try
{
  IL_0000:  ldsfld     ""int Program.x""
  IL_0005:  ldc.i4.s   10
  IL_0007:  bne.un.s   IL_000b
  IL_0009:  leave.s    IL_0029
  IL_000b:  ldsfld     ""int Program.x""
  IL_0010:  brfalse.s  IL_001c
  IL_0012:  ldstr      ""2""
  IL_0017:  call       ""void System.Console.WriteLine(string)""
  IL_001c:  leave.s    IL_0029
}
  finally
{
  IL_001e:  ldstr      ""Finally""
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  endfinally
}
  IL_0029:  ldstr      ""Out""
  IL_002e:  call       ""void System.Console.WriteLine(string)""
  IL_0033:  ret
}
");
        }

        [Fact(), WorkItem(2443, "https://github.com/dotnet/roslyn/issues/2443")]
        public void OptimizeEmptyTryBlock()
        {
            var source = @"
using System;

class Program
{
    public static void Main(string[] args)
    {
        try
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"");
            compilation.VerifyIL("Program.Main",
@"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
");
        }

        [Fact]
        [WorkItem(29481, "https://github.com/dotnet/roslyn/issues/29481")]
        public void Issue29481()
        {
            var source = @"
using System;

public class Program
{
    public static void Main()
    {
        try
        {
            bool b = false;
            if (b)
            {
                try
                {
                    return;
                }
                finally
                {
                    Console.WriteLine(""Prints"");
                }
            }
            else
            {
                return;
            }
        }
        finally
        {
            GC.KeepAlive(null);
        }
    }
}";

            CompileAndVerify(source, expectedOutput: "", options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: "", options: TestOptions.ReleaseExe).VerifyIL("Program.Main",
@"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .try
  {
    IL_0000:  ldc.i4.0
    IL_0001:  brfalse.s  IL_0010
    .try
    {
      IL_0003:  leave.s    IL_0019
    }
    finally
    {
      IL_0005:  ldstr      ""Prints""
      IL_000a:  call       ""void System.Console.WriteLine(string)""
      IL_000f:  endfinally
    }
    IL_0010:  leave.s    IL_0019
  }
  finally
  {
    IL_0012:  ldnull
    IL_0013:  call       ""void System.GC.KeepAlive(object)""
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void WhenWithAlwaysThrowingExpression_01()
        {
            var source = """
class C
{
    static bool _throw;

    static void Main()
    {
        M();

        _throw = true;

        try 
        {
            M();
        }
        catch (System.NullReferenceException)
        {
            System.Console.Write("Catch");
        }
    }

    static void M()
    {
        try
        {
            M1();
        }
        catch when (true ? throw M2() : true)
        {
            M3();
        }

        M4();
    }

    static void M1()
    {
        System.Console.Write("M1");
        if (_throw) throw null;
    }

    static System.Exception M2()
    {
        System.Console.Write("M2");
        return new System.NotSupportedException();
    }

    static void M3()
    {
        System.Console.Write("M3");
    }

    static void M4()
    {
        System.Console.Write("M4");
    }
}
""";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "M1M4M1M2Catch",
                // False PEVerify failure:
                verify: Verification.FailsPEVerify with
                {
                    PEVerifyMessage = "[ : C::M][offset 0x0000000E] Stack not empty when leaving an exception filter."
                }).VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       30 (0x1e)
  .maxstack  2
  .try
  {
    IL_0000:  call       "void C.M1()"
    IL_0005:  leave.s    IL_0018
  }
  filter
  {
    IL_0007:  pop
    IL_0008:  call       "System.Exception C.M2()"
    IL_000d:  throw
    IL_000e:  endfilter
  }  // end filter
  {  // handler
    IL_0010:  pop
    IL_0011:  call       "void C.M3()"
    IL_0016:  leave.s    IL_0018
  }
  IL_0018:  call       "void C.M4()"
  IL_001d:  ret
}
""");

            verifier = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: "M1M4M1M2Catch",
                // False PEVerify failure:
                verify: Verification.FailsPEVerify with
                {
                    PEVerifyMessage = "[ : C::M][offset 0x00000012] Stack not empty when leaving an exception filter."
                }).VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  call       "void C.M1()"
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  leave.s    IL_001f
  }
  filter
  {
    IL_000b:  pop
    IL_000c:  call       "System.Exception C.M2()"
    IL_0011:  throw
    IL_0012:  endfilter
  }  // end filter
  {  // handler
    IL_0014:  pop
    IL_0015:  nop
    IL_0016:  call       "void C.M3()"
    IL_001b:  nop
    IL_001c:  nop
    IL_001d:  leave.s    IL_001f
  }
  IL_001f:  call       "void C.M4()"
  IL_0024:  nop
  IL_0025:  ret
}
""");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void WhenWithAlwaysThrowingExpression_02()
        {
            var source = """
class C
{
    static void M()
    {
        try
        {
            M1();
        }
        catch (System.ArgumentException) when (true ? throw null : true)
        {
            M2();
        }
    }

    static void M1(){}
    static void M2(){}
}
""";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseDll).VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       33 (0x21)
  .maxstack  2
  .try
  {
    IL_0000:  call       "void C.M1()"
    IL_0005:  leave.s    IL_0020
  }
  filter
  {
    IL_0007:  isinst     "System.ArgumentException"
    IL_000c:  dup
    IL_000d:  brtrue.s   IL_0013
    IL_000f:  pop
    IL_0010:  ldc.i4.0
    IL_0011:  br.s       IL_0016
    IL_0013:  pop
    IL_0014:  ldnull
    IL_0015:  throw
    IL_0016:  endfilter
  }  // end filter
  {  // handler
    IL_0018:  pop
    IL_0019:  call       "void C.M2()"
    IL_001e:  leave.s    IL_0020
  }
  IL_0020:  ret
}
""");

            verifier = CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  call       "void C.M1()"
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  leave.s    IL_0027
  }
  filter
  {
    IL_000b:  isinst     "System.ArgumentException"
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_001a
    IL_0017:  pop
    IL_0018:  ldnull
    IL_0019:  throw
    IL_001a:  endfilter
  }  // end filter
  {  // handler
    IL_001c:  pop
    IL_001d:  nop
    IL_001e:  call       "void C.M2()"
    IL_0023:  nop
    IL_0024:  nop
    IL_0025:  leave.s    IL_0027
  }
  IL_0027:  ret
}
""");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void WhenWithAlwaysThrowingExpression_03()
        {
            var source = """
class C
{
    static void M()
    {
        try
        {
            M1();
        }
        catch when (throw null)
        {
            M2();
        }
    }

    static void M1(){}
    static void M2(){}
}
""";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (9,21): error CS8115: A throw expression is not allowed in this context.
                //         catch when (throw null)
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(9, 21)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void WhenWithAlwaysThrowingExpression_04()
        {
            var source = """
class C
{
    static void M()
    {
        try
        {
            M1();
        }
        catch (System.ArgumentException) when (throw null)
        {
            M2();
        }
    }

    static void M1(){}
    static void M2(){}
}
""";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (9,48): error CS8115: A throw expression is not allowed in this context.
                //         catch (System.ArgumentException) when (throw null)
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(9, 48)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void UnreachableCatch_01()
        {
            var source = """
class C
{
    static void M()
    {
        try
        {
            M1();
        }
        catch when (false)
        {
            M2();
        }
    }

    static void M1(){}
    static void M2(){}
}
""";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (9,21): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(9, 21),
                // (11,13): warning CS0162: Unreachable code detected
                //             M2();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "M2").WithLocation(11, 13)
                );

            verifier.VerifyIL("C.M", """
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       "void C.M1()"
  IL_0005:  ret
}
""");

            verifier = CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics(
                // (9,21): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(9, 21),
                // (11,13): warning CS0162: Unreachable code detected
                //             M2();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "M2").WithLocation(11, 13)
                );
            verifier.VerifyIL("C.M", """
{
  // Code size       10 (0xa)
  .maxstack  0
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       "void C.M1()"
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ret
}
""");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void UnreachableCatch_02()
        {
            var source = """
class C
{
    static void M()
    {
        try
        {
            M1();
        }
        catch (System.ArgumentException) when (false)
        {
            M2();
        }
    }

    static void M1(){}
    static void M2(){}
}
""";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (9,48): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch (System.ArgumentException) when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(9, 48),
                // (11,13): warning CS0162: Unreachable code detected
                //             M2();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "M2").WithLocation(11, 13)
                );
            verifier.VerifyIL("C.M", """
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       "void C.M1()"
  IL_0005:  ret
}
""");

            verifier = CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics(
                // (9,48): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch (System.ArgumentException) when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(9, 48),
                // (11,13): warning CS0162: Unreachable code detected
                //             M2();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "M2").WithLocation(11, 13)
                );
            verifier.VerifyIL("C.M", """
{
  // Code size       10 (0xa)
  .maxstack  0
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       "void C.M1()"
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ret
}
""");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67494")]
        public void UnreachableCatch_03()
        {
            var source = """
class C
{
    static void M()
    {
        try
        {
            M1();
        }
        catch when (true ? false : true)
        {
            M2();
        }
    }

    static void M1(){}
    static void M2(){}
}
""";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (9,21): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch when (true ? false : true)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "true ? false : true").WithLocation(9, 21),
                // (11,13): warning CS0162: Unreachable code detected
                //             M2();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "M2").WithLocation(11, 13)
                );

            verifier.VerifyIL("C.M", """
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       "void C.M1()"
  IL_0005:  ret
}
""");

            verifier = CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics(
                // (9,21): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch when (true ? false : true)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "true ? false : true").WithLocation(9, 21),
                // (11,13): warning CS0162: Unreachable code detected
                //             M2();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "M2").WithLocation(11, 13)
                );
            verifier.VerifyIL("C.M", """
{
  // Code size       10 (0xa)
  .maxstack  0
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       "void C.M1()"
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70200")]
        public void Repro70200()
        {
            var source = """
using System;
using System.Threading.Tasks;

class C
{
    async Task M()
    {
        return;

        try
        {
            Console.WriteLine(string.Empty);
        }
        catch (Exception e1) when (e1.InnerException is Exception { InnerException: { } e2 })
        {
            Console.WriteLine(e2.Message);
        }
    }
}
""";

            var expectedDiagnostics = new[]
            {
                // (6,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async Task M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(6, 16),
                // (10,9): warning CS0162: Unreachable code detected
                //         try
                Diagnostic(ErrorCode.WRN_UnreachableCode, "try").WithLocation(10, 9)
            };

            CompileAndVerify(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(expectedDiagnostics);
            CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70200")]
        public void Repro70200_WithoutReturn()
        {
            var source = """
using System;
using System.Threading.Tasks;

class C
{
    async Task M()
    {
        try
        {
            Console.WriteLine(string.Empty);
        }
        catch (Exception e1) when (e1.InnerException is Exception { InnerException: { } e2 })
        {
            Console.WriteLine(e2.Message);
        }
    }
}
""";

            CompileAndVerify(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async Task M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(6, 16)
                );
        }
    }
}
