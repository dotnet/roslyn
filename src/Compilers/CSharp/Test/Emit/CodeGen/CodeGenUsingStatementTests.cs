// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class UsingStatementTests : EmitMetadataTestBase
    {
        #region "From UsingStatementTests"
        private const string DisposableClass = @"
public class DisposableClass : System.IDisposable
{
    private readonly string name;

    public DisposableClass(string name) 
    {
        this.name = name;
        System.Console.WriteLine(""Creating "" + name);
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing "" + name);
    }
}
";

        private const string DisposableStruct = @"
public struct DisposableStruct : System.IDisposable
{
    private readonly string name;

    public DisposableStruct(string name) 
    {
        this.name = name;
        System.Console.WriteLine(""Creating "" + name);
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing "" + name);
    }
}
";

        [Fact]
        public void UsingExpressionNull()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (null)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
In
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""In""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""After""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void UsingExpressionNullConstant()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        const IDisposable o = null;
        using (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
In
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""In""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""After""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void UsingExpressionNullable()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (new DisposableClass(""A""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass;
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
In
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (DisposableClass V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""A""
  IL_000f:  newobj     ""DisposableClass..ctor(string)""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  ldstr      ""In""
    IL_001a:  call       ""void System.Console.WriteLine(string)""
    IL_001f:  leave.s    IL_002b
  }
  finally
  {
    IL_0021:  ldloc.0
    IL_0022:  brfalse.s  IL_002a
    IL_0024:  ldloc.0
    IL_0025:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002a:  endfinally
  }
  IL_002b:  ldstr      ""After""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
}");
        }

        [Fact]
        public void UsingExpressionNonNullable()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (new DisposableStruct(""A""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableStruct;
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
In
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (DisposableStruct V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  ldstr      ""A""
  IL_0011:  call       ""DisposableStruct..ctor(string)""
  .try
{
  IL_0016:  ldstr      ""In""
  IL_001b:  call       ""void System.Console.WriteLine(string)""
  IL_0020:  leave.s    IL_0030
}
  finally
{
  IL_0022:  ldloca.s   V_0
  IL_0024:  constrained. ""DisposableStruct""
  IL_002a:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002f:  endfinally
}
  IL_0030:  ldstr      ""After""
  IL_0035:  call       ""void System.Console.WriteLine(string)""
  IL_003a:  ret
}");
        }

        [Fact]
        public void UsingExpressionUnconstrainedTypeParameter()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        M<DisposableClass>(new DisposableClass(""A""));
        M<DisposableClass>(null);
        M<DisposableStruct>(new DisposableStruct(""B""));
    }

    static void M<T>(T t) where T : IDisposable
    {
        System.Console.WriteLine(""Before"");
        using (t)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass
  + DisposableStruct;

            string expected = @"Creating A
Before
In
Disposing A
After
Before
In
After
Creating B
Before
In
Disposing B
After";

            var verifier = CompileAndVerify(text, expectedOutput: expected);
            verifier.VerifyIL("Test.M<T>", @"
{
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  .try
  {
    IL_000c:  ldstr      ""In""
    IL_0011:  call       ""void System.Console.WriteLine(string)""
    IL_0016:  leave.s    IL_002e
  }
  finally
  {
    IL_0018:  ldloc.0
    IL_0019:  box        ""T""
    IL_001e:  brfalse.s  IL_002d
    IL_0020:  ldloca.s   V_0
    IL_0022:  constrained. ""T""
    IL_0028:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002d:  endfinally
  }
  IL_002e:  ldstr      ""After""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ret
}");
        }

        [Fact]
        public void UsingExpressionNonNullableTypeParameter()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        M(new DisposableStruct(""A""));
    }

    static void M<T>(T t) where T : struct, IDisposable
    {
        System.Console.WriteLine(""Before"");
        using (t)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableStruct;
            var verifier = CompileAndVerify(text, expectedOutput: @"Creating A
Before
In
Disposing A
After");
            verifier.VerifyIL("Test.M<T>", @"
{
  // Code size       49 (0x31)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  .try
  {
    IL_000c:  ldstr      ""In""
    IL_0011:  call       ""void System.Console.WriteLine(string)""
    IL_0016:  leave.s    IL_0026
  }
  finally
  {
    IL_0018:  ldloca.s   V_0
    IL_001a:  constrained. ""T""
    IL_0020:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0025:  endfinally
  }
  IL_0026:  ldstr      ""After""
  IL_002b:  call       ""void System.Console.WriteLine(string)""
  IL_0030:  ret
}");
        }

        [Fact]
        public void UsingDeclarationNull()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (IDisposable i = null)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
In
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""In""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""After""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void UsingDeclarationNullConstant()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        const IDisposable o = null;
        using (IDisposable i = o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
In
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""In""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""After""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void UsingDeclarationNullable()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (DisposableClass d = new DisposableClass(""A""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass;
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
In
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (DisposableClass V_0) //d
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""A""
  IL_000f:  newobj     ""DisposableClass..ctor(string)""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  ldstr      ""In""
    IL_001a:  call       ""void System.Console.WriteLine(string)""
    IL_001f:  leave.s    IL_002b
  }
  finally
  {
    IL_0021:  ldloc.0
    IL_0022:  brfalse.s  IL_002a
    IL_0024:  ldloc.0
    IL_0025:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002a:  endfinally
  }
  IL_002b:  ldstr      ""After""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
}");
        }

        [Fact]
        public void UsingDeclarationNonNullable()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (DisposableStruct d = new DisposableStruct(""A""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableStruct;
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
In
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (DisposableStruct V_0) //d
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  ldstr      ""A""
  IL_0011:  call       ""DisposableStruct..ctor(string)""
  .try
  {
    IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
    IL_0020:  leave.s    IL_0030
  }
  finally
  {
    IL_0022:  ldloca.s   V_0
    IL_0024:  constrained. ""DisposableStruct""
    IL_002a:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002f:  endfinally
  }
  IL_0030:  ldstr      ""After""
  IL_0035:  call       ""void System.Console.WriteLine(string)""
  IL_003a:  ret
}");
        }

        [Fact]
        public void UsingDeclarationNullableTypeParameter()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        M<DisposableClass>(new DisposableClass(""A""));
        M<DisposableClass>(null);
        M<DisposableStruct>(new DisposableStruct(""B""));
    }

    static void M<T>(T t) where T : IDisposable
    {
        System.Console.WriteLine(""Before"");
        using (T d = t)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass
  + DisposableStruct;

            string expected = @"Creating A
Before
In
Disposing A
After
Before
In
After
Creating B
Before
In
Disposing B
After";

            var verifier = CompileAndVerify(text, expectedOutput: expected);
            verifier.VerifyIL("Test.M<T>", @"
{
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (T V_0) //d
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  .try
  {
    IL_000c:  ldstr      ""In""
    IL_0011:  call       ""void System.Console.WriteLine(string)""
    IL_0016:  leave.s    IL_002e
  }
  finally
  {
    IL_0018:  ldloc.0
    IL_0019:  box        ""T""
    IL_001e:  brfalse.s  IL_002d
    IL_0020:  ldloca.s   V_0
    IL_0022:  constrained. ""T""
    IL_0028:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002d:  endfinally
  }
  IL_002e:  ldstr      ""After""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ret
}");
        }

        [Fact]
        public void UsingDeclarationNonNullableTypeParameter()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        M(new DisposableStruct(""A""));
    }

    static void M<T>(T t) where T : struct, IDisposable
    {
        System.Console.WriteLine(""Before"");
        using (T d = t)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableStruct;
            var verifier = CompileAndVerify(text, expectedOutput: @"Creating A
Before
In
Disposing A
After");
            verifier.VerifyIL("Test.M<T>", @"
{
  // Code size       49 (0x31)
  .maxstack  1
  .locals init (T V_0) //d
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  .try
  {
    IL_000c:  ldstr      ""In""
    IL_0011:  call       ""void System.Console.WriteLine(string)""
    IL_0016:  leave.s    IL_0026
  }
  finally
  {
    IL_0018:  ldloca.s   V_0
    IL_001a:  constrained. ""T""
    IL_0020:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0025:  endfinally
  }
  IL_0026:  ldstr      ""After""
  IL_002b:  call       ""void System.Console.WriteLine(string)""
  IL_0030:  ret
}");
        }

        [Fact]
        public void UsingDeclarationMultipleNullable()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (DisposableClass d1 = new DisposableClass(""A""), d2 = new DisposableClass(""B""), d3 = new DisposableClass(""C""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass;
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
Creating B
Creating C
In
Disposing C
Disposing B
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       96 (0x60)
  .maxstack  1
  .locals init (DisposableClass V_0, //d1
                DisposableClass V_1, //d2
                DisposableClass V_2) //d3
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""A""
  IL_000f:  newobj     ""DisposableClass..ctor(string)""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  ldstr      ""B""
    IL_001a:  newobj     ""DisposableClass..ctor(string)""
    IL_001f:  stloc.1
    .try
    {
      IL_0020:  ldstr      ""C""
      IL_0025:  newobj     ""DisposableClass..ctor(string)""
      IL_002a:  stloc.2
      .try
      {
        IL_002b:  ldstr      ""In""
        IL_0030:  call       ""void System.Console.WriteLine(string)""
        IL_0035:  leave.s    IL_0055
      }
      finally
      {
        IL_0037:  ldloc.2
        IL_0038:  brfalse.s  IL_0040
        IL_003a:  ldloc.2
        IL_003b:  callvirt   ""void System.IDisposable.Dispose()""
        IL_0040:  endfinally
      }
    }
    finally
    {
      IL_0041:  ldloc.1
      IL_0042:  brfalse.s  IL_004a
      IL_0044:  ldloc.1
      IL_0045:  callvirt   ""void System.IDisposable.Dispose()""
      IL_004a:  endfinally
    }
  }
  finally
  {
    IL_004b:  ldloc.0
    IL_004c:  brfalse.s  IL_0054
    IL_004e:  ldloc.0
    IL_004f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0054:  endfinally
  }
  IL_0055:  ldstr      ""After""
  IL_005a:  call       ""void System.Console.WriteLine(string)""
  IL_005f:  ret
}");
        }

        [Fact]
        public void UsingDeclarationMultipleNonNullable()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (DisposableStruct d1 = new DisposableStruct(""A""), d2 = new DisposableStruct(""B""), d3 = new DisposableStruct(""C""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableStruct;
            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
Creating B
Creating C
In
Disposing C
Disposing B
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size      109 (0x6d)
  .maxstack  2
  .locals init (DisposableStruct V_0, //d1
                DisposableStruct V_1, //d2
                DisposableStruct V_2) //d3
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  ldstr      ""A""
  IL_0011:  call       ""DisposableStruct..ctor(string)""
  .try
  {
    IL_0016:  ldstr      ""B""
    IL_001b:  newobj     ""DisposableStruct..ctor(string)""
    IL_0020:  stloc.1
    .try
    {
      IL_0021:  ldstr      ""C""
      IL_0026:  newobj     ""DisposableStruct..ctor(string)""
      IL_002b:  stloc.2
      .try
      {
        IL_002c:  ldstr      ""In""
        IL_0031:  call       ""void System.Console.WriteLine(string)""
        IL_0036:  leave.s    IL_0062
      }
      finally
      {
        IL_0038:  ldloca.s   V_2
        IL_003a:  constrained. ""DisposableStruct""
        IL_0040:  callvirt   ""void System.IDisposable.Dispose()""
        IL_0045:  endfinally
      }
    }
    finally
    {
      IL_0046:  ldloca.s   V_1
      IL_0048:  constrained. ""DisposableStruct""
      IL_004e:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0053:  endfinally
    }
  }
  finally
  {
    IL_0054:  ldloca.s   V_0
    IL_0056:  constrained. ""DisposableStruct""
    IL_005c:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0061:  endfinally
  }
  IL_0062:  ldstr      ""After""
  IL_0067:  call       ""void System.Console.WriteLine(string)""
  IL_006c:  ret
}");
        }

        [Fact]
        public void UsingDeclarationMultipleSomeNull()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (DisposableClass d1 = new DisposableClass(""A""), d2 = null, d3 = new DisposableClass(""C""))
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass + DisposableStruct;

            var verifier = CompileAndVerify(text, expectedOutput: @"Before
Creating A
Creating C
In
Disposing C
Disposing A
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       75 (0x4b)
  .maxstack  1
  .locals init (DisposableClass V_0, //d1
                DisposableClass V_1) //d3
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""A""
  IL_000f:  newobj     ""DisposableClass..ctor(string)""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  ldstr      ""C""
    IL_001a:  newobj     ""DisposableClass..ctor(string)""
    IL_001f:  stloc.1
    .try
    {
      IL_0020:  ldstr      ""In""
      IL_0025:  call       ""void System.Console.WriteLine(string)""
      IL_002a:  leave.s    IL_0040
    }
    finally
    {
      IL_002c:  ldloc.1
      IL_002d:  brfalse.s  IL_0035
      IL_002f:  ldloc.1
      IL_0030:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0035:  endfinally
    }
  }
  finally
  {
    IL_0036:  ldloc.0
    IL_0037:  brfalse.s  IL_003f
    IL_0039:  ldloc.0
    IL_003a:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003f:  endfinally
  }
  IL_0040:  ldstr      ""After""
  IL_0045:  call       ""void System.Console.WriteLine(string)""
  IL_004a:  ret
}");
        }

        [Fact]
        public void UsingDeclarationMultipleAllNull()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(""Before"");
        using (DisposableClass d1 = null, d2 = null, d3 = null)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
" + DisposableClass + DisposableStruct;

            var verifier = CompileAndVerify(text, expectedOutput: @"Before
In
After");
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""In""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""After""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ret
}");
        }

        [Fact, WorkItem(543249, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543249")]
        public void UsingInCatchBlock()
        {
            var text = @"
class Test
{
    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch
        {
            using (var d = new DisposeImpl()) { }
        }
        System.Console.Write(""2"");
    }

    class DisposeImpl : System.IDisposable
    {
        public void Dispose() { System.Console.Write(""1""); }
    }
}";
            CompileAndVerify(text, expectedOutput: "12");
        }
        #endregion

        // The object could be created inside the "using" statement 
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void ObjectCreateInsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedClass mnObj = new MyManagedClass())
        {
            mnObj.Use();
        }
    }
    class MyManagedClass : System.IDisposable
    {
        public void Dispose()
        { }
        public void Use() { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0) //mnObj
  // sequence point: MyManagedClass mnObj = new MyManagedClass()
  IL_0000:  newobj     ""Program.MyManagedClass..ctor()""
  IL_0005:  stloc.0
  .try
  {
    // sequence point: mnObj.Use();
    IL_0006:  ldloc.0
    IL_0007:  callvirt   ""void Program.MyManagedClass.Use()""
    // sequence point: }
    IL_000c:  leave.s    IL_0018
  }
  finally
  {
    // sequence point: <hidden>
    IL_000e:  ldloc.0
    IL_000f:  brfalse.s  IL_0017
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
    // sequence point: <hidden>
    IL_0017:  endfinally
  }
  // sequence point: }
  IL_0018:  ret
}", sequencePoints: "Program.Main", source: source);
        }

        [Fact]
        public void UsingPatternTest()
        {
            var source = @"
using System;
ref struct S1
{
    public void Dispose()
    {
        Console.WriteLine(""S1.Dispose()"");
    }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "S1.Dispose()").VerifyIL("C2.Main()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (S1 V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  .try
  {
    IL_0008:  leave.s    IL_0012
  }
  finally
  {
    IL_000a:  ldloca.s   V_0
    IL_000c:  call       ""void S1.Dispose()""
    IL_0011:  endfinally
  }
  IL_0012:  ret
}
");
        }

        [Fact]
        public void UsingPatternDiffParameterOverloadTest()
        {
            var source = @"
using System;
ref struct S1
{
    public void Dispose()
    {
        Console.WriteLine(""S1.Dispose()"");
    }
    public void Dispose(int x) 
    {
        Console.WriteLine(""S1.Dispose(int)"");
    }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "S1.Dispose()").VerifyIL("C2.Main()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (S1 V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  .try
  {
    IL_0008:  leave.s    IL_0012
  }
  finally
  {
    IL_000a:  ldloca.s   V_0
    IL_000c:  call       ""void S1.Dispose()""
    IL_0011:  endfinally
  }
  IL_0012:  ret
}
"
            );
        }

        [Fact]
        public void UsingPatternExtensionMethodTest()
        {
            var source = @"
ref struct S1
{
}

static class C2
{
    public static void Dispose(this S1 s1) 
    {
    }
}

class C3
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (17,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(17, 16)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodResolutionTest()
        {
            var source = @"
using System;
ref struct S1
{
    public void Dispose()
    {
        Console.WriteLine(""S1.Dispose()"");
    }
}

static class C2
{
    public static void Dispose(this S1 s1)
    { 
        Console.WriteLine(""C2.Dispose(S1)"");
    }
}

class C3
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "S1.Dispose()").VerifyIL("C3.Main()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (S1 V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  .try
  {
    IL_0008:  leave.s    IL_0012
  }
  finally
  {
    IL_000a:  ldloca.s   V_0
    IL_000c:  call       ""void S1.Dispose()""
    IL_0011:  endfinally
  }
  IL_0012:  ret
}
");
        }

        [Fact]
        public void UsingPatternExtensionMethodWithDefaultArguments()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
    internal static void Dispose(this S1 s1, int a = 1) { }
}

class C3
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodWithParams()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
    internal static void Dispose(this S1 s1, params int[] args) { }
}

class C3
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15)
                );
        }

        // The object could be created outside the "using" statement 
        [Fact]
        public void ObjectCreateOutsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        MyManagedClass mnObj1 = new MyManagedClass();
        using (mnObj1)
        {
            mnObj1.Use(); 
        } 
    }
    class MyManagedClass : System.IDisposable
    {
        public void Dispose()
        { }
        public void Use() { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0, //mnObj1
  Program.MyManagedClass V_1)
  IL_0000:  newobj     ""Program.MyManagedClass..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  callvirt   ""void Program.MyManagedClass.Use()""
  IL_000e:  leave.s    IL_001a
}
  finally
{
  IL_0010:  ldloc.1
  IL_0011:  brfalse.s  IL_0019
  IL_0013:  ldloc.1
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  endfinally
}
  IL_001a:  ret
}");
        }

        // The object could both be created outside the "using" statement 
        [Fact]
        public void NoVarNameInsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        MyManagedClass mnObj1 = new MyManagedClass();
        using (mnObj1)
        {
            mnObj1.Use(); 
        } 
    }
    class MyManagedClass : System.IDisposable
    {
        public void Dispose()
        { }
        public void Use() { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0, //mnObj1
  Program.MyManagedClass V_1)
  IL_0000:  newobj     ""Program.MyManagedClass..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  callvirt   ""void Program.MyManagedClass.Use()""
  IL_000e:  leave.s    IL_001a
}
  finally
{
  IL_0010:  ldloc.1
  IL_0011:  brfalse.s  IL_0019
  IL_0013:  ldloc.1
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  endfinally
}
  IL_001a:  ret
}");
        }

        // Take the type parameter as object
        [Fact]
        public void TypeParameterAsUsingResource()
        {
            var source = @"
class Gen<T>
{
	public static void TestUsing(T obj)
	{
		using (obj)
		{
		}
	}
}
";
            CreateCompilation(source).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoConvToIDisp, "obj").WithArguments("T"));
        }

        [Fact]
        public void TypeParameterAsUsingResource_2()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Test<T>
{
    public static IEnumerator<T> M<U>(IEnumerable<T> items) where U : IDisposable, new()
    {
        T val = default(T);
        U u = default(U);
        using (u = new U())
        {
        }
        using (val)
        {
        }
        return null;
    }
}

class Gen<T> where T : new()
{
    public static void TestUsing()
    {
        using (T disp = new T()) // Invalid
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,16): error CS1674: 'T': type used in a using statement must implement 'System.IDisposable'.
                //         using (val)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "val").WithArguments("T"),
                // (24,16): error CS1674: 'T': type used in a using statement must implement 'System.IDisposable'.
                //         using (T disp = new T()) // Invalid
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "T disp = new T()").WithArguments("T"));
        }

        // Take the out parameter as object
        [Fact]
        public void OutParameterAsUsingResource()
        {
            var source = @"
class Program
{
    static void goo(ref MyManagedClass x, out MyManagedClass y)
    {
        using (x)
        {
        }
        using (y) // Invalid
        {
        }
        y = new MyManagedClass();
    }
    class MyManagedClass : System.IDisposable
    {
        public void Dispose()
        { }
        public void Use() { }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y").WithArguments("y"));
        }

        // Doesn't implement IDisposable, but has a Dispose() function
        [Fact]
        public void NotImplementIDispose()
        {
            var source = @"
class Program
{
    static void goo()
    {
        MyManagedClass res = new MyManagedClass();
        using (res) // Invalid
        {
        }
    }
    class MyManagedClass 
    {
        public void Dispose()
        { }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,16): error CS1674: 'Program.MyManagedClass': type used in a using statement must implement 'System.IDisposable'.
                //         using (res) // Invalid
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "res").WithArguments("Program.MyManagedClass").WithLocation(7, 16)
                );
        }

        [Fact]
        public void NotImplementIDispose_Struct()
        {
            var source = @"
class Program
{
    static void goo()
    {
        MyManagedClass res = new MyManagedClass();
        using (res) // Invalid
        {
        }
    }
    struct MyManagedClass
    {
        public void Dispose()
        { }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,16): error CS1674: 'Program.MyManagedClass': type used in a using statement must implement 'System.IDisposable'.
                //         using (res) // Invalid
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "res").WithArguments("Program.MyManagedClass").WithLocation(7, 16)
                );
        }

        // Implicit implement IDisposable
        [Fact]
        public void ImplicitImplementIDispose()
        {
            var source = @"
using System;
class Program
{
    static void goo()
    {
        MyManagedClass res = new MyManagedClass();
        using (res) 
        {
        }
    }
    class MyManagedClass :IDisposable
    {
        public void  Dispose() 
        { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.goo", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0)
  IL_0000:  newobj     ""Program.MyManagedClass..ctor()""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  leave.s    IL_0012
}
  finally
{
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0011
  IL_000b:  ldloc.0
  IL_000c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0011:  endfinally
}
  IL_0012:  ret
}");
        }

        [Fact]
        public void ImplicitImplementIDispose_Struct()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        MyManagedClass res = new MyManagedClass();
        using (res) 
        {
        }
    }
    struct MyManagedClass :IDisposable
    {
        public void  Dispose() 
        { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.MyManagedClass""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  .try
{
  IL_000a:  leave.s    IL_001a
}
  finally
{
  IL_000c:  ldloca.s   V_0
  IL_000e:  constrained. ""Program.MyManagedClass""
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  endfinally
}
  IL_001a:  ret
}");
        }

        // Explicit implement IDisposable
        [Fact]
        public void ExplicitImplementIDispose()
        {
            var source = @"
using System;
class Program
{
    static void goo()
    {
        MyManagedClass res = new MyManagedClass();
        using (res) 
        {
        }
    }
    class MyManagedClass :IDisposable
    {
        public void  Dispose() 
        { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.goo", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0)
  IL_0000:  newobj     ""Program.MyManagedClass..ctor()""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  leave.s    IL_0012
}
  finally
{
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0011
  IL_000b:  ldloc.0
  IL_000c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0011:  endfinally
}
  IL_0012:  ret
}");
        }

        [Fact]
        public void ExplicitImplementIDispose_Struct()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        MyManagedClass res = new MyManagedClass();
        using (res) 
        {
        }
    }
    struct MyManagedClass :IDisposable
    {
        public void  Dispose() 
        { }
    }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Program.MyManagedClass V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.MyManagedClass""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  .try
{
  IL_000a:  leave.s    IL_001a
}
  finally
{
  IL_000c:  ldloca.s   V_0
  IL_000e:  constrained. ""Program.MyManagedClass""
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  endfinally
}
  IL_001a:  ret
}");
        }

        // The variable declared out of  using is not read-only
        [Fact]
        public void VarDeclaredOutsideIsNotReadOnly()
        {
            var source = @"
using System;
class Program
{
    static Res x;
    static void Main(string[] args)
    {
        using (x)
        {
            x = new Res(); // OK
        }
    }
}
class Res : IDisposable
{
    void IDisposable.Dispose()
    { }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (Res V_0)
  IL_0000:  ldsfld     ""Res Program.x""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  newobj     ""Res..ctor()""
  IL_000b:  stsfld     ""Res Program.x""
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.0
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001b:  endfinally
}
  IL_001c:  ret
}");
        }

        // The used variable is nulled-out in the using block
        [Fact]
        public void NulledVarDeclaredInUsing()
        {
            var source = @"
using System;
class Program
{
    static MyManagedClass res1 = new MyManagedClass();
    static void Main(string[] args)
    {
        using (res1)
        {
            res1.Func();
            res1 = null;
        }
    }
}
class MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.res1""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldsfld     ""MyManagedClass Program.res1""
  IL_000b:  callvirt   ""void MyManagedClass.Func()""
  IL_0010:  ldnull
  IL_0011:  stsfld     ""MyManagedClass Program.res1""
  IL_0016:  leave.s    IL_0022
}
  finally
{
  IL_0018:  ldloc.0
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.0
  IL_001c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0021:  endfinally
}
  IL_0022:  ret
}");
        }

        // Dispose() not called if the object(Reference type) not created
        [Fact]
        public void ResourceIsNull_ReferenceType()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass MyProperty;
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            System.Console.WriteLine(""InUsing"");
        }
    }
}
class MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            CompileAndVerify(source, expectedOutput: "InUsing").VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldstr      ""InUsing""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.0
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001b:  endfinally
}
  IL_001c:  ret
}");
        }

        [Fact]
        public void ResourceIsNull_ReferenceType_2()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass MyProperty;
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            MyProperty = new MyManagedClass();
        }
    }
}
class MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            CompileAndVerify(source, expectedOutput: "").VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  newobj     ""MyManagedClass..ctor()""
  IL_000b:  stsfld     ""MyManagedClass Program.MyProperty""
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.0
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001b:  endfinally
}
  IL_001c:  ret
}");
        }

        // Dispose() called if the object(Value type) not created
        [Fact]
        public void ResourceIsNull_ValueType()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass MyProperty;
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            System.Console.WriteLine(""InUsing"");
        }
    }
}
struct MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            CompileAndVerify(source, expectedOutput: @"InUsing
Dispose").VerifyIL("Program.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldstr      ""InUsing""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  leave.s    IL_0020
}
  finally
{
  IL_0012:  ldloca.s   V_0
  IL_0014:  constrained. ""MyManagedClass""
  IL_001a:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001f:  endfinally
}
  IL_0020:  ret
}");
        }

        [Fact]
        public void ResourceIsNull_ValueType_2()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass MyProperty;
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            MyProperty = new MyManagedClass();
        }
    }
}
struct MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            CompileAndVerify(source, expectedOutput: "Dispose").VerifyIL("Program.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldsflda    ""MyManagedClass Program.MyProperty""
  IL_000b:  initobj    ""MyManagedClass""
  IL_0011:  leave.s    IL_0021
}
  finally
{
  IL_0013:  ldloca.s   V_0
  IL_0015:  constrained. ""MyManagedClass""
  IL_001b:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0020:  endfinally
}
  IL_0021:  ret
}");
        }

        // Dispose() Not called if the object(Nullable type) not created 
        [Fact]
        public void ResourceIsNull_NullableType()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass? MyProperty;
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            System.Console.WriteLine(""InUsing"");
        }
    }
}
struct MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            // This is not the same IL as is generated by the native compiler; the native compiler
            // generates the finally block with both a HasValue check *and* a boxing conversion:
            //
            // if (temp.HasValue) {((IDisposable) temp).Dispose(); }
            //
            // this is correct but slightly odd; if you're going to box then why not generate
            // 
            // IDisposable temp2 = temp as IDisposable; if(temp2 != null) temp2.Dispose();
            //
            // and if you're going to call HasValue then why not generate
            //
            // if (temp.HasValue) temp.GetValueOrDefault().Dispose();
            //
            // without boxing?
            //
            // Roslyn does the latter.

            var comp = CompileAndVerify(source, expectedOutput: @"InUsing");
            comp.VerifyIL("Program.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  1
  .locals init (MyManagedClass? V_0,
  MyManagedClass V_1)
  IL_0000:  ldsfld     ""MyManagedClass? Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldstr      ""InUsing""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  leave.s    IL_0031
}
  finally
{
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool MyManagedClass?.HasValue.get""
  IL_0019:  brfalse.s  IL_0030
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""MyManagedClass MyManagedClass?.GetValueOrDefault()""
  IL_0022:  stloc.1
  IL_0023:  ldloca.s   V_1
  IL_0025:  constrained. ""MyManagedClass""
  IL_002b:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0030:  endfinally
}
  IL_0031:  ret
}");
        }

        [Fact]
        public void ResourceIsNull_NullableType_2()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass? MyProperty;
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            MyProperty = new MyManagedClass();
        }
    }
}
struct MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "");

            // Note that changing the value of the expression does not cause the
            // Dispose to be called; we dispose the original value, not the new value.
            //
            // See comments in previous test regarding this codegen.

            comp.VerifyIL("Program.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (MyManagedClass? V_0,
  MyManagedClass V_1)
  IL_0000:  ldsfld     ""MyManagedClass? Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldloca.s   V_1
  IL_0008:  initobj    ""MyManagedClass""
  IL_000e:  ldloc.1
  IL_000f:  newobj     ""MyManagedClass?..ctor(MyManagedClass)""
  IL_0014:  stsfld     ""MyManagedClass? Program.MyProperty""
  IL_0019:  leave.s    IL_003a
}
  finally
{
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""bool MyManagedClass?.HasValue.get""
  IL_0022:  brfalse.s  IL_0039
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""MyManagedClass MyManagedClass?.GetValueOrDefault()""
  IL_002b:  stloc.1
  IL_002c:  ldloca.s   V_1
  IL_002e:  constrained. ""MyManagedClass""
  IL_0034:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0039:  endfinally
}
  IL_003a:  ret
}");
        }

        [Fact]
        public void ResourceIsNull_NullableType_3()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass? MyProperty = new MyManagedClass();
    static void Main(string[] args)
    {
        using (MyManagedClass? c = MyProperty)
        {
            c.Value.Func();
        }
    }
}
struct MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
}
";
            string expected = @"Func
Dispose";

            var comp = CompileAndVerify(source, expectedOutput: expected);

            // Note that changing the value of the expression does not cause the
            // Dispose to be called; we dispose the original value, not the new value.
            //
            // See comments in previous test regarding this codegen.

            comp.VerifyIL("Program.Main", @"
{
  // Code size       55 (0x37)
  .maxstack  1
  .locals init (MyManagedClass? V_0, //c
  MyManagedClass V_1)
  IL_0000:  ldsfld     ""MyManagedClass? Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""MyManagedClass MyManagedClass?.Value.get""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       ""void MyManagedClass.Func()""
  IL_0015:  leave.s    IL_0036
}
  finally
{
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""bool MyManagedClass?.HasValue.get""
  IL_001e:  brfalse.s  IL_0035
  IL_0020:  ldloca.s   V_0
  IL_0022:  call       ""MyManagedClass MyManagedClass?.GetValueOrDefault()""
  IL_0027:  stloc.1
  IL_0028:  ldloca.s   V_1
  IL_002a:  constrained. ""MyManagedClass""
  IL_0030:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0035:  endfinally
}
  IL_0036:  ret
}");
        }

        // Dispose() called for nested using
        [Fact, WorkItem(528943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528943")]
        public void DisposeCalled_NestedUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        MyManagedClass1 res1 = new MyManagedClass1(1);
        MyManagedClass1 res2 = new MyManagedClass1(2);
        using (res1)
        {
            using (res1)
            {
            }
        }
    }
}
struct MyManagedClass1 : IDisposable
{
    int Count;
    public MyManagedClass1(int x)
    { this.Count = x; }
    void IDisposable.Dispose()
    { System.Console.WriteLine(string.Format (""{0}:Dispose"",Count )); }
    public void Throw() { throw new Exception(); }
}
";
            CompileAndVerify(source, expectedOutput: @"1:Dispose
1:Dispose").VerifyIL("Program.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (MyManagedClass1 V_0, //res1
  MyManagedClass1 V_1,
  MyManagedClass1 V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""MyManagedClass1..ctor(int)""
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     ""MyManagedClass1..ctor(int)""
  IL_000e:  pop
  IL_000f:  ldloc.0
  IL_0010:  stloc.1
  .try
{
  IL_0011:  ldloc.0
  IL_0012:  stloc.2
  .try
{
  IL_0013:  leave.s    IL_0031
}
  finally
{
  IL_0015:  ldloca.s   V_2
  IL_0017:  constrained. ""MyManagedClass1""
  IL_001d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0022:  endfinally
}
}
  finally
{
  IL_0023:  ldloca.s   V_1
  IL_0025:  constrained. ""MyManagedClass1""
  IL_002b:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0030:  endfinally
}
  IL_0031:  ret
}");
        }

        // Dispose() called after throw
        [Fact]
        public void DisposeCalledAfterThrow_ReferenceType()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass MyProperty= new MyManagedClass ();
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            MyProperty.Throw();
        }
    }
}
class MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Throw()
    { throw (new Exception(""Res1"")); }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_000b:  callvirt   ""void MyManagedClass.Throw()""
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.0
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001b:  endfinally
}
  IL_001c:  ret
}");
        }

        // Dispose() called after throw
        [Fact]
        public void DisposeCalledAfterThrow_ValueType()
        {
            var source = @"
using System;
class Program
{
    public static MyManagedClass MyProperty= new MyManagedClass ();
    static void Main(string[] args)
    {
        using (MyProperty)
        {
            MyProperty.Throw();
        }
    }
}
struct MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Throw()
    { throw (new Exception(""Res1"")); }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldsfld     ""MyManagedClass Program.MyProperty""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldsflda    ""MyManagedClass Program.MyProperty""
  IL_000b:  call       ""void MyManagedClass.Throw()""
  IL_0010:  leave.s    IL_0020
}
  finally
{
  IL_0012:  ldloca.s   V_0
  IL_0014:  constrained. ""MyManagedClass""
  IL_001a:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001f:  endfinally
}
  IL_0020:  ret
}");
        }

        // Dispose() called for first objects with exception thrown after second block
        [Fact, WorkItem(542982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542982")]
        public void DisposeCalledAfterThrow_NestedUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedClass res1 = new MyManagedClass())
        {
            res1.Func();
            using (MyManagedClass res2 = new MyManagedClass())
            {
                res2.Func();
            }
            res1.Throw();
        }
    }
}
struct  MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Func()
    { System.Console.WriteLine(""Func""); }
    public void Throw()
    { throw new Exception(); }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       70 (0x46)
  .maxstack  1
  .locals init (MyManagedClass V_0, //res1
  MyManagedClass V_1) //res2
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""MyManagedClass""
  .try
{
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""void MyManagedClass.Func()""
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""MyManagedClass""
  .try
{
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""void MyManagedClass.Func()""
  IL_001e:  leave.s    IL_002e
}
  finally
{
  IL_0020:  ldloca.s   V_1
  IL_0022:  constrained. ""MyManagedClass""
  IL_0028:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002d:  endfinally
}
  IL_002e:  ldloca.s   V_0
  IL_0030:  call       ""void MyManagedClass.Throw()""
  IL_0035:  leave.s    IL_0045
}
  finally
{
  IL_0037:  ldloca.s   V_0
  IL_0039:  constrained. ""MyManagedClass""
  IL_003f:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0044:  endfinally
}
  IL_0045:  ret
}");
        }

        [Fact, WorkItem(542982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542982")]
        public void DisposeCalledAfterThrow_NestedUsing_2()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedClass res1 = new MyManagedClass())
        {
            using (MyManagedClass res2 = new MyManagedClass())
            {
                res1.Throw();
            }
        }
    }
}
struct  MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    { System.Console.WriteLine(""Dispose""); }
    public void Throw()
    { throw new Exception(); }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (MyManagedClass V_0, //res1
  MyManagedClass V_1) //res2
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""MyManagedClass""
  .try
{
  IL_0008:  ldloca.s   V_1
  IL_000a:  initobj    ""MyManagedClass""
  .try
{
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""void MyManagedClass.Throw()""
  IL_0017:  leave.s    IL_0035
}
  finally
{
  IL_0019:  ldloca.s   V_1
  IL_001b:  constrained. ""MyManagedClass""
  IL_0021:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0026:  endfinally
}
}
  finally
{
  IL_0027:  ldloca.s   V_0
  IL_0029:  constrained. ""MyManagedClass""
  IL_002f:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0034:  endfinally
}
  IL_0035:  ret
}");
        }

        // Multiple objects can be used in with a using statement, but they must be declared inside the using statement
        [Fact]
        public void MultipleResourceInUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        MyManagedClass r1, r2;
        using (r1;r2) // Invalid
        {
        }
    }
}
struct  MyManagedClass : IDisposable
{
    void IDisposable.Dispose()
    {  }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,18): error CS1026: ) expected
                //         using (r1;r2) // Invalid
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";"),
                // (8,18): warning CS0642: Possible mistaken empty statement
                //         using (r1;r2) // Invalid
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"),
                // (8,21): error CS1002: ; expected
                //         using (r1;r2) // Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                // (8,21): error CS1513: } expected
                //         using (r1;r2) // Invalid
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")"));
        }

        // Multiple objects can be used in with a using statement, but they must be declared inside the using statement
        [Fact, WorkItem(542982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542982")]
        public void MultipleResourceInUsing_2()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedClass1 res1 = new MyManagedClass1(), res2)// Invalid
        {
        }
    }
}
struct MyManagedClass1 : IDisposable
{
    void IDisposable.Dispose()
    { }
}
";
            CreateCompilation(source).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FixedMustInit, "res2"));
        }

        // Dispose() called for both objects when exception thrown in compound case
        [Fact, WorkItem(528943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528943")]
        public void DisposeCalledForMultiResources()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedClass1 res1 = new MyManagedClass1(1), res2 = new MyManagedClass1(2))
        {
            res1.Throw();
        }
    }
}
struct MyManagedClass1 : IDisposable
{
    int Count;
    public MyManagedClass1(int x)
    { this.Count = x; }
    void IDisposable.Dispose()
    { System.Console.WriteLine(string.Format (""{0}:Dispose"",Count )); }
    public void Throw() { throw new Exception(); }
}
";
            CompileAndVerify(source).VerifyIL("Program.Main", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (MyManagedClass1 V_0, //res1
  MyManagedClass1 V_1) //res2
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""MyManagedClass1..ctor(int)""
  .try
{
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     ""MyManagedClass1..ctor(int)""
  IL_000e:  stloc.1
  .try
{
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       ""void MyManagedClass1.Throw()""
  IL_0016:  leave.s    IL_0034
}
  finally
{
  IL_0018:  ldloca.s   V_1
  IL_001a:  constrained. ""MyManagedClass1""
  IL_0020:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0025:  endfinally
}
}
  finally
{
  IL_0026:  ldloca.s   V_0
  IL_0028:  constrained. ""MyManagedClass1""
  IL_002e:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0033:  endfinally
}
  IL_0034:  ret
}");
        }

        // Dangling using keyword
        [WorkItem(528933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528933")]
        [Fact]
        public void DanglingUsing()
        {
            var source = @"
class A
{
    void B()
	{
		using
	}
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,8): error CS1031: Type expected
                // 		using
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(6, 8),
                // (6,8): error CS1001: Identifier expected
                // 		using
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(6, 8),
                // (6,8): error CS1002: ; expected
                // 		using
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 8),
                // (7,1): error CS0210: You must provide an initializer in a fixed or using statement declaration
                // 	}
                Diagnostic(ErrorCode.ERR_FixedMustInit, "").WithLocation(7, 1));
        }

        // If statement directly following a using ()
        [Fact]
        public void IfFollowingUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        int n = 0;
        using (new MyManagedClass1())
            if (true) n++;
    }
}
class MyManagedClass1 : IDisposable
{
    void IDisposable.Dispose()
    {  }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        // Query expression in using statement
        [Fact]
        public void QueryAsUsing()
        {
            var source = @"
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        using (from x in new int[] { 1 } select x)
        {
        }
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoConvToIDisp, "from x in new int[] { 1 } select x").WithArguments("System.Collections.Generic.IEnumerable<int>"));
        }

        // Error when using a lambda in a using()
        [Fact]
        public void LambdaAsUsing()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        using (x => x)     // err
        {
        }
        using (() => { })     // err
        {
        }
        using ((int @int) => { return @int; })     // err
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,16): error CS1674: 'lambda expression': type used in a using statement must implement 'System.IDisposable'.
                //         using (x => x)     // err
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "x => x").WithArguments("lambda expression"),
                // (9,16): error CS1674: 'lambda expression': type used in a using statement must implement 'System.IDisposable'.
                //         using (() => { })     // err
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "() => { }").WithArguments("lambda expression"),
                // (12,16): error CS1674: 'lambda expression': type used in a using statement must implement 'System.IDisposable'.
                //         using ((int @int) => { return @int; })     // err
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "(int @int) => { return @int; }").WithArguments("lambda expression"));
        }

        // Anonymous types cannot appear in using
        [Fact]
        public void AnonymousAsUsing()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        using (var a = new { })
        {
        }
        using (var b = new { p1 = 10 })
        {
        }
        using (var c = new { p1 = 10.0, p2 = 'a' })
        {
        }
        using (new { })
        {
            ;
        }
        using (new { f1 = ""12345"", f2 = 'S' })
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
    // (6,16): error CS1674: '<empty anonymous type>': type used in a using statement must implement 'System.IDisposable'.
    //         using (var a = new { })
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var a = new { }").WithArguments("<empty anonymous type>"),
    // (9,16): error CS1674: '<anonymous type: int p1>': type used in a using statement must implement 'System.IDisposable'.
    //         using (var b = new { p1 = 10 })
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var b = new { p1 = 10 }").WithArguments("<anonymous type: int p1>"),
    // (12,16): error CS1674: '<anonymous type: double p1, char p2>': type used in a using statement must implement 'System.IDisposable'.
    //         using (var c = new { p1 = 10.0, p2 = 'a' })
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var c = new { p1 = 10.0, p2 = 'a' }").WithArguments("<anonymous type: double p1, char p2>"),
    // (15,16): error CS1674: '<empty anonymous type>': type used in a using statement must implement 'System.IDisposable'.
    //         using (new { })
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new { }").WithArguments("<empty anonymous type>"),
    // (19,16): error CS1674: '<anonymous type: string f1, char f2>': type used in a using statement must implement 'System.IDisposable'.
    //         using (new { f1 = "12345", f2 = 'S' })
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, @"new { f1 = ""12345"", f2 = 'S' }").WithArguments("<anonymous type: string f1, char f2>")
    );
        }

        // User-defined Conversions
        [Fact]
        public void UserDefinedConversions()
        {
            var source = @"
using System;
struct MyManagedClass1 : IDisposable
{
    public int Number;
    public void Dispose()
    {
        Number = -1;
    }
    public static implicit operator MyManagedClass1(int i)
    {
        MyManagedClass1 temp = new MyManagedClass1();
        temp.Number = i;
        return temp;
    }
}
class Program
{
    static void Main()
    {
        using (MyManagedClass1? str = (int?)1)
        {
        }
        using (MyManagedClass1? str = (int?)null)
        {
        }
        using (MyManagedClass1? str = 2)
        {
        }
        using (MyManagedClass1 str = (MyManagedClass1)(int?)3)
        {
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"");
        }

        // Anonymous Delegate in using block
        [Fact]
        public void AnonymousDelegateInUsing()
        {
            var source = @"
using System;
delegate T D1<T>(T t);
class A1
{
    static void Goo<T>(T t) where T : IDisposable
    {
        T local = t;
        using (T t1 = ((D1<T>)delegate(T tt) { return t; })(t))
        {
        }
    }
    static void Main()
    {
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        // Put the using around the try
        [Fact, WorkItem(528943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528943")]
        public void UsingAroundTry()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        MyManagedClass1 x = new MyManagedClass1();
        using (x)
        {
            try
            {
                System.Console.WriteLine(""Try"");
            }
            finally
            {
                x = null;// Warning CS0728
                System.Console.WriteLine(""Catch"");
            }
        }
    }
}
public class MyManagedClass1 : IDisposable
{
    public void Dispose()
    {
        System.Console.WriteLine(""Dispose()"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"Try
Catch
Dispose()").VerifyIL("Program.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (MyManagedClass1 V_0)
  IL_0000:  newobj     ""MyManagedClass1..ctor()""
  IL_0005:  stloc.0
  .try
{
  .try
{
  IL_0006:  ldstr      ""Try""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  leave.s    IL_0027
}
  finally
{
  IL_0012:  ldstr      ""Catch""
  IL_0017:  call       ""void System.Console.WriteLine(string)""
  IL_001c:  endfinally
}
}
  finally
{
  IL_001d:  ldloc.0
  IL_001e:  brfalse.s  IL_0026
  IL_0020:  ldloc.0
  IL_0021:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0026:  endfinally
}
  IL_0027:  ret
}");
        }

        // Put the using in the try
        [Fact]
        public void UsingInTry()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        MyManagedClass1 x = new MyManagedClass1();
        try
        {
            System.Console.WriteLine(""Try"");
            using (x)
            { throw new Exception(); }
        }
        catch
        {
            System.Console.WriteLine(""Catch"");
            x = null;
        }
    }
}
public class MyManagedClass1 : IDisposable
{
    public void Dispose()
    {
        System.Console.WriteLine(""Dispose()"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"Try
Dispose()
Catch").VerifyIL("Program.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  1
  .locals init (MyManagedClass1 V_0, //x
  MyManagedClass1 V_1)
  IL_0000:  newobj     ""MyManagedClass1..ctor()""
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldstr      ""Try""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  .try
{
  IL_0012:  newobj     ""System.Exception..ctor()""
  IL_0017:  throw
}
  finally
{
  IL_0018:  ldloc.1
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.1
  IL_001c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0021:  endfinally
}
}
  catch object
{
  IL_0022:  pop
  IL_0023:  ldstr      ""Catch""
  IL_0028:  call       ""void System.Console.WriteLine(string)""
  IL_002d:  ldnull
  IL_002e:  stloc.0
  IL_002f:  leave.s    IL_0031
}
  IL_0031:  ret
}");
        }

        [Fact]
        public void TestValueTypeUsingVariableCanBeMutatedByInstanceMethods()
        {
            const string source = @"
struct A : System.IDisposable
{
    int field;

    void Set(A a)
    {
        this = a;
    }

    public void Dispose() { }

    static void Main()
    {
        using (var a = new A())
        {
            a.Set(new A { field = 5 });
            System.Console.Write(a.field);
        }
    }  
}";

            CompileAndVerify(source, expectedOutput: "5");
        }

        [Fact, WorkItem(1077204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077204")]
        public void TestValueTypeUsingVariableFieldsAreReadonly()
        {
            const string source = @"
struct B
{
    public int field;

    public void SetField(int value)
    {
        field = value;
    }
}

struct A : System.IDisposable
{
    B b;

    public void Dispose() { }

    static void Main()
    {
        using (var a = new A())
        {
            a.b.SetField(5);
            System.Console.Write(a.b.field);
        }
    }
}";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [Fact, WorkItem(1077204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077204")]
        public void TestValueTypeUsingVariableFieldsAreReadonly2()
        {
            const string source = @"
struct C
{
    public int field;

    public void SetField(int value)
    {
        field = value;
    }
}

struct B
{
    public C c;
}

struct A : System.IDisposable
{
    B b;

    public void Dispose() { }

    static void Main()
    {
        using (var a = new A())
        {
            a.b.c.SetField(5);
            System.Console.Write(a.b.c.field);
        }
    }
}";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [Theory, CombinatorialData]
        public void OptionalParameter([CombinatorialValues("ref", "in", "ref readonly", "")] string modifier)
        {
            var source = $$"""
                using System;
                using (var s = new S())
                {
                    Console.Write("1");
                }
                ref struct S
                {
                    public void Dispose({{modifier}} int x = 2) => Console.Write(x);
                }
                """;
            if (modifier == "ref")
            {
                CreateCompilation(source).VerifyDiagnostics(
                    // (2,8): error CS1674: 'S': type used in a using statement must implement 'System.IDisposable'.
                    // using (var s = new S())
                    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var s = new S()").WithArguments("S").WithLocation(2, 8),
                    // (8,25): error CS1741: A ref or out parameter cannot have a default value
                    //     public void Dispose(ref int x = 2) => Console.Write(x);
                    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(8, 25));
            }
            else
            {
                var verifier = CompileAndVerify(source, expectedOutput: "12");
                if (modifier == "ref readonly")
                {
                    verifier.VerifyDiagnostics(
                        // (8,46): warning CS9200: A default value is specified for 'ref readonly' parameter 'x', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                        //     public void Dispose(ref readonly int x = 2) => Console.Write(x);
                        Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "2").WithArguments("x").WithLocation(8, 46));
                }
                else
                {
                    verifier.VerifyDiagnostics();
                }
            }
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_01(
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V v = default;

                using (var d = new R(ref v))
                {
                    d.V.F++;
                }

                Console.Write(v.F);

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref V V = ref v;
                    public void Dispose() { }
                }

                struct V
                {
                    public int F;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.FailsPEVerify,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "1").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_02(
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V v = default;

                using (var d = new R(ref v))
                {
                    d.V.F += 2;
                }

                Console.Write(v.F);

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref V V = ref v;
                    public void Dispose() { }
                }

                struct V
                {
                    public int F;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.FailsPEVerify,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "2").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_03(
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V v = default;

                using (var d = new R(ref v))
                {
                    d.V.S.Inc();
                }

                Console.Write(v.S.F);

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref V V = ref v;
                    public void Dispose() { }
                }
                                
                struct V
                {
                    public S S;
                }

                struct S
                {
                    public int F;
                    public void Inc() => F++;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.FailsPEVerify,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "1").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_04(
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V v = default;

                using (var d = new R(ref v))
                {
                    d.V.F++;
                }

                Console.Write(v.F);

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref readonly V V = ref v;
                    public void Dispose() { }
                }

                struct V
                {
                    public int F;
                }
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (7,5): error CS8332: Cannot assign to a member of field 'V' or use it as the right hand side of a ref assignment because it is a readonly variable
                //     d.V.F++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "d.V.F").WithArguments("field", "V").WithLocation(7, 5));
        }
    }
}
