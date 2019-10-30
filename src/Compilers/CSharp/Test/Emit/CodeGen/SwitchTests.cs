// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class SwitchTests : EmitMetadataTestBase
    {
        #region Functionality tests

        [Fact]
        public void DefaultOnlySwitch()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 1;
        switch (true) {
        default:
            ret = 0;
            break;
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main", @"
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldc.i4.0
                  IL_0001:  dup
                  IL_0002:  call       ""void System.Console.Write(int)""
                  IL_0007:  ret
                }");
        }

        [WorkItem(542298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542298")]
        [Fact]
        public void DefaultOnlySwitch_02()
        {
            string text = @"using System;
public class Test
{
    public static void Main()
    {
        int status = 2;
        switch (status)
        {
            default: status--; break;
        }

        string str = ""string"";
        switch (str)
        {
            default: status--; break;
        }

        Console.WriteLine(status);
    }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main",
@"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  ret
}"
            );
        }

        [Fact]
        public void ConstantIntegerSwitchArgumentExpression()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 1;
        switch (true) {
        case true:
            ret = 0;
            break;
        }

        Console.Write(ret);
        return(ret);
    }
}";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main", @"
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldc.i4.0
                  IL_0001:  dup
                  IL_0002:  call       ""void System.Console.Write(int)""
                  IL_0007:  ret
                }"
            );
        }

        [Fact]
        public void ConstantNullSwitchArgumentExpression()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = 1;
    const string s = null;

    switch (s)
    {
      case null:
        ret = 0;
        break;
    }

    Console.Write(ret);
    return(ret);
  }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main", @"
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldc.i4.0
                  IL_0001:  dup
                  IL_0002:  call       ""void System.Console.Write(int)""
                  IL_0007:  ret
                }"
            );
        }

        [Fact]
        public void NonConstantSwitchArgumentExpression()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 0;
        int value = 1;
        
        switch (value) {
        case 2:
            ret = 1;
            break;		
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  bne.un.s   IL_0008
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""void System.Console.Write(int)""
  IL_000e:  ldloc.0
  IL_000f:  ret
}"
            );
        }

        [Fact]
        public void ConstantVariableInCaseLabel()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 1;

        int value = 23;

        switch (value) {
        case kValue:
            ret = 0;
            break;
        default:
            ret = 1;
            break;
        }

        Console.Write(ret);
        return(ret);
    }

    const int kValue = 23;
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.s   23
  IL_0004:  ldc.i4.s   23
  IL_0006:  bne.un.s   IL_000c
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.0
  IL_000a:  br.s       IL_000e
  IL_000c:  ldc.i4.1
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""void System.Console.Write(int)""
  IL_0014:  ldloc.0
  IL_0015:  ret
}"
            );
        }

        [Fact]
        public void DefaultExpressionInLabel()
        {
            var source = @"
class C
{
    static void Main()
    {
        switch (0)
        {
            case default(int): return;
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void SwitchWith_NoMatchingCaseLabel_And_NoDefaultLabel()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    int i = 5;

    switch (i)
    {
      case 1:
      case 2:
      case 3:
        return 1;
      case 1001:
      case 1002:
      case 1003:
        return 2;
    }
    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  ldc.i4.2
  IL_0006:  ble.un.s   IL_0014
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4     0x3e9
  IL_000e:  sub
  IL_000f:  ldc.i4.2
  IL_0010:  ble.un.s   IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  ldc.i4.1
  IL_0015:  ret
  IL_0016:  ldc.i4.2
  IL_0017:  ret
  IL_0018:  ldc.i4.0
  IL_0019:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch001()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(100);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int i)
  {
    switch (i)
    {
      case 0:
      case 1:
      case 2:
      case 3:
      case 4:
      case 5:
      case 6:
        return 1;

      case 100:
        goto case 3;
    }

    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.6
  IL_0002:  ble.un.s   IL_0009
  IL_0004:  ldarg.0
  IL_0005:  ldc.i4.s   100
  IL_0007:  bne.un.s   IL_000b
  IL_0009:  ldc.i4.1
  IL_000a:  ret
  IL_000b:  ldc.i4.0
  IL_000c:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch001_Debug()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(100);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int i)
  {
    switch (i)
    {
      case 0:       
      case 1:
      case 2:
      case 3:
      case 4:
      case 5:
      case 6:
        return 1;

      case 100:
        goto case 3;
    }

    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1", options: TestOptions.DebugExe);
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int V_0,
                int V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  stloc.0
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.6
  IL_0007:  ble.un.s   IL_0012
  IL_0009:  br.s       IL_000b
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.s   100
  IL_000e:  beq.s      IL_0016
  IL_0010:  br.s       IL_0018
  IL_0012:  ldc.i4.1
  IL_0013:  stloc.2
  IL_0014:  br.s       IL_001c
  IL_0016:  br.s       IL_0012
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.2
  IL_001a:  br.s       IL_001c
  IL_001c:  ldloc.2
  IL_001d:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch002()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(5);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int i)
  {
    switch (i)
    {
      case 1:
      case 5:
      case 6:
      case 3:
      case 4:
      case 2:
        return 1;
    }

    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  ldc.i4.5
  IL_0004:  bgt.un.s   IL_0008
  IL_0006:  ldc.i4.1
  IL_0007:  ret
  IL_0008:  ldc.i4.0
  IL_0009:  ret
}
"
            );
        }

        [Fact]
        public void DegenerateSwitch003()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(4);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int i)
  {
    switch (i)
    {
      case -2:
      case -1:
      case 0:
      case 2:
      case 1:
      case 4:
      case 3:
        return 1;
    }

    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -2
  IL_0003:  sub
  IL_0004:  ldc.i4.6
  IL_0005:  bgt.un.s   IL_0009
  IL_0007:  ldc.i4.1
  IL_0008:  ret
  IL_0009:  ldc.i4.0
  IL_000a:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch004()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(int.MaxValue - 1);
    Console.Write(ret);
    return(ret);
  }

    public static int M(int i)
    {
        switch (i)
        {
            case int.MinValue + 1:
            case int.MinValue:
            case int.MaxValue:
            case int.MaxValue - 1:
            case int.MaxValue - 2:
                return 1;
        }

        return 0;
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x80000000
  IL_0006:  sub
  IL_0007:  ldc.i4.1
  IL_0008:  ble.un.s   IL_0014
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4     0x7ffffffd
  IL_0010:  sub
  IL_0011:  ldc.i4.2
  IL_0012:  bgt.un.s   IL_0016
  IL_0014:  ldc.i4.1
  IL_0015:  ret
  IL_0016:  ldc.i4.0
  IL_0017:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch005()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(int.MaxValue + 1L);
    Console.Write(ret);
    return(ret);
  }

    public static int M(long i)
    {
        switch (i)
        {
            case int.MaxValue:
            case int.MaxValue + 1L:
            case int.MaxValue + 2L:
            case int.MaxValue + 3L:
                return 1;
        }

        return 0;
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x7fffffff
  IL_0006:  conv.i8
  IL_0007:  sub
  IL_0008:  ldc.i4.3
  IL_0009:  conv.i8
  IL_000a:  bgt.un.s   IL_000e
  IL_000c:  ldc.i4.1
  IL_000d:  ret
  IL_000e:  ldc.i4.0
  IL_000f:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch006()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(35);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int i)
  {
    switch (i)
    {
      case 1:
      case 5:
      case 6:
      case 3:
      case 4:
      case 2:
        return 1;
      case 31:
      case 35:
      case 36:
      case 33:
      case 34:
      case 32:
        return 4;
      case 41:
      case 45:
      case 46:
      case 43:
      case 44:
      case 42:
        return 5;
    }

    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "4");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  ldc.i4.5
  IL_0004:  ble.un.s   IL_0016
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.s   31
  IL_0009:  sub
  IL_000a:  ldc.i4.5
  IL_000b:  ble.un.s   IL_0018
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.s   41
  IL_0010:  sub
  IL_0011:  ldc.i4.5
  IL_0012:  ble.un.s   IL_001a
  IL_0014:  br.s       IL_001c
  IL_0016:  ldc.i4.1
  IL_0017:  ret
  IL_0018:  ldc.i4.4
  IL_0019:  ret
  IL_001a:  ldc.i4.5
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}"
            );
        }

        [Fact]
        public void NotDegenerateSwitch006()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(35);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int i)
  {
    switch (i)
    {
      case 1:
      case 5:
      case 6:
      case 3:
      case 4:
      case 2:
        return 1;
      case 11:
      case 15:
      case 16:
      case 13:
      case 14:
      case 12:
        return 2;
      case 21:
      case 25:
      case 26:
      case 23:
      case 24:
      case 22:
        return 3;
      case 31:
      case 35:
      case 36:
      case 33:
      case 34:
      case 32:
        return 4;
      case 41:
      case 45:
      case 46:
      case 43:
      case 44:
      case 42:
        return 5;
    }

    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "4");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size      206 (0xce)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  switch    (
        IL_00c2,
        IL_00c2,
        IL_00c2,
        IL_00c2,
        IL_00c2,
        IL_00c2,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00c4,
        IL_00c4,
        IL_00c4,
        IL_00c4,
        IL_00c4,
        IL_00c4,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00c6,
        IL_00c6,
        IL_00c6,
        IL_00c6,
        IL_00c6,
        IL_00c6,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00c8,
        IL_00c8,
        IL_00c8,
        IL_00c8,
        IL_00c8,
        IL_00c8,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00cc,
        IL_00ca,
        IL_00ca,
        IL_00ca,
        IL_00ca,
        IL_00ca,
        IL_00ca)
  IL_00c0:  br.s       IL_00cc
  IL_00c2:  ldc.i4.1
  IL_00c3:  ret
  IL_00c4:  ldc.i4.2
  IL_00c5:  ret
  IL_00c6:  ldc.i4.3
  IL_00c7:  ret
  IL_00c8:  ldc.i4.4
  IL_00c9:  ret
  IL_00ca:  ldc.i4.5
  IL_00cb:  ret
  IL_00cc:  ldc.i4.0
  IL_00cd:  ret
}");
        }

        [Fact]
        public void DegenerateSwitch007()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(5);
    Console.Write(ret);
    return(ret);
  }

  public static int M(int? i)
  {
    switch (i)
    {
      case 0:       
      case 1:
      case 2:
      case 3:
      case 4:
      case 5:
      case 6:
        return 1;

      default:
        return 0;

      case null:
        return 2;
    }
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brfalse.s  IL_0019
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""int int?.GetValueOrDefault()""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.6
  IL_0013:  bgt.un.s   IL_0017
  IL_0015:  ldc.i4.1
  IL_0016:  ret
  IL_0017:  ldc.i4.0
  IL_0018:  ret
  IL_0019:  ldc.i4.2
  IL_001a:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch008()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M((uint)int.MaxValue + (uint)1);
    Console.Write(ret);
    return(ret);
  }

    public static int M(uint i)
    {
        switch (i)
        {
            case (uint)int.MaxValue:
            case (uint)int.MaxValue + (uint)1:
            case (uint)int.MaxValue + (uint)2:
            case (uint)int.MaxValue + (uint)3:
                return 1;
        }

        return 0;
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x7fffffff
  IL_0006:  sub
  IL_0007:  ldc.i4.3
  IL_0008:  bgt.un.s   IL_000c
  IL_000a:  ldc.i4.1
  IL_000b:  ret
  IL_000c:  ldc.i4.0
  IL_000d:  ret
}"
            );
        }

        [Fact]
        public void DegenerateSwitch009()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M(uint.MaxValue);
    Console.Write(ret);
    return(ret);
  }

    public static int M(uint i)
    {
        switch (i)
        {
            case 0:
            case 1:
            case uint.MaxValue:
            case uint.MaxValue - 1:
            case uint.MaxValue - 2:
            case uint.MaxValue - 3:
                return 1;
        }

        return 0;
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ble.un.s   IL_000b
  IL_0004:  ldarg.0
  IL_0005:  ldc.i4.s   -4
  IL_0007:  sub
  IL_0008:  ldc.i4.3
  IL_0009:  bgt.un.s   IL_000d
  IL_000b:  ldc.i4.1
  IL_000c:  ret
  IL_000d:  ldc.i4.0
  IL_000e:  ret
}"
            );
        }

        [Fact]
        public void ByteTypeSwitchArgumentExpression()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 0;
        ret = DoByte();
        return(ret);
    }

    private static int DoByte()
    {
        int ret = 2;
        byte b = 2;

        switch (b) {
        case 1:
        case 2:
            ret--;
            break;
        case 3:
            break;
        default:
            break;
        }

        switch (b) {
        case 1:
        case 3:
            break;
        default:
            ret--;
            break;
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.DoByte",
@"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0, //ret
                byte V_1) //b
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.2
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  ldc.i4.1
  IL_0006:  sub
  IL_0007:  ldc.i4.1
  IL_0008:  ble.un.s   IL_0010
  IL_000a:  ldloc.1
  IL_000b:  ldc.i4.3
  IL_000c:  beq.s      IL_0014
  IL_000e:  br.s       IL_0014
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.1
  IL_0012:  sub
  IL_0013:  stloc.0
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  beq.s      IL_0020
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.3
  IL_001a:  beq.s      IL_0020
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  call       ""void System.Console.Write(int)""
  IL_0026:  ldloc.0
  IL_0027:  ret
}"
            );
        }

        [Fact]
        public void LongTypeSwitchArgumentExpression()
        {
            var text = @"
using System;

public class Test
{
    public static void Main(string [] args)
    {
        int ret = 0;
        ret = DoLong(2);
        Console.Write(ret);
        ret = DoLong(4);
        Console.Write(ret);
        ret = DoLong(42);
        Console.Write(ret);
    }

    private static int DoLong(long b)
    {
        int ret = 2;

        switch (b) {
        case 1:
            ret++;
            break;            
        case 2:
            ret--;
            break;
        case 3:
            break;
        case 4:
            ret += 7;
            break;
        default:
            ret+=2;
            break;
        }

        return ret;
    }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "194");
            compVerifier.VerifyIL("Test.DoLong",
@"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  conv.i8
  IL_0005:  sub
  IL_0006:  dup
  IL_0007:  ldc.i4.3
  IL_0008:  conv.i8
  IL_0009:  ble.un.s   IL_000e
  IL_000b:  pop
  IL_000c:  br.s       IL_0038
  IL_000e:  conv.u4
  IL_000f:  switch    (
        IL_0026,
        IL_002c,
        IL_003c,
        IL_0032)
  IL_0024:  br.s       IL_0038
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.1
  IL_0028:  add
  IL_0029:  stloc.0
  IL_002a:  br.s       IL_003c
  IL_002c:  ldloc.0
  IL_002d:  ldc.i4.1
  IL_002e:  sub
  IL_002f:  stloc.0
  IL_0030:  br.s       IL_003c
  IL_0032:  ldloc.0
  IL_0033:  ldc.i4.7
  IL_0034:  add
  IL_0035:  stloc.0
  IL_0036:  br.s       IL_003c
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.2
  IL_003a:  add
  IL_003b:  stloc.0
  IL_003c:  ldloc.0
  IL_003d:  ret
}"
            );
        }

        [WorkItem(740058, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/740058")]
        [Fact]
        public void LongTypeSwitchArgumentExpressionOverflow()
        {
            var text = @"
using System;

public class Test
{
    public static void Main(string[] args)
    {
        string ret;
        ret = DoLong(long.MaxValue);
        Console.Write(ret);
        ret = DoLong(long.MinValue);
        Console.Write(ret);
        ret = DoLong(1L);
        Console.Write(ret);
        ret = DoLong(0L);
        Console.Write(ret);
    }

    private static string DoLong(long b)
    {
        switch (b)
        {
            case long.MaxValue:
                return ""max"";
            case 1L:
                return ""one"";
            case long.MinValue:
                return ""min"";
            default:
                return ""default"";
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "maxminonedefault");
            compVerifier.VerifyIL("Test.DoLong",
@"
{
  // Code size       53 (0x35)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i8     0x8000000000000000
  IL_000a:  beq.s      IL_0029
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.1
  IL_000e:  conv.i8
  IL_000f:  beq.s      IL_0023
  IL_0011:  ldarg.0
  IL_0012:  ldc.i8     0x7fffffffffffffff
  IL_001b:  bne.un.s   IL_002f
  IL_001d:  ldstr      ""max""
  IL_0022:  ret
  IL_0023:  ldstr      ""one""
  IL_0028:  ret
  IL_0029:  ldstr      ""min""
  IL_002e:  ret
  IL_002f:  ldstr      ""default""
  IL_0034:  ret
}"
            );
        }


        [Fact]
        public void ULongTypeSwitchArgumentExpression()
        {
            var text = @"
using System;

public class Test
{
    public static void Main(string[] args)
    {
        int ret = 0;
        ret = DoULong(0x1000000000000001L);
        Console.Write(ret);
    }

    private static int DoULong(ulong b)
    {
        int ret = 2;

        switch (b)
        {
            case 0:
                ret++;
                break;
            case 1:
                ret--;
                break;
            case 2:
                break;
            case 3:
                ret += 7;
                break;
            default:
                ret += 40;
                break;
        }

        return ret;
    }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "42");
            compVerifier.VerifyIL("Test.DoULong",
@"
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  dup
  IL_0004:  ldc.i4.3
  IL_0005:  conv.i8
  IL_0006:  ble.un.s   IL_000b
  IL_0008:  pop
  IL_0009:  br.s       IL_0035
  IL_000b:  conv.u4
  IL_000c:  switch    (
        IL_0023,
        IL_0029,
        IL_003a,
        IL_002f)
  IL_0021:  br.s       IL_0035
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.0
  IL_0027:  br.s       IL_003a
  IL_0029:  ldloc.0
  IL_002a:  ldc.i4.1
  IL_002b:  sub
  IL_002c:  stloc.0
  IL_002d:  br.s       IL_003a
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.7
  IL_0031:  add
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003a
  IL_0035:  ldloc.0
  IL_0036:  ldc.i4.s   40
  IL_0038:  add
  IL_0039:  stloc.0
  IL_003a:  ldloc.0
  IL_003b:  ret
}"
            );
        }


        [Fact]
        public void EnumTypeSwitchArgumentExpressionWithCasts()
        {
            var text = @"using System;

public class Test
{
    enum eTypes {
        kFirst,
        kSecond,
        kThird,
    };
    public static int Main(string [] args)
    {
        int ret = 0;
        ret = DoEnum();
        return(ret);
    }
    
    private static int DoEnum()
    {
        int ret = 2;
        eTypes e = eTypes.kSecond;

        switch (e) {
        case eTypes.kThird:
        case eTypes.kSecond:
            ret--;
            break;
        case (eTypes) (-1):
            break;
        default:
            break;
        }

        switch (e) {
        case (eTypes)100:
        case (eTypes) (-1):
            break;
        default:
            ret--;
            break;
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.DoEnum",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (int V_0, //ret
                Test.eTypes V_1) //e
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  ldc.i4.m1
  IL_0006:  beq.s      IL_0012
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.1
  IL_000a:  sub
  IL_000b:  ldc.i4.1
  IL_000c:  bgt.un.s   IL_0012
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  sub
  IL_0011:  stloc.0
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.m1
  IL_0014:  beq.s      IL_001f
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.s   100
  IL_0019:  beq.s      IL_001f
  IL_001b:  ldloc.0
  IL_001c:  ldc.i4.1
  IL_001d:  sub
  IL_001e:  stloc.0
  IL_001f:  ldloc.0
  IL_0020:  call       ""void System.Console.Write(int)""
  IL_0025:  ldloc.0
  IL_0026:  ret
}"
            );
        }

        [Fact]
        public void NullableEnumTypeSwitchArgumentExpression()
        {
            var text = @"using System;

public class Test
{
    enum eTypes {
        kFirst,
        kSecond,
        kThird,
    };
    public static int Main(string [] args)
    {
        int ret = 0;
        ret = DoEnum();
        return(ret);
    }
    
    private static int DoEnum()
    {
        int ret = 3;
        eTypes? e = eTypes.kSecond;

        switch (e)
        {
            case eTypes.kThird:
            case eTypes.kSecond:
                ret--;
                break;
            default:
                break;
        }

        switch (e)
        {
            case null:
            case (eTypes) (-1):
                break;
            default:
                ret--;
                break;
        }
        
        e = null;
        switch (e)
        {
            case null:
                ret--;
                break;
            case (eTypes) (-1):
            case eTypes.kThird:
            case eTypes.kSecond:
            default:
                break;
        }

        Console.Write(ret);
        return(ret);
    }
}";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.DoEnum", @"
{
  // Code size      109 (0x6d)
  .maxstack  2
  .locals init (int V_0, //ret
                Test.eTypes? V_1, //e
                Test.eTypes V_2)
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.1
  IL_0005:  call       ""Test.eTypes?..ctor(Test.eTypes)""
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""bool Test.eTypes?.HasValue.get""
  IL_0011:  brfalse.s  IL_0025
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""Test.eTypes Test.eTypes?.GetValueOrDefault()""
  IL_001a:  stloc.2
  IL_001b:  ldloc.2
  IL_001c:  ldc.i4.1
  IL_001d:  sub
  IL_001e:  ldc.i4.1
  IL_001f:  bgt.un.s   IL_0025
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.1
  IL_0023:  sub
  IL_0024:  stloc.0
  IL_0025:  ldloca.s   V_1
  IL_0027:  call       ""bool Test.eTypes?.HasValue.get""
  IL_002c:  brfalse.s  IL_003c
  IL_002e:  ldloca.s   V_1
  IL_0030:  call       ""Test.eTypes Test.eTypes?.GetValueOrDefault()""
  IL_0035:  ldc.i4.m1
  IL_0036:  beq.s      IL_003c
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.1
  IL_003a:  sub
  IL_003b:  stloc.0
  IL_003c:  ldloca.s   V_1
  IL_003e:  initobj    ""Test.eTypes?""
  IL_0044:  ldloca.s   V_1
  IL_0046:  call       ""bool Test.eTypes?.HasValue.get""
  IL_004b:  brfalse.s  IL_0061
  IL_004d:  ldloca.s   V_1
  IL_004f:  call       ""Test.eTypes Test.eTypes?.GetValueOrDefault()""
  IL_0054:  stloc.2
  IL_0055:  ldloc.2
  IL_0056:  ldc.i4.m1
  IL_0057:  beq.s      IL_0065
  IL_0059:  ldloc.2
  IL_005a:  ldc.i4.1
  IL_005b:  sub
  IL_005c:  ldc.i4.1
  IL_005d:  ble.un.s   IL_0065
  IL_005f:  br.s       IL_0065
  IL_0061:  ldloc.0
  IL_0062:  ldc.i4.1
  IL_0063:  sub
  IL_0064:  stloc.0
  IL_0065:  ldloc.0
  IL_0066:  call       ""void System.Console.Write(int)""
  IL_006b:  ldloc.0
  IL_006c:  ret
}");
        }

        [Fact]
        public void SwitchSectionWithGotoNonSwitchLabel()
        {
            var text = @"
class Test
{
    public static int Main()
    {
        int ret = 1;
        switch (10)
        {
            case 123:       // No unreachable code warning here
            mylabel:
                --ret;
                break;
            case 9:         // No unreachable code warning here
            case 10:
            case 11:        // No unreachable code warning here
                goto mylabel;
        }

        System.Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyDiagnostics();
            compVerifier.VerifyIL("Test.Main",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""void System.Console.Write(int)""
  IL_000c:  ldloc.0
  IL_000d:  ret
}"
            );
        }

        [Fact]
        public void SwitchSectionWithGotoNonSwitchLabel_02()
        {
            var text = @"class Test
{
    delegate void D();

    public static void Main()
    {
        int i = 0;

        switch (10)
        {
            case 5:
                goto mylabel;
            case 10:
                goto case 5;
            case 15:
            mylabel:
                D d1 = delegate() { System.Console.Write(i); };
                d1();
                return;
        }        
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Test.<>c__DisplayClass1_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Test.<>c__DisplayClass1_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Test.<>c__DisplayClass1_0.i""
  IL_000d:  ldloc.0
  IL_000e:  ldftn      ""void Test.<>c__DisplayClass1_0.<Main>b__0()""
  IL_0014:  newobj     ""Test.D..ctor(object, System.IntPtr)""
  IL_0019:  callvirt   ""void Test.D.Invoke()""
  IL_001e:  ret
}
"
            );
        }

        [Fact]
        public void SwitchSectionWithReturnStatement()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 3;

        for (int i = 0; i < 3; i++) {
            switch (i) {
            case 1:
            case 0:
            case 2:
                ret--;
                break;
            default:
                Console.Write(1);
                return(1);
            }
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.Main",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0, //ret
                int V_1) //i
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_001c
  IL_0006:  ldloc.1
  IL_0007:  ldc.i4.2
  IL_0008:  bgt.un.s   IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  sub
  IL_000d:  stloc.0
  IL_000e:  br.s       IL_0018
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  ldc.i4.1
  IL_0017:  ret
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  ldc.i4.3
  IL_001e:  blt.s      IL_0006
  IL_0020:  ldloc.0
  IL_0021:  call       ""void System.Console.Write(int)""
  IL_0026:  ldloc.0
  IL_0027:  ret
}"
            );
        }

        [Fact]
        public void SwitchSectionWithReturnStatement_02()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    int i = 5;

    switch (i)
    {
      case 1:
        return 1;
    }
    return 0;
  }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.1
  IL_0002:  bne.un.s   IL_0006
  IL_0004:  ldc.i4.1
  IL_0005:  ret
  IL_0006:  ldc.i4.0
  IL_0007:  ret
}"
            );
        }

        [Fact]
        public void CaseLabelWithTypeCastToGoverningType()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    int i = 5;

    switch (i)
    {
      case (int) 5.0f:
        return 0;
      default:
        return 1;
    }
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.5
  IL_0002:  bne.un.s   IL_0006
  IL_0004:  ldc.i4.0
  IL_0005:  ret
  IL_0006:  ldc.i4.1
  IL_0007:  ret
}"
            );
        }

        [Fact]
        public void SwitchSectionWithTryFinally()
        {
            var text = @"using System;

class Class1
{
    static int Main()
    {
        int j = 0;
        int i = 3;
        
        switch (j)
        {
            case 0:
                try
                {
                    i = 0;
                }
                catch
                {
                    i = 1;
                }
                finally
                {
                    j = 2;
                }
                break;

            default:
                i = 2;
                break;
        }

        Console.Write(i);
        return i;
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Class1.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.3
  IL_0002:  stloc.0
  IL_0003:  brtrue.s   IL_0010
  IL_0005:  nop
  .try
  {
    .try
    {
      IL_0006:  ldc.i4.0
      IL_0007:  stloc.0
      IL_0008:  leave.s    IL_0012
    }
    catch object
    {
      IL_000a:  pop
      IL_000b:  ldc.i4.1
      IL_000c:  stloc.0
      IL_000d:  leave.s    IL_0012
    }
  }
  finally
  {
    IL_000f:  endfinally
  }
  IL_0010:  ldc.i4.2
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  call       ""void System.Console.Write(int)""
  IL_0018:  ldloc.0
  IL_0019:  ret
}"
            );
        }

        [Fact]
        public void MultipleSwitchSectionsWithGotoCase()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = M();
        Console.Write(ret);
        return(ret);
    }

    public static int M()
    {
        int ret = 6;

        switch (ret) {
        case 0:
            ret--; // 2
            Console.Write(""case 0: "");
            Console.WriteLine(ret);
            goto case 9999;
        case 2:
            ret--; // 4
            Console.Write(""case 2: "");
            Console.WriteLine(ret);
            goto case 255;
        case 6: // start here
            ret--; // 5
            Console.Write(""case 5: "");
            Console.WriteLine(ret);
            goto case 2;
        case 9999:
            ret--; // 1
            Console.Write(""case 9999: "");
            Console.WriteLine(ret);
            goto default;
        case 0xff:
            ret--; // 3
            Console.Write(""case 0xff: "");
            Console.WriteLine(ret);
            goto case 0;
        default:
            ret--;
            Console.Write(""Default: "");
            Console.WriteLine(ret);
            if (ret > 0) {
                goto case -1;
            }
            break;
        case -1:
            ret = 999;
            Console.WriteLine(""case -1: "");
            Console.Write(ret);
            break;
        }

        return(ret);
    }
}";
            string expectedOutput = @"case 5: 5
case 2: 4
case 0xff: 3
case 0: 2
case 9999: 1
Default: 0
0";
            var compVerifier = CompileAndVerify(text, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.M",
@"
{
  // Code size      215 (0xd7)
  .maxstack  2
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.6
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.6
  IL_0004:  bgt.s      IL_0027
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.m1
  IL_0008:  sub
  IL_0009:  switch    (
        IL_00bf,
        IL_0039,
        IL_00a7,
        IL_004f)
  IL_001e:  ldloc.0
  IL_001f:  ldc.i4.6
  IL_0020:  beq.s      IL_0065
  IL_0022:  br         IL_00a7
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4     0xff
  IL_002d:  beq.s      IL_0091
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4     0x270f
  IL_0035:  beq.s      IL_007b
  IL_0037:  br.s       IL_00a7
  IL_0039:  ldloc.0
  IL_003a:  ldc.i4.1
  IL_003b:  sub
  IL_003c:  stloc.0
  IL_003d:  ldstr      ""case 0: ""
  IL_0042:  call       ""void System.Console.Write(string)""
  IL_0047:  ldloc.0
  IL_0048:  call       ""void System.Console.WriteLine(int)""
  IL_004d:  br.s       IL_007b
  IL_004f:  ldloc.0
  IL_0050:  ldc.i4.1
  IL_0051:  sub
  IL_0052:  stloc.0
  IL_0053:  ldstr      ""case 2: ""
  IL_0058:  call       ""void System.Console.Write(string)""
  IL_005d:  ldloc.0
  IL_005e:  call       ""void System.Console.WriteLine(int)""
  IL_0063:  br.s       IL_0091
  IL_0065:  ldloc.0
  IL_0066:  ldc.i4.1
  IL_0067:  sub
  IL_0068:  stloc.0
  IL_0069:  ldstr      ""case 5: ""
  IL_006e:  call       ""void System.Console.Write(string)""
  IL_0073:  ldloc.0
  IL_0074:  call       ""void System.Console.WriteLine(int)""
  IL_0079:  br.s       IL_004f
  IL_007b:  ldloc.0
  IL_007c:  ldc.i4.1
  IL_007d:  sub
  IL_007e:  stloc.0
  IL_007f:  ldstr      ""case 9999: ""
  IL_0084:  call       ""void System.Console.Write(string)""
  IL_0089:  ldloc.0
  IL_008a:  call       ""void System.Console.WriteLine(int)""
  IL_008f:  br.s       IL_00a7
  IL_0091:  ldloc.0
  IL_0092:  ldc.i4.1
  IL_0093:  sub
  IL_0094:  stloc.0
  IL_0095:  ldstr      ""case 0xff: ""
  IL_009a:  call       ""void System.Console.Write(string)""
  IL_009f:  ldloc.0
  IL_00a0:  call       ""void System.Console.WriteLine(int)""
  IL_00a5:  br.s       IL_0039
  IL_00a7:  ldloc.0
  IL_00a8:  ldc.i4.1
  IL_00a9:  sub
  IL_00aa:  stloc.0
  IL_00ab:  ldstr      ""Default: ""
  IL_00b0:  call       ""void System.Console.Write(string)""
  IL_00b5:  ldloc.0
  IL_00b6:  call       ""void System.Console.WriteLine(int)""
  IL_00bb:  ldloc.0
  IL_00bc:  ldc.i4.0
  IL_00bd:  ble.s      IL_00d5
  IL_00bf:  ldc.i4     0x3e7
  IL_00c4:  stloc.0
  IL_00c5:  ldstr      ""case -1: ""
  IL_00ca:  call       ""void System.Console.WriteLine(string)""
  IL_00cf:  ldloc.0
  IL_00d0:  call       ""void System.Console.Write(int)""
  IL_00d5:  ldloc.0
  IL_00d6:  ret
}"
            );
        }

        [Fact]
        public void Switch_TestSwitchBuckets_01()
        {
            var text = @"using System;
public class Test
{
  public static int Main(string [] args)
  {
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    int i = 0;

    // Expected switch buckets: (10, 12, 14) (1000) (2000, 2001, 2005, 2008, 2009, 2010)
    switch (i)
    {
      case 10:
      case 2000:
      case 12:
        return 1;
      case 2001:
      case 14:
        return 2;
      case 1000:
      case 2010:
      case 2008:
        return 3;
      case 2005:
      case 2009:
        return 2;
    }
    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size      107 (0x6b)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.s   10
  IL_0005:  sub
  IL_0006:  switch    (
        IL_0061,
        IL_0069,
        IL_0061,
        IL_0069,
        IL_0063)
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4     0x3e8
  IL_0025:  beq.s      IL_0065
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4     0x7d0
  IL_002d:  sub
  IL_002e:  switch    (
        IL_0061,
        IL_0063,
        IL_0069,
        IL_0069,
        IL_0069,
        IL_0067,
        IL_0069,
        IL_0069,
        IL_0065,
        IL_0067,
        IL_0065)
  IL_005f:  br.s       IL_0069
  IL_0061:  ldc.i4.1
  IL_0062:  ret
  IL_0063:  ldc.i4.2
  IL_0064:  ret
  IL_0065:  ldc.i4.3
  IL_0066:  ret
  IL_0067:  ldc.i4.2
  IL_0068:  ret
  IL_0069:  ldc.i4.0
  IL_006a:  ret
}"
            );
        }

        [Fact]
        public void Switch_TestSwitchBuckets_02()
        {
            var text = @"using System;
public class Test
{
  public static int Main(string [] args)
  {
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    int i = 0;

    // Expected switch buckets: (10, 12, 14) (1000) (2000, 2001) (2008, 2009, 2010)
    switch (i)
    {
      case 10:
      case 2000:
      case 12:
        return 1;
      case 2001:
      case 14:
        return 2;
      case 1000:
      case 2010:
      case 2008:
        return 3;
      case 2009:
        return 2;
    }
    return 0;
  }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size      101 (0x65)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4     0x3e8
  IL_0008:  bgt.s      IL_0031
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.s   10
  IL_000d:  sub
  IL_000e:  switch    (
        IL_005b,
        IL_0063,
        IL_005b,
        IL_0063,
        IL_005d)
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4     0x3e8
  IL_002d:  beq.s      IL_005f
  IL_002f:  br.s       IL_0063
  IL_0031:  ldloc.0
  IL_0032:  ldc.i4     0x7d0
  IL_0037:  beq.s      IL_005b
  IL_0039:  ldloc.0
  IL_003a:  ldc.i4     0x7d1
  IL_003f:  beq.s      IL_005d
  IL_0041:  ldloc.0
  IL_0042:  ldc.i4     0x7d8
  IL_0047:  sub
  IL_0048:  switch    (
        IL_005f,
        IL_0061,
        IL_005f)
  IL_0059:  br.s       IL_0063
  IL_005b:  ldc.i4.1
  IL_005c:  ret
  IL_005d:  ldc.i4.2
  IL_005e:  ret
  IL_005f:  ldc.i4.3
  IL_0060:  ret
  IL_0061:  ldc.i4.2
  IL_0062:  ret
  IL_0063:  ldc.i4.0
  IL_0064:  ret
}"
            );
        }

        [WorkItem(542398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542398")]
        [Fact]
        public void MaxValueGotoCaseExpression()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        ulong a = 0;
        switch (a)
        {
            case long.MaxValue:                
                goto case ulong.MaxValue;
            case ulong.MaxValue:
                break;
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "");
        }

        [WorkItem(543967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543967")]
        [Fact()]
        public void NullableAsSwitchExpression()
        {
            var text = @"using System;
class Program
{
    static void Main()
    {
        sbyte? local = null;
        switch (local)
        {
            case null:
                Console.Write(""null "");
                break;
            case 1:
                Console.Write(""1 "");
                break;
        }

        Goo(1);
    }

    static void Goo(sbyte? p)
    {
        switch (p)
        {
            case 1:
                Console.Write(""1 "");
                break;
            case null:
                Console.Write(""null "");
                break;
        }
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: "null 1");
            verifier.VerifyIL("Program.Main", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (sbyte? V_0) //local
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""sbyte?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool sbyte?.HasValue.get""
  IL_000f:  brfalse.s  IL_001d
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""sbyte sbyte?.GetValueOrDefault()""
  IL_0018:  ldc.i4.1
  IL_0019:  beq.s      IL_0029
  IL_001b:  br.s       IL_0033
  IL_001d:  ldstr      ""null ""
  IL_0022:  call       ""void System.Console.Write(string)""
  IL_0027:  br.s       IL_0033
  IL_0029:  ldstr      ""1 ""
  IL_002e:  call       ""void System.Console.Write(string)""
  IL_0033:  ldc.i4.1
  IL_0034:  conv.i1
  IL_0035:  newobj     ""sbyte?..ctor(sbyte)""
  IL_003a:  call       ""void Program.Goo(sbyte?)""
  IL_003f:  ret
}");
        }

        [WorkItem(543967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543967")]
        [Fact()]
        public void NullableAsSwitchExpression_02()
        {
            var text = @"using System;
class Program
{
    static void Main()
    {
        Goo(null);
        Goo(100);
    }

    static void Goo(short? p)
    {
        switch (p)
        {
            case null:
                Console.Write(""null "");
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            case 100:
                Console.Write(""100 "");
                break;
            case 101:
                break;
            case 103:
                break;
            case 1001:
                break;
            case 1002:
                break;
            case 1003:
                break;
        }

        switch (p)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            case 101:
                break;
            case 103:
                break;
            case 1001:
                break;
            case 1002:
                break;
            case 1003:
                break;
            default:
                Console.Write(""default "");
                break;
        }

        switch (p)
        {
            case 1:
                Console.Write(""FAIL"");
                break;
            case 2:
                Console.Write(""FAIL"");
                break;
            case 3:
                Console.Write(""FAIL"");
                break;
            case 101:
                Console.Write(""FAIL"");
                break;
            case 103:
                Console.Write(""FAIL"");
                break;
            case 1001:
                Console.Write(""FAIL"");
                break;
            case 1002:
                Console.Write(""FAIL"");
                break;
            case 1003:
                Console.Write(""FAIL"");
                break;
        }
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: "null default 100 default ");
            verifier.VerifyIL("Program.Goo", @"
{
  // Code size      367 (0x16f)
  .maxstack  2
  .locals init (short V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool short?.HasValue.get""
  IL_0007:  brfalse.s  IL_0058
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""short short?.GetValueOrDefault()""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.1
  IL_0013:  sub
  IL_0014:  switch    (
        IL_006e,
        IL_006e,
        IL_006e)
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.s   100
  IL_0028:  sub
  IL_0029:  switch    (
        IL_0064,
        IL_006e,
        IL_006e,
        IL_006e)
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4     0x3e9
  IL_0044:  sub
  IL_0045:  switch    (
        IL_006e,
        IL_006e,
        IL_006e)
  IL_0056:  br.s       IL_006e
  IL_0058:  ldstr      ""null ""
  IL_005d:  call       ""void System.Console.Write(string)""
  IL_0062:  br.s       IL_006e
  IL_0064:  ldstr      ""100 ""
  IL_0069:  call       ""void System.Console.Write(string)""
  IL_006e:  ldarga.s   V_0
  IL_0070:  call       ""bool short?.HasValue.get""
  IL_0075:  brfalse.s  IL_00bc
  IL_0077:  ldarga.s   V_0
  IL_0079:  call       ""short short?.GetValueOrDefault()""
  IL_007e:  stloc.0
  IL_007f:  ldloc.0
  IL_0080:  ldc.i4.s   101
  IL_0082:  bgt.s      IL_009f
  IL_0084:  ldloc.0
  IL_0085:  ldc.i4.1
  IL_0086:  sub
  IL_0087:  switch    (
        IL_00c6,
        IL_00c6,
        IL_00c6)
  IL_0098:  ldloc.0
  IL_0099:  ldc.i4.s   101
  IL_009b:  beq.s      IL_00c6
  IL_009d:  br.s       IL_00bc
  IL_009f:  ldloc.0
  IL_00a0:  ldc.i4.s   103
  IL_00a2:  beq.s      IL_00c6
  IL_00a4:  ldloc.0
  IL_00a5:  ldc.i4     0x3e9
  IL_00aa:  sub
  IL_00ab:  switch    (
        IL_00c6,
        IL_00c6,
        IL_00c6)
  IL_00bc:  ldstr      ""default ""
  IL_00c1:  call       ""void System.Console.Write(string)""
  IL_00c6:  ldarga.s   V_0
  IL_00c8:  call       ""bool short?.HasValue.get""
  IL_00cd:  brfalse    IL_016e
  IL_00d2:  ldarga.s   V_0
  IL_00d4:  call       ""short short?.GetValueOrDefault()""
  IL_00d9:  stloc.0
  IL_00da:  ldloc.0
  IL_00db:  ldc.i4.s   101
  IL_00dd:  bgt.s      IL_00f9
  IL_00df:  ldloc.0
  IL_00e0:  ldc.i4.1
  IL_00e1:  sub
  IL_00e2:  switch    (
        IL_0117,
        IL_0122,
        IL_012d)
  IL_00f3:  ldloc.0
  IL_00f4:  ldc.i4.s   101
  IL_00f6:  beq.s      IL_0138
  IL_00f8:  ret
  IL_00f9:  ldloc.0
  IL_00fa:  ldc.i4.s   103
  IL_00fc:  beq.s      IL_0143
  IL_00fe:  ldloc.0
  IL_00ff:  ldc.i4     0x3e9
  IL_0104:  sub
  IL_0105:  switch    (
        IL_014e,
        IL_0159,
        IL_0164)
  IL_0116:  ret
  IL_0117:  ldstr      ""FAIL""
  IL_011c:  call       ""void System.Console.Write(string)""
  IL_0121:  ret
  IL_0122:  ldstr      ""FAIL""
  IL_0127:  call       ""void System.Console.Write(string)""
  IL_012c:  ret
  IL_012d:  ldstr      ""FAIL""
  IL_0132:  call       ""void System.Console.Write(string)""
  IL_0137:  ret
  IL_0138:  ldstr      ""FAIL""
  IL_013d:  call       ""void System.Console.Write(string)""
  IL_0142:  ret
  IL_0143:  ldstr      ""FAIL""
  IL_0148:  call       ""void System.Console.Write(string)""
  IL_014d:  ret
  IL_014e:  ldstr      ""FAIL""
  IL_0153:  call       ""void System.Console.Write(string)""
  IL_0158:  ret
  IL_0159:  ldstr      ""FAIL""
  IL_015e:  call       ""void System.Console.Write(string)""
  IL_0163:  ret
  IL_0164:  ldstr      ""FAIL""
  IL_0169:  call       ""void System.Console.Write(string)""
  IL_016e:  ret
}");
        }

        [Fact, WorkItem(7625, "https://github.com/dotnet/roslyn/issues/7625")]
        public void SwitchOnNullableInt64WithInt32Label()
        {
            var text = @"public static class C
{
    public static bool F(long? x)
    {
        switch (x)
        {
            case 1:
                return true;
            default:
                return false;
        }
    }
    static void Main()
    {
        System.Console.WriteLine(F(1));
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "True");
            compVerifier.VerifyIL("C.F(long?)",
@"{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool long?.HasValue.get""
  IL_0007:  brfalse.s  IL_0016
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""long long?.GetValueOrDefault()""
  IL_0010:  ldc.i4.1
  IL_0011:  conv.i8
  IL_0012:  bne.un.s   IL_0016
  IL_0014:  ldc.i4.1
  IL_0015:  ret
  IL_0016:  ldc.i4.0
  IL_0017:  ret
}"
            );
        }

        [Fact, WorkItem(7625, "https://github.com/dotnet/roslyn/issues/7625")]
        public void SwitchOnNullableWithNonConstant()
        {
            var text = @"public static class C
{
    public static bool F(int? x)
    {
        int i = 4;
        switch (x)
        {
            case i:
                return true;
            default:
                return false;
        }
    }
    static void Main()
    {
        System.Console.WriteLine(F(1));
    }
}";
            var compilation = base.CreateCSharpCompilation(text);
            compilation.VerifyDiagnostics(
                // (8,18): error CS0150: A constant value is expected
                //             case i:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "i").WithLocation(8, 18)
                );
        }

        [Fact, WorkItem(7625, "https://github.com/dotnet/roslyn/issues/7625")]
        public void SwitchOnNullableWithNonCompatibleType()
        {
            var text = @"public static class C
{
    public static bool F(int? x)
    {
        switch (x)
        {
            case default(System.DateTime):
                return true;
            default:
                return false;
        }
    }
    static void Main()
    {
        System.Console.WriteLine(F(1));
    }
}";
            var compilation = base.CreateCSharpCompilation(text);

            // (7,18): error CS0029: Cannot implicitly convert type 'System.DateTime' to 'int?'
            //             case default(System.DateTime):
            var expected = Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(System.DateTime)").WithArguments("System.DateTime", "int?");

            compilation.VerifyDiagnostics(expected);
        }

        [Fact]
        public void SwitchOnNullableInt64WithInt32LabelWithEnum()
        {
            var text = @"public static class C
{
    public static bool F(long? x)
    {
        switch (x)
        {
            case (int)Goo.X:
                return true;
            default:
                return false;
        }
    }

    public enum Goo : int { X  = 1 }

    static void Main()
    {
        System.Console.WriteLine(F(1));
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "True");
            compVerifier.VerifyIL("C.F(long?)",
@"{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool long?.HasValue.get""
  IL_0007:  brfalse.s  IL_0016
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""long long?.GetValueOrDefault()""
  IL_0010:  ldc.i4.1
  IL_0011:  conv.i8
  IL_0012:  bne.un.s   IL_0016
  IL_0014:  ldc.i4.1
  IL_0015:  ret
  IL_0016:  ldc.i4.0
  IL_0017:  ret
}"
            );
        }

        // TODO: Add more targeted tests for verifying switch bucketing

        #region "String tests"

        [Fact]
        public void StringTypeSwitchArgumentExpression()
        {
            var text = @"using System;

public class Test
{

  public static int Main(string [] args)
  {
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    string s = ""hello"";
    switch (s)
    {
      default:
        return 1;
      case null:
        return 1;
      case ""hello"":
        return 0;
    }
    return 1;
  }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
            // (25,5): warning CS0162: Unreachable code detected
            //     return 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"));

            compVerifier.VerifyIL("Test.M", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //s
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0018
  IL_0009:  ldloc.0
  IL_000a:  ldstr      ""hello""
  IL_000f:  call       ""bool string.op_Equality(string, string)""
  IL_0014:  brtrue.s   IL_001a
  IL_0016:  ldc.i4.1
  IL_0017:  ret
  IL_0018:  ldc.i4.1
  IL_0019:  ret
  IL_001a:  ldc.i4.0
  IL_001b:  ret
}"
            );

            // We shouldn't generate a string hash synthesized method if we are generating non hash string switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: false);
        }

        [Fact]
        public void StringSwitch_SwitchOnNull()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string[] args)
    {
        string s = null;
        int ret = 0;

        switch (s)
        {
            case null + ""abc"":
                ret = 1;
                break;

            case null + ""def"":
                ret = 1;
                break;
        }

        Console.Write(ret);
        return ret;
    }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (string V_0, //s
                int V_1) //ret
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  brfalse.s  IL_0029
  IL_0007:  ldloc.0
  IL_0008:  ldstr      ""abc""
  IL_000d:  call       ""bool string.op_Equality(string, string)""
  IL_0012:  brtrue.s   IL_0023
  IL_0014:  ldloc.0
  IL_0015:  ldstr      ""def""
  IL_001a:  call       ""bool string.op_Equality(string, string)""
  IL_001f:  brtrue.s   IL_0027
  IL_0021:  br.s       IL_0029
  IL_0023:  ldc.i4.1
  IL_0024:  stloc.1
  IL_0025:  br.s       IL_0029
  IL_0027:  ldc.i4.1
  IL_0028:  stloc.1
  IL_0029:  ldloc.1
  IL_002a:  call       ""void System.Console.Write(int)""
  IL_002f:  ldloc.1
  IL_0030:  ret
}"
            );

            // We shouldn't generate a string hash synthesized method if we are generating non hash string switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: false);
        }

        [Fact]
        [WorkItem(546632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546632")]
        public void StringSwitch_HashTableSwitch_01()
        {
            var text = @"using System;

class Test
{
    public static bool M(string test)
    {
        string	value = """";

        if (test != null && test.IndexOf(""C#"") != -1)
            test = test.Remove(0, 2);

        switch (test)
        {
            case null:
                value = null;
                break;
            case """":
                break;
            case ""_"":
                value = ""_"";
                break;
            case ""W"":
                value = ""W"";
                break;
            case ""B"":
                value = ""B"";
                break;
            case ""C"":
                value = ""C"";
                break;
            case ""<"":
                value = ""<"";
                break;
            case ""T"":
                value = ""T"";
                break;
            case ""M"":
                value = ""M"";
                break;
        }

        return (value == test);
    }
    public static void Main()
    {
        bool success = Test.M(""C#"");
        success &= Test.M(""C#_"");
        success &= Test.M(""C#W"");
        success &= Test.M(""C#B"");
        success &= Test.M(""C#C"");
        success &= Test.M(""C#<"");
        success &= Test.M(""C#T"");
        success &= Test.M(""C#M"");
        success &= Test.M(null);
        Console.WriteLine(success);
    }
}";
            var compVerifier = CompileAndVerify(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: "True");

            compVerifier.VerifyIL("Test.M", @"
{
  // Code size      365 (0x16d)
  .maxstack  3
  .locals init (string V_0, //value
                uint V_1)
  IL_0000:  ldstr      """"
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  brfalse.s  IL_0021
  IL_0009:  ldarg.0
  IL_000a:  ldstr      ""C#""
  IL_000f:  callvirt   ""int string.IndexOf(string)""
  IL_0014:  ldc.i4.m1
  IL_0015:  beq.s      IL_0021
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.0
  IL_0019:  ldc.i4.2
  IL_001a:  callvirt   ""string string.Remove(int, int)""
  IL_001f:  starg.s    V_0
  IL_0021:  ldarg.0
  IL_0022:  brfalse    IL_012b
  IL_0027:  ldarg.0
  IL_0028:  call       ""ComputeStringHash""
  IL_002d:  stloc.1
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4     0xc70bfb85
  IL_0034:  bgt.un.s   IL_006e
  IL_0036:  ldloc.1
  IL_0037:  ldc.i4     0x811c9dc5
  IL_003c:  bgt.un.s   IL_0056
  IL_003e:  ldloc.1
  IL_003f:  ldc.i4     0x390caefb
  IL_0044:  beq        IL_00fe
  IL_0049:  ldloc.1
  IL_004a:  ldc.i4     0x811c9dc5
  IL_004f:  beq.s      IL_00a6
  IL_0051:  br         IL_0165
  IL_0056:  ldloc.1
  IL_0057:  ldc.i4     0xc60bf9f2
  IL_005c:  beq        IL_00ef
  IL_0061:  ldloc.1
  IL_0062:  ldc.i4     0xc70bfb85
  IL_0067:  beq.s      IL_00e0
  IL_0069:  br         IL_0165
  IL_006e:  ldloc.1
  IL_006f:  ldc.i4     0xd10c0b43
  IL_0074:  bgt.un.s   IL_0091
  IL_0076:  ldloc.1
  IL_0077:  ldc.i4     0xc80bfd18
  IL_007c:  beq        IL_011c
  IL_0081:  ldloc.1
  IL_0082:  ldc.i4     0xd10c0b43
  IL_0087:  beq        IL_010d
  IL_008c:  br         IL_0165
  IL_0091:  ldloc.1
  IL_0092:  ldc.i4     0xd20c0cd6
  IL_0097:  beq.s      IL_00ce
  IL_0099:  ldloc.1
  IL_009a:  ldc.i4     0xda0c196e
  IL_009f:  beq.s      IL_00bc
  IL_00a1:  br         IL_0165
  IL_00a6:  ldarg.0
  IL_00a7:  brfalse    IL_0165
  IL_00ac:  ldarg.0
  IL_00ad:  call       ""int string.Length.get""
  IL_00b2:  brfalse    IL_0165
  IL_00b7:  br         IL_0165
  IL_00bc:  ldarg.0
  IL_00bd:  ldstr      ""_""
  IL_00c2:  call       ""bool string.op_Equality(string, string)""
  IL_00c7:  brtrue.s   IL_012f
  IL_00c9:  br         IL_0165
  IL_00ce:  ldarg.0
  IL_00cf:  ldstr      ""W""
  IL_00d4:  call       ""bool string.op_Equality(string, string)""
  IL_00d9:  brtrue.s   IL_0137
  IL_00db:  br         IL_0165
  IL_00e0:  ldarg.0
  IL_00e1:  ldstr      ""B""
  IL_00e6:  call       ""bool string.op_Equality(string, string)""
  IL_00eb:  brtrue.s   IL_013f
  IL_00ed:  br.s       IL_0165
  IL_00ef:  ldarg.0
  IL_00f0:  ldstr      ""C""
  IL_00f5:  call       ""bool string.op_Equality(string, string)""
  IL_00fa:  brtrue.s   IL_0147
  IL_00fc:  br.s       IL_0165
  IL_00fe:  ldarg.0
  IL_00ff:  ldstr      ""<""
  IL_0104:  call       ""bool string.op_Equality(string, string)""
  IL_0109:  brtrue.s   IL_014f
  IL_010b:  br.s       IL_0165
  IL_010d:  ldarg.0
  IL_010e:  ldstr      ""T""
  IL_0113:  call       ""bool string.op_Equality(string, string)""
  IL_0118:  brtrue.s   IL_0157
  IL_011a:  br.s       IL_0165
  IL_011c:  ldarg.0
  IL_011d:  ldstr      ""M""
  IL_0122:  call       ""bool string.op_Equality(string, string)""
  IL_0127:  brtrue.s   IL_015f
  IL_0129:  br.s       IL_0165
  IL_012b:  ldnull
  IL_012c:  stloc.0
  IL_012d:  br.s       IL_0165
  IL_012f:  ldstr      ""_""
  IL_0134:  stloc.0
  IL_0135:  br.s       IL_0165
  IL_0137:  ldstr      ""W""
  IL_013c:  stloc.0
  IL_013d:  br.s       IL_0165
  IL_013f:  ldstr      ""B""
  IL_0144:  stloc.0
  IL_0145:  br.s       IL_0165
  IL_0147:  ldstr      ""C""
  IL_014c:  stloc.0
  IL_014d:  br.s       IL_0165
  IL_014f:  ldstr      ""<""
  IL_0154:  stloc.0
  IL_0155:  br.s       IL_0165
  IL_0157:  ldstr      ""T""
  IL_015c:  stloc.0
  IL_015d:  br.s       IL_0165
  IL_015f:  ldstr      ""M""
  IL_0164:  stloc.0
  IL_0165:  ldloc.0
  IL_0166:  ldarg.0
  IL_0167:  call       ""bool string.op_Equality(string, string)""
  IL_016c:  ret
}"
            );

            // Verify string hash synthesized method for hash table switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: true);

            // verify that hash method is internal:
            var reference = compVerifier.Compilation.EmitToImageReference();
            var comp = CSharpCompilation.Create("Name", references: new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var pid = ((NamedTypeSymbol)comp.GlobalNamespace.GetMembers().Single(s => s.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)));
            var member = pid.GetMembers(PrivateImplementationDetails.SynthesizedStringHashFunctionName).Single();
            Assert.Equal(Accessibility.Internal, member.DeclaredAccessibility);
        }

        [Fact]
        public void StringSwitch_HashTableSwitch_02()
        {
            var text = @"
using System;
using System.Text;

class Test
{
    public static bool Switcheroo(string test)
    {
        string	value = """";

        if (test.IndexOf(""C#"") != -1)
            test = test.Remove(0, 2);

        switch (test)
        {
            case ""N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9"":
                value = ""N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9"";
                break;
            case ""emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I"":
                value = ""emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I"";
                break;
            case ""Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m"":
                value = ""Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m"";
                break;
            case ""[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD"":
                value = ""[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD"";
                break;
            case ""<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]"":
                value = ""<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]"";
                break;
            case ""[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c"":
                value = ""[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c"";
                break;
            case ""d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0"":
                value = ""d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0"";
                break;
            case ""n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>"":
                value = ""n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>"";
                break;
            case ""H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF"":
                value = ""H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF"";
                break;
            case ""iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:"":
                value = ""iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:"";
                break;
            case "">AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj"":
                value = "">AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj"";
                break;
            case ""d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:"":
                value = ""d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:"";
                break;
            case ""fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU"":
                value = ""fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU"";
                break;
            case ""9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR"":
                value = ""9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR"";
                break;
            case ""A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj"":
                value = ""A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj"";
                break;
            case ""SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k"":
                value = ""SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k"";
                break;
            case ""0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4"":
                value = ""0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4"";
                break;
            case ""4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O"":
                value = ""4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O"";
                break;
            case ""EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe"":
                value = ""EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe"";
                break;
            case ""?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM"":
                value = ""?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM"";
                break;
            case ""HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W"":
                value = ""HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W"";
                break;
            case ""XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D"":
                value = ""XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D"";
                break;
            case ""MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9"":
                value = ""MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9"";
                break;
            case ""L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4"":
                value = ""L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4"";
                break;
        }

        return (value == test);
    }
    public static void Main()
    {
        string status = ""PASS"";
        bool retval;
        retval = Test.Switcheroo(""C#N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#>AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4"");
        if (!retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#a@=FkgImdk5<Wn0DRYa?m0<F0JT4kha;H:HIZ;6C"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#Zbk^]59O<<GHe8MjRMOh4]c3@RQ?hU>^G81cOMW:"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#hP<l25H@W60UF4bYYDU]0AjIE6oCQ^k66F9gNJ`Q"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#XS9dCIb;9T`;JJ1Jmimba@@0[l[B=BgDhKZ05DO2"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#JbMbko?;e@1XLU>:=c_Vg>0YTJ7Qd]6KLh26miBV"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#IW3:>L<H<kf:AS2ZYDGaE`?^HZY_D]cRO[lNjd4;"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#NC>J^E3;VJ`nKSjVbJ_Il^^@0Xof9CFA2I1I^9c>"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#TfjQOCnAhM8[T3JUbLlQMS=`F=?:FPT3=X0Q]aj:"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#H_6fHA8OKO><TYDXiIg[Qed<=71KC>^6cTMOjT]d"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#jN>cSCF0?I<1RSQ^g^HnBABPhUc>5Y`ahlY9HS]5"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#V;`Q_kEGIX=Mh9=UVF[Q@Q=QTT@oC]IRF]<bA1R9"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#7DKT?2VIk2^XUJ>C]G_IDe?299DJTD>1RO18Ql>F"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#U`^]IeJC;o^90V=5<ToV<Gj26hnZLolffohc8iZX"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#9S?>A?E>gBl_dC[iCiiNnW7<BP;eGHf>8ceTGZ6C"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#ALLhjN7]XVBBA=VcYM8iWg^FGiG[QG03dlKYnIAe"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#6<]i]EllPZf6mnAM0D1;0eP6_G1HmRSi?`1o[a_a"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#]5Tb^6:NB]>ahEoWI5U9N5beS<?da]A]fL08AelQ"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#KhPBV8=H?G^Hmaaf^n<GcoI8eC1O_0]579;MY=81"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#A8iN_OFUPIcWac0^LU1;^HaEX[_E]<8h3N:Hm_XX"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#Y811aKWEJYcX6Y1aj_I]O7TXP5j_lo;71kAiB:;:"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#P@ok2DgbUDeLVA:POd`B_S@2Ocg99VQBZ<LI<gd1"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#9V84FSRoD9453VdERM86a6B12VeN]hNNU:]XE`W9"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#Tegah0mKcWmFhaH0K0oSjKGkmH8gDEF3SBVd2H1P"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#=II;VADkVKf7JV55ca5oUjkPaWSY7`LXTlgTV4^W"");
        if (retval) status = ""FAIL"";
        retval = Test.Switcheroo(""C#k7XPoXNhd8P0V7@Sd5ohO>h7io3Pl[J[8g:[_da^"");
        if (retval) status = ""FAIL"";
        Console.Write(status);
    }
}";
            var compVerifier = CompileAndVerify(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: "PASS");

            compVerifier.VerifyIL("Test.Switcheroo", @"
{
  // Code size     1121 (0x461)
  .maxstack  3
  .locals init (string V_0, //value
                uint V_1)
  IL_0000:  ldstr      """"
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldstr      ""C#""
  IL_000c:  callvirt   ""int string.IndexOf(string)""
  IL_0011:  ldc.i4.m1
  IL_0012:  beq.s      IL_001e
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.0
  IL_0016:  ldc.i4.2
  IL_0017:  callvirt   ""string string.Remove(int, int)""
  IL_001c:  starg.s    V_0
  IL_001e:  ldarg.0
  IL_001f:  brfalse    IL_0459
  IL_0024:  ldarg.0
  IL_0025:  call       ""ComputeStringHash""
  IL_002a:  stloc.1
  IL_002b:  ldloc.1
  IL_002c:  ldc.i4     0xb2f29419
  IL_0031:  bgt.un     IL_00e6
  IL_0036:  ldloc.1
  IL_0037:  ldc.i4     0x619348d8
  IL_003c:  bgt.un.s   IL_0092
  IL_003e:  ldloc.1
  IL_003f:  ldc.i4     0x36758e37
  IL_0044:  bgt.un.s   IL_006c
  IL_0046:  ldloc.1
  IL_0047:  ldc.i4     0x144fd20d
  IL_004c:  beq        IL_02b4
  IL_0051:  ldloc.1
  IL_0052:  ldc.i4     0x14ca99e2
  IL_0057:  beq        IL_0260
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4     0x36758e37
  IL_0062:  beq        IL_020c
  IL_0067:  br         IL_0459
  IL_006c:  ldloc.1
  IL_006d:  ldc.i4     0x5398a778
  IL_0072:  beq        IL_0221
  IL_0077:  ldloc.1
  IL_0078:  ldc.i4     0x616477cf
  IL_007d:  beq        IL_01b8
  IL_0082:  ldloc.1
  IL_0083:  ldc.i4     0x619348d8
  IL_0088:  beq        IL_02f3
  IL_008d:  br         IL_0459
  IL_0092:  ldloc.1
  IL_0093:  ldc.i4     0x78a826a8
  IL_0098:  bgt.un.s   IL_00c0
  IL_009a:  ldloc.1
  IL_009b:  ldc.i4     0x65b3e3e5
  IL_00a0:  beq        IL_02c9
  IL_00a5:  ldloc.1
  IL_00a6:  ldc.i4     0x7822b5bc
  IL_00ab:  beq        IL_028a
  IL_00b0:  ldloc.1
  IL_00b1:  ldc.i4     0x78a826a8
  IL_00b6:  beq        IL_01e2
  IL_00bb:  br         IL_0459
  IL_00c0:  ldloc.1
  IL_00c1:  ldc.i4     0x7f66da4e
  IL_00c6:  beq        IL_035c
  IL_00cb:  ldloc.1
  IL_00cc:  ldc.i4     0xb13d374d
  IL_00d1:  beq        IL_0332
  IL_00d6:  ldloc.1
  IL_00d7:  ldc.i4     0xb2f29419
  IL_00dc:  beq        IL_0308
  IL_00e1:  br         IL_0459
  IL_00e6:  ldloc.1
  IL_00e7:  ldc.i4     0xd59864f4
  IL_00ec:  bgt.un.s   IL_0142
  IL_00ee:  ldloc.1
  IL_00ef:  ldc.i4     0xbf4a9f8e
  IL_00f4:  bgt.un.s   IL_011c
  IL_00f6:  ldloc.1
  IL_00f7:  ldc.i4     0xb6e02d3a
  IL_00fc:  beq        IL_029f
  IL_0101:  ldloc.1
  IL_0102:  ldc.i4     0xbaed3db3
  IL_0107:  beq        IL_031d
  IL_010c:  ldloc.1
  IL_010d:  ldc.i4     0xbf4a9f8e
  IL_0112:  beq        IL_0236
  IL_0117:  br         IL_0459
  IL_011c:  ldloc.1
  IL_011d:  ldc.i4     0xc6284d42
  IL_0122:  beq        IL_01f7
  IL_0127:  ldloc.1
  IL_0128:  ldc.i4     0xd1761402
  IL_012d:  beq        IL_01cd
  IL_0132:  ldloc.1
  IL_0133:  ldc.i4     0xd59864f4
  IL_0138:  beq        IL_0275
  IL_013d:  br         IL_0459
  IL_0142:  ldloc.1
  IL_0143:  ldc.i4     0xeb323c73
  IL_0148:  bgt.un.s   IL_0170
  IL_014a:  ldloc.1
  IL_014b:  ldc.i4     0xdca4b248
  IL_0150:  beq        IL_024b
  IL_0155:  ldloc.1
  IL_0156:  ldc.i4     0xe926f470
  IL_015b:  beq        IL_0371
  IL_0160:  ldloc.1
  IL_0161:  ldc.i4     0xeb323c73
  IL_0166:  beq        IL_02de
  IL_016b:  br         IL_0459
  IL_0170:  ldloc.1
  IL_0171:  ldc.i4     0xf1ea0ad5
  IL_0176:  beq        IL_0347
  IL_017b:  ldloc.1
  IL_017c:  ldc.i4     0xfa67b44d
  IL_0181:  beq.s      IL_01a3
  IL_0183:  ldloc.1
  IL_0184:  ldc.i4     0xfea21584
  IL_0189:  bne.un     IL_0459
  IL_018e:  ldarg.0
  IL_018f:  ldstr      ""N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9""
  IL_0194:  call       ""bool string.op_Equality(string, string)""
  IL_0199:  brtrue     IL_0386
  IL_019e:  br         IL_0459
  IL_01a3:  ldarg.0
  IL_01a4:  ldstr      ""emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I""
  IL_01a9:  call       ""bool string.op_Equality(string, string)""
  IL_01ae:  brtrue     IL_0391
  IL_01b3:  br         IL_0459
  IL_01b8:  ldarg.0
  IL_01b9:  ldstr      ""Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m""
  IL_01be:  call       ""bool string.op_Equality(string, string)""
  IL_01c3:  brtrue     IL_039c
  IL_01c8:  br         IL_0459
  IL_01cd:  ldarg.0
  IL_01ce:  ldstr      ""[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD""
  IL_01d3:  call       ""bool string.op_Equality(string, string)""
  IL_01d8:  brtrue     IL_03a7
  IL_01dd:  br         IL_0459
  IL_01e2:  ldarg.0
  IL_01e3:  ldstr      ""<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]""
  IL_01e8:  call       ""bool string.op_Equality(string, string)""
  IL_01ed:  brtrue     IL_03b2
  IL_01f2:  br         IL_0459
  IL_01f7:  ldarg.0
  IL_01f8:  ldstr      ""[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c""
  IL_01fd:  call       ""bool string.op_Equality(string, string)""
  IL_0202:  brtrue     IL_03bd
  IL_0207:  br         IL_0459
  IL_020c:  ldarg.0
  IL_020d:  ldstr      ""d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0""
  IL_0212:  call       ""bool string.op_Equality(string, string)""
  IL_0217:  brtrue     IL_03c8
  IL_021c:  br         IL_0459
  IL_0221:  ldarg.0
  IL_0222:  ldstr      ""n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>""
  IL_0227:  call       ""bool string.op_Equality(string, string)""
  IL_022c:  brtrue     IL_03d3
  IL_0231:  br         IL_0459
  IL_0236:  ldarg.0
  IL_0237:  ldstr      ""H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF""
  IL_023c:  call       ""bool string.op_Equality(string, string)""
  IL_0241:  brtrue     IL_03db
  IL_0246:  br         IL_0459
  IL_024b:  ldarg.0
  IL_024c:  ldstr      ""iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:""
  IL_0251:  call       ""bool string.op_Equality(string, string)""
  IL_0256:  brtrue     IL_03e3
  IL_025b:  br         IL_0459
  IL_0260:  ldarg.0
  IL_0261:  ldstr      "">AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj""
  IL_0266:  call       ""bool string.op_Equality(string, string)""
  IL_026b:  brtrue     IL_03eb
  IL_0270:  br         IL_0459
  IL_0275:  ldarg.0
  IL_0276:  ldstr      ""d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:""
  IL_027b:  call       ""bool string.op_Equality(string, string)""
  IL_0280:  brtrue     IL_03f3
  IL_0285:  br         IL_0459
  IL_028a:  ldarg.0
  IL_028b:  ldstr      ""fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU""
  IL_0290:  call       ""bool string.op_Equality(string, string)""
  IL_0295:  brtrue     IL_03fb
  IL_029a:  br         IL_0459
  IL_029f:  ldarg.0
  IL_02a0:  ldstr      ""9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR""
  IL_02a5:  call       ""bool string.op_Equality(string, string)""
  IL_02aa:  brtrue     IL_0403
  IL_02af:  br         IL_0459
  IL_02b4:  ldarg.0
  IL_02b5:  ldstr      ""A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj""
  IL_02ba:  call       ""bool string.op_Equality(string, string)""
  IL_02bf:  brtrue     IL_040b
  IL_02c4:  br         IL_0459
  IL_02c9:  ldarg.0
  IL_02ca:  ldstr      ""SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k""
  IL_02cf:  call       ""bool string.op_Equality(string, string)""
  IL_02d4:  brtrue     IL_0413
  IL_02d9:  br         IL_0459
  IL_02de:  ldarg.0
  IL_02df:  ldstr      ""0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4""
  IL_02e4:  call       ""bool string.op_Equality(string, string)""
  IL_02e9:  brtrue     IL_041b
  IL_02ee:  br         IL_0459
  IL_02f3:  ldarg.0
  IL_02f4:  ldstr      ""4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O""
  IL_02f9:  call       ""bool string.op_Equality(string, string)""
  IL_02fe:  brtrue     IL_0423
  IL_0303:  br         IL_0459
  IL_0308:  ldarg.0
  IL_0309:  ldstr      ""EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe""
  IL_030e:  call       ""bool string.op_Equality(string, string)""
  IL_0313:  brtrue     IL_042b
  IL_0318:  br         IL_0459
  IL_031d:  ldarg.0
  IL_031e:  ldstr      ""?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM""
  IL_0323:  call       ""bool string.op_Equality(string, string)""
  IL_0328:  brtrue     IL_0433
  IL_032d:  br         IL_0459
  IL_0332:  ldarg.0
  IL_0333:  ldstr      ""HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W""
  IL_0338:  call       ""bool string.op_Equality(string, string)""
  IL_033d:  brtrue     IL_043b
  IL_0342:  br         IL_0459
  IL_0347:  ldarg.0
  IL_0348:  ldstr      ""XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D""
  IL_034d:  call       ""bool string.op_Equality(string, string)""
  IL_0352:  brtrue     IL_0443
  IL_0357:  br         IL_0459
  IL_035c:  ldarg.0
  IL_035d:  ldstr      ""MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9""
  IL_0362:  call       ""bool string.op_Equality(string, string)""
  IL_0367:  brtrue     IL_044b
  IL_036c:  br         IL_0459
  IL_0371:  ldarg.0
  IL_0372:  ldstr      ""L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4""
  IL_0377:  call       ""bool string.op_Equality(string, string)""
  IL_037c:  brtrue     IL_0453
  IL_0381:  br         IL_0459
  IL_0386:  ldstr      ""N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9""
  IL_038b:  stloc.0
  IL_038c:  br         IL_0459
  IL_0391:  ldstr      ""emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I""
  IL_0396:  stloc.0
  IL_0397:  br         IL_0459
  IL_039c:  ldstr      ""Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m""
  IL_03a1:  stloc.0
  IL_03a2:  br         IL_0459
  IL_03a7:  ldstr      ""[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD""
  IL_03ac:  stloc.0
  IL_03ad:  br         IL_0459
  IL_03b2:  ldstr      ""<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]""
  IL_03b7:  stloc.0
  IL_03b8:  br         IL_0459
  IL_03bd:  ldstr      ""[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c""
  IL_03c2:  stloc.0
  IL_03c3:  br         IL_0459
  IL_03c8:  ldstr      ""d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0""
  IL_03cd:  stloc.0
  IL_03ce:  br         IL_0459
  IL_03d3:  ldstr      ""n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>""
  IL_03d8:  stloc.0
  IL_03d9:  br.s       IL_0459
  IL_03db:  ldstr      ""H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF""
  IL_03e0:  stloc.0
  IL_03e1:  br.s       IL_0459
  IL_03e3:  ldstr      ""iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:""
  IL_03e8:  stloc.0
  IL_03e9:  br.s       IL_0459
  IL_03eb:  ldstr      "">AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj""
  IL_03f0:  stloc.0
  IL_03f1:  br.s       IL_0459
  IL_03f3:  ldstr      ""d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:""
  IL_03f8:  stloc.0
  IL_03f9:  br.s       IL_0459
  IL_03fb:  ldstr      ""fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU""
  IL_0400:  stloc.0
  IL_0401:  br.s       IL_0459
  IL_0403:  ldstr      ""9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR""
  IL_0408:  stloc.0
  IL_0409:  br.s       IL_0459
  IL_040b:  ldstr      ""A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj""
  IL_0410:  stloc.0
  IL_0411:  br.s       IL_0459
  IL_0413:  ldstr      ""SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k""
  IL_0418:  stloc.0
  IL_0419:  br.s       IL_0459
  IL_041b:  ldstr      ""0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4""
  IL_0420:  stloc.0
  IL_0421:  br.s       IL_0459
  IL_0423:  ldstr      ""4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O""
  IL_0428:  stloc.0
  IL_0429:  br.s       IL_0459
  IL_042b:  ldstr      ""EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe""
  IL_0430:  stloc.0
  IL_0431:  br.s       IL_0459
  IL_0433:  ldstr      ""?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM""
  IL_0438:  stloc.0
  IL_0439:  br.s       IL_0459
  IL_043b:  ldstr      ""HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W""
  IL_0440:  stloc.0
  IL_0441:  br.s       IL_0459
  IL_0443:  ldstr      ""XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D""
  IL_0448:  stloc.0
  IL_0449:  br.s       IL_0459
  IL_044b:  ldstr      ""MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9""
  IL_0450:  stloc.0
  IL_0451:  br.s       IL_0459
  IL_0453:  ldstr      ""L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4""
  IL_0458:  stloc.0
  IL_0459:  ldloc.0
  IL_045a:  ldarg.0
  IL_045b:  call       ""bool string.op_Equality(string, string)""
  IL_0460:  ret
}"
            );

            // Verify string hash synthesized method for hash table switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: true);
        }

        [WorkItem(544322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544322")]
        [Fact]
        public void StringSwitch_HashTableSwitch_03()
        {
            var text = @"
using System;

class Goo
{
  public static void Main()
  {
    Console.WriteLine(M(""blah""));
  }

  static int M(string s)
  {
    byte[] bytes = {1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21};
    switch (s)
    {
      case ""blah"":
        return 1;
      case ""help"":
        return 2;
      case ""ooof"":
        return 3;
      case ""walk"":
        return 4;
      case ""bark"":
        return 5;
      case ""xblah"":
        return 1;
      case ""xhelp"":
        return 2;
      case ""xooof"":
        return 3;
      case ""xwalk"":
        return 4;
      case ""xbark"":
        return 5;
      case ""rblah"":
        return 1;
      case ""rhelp"":
        return 2;
      case ""rooof"":
        return 3;
      case ""rwalk"":
        return 4;
      case ""rbark"":
        return 5;
    }

    return bytes.Length;
  }
}         
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "1");

            // Verify string hash synthesized method for hash table switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: true);
        }

        private static void VerifySynthesizedStringHashMethod(CompilationVerifier compVerifier, bool expected)
        {
            compVerifier.VerifyMemberInIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName, expected);

            if (expected)
            {
                compVerifier.VerifyIL(PrivateImplementationDetails.SynthesizedStringHashFunctionName,
                    @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (uint V_0,
  int V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_002a
  IL_0003:  ldc.i4     0x811c9dc5
  IL_0008:  stloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.1
  IL_000b:  br.s       IL_0021
  IL_000d:  ldarg.0
  IL_000e:  ldloc.1
  IL_000f:  callvirt   ""char string.this[int].get""
  IL_0014:  ldloc.0
  IL_0015:  xor
  IL_0016:  ldc.i4     0x1000193
  IL_001b:  mul
  IL_001c:  stloc.0
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.1
  IL_001f:  add
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldarg.0
  IL_0023:  callvirt   ""int string.Length.get""
  IL_0028:  blt.s      IL_000d
  IL_002a:  ldloc.0
  IL_002b:  ret
}
");
            }
        }

        #endregion

        #region "Implicit user defined conversion tests"

        [WorkItem(543602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543602")]
        [WorkItem(543660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543660")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_01()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var source = @"using System;
public class Test
{
    public static implicit operator int(Test val)
    {
        return 1;
    }

    public static implicit operator float(Test val2)
    {
        return 2.1f;
    }

    public static int Main()
    {
        Test t = new Test();
        switch (t)
        {
            case 1:
                Console.WriteLine(0);
                return 0;
            default:
                Console.WriteLine(1);
                return 1;
        }
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0");
        }

        [WorkItem(543660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543660")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_02()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
class X {}
class Conv
{
    public static implicit operator int (Conv C)
    {
        return 1;
    }
    
    public static implicit operator X (Conv C2)
    {
        return new X();
    }
    
    public static int Main()
    {
        Conv C = new Conv();
        switch(C)
        {
            case 1:
                System.Console.WriteLine(""Pass"");
                return 0;
            default:
                System.Console.WriteLine(""Fail"");
                return 1;
        }
    }		
}
";
            CompileAndVerify(text, expectedOutput: "Pass");
        }

        [WorkItem(543660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543660")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_03()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
enum X { F = 0 }
class Conv
{
    // only valid operator
    public static implicit operator int (Conv C)
    {
        return 1;
    }
    
    // bool type is not valid
    public static implicit operator bool (Conv C2)
    {
        return false;
    }

    // enum type is not valid
    public static implicit operator X (Conv C3)
    {
        return X.F;
    }
    
    
    public static int Main()
    {
        Conv C = new Conv();
        switch(C)
        {
            case 1:
                System.Console.WriteLine(""Pass"");
                return 0;
            default:
                System.Console.WriteLine(""Fail"");
                return 1;
        }
    }		
}
";
            CompileAndVerify(text, expectedOutput: "Pass");
        }

        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_04()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
    public static implicit operator int (Conv C)
    {
        return 1;
    }
    
    public static implicit operator int? (Conv? C2)
    {
        return null;
    }
    
    public static int Main()
    {
        Conv? D = new Conv();
        switch(D)
        {
            case 1:
                System.Console.WriteLine(""Fail"");
                return 1;
            case null:
                System.Console.WriteLine(""Pass"");
                return 0;
            default:
                System.Console.WriteLine(""Fail"");
                return 1;
        }
    }		
}
";
            CompileAndVerify(text, expectedOutput: "Pass");
        }

        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_06()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
    public static implicit operator int (Conv C)
    {
        return 1;
    }
    
    public static implicit operator int? (Conv? C)
    {
        return null;
    }
    
    public static int Main()
    {
        Conv? C = new Conv();
        switch(C)
        {
            case null:
                System.Console.WriteLine(""Pass"");
                return 0;
            case 1:
                System.Console.WriteLine(""Fail"");
                return 0;
            default:
                System.Console.WriteLine(""Fail"");
                return 0;
        }
    }		
}
";
            CompileAndVerify(text, expectedOutput: "Pass");
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_3()
        {
            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "0");
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_4()
        {
            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "1");
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_3_1()
        {
            var text =
@"using System;
 
struct A
{
    public static implicit operator int(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "1");
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_3_3()
        {
            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int(A a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "1");
        }

        #endregion

        [WorkItem(634404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634404")]
        [Fact()]
        public void MissingCharsProperty()
        {
            var text = @"
using System;
class Program
{
    static string d = null;

    static void Main()
    {
        string s = ""hello"";

        switch (s)
        {
            case ""Hi"":
                d = "" Hi "";
                break;
            case ""Bye"":
                d = "" Bye "";
                break;
            case ""qwe"":
                d = "" qwe "";
                break;
            case ""ert"":
                d = "" ert "";
                break;
            case ""asd"":
                d = "" asd "";
                break;
            case ""hello"":
                d = "" hello "";
                break;
            case ""qrs"":
                d = "" qrs "";
                break;
            case ""tuv"":
                d = "" tuv "";
                break;
            case ""wxy"":
                d = "" wxy "";
                break;
        }
    }
}
";
            var comp = CreateEmptyCompilation(
                source: new[] { Parse(text) },
                references: new[] { AacorlibRef });


            var verifier = CompileAndVerify(comp, verify: Verification.Fails);
            verifier.VerifyIL("Program.Main", @"
{
  // Code size      229 (0xe5)
  .maxstack  2
  .locals init (string V_0) //s
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse    IL_00e4
  IL_000c:  ldloc.0
  IL_000d:  ldstr      ""Hi""
  IL_0012:  call       ""bool string.op_Equality(string, string)""
  IL_0017:  brtrue.s   IL_0082
  IL_0019:  ldloc.0
  IL_001a:  ldstr      ""Bye""
  IL_001f:  call       ""bool string.op_Equality(string, string)""
  IL_0024:  brtrue.s   IL_008d
  IL_0026:  ldloc.0
  IL_0027:  ldstr      ""qwe""
  IL_002c:  call       ""bool string.op_Equality(string, string)""
  IL_0031:  brtrue.s   IL_0098
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""ert""
  IL_0039:  call       ""bool string.op_Equality(string, string)""
  IL_003e:  brtrue.s   IL_00a3
  IL_0040:  ldloc.0
  IL_0041:  ldstr      ""asd""
  IL_0046:  call       ""bool string.op_Equality(string, string)""
  IL_004b:  brtrue.s   IL_00ae
  IL_004d:  ldloc.0
  IL_004e:  ldstr      ""hello""
  IL_0053:  call       ""bool string.op_Equality(string, string)""
  IL_0058:  brtrue.s   IL_00b9
  IL_005a:  ldloc.0
  IL_005b:  ldstr      ""qrs""
  IL_0060:  call       ""bool string.op_Equality(string, string)""
  IL_0065:  brtrue.s   IL_00c4
  IL_0067:  ldloc.0
  IL_0068:  ldstr      ""tuv""
  IL_006d:  call       ""bool string.op_Equality(string, string)""
  IL_0072:  brtrue.s   IL_00cf
  IL_0074:  ldloc.0
  IL_0075:  ldstr      ""wxy""
  IL_007a:  call       ""bool string.op_Equality(string, string)""
  IL_007f:  brtrue.s   IL_00da
  IL_0081:  ret
  IL_0082:  ldstr      "" Hi ""
  IL_0087:  stsfld     ""string Program.d""
  IL_008c:  ret
  IL_008d:  ldstr      "" Bye ""
  IL_0092:  stsfld     ""string Program.d""
  IL_0097:  ret
  IL_0098:  ldstr      "" qwe ""
  IL_009d:  stsfld     ""string Program.d""
  IL_00a2:  ret
  IL_00a3:  ldstr      "" ert ""
  IL_00a8:  stsfld     ""string Program.d""
  IL_00ad:  ret
  IL_00ae:  ldstr      "" asd ""
  IL_00b3:  stsfld     ""string Program.d""
  IL_00b8:  ret
  IL_00b9:  ldstr      "" hello ""
  IL_00be:  stsfld     ""string Program.d""
  IL_00c3:  ret
  IL_00c4:  ldstr      "" qrs ""
  IL_00c9:  stsfld     ""string Program.d""
  IL_00ce:  ret
  IL_00cf:  ldstr      "" tuv ""
  IL_00d4:  stsfld     ""string Program.d""
  IL_00d9:  ret
  IL_00da:  ldstr      "" wxy ""
  IL_00df:  stsfld     ""string Program.d""
  IL_00e4:  ret
}");
        }

        [WorkItem(642186, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642186")]
        [Fact()]
        public void IsWarningSwitchEmit()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication24
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeIt();
            TimeIt();
            TimeIt();
            TimeIt();
            TimeIt();
            TimeIt();
            TimeIt();
        }

        private static void TimeIt()
        {
//            var sw = System.Diagnostics.Stopwatch.StartNew();
//
//            for (int i = 0; i < 100000000; i++)
//            {
//                IsWarning((ErrorCode)(i % 10002));
//            }
//
//            sw.Stop();
//            System.Console.WriteLine(sw.ElapsedMilliseconds);
        }

        public static bool IsWarning(ErrorCode code)
        {
            switch (code)
            {
                case ErrorCode.WRN_InvalidMainSig:
                case ErrorCode.WRN_UnreferencedEvent:
                case ErrorCode.WRN_LowercaseEllSuffix:
                case ErrorCode.WRN_DuplicateUsing:
                case ErrorCode.WRN_NewRequired:
                case ErrorCode.WRN_NewNotRequired:
                case ErrorCode.WRN_NewOrOverrideExpected:
                case ErrorCode.WRN_UnreachableCode:
                case ErrorCode.WRN_UnreferencedLabel:
                case ErrorCode.WRN_UnreferencedVar:
                case ErrorCode.WRN_UnreferencedField:
                case ErrorCode.WRN_IsAlwaysTrue:
                case ErrorCode.WRN_IsAlwaysFalse:
                case ErrorCode.WRN_ByRefNonAgileField:
                case ErrorCode.WRN_OldWarning_UnsafeProp:
                case ErrorCode.WRN_UnreferencedVarAssg:
                case ErrorCode.WRN_NegativeArrayIndex:
                case ErrorCode.WRN_BadRefCompareLeft:
                case ErrorCode.WRN_BadRefCompareRight:
                case ErrorCode.WRN_PatternIsAmbiguous:
                case ErrorCode.WRN_PatternStaticOrInaccessible:
                case ErrorCode.WRN_PatternBadSignature:
                case ErrorCode.WRN_SequentialOnPartialClass:
                case ErrorCode.WRN_MainCantBeGeneric:
                case ErrorCode.WRN_UnreferencedFieldAssg:
                case ErrorCode.WRN_AmbiguousXMLReference:
                case ErrorCode.WRN_VolatileByRef:
                case ErrorCode.WRN_IncrSwitchObsolete:
                case ErrorCode.WRN_UnreachableExpr:
                case ErrorCode.WRN_SameFullNameThisNsAgg:
                case ErrorCode.WRN_SameFullNameThisAggAgg:
                case ErrorCode.WRN_SameFullNameThisAggNs:
                case ErrorCode.WRN_GlobalAliasDefn:
                case ErrorCode.WRN_UnexpectedPredefTypeLoc:
                case ErrorCode.WRN_AlwaysNull:
                case ErrorCode.WRN_CmpAlwaysFalse:
                case ErrorCode.WRN_FinalizeMethod:
                case ErrorCode.WRN_AmbigLookupMeth:
                case ErrorCode.WRN_GotoCaseShouldConvert:
                case ErrorCode.WRN_NubExprIsConstBool:
                case ErrorCode.WRN_ExplicitImplCollision:
                case ErrorCode.WRN_FeatureDeprecated:
                case ErrorCode.WRN_DeprecatedSymbol:
                case ErrorCode.WRN_DeprecatedSymbolStr:
                case ErrorCode.WRN_ExternMethodNoImplementation:
                case ErrorCode.WRN_ProtectedInSealed:
                case ErrorCode.WRN_PossibleMistakenNullStatement:
                case ErrorCode.WRN_UnassignedInternalField:
                case ErrorCode.WRN_VacuousIntegralComp:
                case ErrorCode.WRN_AttributeLocationOnBadDeclaration:
                case ErrorCode.WRN_InvalidAttributeLocation:
                case ErrorCode.WRN_EqualsWithoutGetHashCode:
                case ErrorCode.WRN_EqualityOpWithoutEquals:
                case ErrorCode.WRN_EqualityOpWithoutGetHashCode:
                case ErrorCode.WRN_IncorrectBooleanAssg:
                case ErrorCode.WRN_NonObsoleteOverridingObsolete:
                case ErrorCode.WRN_BitwiseOrSignExtend:
                case ErrorCode.WRN_OldWarning_ProtectedInternal:
                case ErrorCode.WRN_OldWarning_AccessibleReadonly:
                case ErrorCode.WRN_CoClassWithoutComImport:
                case ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter:
                case ErrorCode.WRN_AssignmentToLockOrDispose:
                case ErrorCode.WRN_ObsoleteOverridingNonObsolete:
                case ErrorCode.WRN_DebugFullNameTooLong:
                case ErrorCode.WRN_ExternCtorNoImplementation:
                case ErrorCode.WRN_WarningDirective:
                case ErrorCode.WRN_UnreachableGeneralCatch:
                case ErrorCode.WRN_UninitializedField:
                case ErrorCode.WRN_DeprecatedCollectionInitAddStr:
                case ErrorCode.WRN_DeprecatedCollectionInitAdd:
                case ErrorCode.WRN_DefaultValueForUnconsumedLocation:
                case ErrorCode.WRN_FeatureDeprecated2:
                case ErrorCode.WRN_FeatureDeprecated3:
                case ErrorCode.WRN_FeatureDeprecated4:
                case ErrorCode.WRN_FeatureDeprecated5:
                case ErrorCode.WRN_OldWarning_FeatureDefaultDeprecated:
                case ErrorCode.WRN_EmptySwitch:
                case ErrorCode.WRN_XMLParseError:
                case ErrorCode.WRN_DuplicateParamTag:
                case ErrorCode.WRN_UnmatchedParamTag:
                case ErrorCode.WRN_MissingParamTag:
                case ErrorCode.WRN_BadXMLRef:
                case ErrorCode.WRN_BadXMLRefParamType:
                case ErrorCode.WRN_BadXMLRefReturnType:
                case ErrorCode.WRN_BadXMLRefSyntax:
                case ErrorCode.WRN_UnprocessedXMLComment:
                case ErrorCode.WRN_FailedInclude:
                case ErrorCode.WRN_InvalidInclude:
                case ErrorCode.WRN_MissingXMLComment:
                case ErrorCode.WRN_XMLParseIncludeError:
                case ErrorCode.WRN_OldWarning_MultipleTypeDefs:
                case ErrorCode.WRN_OldWarning_DocFileGenAndIncr:
                case ErrorCode.WRN_XMLParserNotFound:
                case ErrorCode.WRN_ALinkWarn:
                case ErrorCode.WRN_DeleteAutoResFailed:
                case ErrorCode.WRN_CmdOptionConflictsSource:
                case ErrorCode.WRN_IllegalPragma:
                case ErrorCode.WRN_IllegalPPWarning:
                case ErrorCode.WRN_BadRestoreNumber:
                case ErrorCode.WRN_NonECMAFeature:
                case ErrorCode.WRN_ErrorOverride:
                case ErrorCode.WRN_OldWarning_ReservedIdentifier:
                case ErrorCode.WRN_InvalidSearchPathDir:
                case ErrorCode.WRN_MissingTypeNested:
                case ErrorCode.WRN_MissingTypeInSource:
                case ErrorCode.WRN_MissingTypeInAssembly:
                case ErrorCode.WRN_MultiplePredefTypes:
                case ErrorCode.WRN_TooManyLinesForDebugger:
                case ErrorCode.WRN_CallOnNonAgileField:
                case ErrorCode.WRN_BadWarningNumber:
                case ErrorCode.WRN_InvalidNumber:
                case ErrorCode.WRN_FileNameTooLong:
                case ErrorCode.WRN_IllegalPPChecksum:
                case ErrorCode.WRN_EndOfPPLineExpected:
                case ErrorCode.WRN_ConflictingChecksum:
                case ErrorCode.WRN_AssumedMatchThis:
                case ErrorCode.WRN_UseSwitchInsteadOfAttribute:
                case ErrorCode.WRN_InvalidAssemblyName:
                case ErrorCode.WRN_UnifyReferenceMajMin:
                case ErrorCode.WRN_UnifyReferenceBldRev:
                case ErrorCode.WRN_DelegateNewMethBind:
                case ErrorCode.WRN_EmptyFileName:
                case ErrorCode.WRN_DuplicateTypeParamTag:
                case ErrorCode.WRN_UnmatchedTypeParamTag:
                case ErrorCode.WRN_MissingTypeParamTag:
                case ErrorCode.WRN_AssignmentToSelf:
                case ErrorCode.WRN_ComparisonToSelf:
                case ErrorCode.WRN_DotOnDefault:
                case ErrorCode.WRN_BadXMLRefTypeVar:
                case ErrorCode.WRN_UnmatchedParamRefTag:
                case ErrorCode.WRN_UnmatchedTypeParamRefTag:
                case ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA:
                case ErrorCode.WRN_TypeNotFoundForNoPIAWarning:
                case ErrorCode.WRN_CantHaveManifestForModule:
                case ErrorCode.WRN_MultipleRuntimeImplementationMatches:
                case ErrorCode.WRN_MultipleRuntimeOverrideMatches:
                case ErrorCode.WRN_DynamicDispatchToConditionalMethod:
                case ErrorCode.WRN_IsDynamicIsConfusing:
                case ErrorCode.WRN_AsyncLacksAwaits:
                case ErrorCode.WRN_FileAlreadyIncluded:
                case ErrorCode.WRN_NoSources:
                case ErrorCode.WRN_UseNewSwitch:
                case ErrorCode.WRN_NoConfigNotOnCommandLine:
                case ErrorCode.WRN_DefineIdentifierRequired:
                case ErrorCode.WRN_BadUILang:
                case ErrorCode.WRN_CLS_NoVarArgs:
                case ErrorCode.WRN_CLS_BadArgType:
                case ErrorCode.WRN_CLS_BadReturnType:
                case ErrorCode.WRN_CLS_BadFieldPropType:
                case ErrorCode.WRN_CLS_BadUnicode:
                case ErrorCode.WRN_CLS_BadIdentifierCase:
                case ErrorCode.WRN_CLS_OverloadRefOut:
                case ErrorCode.WRN_CLS_OverloadUnnamed:
                case ErrorCode.WRN_CLS_BadIdentifier:
                case ErrorCode.WRN_CLS_BadBase:
                case ErrorCode.WRN_CLS_BadInterfaceMember:
                case ErrorCode.WRN_CLS_NoAbstractMembers:
                case ErrorCode.WRN_CLS_NotOnModules:
                case ErrorCode.WRN_CLS_ModuleMissingCLS:
                case ErrorCode.WRN_CLS_AssemblyNotCLS:
                case ErrorCode.WRN_CLS_BadAttributeType:
                case ErrorCode.WRN_CLS_ArrayArgumentToAttribute:
                case ErrorCode.WRN_CLS_NotOnModules2:
                case ErrorCode.WRN_CLS_IllegalTrueInFalse:
                case ErrorCode.WRN_CLS_MeaninglessOnPrivateType:
                case ErrorCode.WRN_CLS_AssemblyNotCLS2:
                case ErrorCode.WRN_CLS_MeaninglessOnParam:
                case ErrorCode.WRN_CLS_MeaninglessOnReturn:
                case ErrorCode.WRN_CLS_BadTypeVar:
                case ErrorCode.WRN_CLS_VolatileField:
                case ErrorCode.WRN_CLS_BadInterface:
                case ErrorCode.WRN_UnobservedAwaitableExpression:
                case ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation:
                case ErrorCode.WRN_UnknownOption:
                case ErrorCode.WRN_MetadataNameTooLong:
                case ErrorCode.WRN_MainIgnored:
                case ErrorCode.WRN_DelaySignButNoKey:
                case ErrorCode.WRN_InvalidVersionFormat:
                case ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath:
                case ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden:
                case ErrorCode.WRN_UnimplementedCommandLineSwitch:
                case ErrorCode.WRN_RefCultureMismatch:
                case ErrorCode.WRN_ConflictingMachineAssembly:
                case ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope1:
                case ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope2:
                case ErrorCode.WRN_CA2202_DoNotDisposeObjectsMultipleTimes:
                    return true;
                default:
                    return false;
            }
        }


        internal enum ErrorCode
        {
            Void = InternalErrorCode.Void,
            Unknown = InternalErrorCode.Unknown,
            FTL_InternalError = 1,
            //FTL_FailedToLoadResource = 2,
            //FTL_NoMemory = 3,
            //ERR_WarningAsError = 4,
            //ERR_MissingOptionArg = 5,
            ERR_NoMetadataFile = 6,
            //FTL_ComPlusInit = 7,
            FTL_MetadataImportFailure = 8,
            FTL_MetadataCantOpenFile = 9,
            //ERR_FatalError = 10,
            //ERR_CantImportBase = 11,
            ERR_NoTypeDef = 12,
            FTL_MetadataEmitFailure = 13,
            //FTL_RequiredFileNotFound = 14,
            ERR_ClassNameTooLong = 15,
            ERR_OutputWriteFailed = 16,
            ERR_MultipleEntryPoints = 17,
            //ERR_UnimplementedOp = 18,
            ERR_BadBinaryOps = 19,
            ERR_IntDivByZero = 20,
            ERR_BadIndexLHS = 21,
            ERR_BadIndexCount = 22,
            ERR_BadUnaryOp = 23,
            //ERR_NoStdLib = 25,        not used in Roslyn
            ERR_ThisInStaticMeth = 26,
            ERR_ThisInBadContext = 27,
            WRN_InvalidMainSig = 28,
            ERR_NoImplicitConv = 29,
            ERR_NoExplicitConv = 30,
            ERR_ConstOutOfRange = 31,
            ERR_AmbigBinaryOps = 34,
            ERR_AmbigUnaryOp = 35,
            ERR_InAttrOnOutParam = 36,
            ERR_ValueCantBeNull = 37,
            //ERR_WrongNestedThis = 38,     No longer given in Roslyn. Less specific ERR_ObjectRequired ""An object reference is required for the non-static...""
            ERR_NoExplicitBuiltinConv = 39,
            //FTL_DebugInit = 40,           Not used in Roslyn. Roslyn gives FTL_DebugEmitFailure with specific error code info.
            FTL_DebugEmitFailure = 41,
            //FTL_DebugInitFile = 42,       Not used in Roslyn. Roslyn gives ERR_CantOpenFileWrite with specific error info.
            //FTL_BadPDBFormat = 43,        Not used in Roslyn. Roslyn gives FTL_DebugEmitFailure with specific error code info.
            ERR_BadVisReturnType = 50,
            ERR_BadVisParamType = 51,
            ERR_BadVisFieldType = 52,
            ERR_BadVisPropertyType = 53,
            ERR_BadVisIndexerReturn = 54,
            ERR_BadVisIndexerParam = 55,
            ERR_BadVisOpReturn = 56,
            ERR_BadVisOpParam = 57,
            ERR_BadVisDelegateReturn = 58,
            ERR_BadVisDelegateParam = 59,
            ERR_BadVisBaseClass = 60,
            ERR_BadVisBaseInterface = 61,
            ERR_EventNeedsBothAccessors = 65,
            ERR_EventNotDelegate = 66,
            WRN_UnreferencedEvent = 67,
            ERR_InterfaceEventInitializer = 68,
            ERR_EventPropertyInInterface = 69,
            ERR_BadEventUsage = 70,
            ERR_ExplicitEventFieldImpl = 71,
            ERR_CantOverrideNonEvent = 72,
            ERR_AddRemoveMustHaveBody = 73,
            ERR_AbstractEventInitializer = 74,
            ERR_PossibleBadNegCast = 75,
            ERR_ReservedEnumerator = 76,
            ERR_AsMustHaveReferenceType = 77,
            WRN_LowercaseEllSuffix = 78,
            ERR_BadEventUsageNoField = 79,
            ERR_ConstraintOnlyAllowedOnGenericDecl = 80,
            ERR_TypeParamMustBeIdentifier = 81,
            ERR_MemberReserved = 82,
            ERR_DuplicateParamName = 100,
            ERR_DuplicateNameInNS = 101,
            ERR_DuplicateNameInClass = 102,
            ERR_NameNotInContext = 103,
            ERR_AmbigContext = 104,
            WRN_DuplicateUsing = 105,
            ERR_BadMemberFlag = 106,
            ERR_BadMemberProtection = 107,
            WRN_NewRequired = 108,
            WRN_NewNotRequired = 109,
            ERR_CircConstValue = 110,
            ERR_MemberAlreadyExists = 111,
            ERR_StaticNotVirtual = 112,
            ERR_OverrideNotNew = 113,
            WRN_NewOrOverrideExpected = 114,
            ERR_OverrideNotExpected = 115,
            ERR_NamespaceUnexpected = 116,
            ERR_NoSuchMember = 117,
            ERR_BadSKknown = 118,
            ERR_BadSKunknown = 119,
            ERR_ObjectRequired = 120,
            ERR_AmbigCall = 121,
            ERR_BadAccess = 122,
            ERR_MethDelegateMismatch = 123,
            ERR_RetObjectRequired = 126,
            ERR_RetNoObjectRequired = 127,
            ERR_LocalDuplicate = 128,
            ERR_AssgLvalueExpected = 131,
            ERR_StaticConstParam = 132,
            ERR_NotConstantExpression = 133,
            ERR_NotNullConstRefField = 134,
            ERR_NameIllegallyOverrides = 135,
            ERR_LocalIllegallyOverrides = 136,
            ERR_BadUsingNamespace = 138,
            ERR_NoBreakOrCont = 139,
            ERR_DuplicateLabel = 140,
            ERR_NoConstructors = 143,
            ERR_NoNewAbstract = 144,
            ERR_ConstValueRequired = 145,
            ERR_CircularBase = 146,
            ERR_BadDelegateConstructor = 148,
            ERR_MethodNameExpected = 149,
            ERR_ConstantExpected = 150,
            // ERR_SwitchGoverningTypeValueExpected shares the same error code (CS0151) with ERR_IntegralTypeValueExpected in Dev10 compiler.
            // However ERR_IntegralTypeValueExpected is currently unused and hence being removed. If we need to generate this error in future
            // we can use error code CS0166. CS0166 was originally reserved for ERR_SwitchFallInto in Dev10, but was never used. 
            ERR_SwitchGoverningTypeValueExpected = 151,
            ERR_DuplicateCaseLabel = 152,
            ERR_InvalidGotoCase = 153,
            ERR_PropertyLacksGet = 154,
            ERR_BadExceptionType = 155,
            ERR_BadEmptyThrow = 156,
            ERR_BadFinallyLeave = 157,
            ERR_LabelShadow = 158,
            ERR_LabelNotFound = 159,
            ERR_UnreachableCatch = 160,
            ERR_ReturnExpected = 161,
            WRN_UnreachableCode = 162,
            ERR_SwitchFallThrough = 163,
            WRN_UnreferencedLabel = 164,
            ERR_UseDefViolation = 165,
            //ERR_NoInvoke = 167,
            WRN_UnreferencedVar = 168,
            WRN_UnreferencedField = 169,
            ERR_UseDefViolationField = 170,
            ERR_UnassignedThis = 171,
            ERR_AmbigQM = 172,
            ERR_InvalidQM = 173,
            ERR_NoBaseClass = 174,
            ERR_BaseIllegal = 175,
            ERR_ObjectProhibited = 176,
            ERR_ParamUnassigned = 177,
            ERR_InvalidArray = 178,
            ERR_ExternHasBody = 179,
            ERR_AbstractAndExtern = 180,
            ERR_BadAttributeParamType = 181,
            ERR_BadAttributeArgument = 182,
            WRN_IsAlwaysTrue = 183,
            WRN_IsAlwaysFalse = 184,
            ERR_LockNeedsReference = 185,
            ERR_NullNotValid = 186,
            ERR_UseDefViolationThis = 188,
            ERR_ArgsInvalid = 190,
            ERR_AssgReadonly = 191,
            ERR_RefReadonly = 192,
            ERR_PtrExpected = 193,
            ERR_PtrIndexSingle = 196,
            WRN_ByRefNonAgileField = 197,
            ERR_AssgReadonlyStatic = 198,
            ERR_RefReadonlyStatic = 199,
            ERR_AssgReadonlyProp = 200,
            ERR_IllegalStatement = 201,
            ERR_BadGetEnumerator = 202,
            ERR_TooManyLocals = 204,
            ERR_AbstractBaseCall = 205,
            ERR_RefProperty = 206,
            WRN_OldWarning_UnsafeProp = 207,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:207"" is specified on the command line.
            ERR_ManagedAddr = 208,
            ERR_BadFixedInitType = 209,
            ERR_FixedMustInit = 210,
            ERR_InvalidAddrOp = 211,
            ERR_FixedNeeded = 212,
            ERR_FixedNotNeeded = 213,
            ERR_UnsafeNeeded = 214,
            ERR_OpTFRetType = 215,
            ERR_OperatorNeedsMatch = 216,
            ERR_BadBoolOp = 217,
            ERR_MustHaveOpTF = 218,
            WRN_UnreferencedVarAssg = 219,
            ERR_CheckedOverflow = 220,
            ERR_ConstOutOfRangeChecked = 221,
            ERR_BadVarargs = 224,
            ERR_ParamsMustBeArray = 225,
            ERR_IllegalArglist = 226,
            ERR_IllegalUnsafe = 227,
            //ERR_NoAccessibleMember = 228,
            ERR_AmbigMember = 229,
            ERR_BadForeachDecl = 230,
            ERR_ParamsLast = 231,
            ERR_SizeofUnsafe = 233,
            ERR_DottedTypeNameNotFoundInNS = 234,
            ERR_FieldInitRefNonstatic = 236,
            ERR_SealedNonOverride = 238,
            ERR_CantOverrideSealed = 239,
            //ERR_NoDefaultArgs = 241,
            ERR_VoidError = 242,
            ERR_ConditionalOnOverride = 243,
            ERR_PointerInAsOrIs = 244,
            ERR_CallingFinalizeDeprecated = 245, //Dev10: ERR_CallingFinalizeDepracated
            ERR_SingleTypeNameNotFound = 246,
            ERR_NegativeStackAllocSize = 247,
            ERR_NegativeArraySize = 248,
            ERR_OverrideFinalizeDeprecated = 249,
            ERR_CallingBaseFinalizeDeprecated = 250,
            WRN_NegativeArrayIndex = 251,
            WRN_BadRefCompareLeft = 252,
            WRN_BadRefCompareRight = 253,
            ERR_BadCastInFixed = 254,
            ERR_StackallocInCatchFinally = 255,
            ERR_VarargsLast = 257,
            ERR_MissingPartial = 260,
            ERR_PartialTypeKindConflict = 261,
            ERR_PartialModifierConflict = 262,
            ERR_PartialMultipleBases = 263,
            ERR_PartialWrongTypeParams = 264,
            ERR_PartialWrongConstraints = 265,
            ERR_NoImplicitConvCast = 266,
            ERR_PartialMisplaced = 267,
            ERR_ImportedCircularBase = 268,
            ERR_UseDefViolationOut = 269,
            ERR_ArraySizeInDeclaration = 270,
            ERR_InaccessibleGetter = 271,
            ERR_InaccessibleSetter = 272,
            ERR_InvalidPropertyAccessMod = 273,
            ERR_DuplicatePropertyAccessMods = 274,
            ERR_PropertyAccessModInInterface = 275,
            ERR_AccessModMissingAccessor = 276,
            ERR_UnimplementedInterfaceAccessor = 277,
            WRN_PatternIsAmbiguous = 278,
            WRN_PatternStaticOrInaccessible = 279,
            WRN_PatternBadSignature = 280,
            ERR_FriendRefNotEqualToThis = 281,
            WRN_SequentialOnPartialClass = 282,
            ERR_BadConstType = 283,
            ERR_NoNewTyvar = 304,
            ERR_BadArity = 305,
            ERR_BadTypeArgument = 306,
            ERR_TypeArgsNotAllowed = 307,
            ERR_HasNoTypeVars = 308,
            ERR_NewConstraintNotSatisfied = 310,
            ERR_GenericConstraintNotSatisfiedRefType = 311,
            ERR_GenericConstraintNotSatisfiedNullableEnum = 312,
            ERR_GenericConstraintNotSatisfiedNullableInterface = 313,
            ERR_GenericConstraintNotSatisfiedTyVar = 314,
            ERR_GenericConstraintNotSatisfiedValType = 315,
            ERR_DuplicateGeneratedName = 316,
            ERR_GlobalSingleTypeNameNotFound = 400,
            ERR_NewBoundMustBeLast = 401,
            WRN_MainCantBeGeneric = 402,
            ERR_TypeVarCantBeNull = 403,
            ERR_AttributeCantBeGeneric = 404,
            ERR_DuplicateBound = 405,
            ERR_ClassBoundNotFirst = 406,
            ERR_BadRetType = 407,
            ERR_DuplicateConstraintClause = 409,
            //ERR_WrongSignature = 410,     unused in Roslyn
            ERR_CantInferMethTypeArgs = 411,
            ERR_LocalSameNameAsTypeParam = 412,
            ERR_AsWithTypeVar = 413,
            WRN_UnreferencedFieldAssg = 414,
            ERR_BadIndexerNameAttr = 415,
            ERR_AttrArgWithTypeVars = 416,
            ERR_NewTyvarWithArgs = 417,
            ERR_AbstractSealedStatic = 418,
            WRN_AmbiguousXMLReference = 419,
            WRN_VolatileByRef = 420,
            WRN_IncrSwitchObsolete = 422,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn"" is specified on the command line.
            ERR_ComImportWithImpl = 423,
            ERR_ComImportWithBase = 424,
            ERR_ImplBadConstraints = 425,
            ERR_DottedTypeNameNotFoundInAgg = 426,
            ERR_MethGrpToNonDel = 428,
            WRN_UnreachableExpr = 429,       // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn"" is specified on the command line.
            ERR_BadExternAlias = 430,
            ERR_ColColWithTypeAlias = 431,
            ERR_AliasNotFound = 432,
            ERR_SameFullNameAggAgg = 433,
            ERR_SameFullNameNsAgg = 434,
            WRN_SameFullNameThisNsAgg = 435,
            WRN_SameFullNameThisAggAgg = 436,
            WRN_SameFullNameThisAggNs = 437,
            ERR_SameFullNameThisAggThisNs = 438,
            ERR_ExternAfterElements = 439,
            WRN_GlobalAliasDefn = 440,
            ERR_SealedStaticClass = 441,
            ERR_PrivateAbstractAccessor = 442,
            ERR_ValueExpected = 443,
            WRN_UnexpectedPredefTypeLoc = 444,  // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn"" is specified on the command line.
            ERR_UnboxNotLValue = 445,
            ERR_AnonMethGrpInForEach = 446,
            //ERR_AttrOnTypeArg = 447,      unused in Roslyn. The scenario for which this error exists should, and does generate a parse error.
            ERR_BadIncDecRetType = 448,
            ERR_RefValBoundMustBeFirst = 449,
            ERR_RefValBoundWithClass = 450,
            ERR_NewBoundWithVal = 451,
            ERR_RefConstraintNotSatisfied = 452,
            ERR_ValConstraintNotSatisfied = 453,
            ERR_CircularConstraint = 454,
            ERR_BaseConstraintConflict = 455,
            ERR_ConWithValCon = 456,
            ERR_AmbigUDConv = 457,
            WRN_AlwaysNull = 458,
            ERR_AddrOnReadOnlyLocal = 459,
            ERR_OverrideWithConstraints = 460,
            ERR_AmbigOverride = 462,
            ERR_DecConstError = 463,
            WRN_CmpAlwaysFalse = 464,
            WRN_FinalizeMethod = 465,
            ERR_ExplicitImplParams = 466,
            WRN_AmbigLookupMeth = 467,
            ERR_SameFullNameThisAggThisAgg = 468,
            WRN_GotoCaseShouldConvert = 469,
            ERR_MethodImplementingAccessor = 470,
            //ERR_TypeArgsNotAllowedAmbig = 471,    no longer issued in Roslyn
            WRN_NubExprIsConstBool = 472,
            WRN_ExplicitImplCollision = 473,
            ERR_AbstractHasBody = 500,
            ERR_ConcreteMissingBody = 501,
            ERR_AbstractAndSealed = 502,
            ERR_AbstractNotVirtual = 503,
            ERR_StaticConstant = 504,
            ERR_CantOverrideNonFunction = 505,
            ERR_CantOverrideNonVirtual = 506,
            ERR_CantChangeAccessOnOverride = 507,
            ERR_CantChangeReturnTypeOnOverride = 508,
            ERR_CantDeriveFromSealedType = 509,
            ERR_AbstractInConcreteClass = 513,
            ERR_StaticConstructorWithExplicitConstructorCall = 514,
            ERR_StaticConstructorWithAccessModifiers = 515,
            ERR_RecursiveConstructorCall = 516,
            ERR_ObjectCallingBaseConstructor = 517,
            ERR_PredefinedTypeNotFound = 518,
            //ERR_PredefinedTypeBadType = 520,
            ERR_StructWithBaseConstructorCall = 522,
            ERR_StructLayoutCycle = 523,
            ERR_InterfacesCannotContainTypes = 524,
            ERR_InterfacesCantContainFields = 525,
            ERR_InterfacesCantContainConstructors = 526,
            ERR_NonInterfaceInInterfaceList = 527,
            ERR_DuplicateInterfaceInBaseList = 528,
            ERR_CycleInInterfaceInheritance = 529,
            ERR_InterfaceMemberHasBody = 531,
            ERR_HidingAbstractMethod = 533,
            ERR_UnimplementedAbstractMethod = 534,
            ERR_UnimplementedInterfaceMember = 535,
            ERR_ObjectCantHaveBases = 537,
            ERR_ExplicitInterfaceImplementationNotInterface = 538,
            ERR_InterfaceMemberNotFound = 539,
            ERR_ClassDoesntImplementInterface = 540,
            ERR_ExplicitInterfaceImplementationInNonClassOrStruct = 541,
            ERR_MemberNameSameAsType = 542,
            ERR_EnumeratorOverflow = 543,
            ERR_CantOverrideNonProperty = 544,
            ERR_NoGetToOverride = 545,
            ERR_NoSetToOverride = 546,
            ERR_PropertyCantHaveVoidType = 547,
            ERR_PropertyWithNoAccessors = 548,
            ERR_NewVirtualInSealed = 549,
            ERR_ExplicitPropertyAddingAccessor = 550,
            ERR_ExplicitPropertyMissingAccessor = 551,
            ERR_ConversionWithInterface = 552,
            ERR_ConversionWithBase = 553,
            ERR_ConversionWithDerived = 554,
            ERR_IdentityConversion = 555,
            ERR_ConversionNotInvolvingContainedType = 556,
            ERR_DuplicateConversionInClass = 557,
            ERR_OperatorsMustBeStatic = 558,
            ERR_BadIncDecSignature = 559,
            ERR_BadUnaryOperatorSignature = 562,
            ERR_BadBinaryOperatorSignature = 563,
            ERR_BadShiftOperatorSignature = 564,
            ERR_InterfacesCantContainOperators = 567,
            ERR_StructsCantContainDefaultConstructor = 568,
            ERR_CantOverrideBogusMethod = 569,
            ERR_BindToBogus = 570,
            ERR_CantCallSpecialMethod = 571,
            ERR_BadTypeReference = 572,
            ERR_FieldInitializerInStruct = 573,
            ERR_BadDestructorName = 574,
            ERR_OnlyClassesCanContainDestructors = 575,
            ERR_ConflictAliasAndMember = 576,
            ERR_ConditionalOnSpecialMethod = 577,
            ERR_ConditionalMustReturnVoid = 578,
            ERR_DuplicateAttribute = 579,
            ERR_ConditionalOnInterfaceMethod = 582,
            //ERR_ICE_Culprit = 583,            No ICE in Roslyn. All of these are unused
            //ERR_ICE_Symbol = 584,
            //ERR_ICE_Node = 585,
            //ERR_ICE_File = 586,
            //ERR_ICE_Stage = 587,
            //ERR_ICE_Lexer = 588,
            //ERR_ICE_Parser = 589,
            ERR_OperatorCantReturnVoid = 590,
            ERR_InvalidAttributeArgument = 591,
            ERR_AttributeOnBadSymbolType = 592,
            ERR_FloatOverflow = 594,
            ERR_ComImportWithoutUuidAttribute = 596,
            ERR_InvalidNamedArgument = 599,
            ERR_DllImportOnInvalidMethod = 601,
            WRN_FeatureDeprecated = 602,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:602"" is specified on the command line.
            // ERR_NameAttributeOnOverride = 609, // removed in Roslyn
            ERR_FieldCantBeRefAny = 610,
            ERR_ArrayElementCantBeRefAny = 611,
            WRN_DeprecatedSymbol = 612,
            ERR_NotAnAttributeClass = 616,
            ERR_BadNamedAttributeArgument = 617,
            WRN_DeprecatedSymbolStr = 618,
            ERR_DeprecatedSymbolStr = 619,
            ERR_IndexerCantHaveVoidType = 620,
            ERR_VirtualPrivate = 621,
            ERR_ArrayInitToNonArrayType = 622,
            ERR_ArrayInitInBadPlace = 623,
            ERR_MissingStructOffset = 625,
            WRN_ExternMethodNoImplementation = 626,
            WRN_ProtectedInSealed = 628,
            ERR_InterfaceImplementedByConditional = 629,
            ERR_IllegalRefParam = 631,
            ERR_BadArgumentToAttribute = 633,
            //ERR_MissingComTypeOrMarshaller = 635,
            ERR_StructOffsetOnBadStruct = 636,
            ERR_StructOffsetOnBadField = 637,
            ERR_AttributeUsageOnNonAttributeClass = 641,
            WRN_PossibleMistakenNullStatement = 642,
            ERR_DuplicateNamedAttributeArgument = 643,
            ERR_DeriveFromEnumOrValueType = 644,
            ERR_DefaultMemberOnIndexedType = 646,
            //ERR_CustomAttributeError = 647,
            ERR_BogusType = 648,
            WRN_UnassignedInternalField = 649,
            ERR_CStyleArray = 650,
            WRN_VacuousIntegralComp = 652,
            ERR_AbstractAttributeClass = 653,
            ERR_BadNamedAttributeArgumentType = 655,
            ERR_MissingPredefinedMember = 656,
            WRN_AttributeLocationOnBadDeclaration = 657,
            WRN_InvalidAttributeLocation = 658,
            WRN_EqualsWithoutGetHashCode = 659,
            WRN_EqualityOpWithoutEquals = 660,
            WRN_EqualityOpWithoutGetHashCode = 661,
            ERR_OutAttrOnRefParam = 662,
            ERR_OverloadRefKind = 663,
            ERR_LiteralDoubleCast = 664,
            WRN_IncorrectBooleanAssg = 665,
            ERR_ProtectedInStruct = 666,
            //ERR_FeatureDeprecated = 667,
            ERR_InconsistentIndexerNames = 668, // Named 'ERR_InconsistantIndexerNames' in native compiler
            ERR_ComImportWithUserCtor = 669,
            ERR_FieldCantHaveVoidType = 670,
            WRN_NonObsoleteOverridingObsolete = 672,
            ERR_SystemVoid = 673,
            ERR_ExplicitParamArray = 674,
            WRN_BitwiseOrSignExtend = 675,
            ERR_VolatileStruct = 677,
            ERR_VolatileAndReadonly = 678,
            WRN_OldWarning_ProtectedInternal = 679,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:679"" is specified on the command line.
            WRN_OldWarning_AccessibleReadonly = 680,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:680"" is specified on the command line.
            ERR_AbstractField = 681,
            ERR_BogusExplicitImpl = 682,
            ERR_ExplicitMethodImplAccessor = 683,
            WRN_CoClassWithoutComImport = 684,
            ERR_ConditionalWithOutParam = 685,
            ERR_AccessorImplementingMethod = 686,
            ERR_AliasQualAsExpression = 687,
            ERR_DerivingFromATyVar = 689,
            //FTL_MalformedMetadata = 690,
            ERR_DuplicateTypeParameter = 692,
            WRN_TypeParameterSameAsOuterTypeParameter = 693,
            ERR_TypeVariableSameAsParent = 694,
            ERR_UnifyingInterfaceInstantiations = 695,
            ERR_GenericDerivingFromAttribute = 698,
            ERR_TyVarNotFoundInConstraint = 699,
            ERR_BadBoundType = 701,
            ERR_SpecialTypeAsBound = 702,
            ERR_BadVisBound = 703,
            ERR_LookupInTypeVariable = 704,
            ERR_BadConstraintType = 706,
            ERR_InstanceMemberInStaticClass = 708,
            ERR_StaticBaseClass = 709,
            ERR_ConstructorInStaticClass = 710,
            ERR_DestructorInStaticClass = 711,
            ERR_InstantiatingStaticClass = 712,
            ERR_StaticDerivedFromNonObject = 713,
            ERR_StaticClassInterfaceImpl = 714,
            ERR_OperatorInStaticClass = 715,
            ERR_ConvertToStaticClass = 716,
            ERR_ConstraintIsStaticClass = 717,
            ERR_GenericArgIsStaticClass = 718,
            ERR_ArrayOfStaticClass = 719,
            ERR_IndexerInStaticClass = 720,
            ERR_ParameterIsStaticClass = 721,
            ERR_ReturnTypeIsStaticClass = 722,
            ERR_VarDeclIsStaticClass = 723,
            ERR_BadEmptyThrowInFinally = 724,
            //ERR_InvalidDecl = 725,
            //ERR_InvalidSpecifier = 726,
            //ERR_InvalidSpecifierUnk = 727,
            WRN_AssignmentToLockOrDispose = 728,
            ERR_ForwardedTypeInThisAssembly = 729,
            ERR_ForwardedTypeIsNested = 730,
            ERR_CycleInTypeForwarder = 731,
            //ERR_FwdedGeneric = 733,
            ERR_AssemblyNameOnNonModule = 734,
            ERR_InvalidFwdType = 735,
            ERR_CloseUnimplementedInterfaceMemberStatic = 736,
            ERR_CloseUnimplementedInterfaceMemberNotPublic = 737,
            ERR_CloseUnimplementedInterfaceMemberWrongReturnType = 738,
            ERR_DuplicateTypeForwarder = 739,
            ERR_ExpectedSelectOrGroup = 742,
            ERR_ExpectedContextualKeywordOn = 743,
            ERR_ExpectedContextualKeywordEquals = 744,
            ERR_ExpectedContextualKeywordBy = 745,
            ERR_InvalidAnonymousTypeMemberDeclarator = 746,
            ERR_InvalidInitializerElementInitializer = 747,
            ERR_InconsistentLambdaParameterUsage = 748,
            ERR_PartialMethodInvalidModifier = 750,
            ERR_PartialMethodOnlyInPartialClass = 751,
            ERR_PartialMethodCannotHaveOutParameters = 752,
            ERR_PartialMethodOnlyMethods = 753,
            ERR_PartialMethodNotExplicit = 754,
            ERR_PartialMethodExtensionDifference = 755,
            ERR_PartialMethodOnlyOneLatent = 756,
            ERR_PartialMethodOnlyOneActual = 757,
            ERR_PartialMethodParamsDifference = 758,
            ERR_PartialMethodMustHaveLatent = 759,
            ERR_PartialMethodInconsistentConstraints = 761,
            ERR_PartialMethodToDelegate = 762,
            ERR_PartialMethodStaticDifference = 763,
            ERR_PartialMethodUnsafeDifference = 764,
            ERR_PartialMethodInExpressionTree = 765,
            ERR_PartialMethodMustReturnVoid = 766,
            ERR_ExplicitImplCollisionOnRefOut = 767,
            //ERR_NoEmptyArrayRanges = 800,
            //ERR_IntegerSpecifierOnOneDimArrays = 801,
            //ERR_IntegerSpecifierMustBePositive = 802,
            //ERR_ArrayRangeDimensionsMustMatch = 803,
            //ERR_ArrayRangeDimensionsWrong = 804,
            //ERR_IntegerSpecifierValidOnlyOnArrays = 805,
            //ERR_ArrayRangeSpecifierValidOnlyOnArrays = 806,
            //ERR_UseAdditionalSquareBrackets = 807,
            //ERR_DotDotNotAssociative = 808,
            WRN_ObsoleteOverridingNonObsolete = 809,
            WRN_DebugFullNameTooLong = 811,                                 // Dev11 name: ERR_DebugFullNameTooLong
            ERR_ImplicitlyTypedVariableAssignedBadValue = 815,              // Dev10 name: ERR_ImplicitlyTypedLocalAssignedBadValue
            ERR_ImplicitlyTypedVariableWithNoInitializer = 818,             // Dev10 name: ERR_ImplicitlyTypedLocalWithNoInitializer
            ERR_ImplicitlyTypedVariableMultipleDeclarator = 819,            // Dev10 name: ERR_ImplicitlyTypedLocalMultipleDeclarator
            ERR_ImplicitlyTypedVariableAssignedArrayInitializer = 820,      // Dev10 name: ERR_ImplicitlyTypedLocalAssignedArrayInitializer
            ERR_ImplicitlyTypedLocalCannotBeFixed = 821,
            ERR_ImplicitlyTypedVariableCannotBeConst = 822,                 // Dev10 name: ERR_ImplicitlyTypedLocalCannotBeConst
            WRN_ExternCtorNoImplementation = 824,
            ERR_TypeVarNotFound = 825,
            ERR_ImplicitlyTypedArrayNoBestType = 826,
            ERR_AnonymousTypePropertyAssignedBadValue = 828,
            ERR_ExpressionTreeContainsBaseAccess = 831,
            ERR_ExpressionTreeContainsAssignment = 832,
            ERR_AnonymousTypeDuplicatePropertyName = 833,
            ERR_StatementLambdaToExpressionTree = 834,
            ERR_ExpressionTreeMustHaveDelegate = 835,
            ERR_AnonymousTypeNotAvailable = 836,
            ERR_LambdaInIsAs = 837,
            ERR_ExpressionTreeContainsMultiDimensionalArrayInitializer = 838,
            ERR_MissingArgument = 839,
            ERR_AutoPropertiesMustHaveBothAccessors = 840,
            ERR_VariableUsedBeforeDeclaration = 841,
            ERR_ExplicitLayoutAndAutoImplementedProperty = 842,
            ERR_UnassignedThisAutoProperty = 843,
            ERR_VariableUsedBeforeDeclarationAndHidesField = 844,
            ERR_ExpressionTreeContainsBadCoalesce = 845,
            ERR_ArrayInitializerExpected = 846,
            ERR_ArrayInitializerIncorrectLength = 847,
            // ERR_OverloadRefOutCtor = 851,                                Replaced By ERR_OverloadRefKind
            ERR_ExpressionTreeContainsNamedArgument = 853,
            ERR_ExpressionTreeContainsOptionalArgument = 854,
            ERR_ExpressionTreeContainsIndexedProperty = 855,
            ERR_IndexedPropertyRequiresParams = 856,
            ERR_IndexedPropertyMustHaveAllOptionalParams = 857,
            ERR_FusionConfigFileNameTooLong = 858,
            ERR_IdentifierExpected = 1001,
            ERR_SemicolonExpected = 1002,
            ERR_SyntaxError = 1003,
            ERR_DuplicateModifier = 1004,
            ERR_DuplicateAccessor = 1007,
            ERR_IntegralTypeExpected = 1008,
            ERR_IllegalEscape = 1009,
            ERR_NewlineInConst = 1010,
            ERR_EmptyCharConst = 1011,
            ERR_TooManyCharsInConst = 1012,
            ERR_InvalidNumber = 1013,
            ERR_GetOrSetExpected = 1014,
            ERR_ClassTypeExpected = 1015,
            ERR_NamedArgumentExpected = 1016,
            ERR_TooManyCatches = 1017,
            ERR_ThisOrBaseExpected = 1018,
            ERR_OvlUnaryOperatorExpected = 1019,
            ERR_OvlBinaryOperatorExpected = 1020,
            ERR_IntOverflow = 1021,
            ERR_EOFExpected = 1022,
            ERR_BadEmbeddedStmt = 1023,
            ERR_PPDirectiveExpected = 1024,
            ERR_EndOfPPLineExpected = 1025,
            ERR_CloseParenExpected = 1026,
            ERR_EndifDirectiveExpected = 1027,
            ERR_UnexpectedDirective = 1028,
            ERR_ErrorDirective = 1029,
            WRN_WarningDirective = 1030,
            ERR_TypeExpected = 1031,
            ERR_PPDefFollowsToken = 1032,
            //ERR_TooManyLines = 1033,      unused in Roslyn.
            //ERR_LineTooLong = 1034,       unused in Roslyn.
            ERR_OpenEndedComment = 1035,
            ERR_OvlOperatorExpected = 1037,
            ERR_EndRegionDirectiveExpected = 1038,
            ERR_UnterminatedStringLit = 1039,
            ERR_BadDirectivePlacement = 1040,
            ERR_IdentifierExpectedKW = 1041,
            ERR_SemiOrLBraceExpected = 1043,
            ERR_MultiTypeInDeclaration = 1044,
            ERR_AddOrRemoveExpected = 1055,
            ERR_UnexpectedCharacter = 1056,
            ERR_ProtectedInStatic = 1057,
            WRN_UnreachableGeneralCatch = 1058,
            ERR_IncrementLvalueExpected = 1059,
            WRN_UninitializedField = 1060,  //unused in Roslyn but preserving for the purposes of not breaking users' /nowarn settings
            ERR_NoSuchMemberOrExtension = 1061,
            WRN_DeprecatedCollectionInitAddStr = 1062,
            ERR_DeprecatedCollectionInitAddStr = 1063,
            WRN_DeprecatedCollectionInitAdd = 1064,
            ERR_DefaultValueNotAllowed = 1065,
            WRN_DefaultValueForUnconsumedLocation = 1066,
            ERR_PartialWrongTypeParamsVariance = 1067,
            ERR_GlobalSingleTypeNameNotFoundFwd = 1068,
            ERR_DottedTypeNameNotFoundInNSFwd = 1069,
            ERR_SingleTypeNameNotFoundFwd = 1070,
            //ERR_NoSuchMemberOnNoPIAType = 1071,   //EE
            // ERR_EOLExpected = 1099, // EE
            // ERR_NotSupportedinEE = 1100, // EE
            ERR_BadThisParam = 1100,
            ERR_BadRefWithThis = 1101,
            ERR_BadOutWithThis = 1102,
            ERR_BadTypeforThis = 1103,
            ERR_BadParamModThis = 1104,
            ERR_BadExtensionMeth = 1105,
            ERR_BadExtensionAgg = 1106,
            ERR_DupParamMod = 1107,
            ERR_MultiParamMod = 1108,
            ERR_ExtensionMethodsDecl = 1109,
            ERR_ExtensionAttrNotFound = 1110,
            //ERR_ExtensionTypeParam = 1111,
            ERR_ExplicitExtension = 1112,
            ERR_ValueTypeExtDelegate = 1113,
            // Below five error codes are unused, but we still need to retain them to suppress CS1691 when ""/nowarn:1200,1201,1202,1203,1204"" is specified on the command line.
            WRN_FeatureDeprecated2 = 1200,
            WRN_FeatureDeprecated3 = 1201,
            WRN_FeatureDeprecated4 = 1202,
            WRN_FeatureDeprecated5 = 1203,
            WRN_OldWarning_FeatureDefaultDeprecated = 1204,
            ERR_BadArgCount = 1501,
            //ERR_BadArgTypes = 1502,
            ERR_BadArgType = 1503,
            ERR_NoSourceFile = 1504,
            ERR_CantRefResource = 1507,
            ERR_ResourceNotUnique = 1508,
            ERR_ImportNonAssembly = 1509,
            ERR_RefLvalueExpected = 1510,
            ERR_BaseInStaticMeth = 1511,
            ERR_BaseInBadContext = 1512,
            ERR_RbraceExpected = 1513,
            ERR_LbraceExpected = 1514,
            ERR_InExpected = 1515,
            ERR_InvalidPreprocExpr = 1517,
            //ERR_BadTokenInType = 1518,    unused in Roslyn
            ERR_InvalidMemberDecl = 1519,
            ERR_MemberNeedsType = 1520,
            ERR_BadBaseType = 1521,
            WRN_EmptySwitch = 1522,
            ERR_ExpectedEndTry = 1524,
            ERR_InvalidExprTerm = 1525,
            ERR_BadNewExpr = 1526,
            ERR_NoNamespacePrivate = 1527,
            ERR_BadVarDecl = 1528,
            ERR_UsingAfterElements = 1529,
            //ERR_NoNewOnNamespaceElement = 1530, EDMAURER we now give BadMemberFlag which is only a little less specific than this.
            //ERR_DontUseInvoke = 1533,
            ERR_BadBinOpArgs = 1534,
            ERR_BadUnOpArgs = 1535,
            ERR_NoVoidParameter = 1536,
            ERR_DuplicateAlias = 1537,
            ERR_BadProtectedAccess = 1540,
            //ERR_CantIncludeDirectory = 1541,
            ERR_AddModuleAssembly = 1542,
            ERR_BindToBogusProp2 = 1545,
            ERR_BindToBogusProp1 = 1546,
            ERR_NoVoidHere = 1547,
            //ERR_CryptoFailed = 1548,
            //ERR_CryptoNotFound = 1549,
            ERR_IndexerNeedsParam = 1551,
            ERR_BadArraySyntax = 1552,
            ERR_BadOperatorSyntax = 1553,
            ERR_BadOperatorSyntax2 = 1554,
            ERR_MainClassNotFound = 1555,
            ERR_MainClassNotClass = 1556,
            ERR_MainClassWrongFile = 1557,
            ERR_NoMainInClass = 1558,
            ERR_MainClassIsImport = 1559,
            //ERR_FileNameTooLong = 1560,
            ERR_OutputFileNameTooLong = 1561,
            ERR_OutputNeedsName = 1562,
            //ERR_OutputNeedsInput = 1563,
            ERR_CantHaveWin32ResAndManifest = 1564,
            ERR_CantHaveWin32ResAndIcon = 1565,
            ERR_CantReadResource = 1566,
            //ERR_AutoResGen = 1567,
            //ERR_DocFileGen = 1569,
            WRN_XMLParseError = 1570,
            WRN_DuplicateParamTag = 1571,
            WRN_UnmatchedParamTag = 1572,
            WRN_MissingParamTag = 1573,
            WRN_BadXMLRef = 1574,
            ERR_BadStackAllocExpr = 1575,
            ERR_InvalidLineNumber = 1576,
            //ERR_ALinkFailed = 1577,               No alink usage in Roslyn
            ERR_MissingPPFile = 1578,
            ERR_ForEachMissingMember = 1579,
            WRN_BadXMLRefParamType = 1580,
            WRN_BadXMLRefReturnType = 1581,
            ERR_BadWin32Res = 1583,
            WRN_BadXMLRefSyntax = 1584,
            ERR_BadModifierLocation = 1585,
            ERR_MissingArraySize = 1586,
            WRN_UnprocessedXMLComment = 1587,
            //ERR_CantGetCORSystemDir = 1588,
            WRN_FailedInclude = 1589,
            WRN_InvalidInclude = 1590,
            WRN_MissingXMLComment = 1591,
            WRN_XMLParseIncludeError = 1592,
            ERR_BadDelArgCount = 1593,
            //ERR_BadDelArgTypes = 1594,
            WRN_OldWarning_MultipleTypeDefs = 1595,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1595"" is specified on the command line.
            WRN_OldWarning_DocFileGenAndIncr = 1596,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1596"" is specified on the command line.
            ERR_UnexpectedSemicolon = 1597,
            WRN_XMLParserNotFound = 1598, // No longer used (though, conceivably, we could report it if Linq to Xml is missing at compile time).
            ERR_MethodReturnCantBeRefAny = 1599,
            ERR_CompileCancelled = 1600,
            ERR_MethodArgCantBeRefAny = 1601,
            ERR_AssgReadonlyLocal = 1604,
            ERR_RefReadonlyLocal = 1605,
            //ERR_ALinkCloseFailed = 1606,
            WRN_ALinkWarn = 1607,     // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1607"" is specified on the command line.
            ERR_CantUseRequiredAttribute = 1608,
            ERR_NoModifiersOnAccessor = 1609,
            WRN_DeleteAutoResFailed = 1610, // Unused but preserving for /nowarn
            ERR_ParamsCantBeRefOut = 1611,
            ERR_ReturnNotLValue = 1612,
            ERR_MissingCoClass = 1613,
            ERR_AmbiguousAttribute = 1614,
            ERR_BadArgExtraRef = 1615,
            WRN_CmdOptionConflictsSource = 1616,
            ERR_BadCompatMode = 1617,
            ERR_DelegateOnConditional = 1618,
            //ERR_CantMakeTempFile = 1619,
            ERR_BadArgRef = 1620,
            ERR_YieldInAnonMeth = 1621,
            ERR_ReturnInIterator = 1622,
            ERR_BadIteratorArgType = 1623,
            ERR_BadIteratorReturn = 1624,
            ERR_BadYieldInFinally = 1625,
            ERR_BadYieldInTryOfCatch = 1626,
            ERR_EmptyYield = 1627,
            ERR_AnonDelegateCantUse = 1628,
            ERR_IllegalInnerUnsafe = 1629,
            //ERR_BadWatsonMode = 1630,
            ERR_BadYieldInCatch = 1631,
            ERR_BadDelegateLeave = 1632,
            WRN_IllegalPragma = 1633,
            WRN_IllegalPPWarning = 1634,
            WRN_BadRestoreNumber = 1635,
            ERR_VarargsIterator = 1636,
            ERR_UnsafeIteratorArgType = 1637,
            //ERR_ReservedIdentifier = 1638,
            ERR_BadCoClassSig = 1639,
            ERR_MultipleIEnumOfT = 1640,
            ERR_FixedDimsRequired = 1641,
            ERR_FixedNotInStruct = 1642,
            ERR_AnonymousReturnExpected = 1643,
            ERR_NonECMAFeature = 1644,
            WRN_NonECMAFeature = 1645,
            ERR_ExpectedVerbatimLiteral = 1646,
            //FTL_StackOverflow = 1647,
            ERR_AssgReadonly2 = 1648,
            ERR_RefReadonly2 = 1649,
            ERR_AssgReadonlyStatic2 = 1650,
            ERR_RefReadonlyStatic2 = 1651,
            ERR_AssgReadonlyLocal2Cause = 1654,
            ERR_RefReadonlyLocal2Cause = 1655,
            ERR_AssgReadonlyLocalCause = 1656,
            ERR_RefReadonlyLocalCause = 1657,
            WRN_ErrorOverride = 1658,
            WRN_OldWarning_ReservedIdentifier = 1659,    // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1659"" is specified on the command line.
            ERR_AnonMethToNonDel = 1660,
            ERR_CantConvAnonMethParams = 1661,
            ERR_CantConvAnonMethReturns = 1662,
            ERR_IllegalFixedType = 1663,
            ERR_FixedOverflow = 1664,
            ERR_InvalidFixedArraySize = 1665,
            ERR_FixedBufferNotFixed = 1666,
            ERR_AttributeNotOnAccessor = 1667,
            WRN_InvalidSearchPathDir = 1668,
            ERR_IllegalVarArgs = 1669,
            ERR_IllegalParams = 1670,
            ERR_BadModifiersOnNamespace = 1671,
            ERR_BadPlatformType = 1672,
            ERR_ThisStructNotInAnonMeth = 1673,
            ERR_NoConvToIDisp = 1674,
            // ERR_InvalidGenericEnum = 1675,    replaced with 7002
            ERR_BadParamRef = 1676,
            ERR_BadParamExtraRef = 1677,
            ERR_BadParamType = 1678,
            ERR_BadExternIdentifier = 1679,
            ERR_AliasMissingFile = 1680,
            ERR_GlobalExternAlias = 1681,
            WRN_MissingTypeNested = 1682,   //unused in Roslyn.
            // 1683 and 1684 are unused warning codes, but we still need to retain them to suppress CS1691 when ""/nowarn:1683"" is specified on the command line.
            // In Roslyn, we generate errors ERR_MissingTypeInSource and ERR_MissingTypeInAssembly instead of warnings WRN_MissingTypeInSource and WRN_MissingTypeInAssembly respectively.
            WRN_MissingTypeInSource = 1683,
            WRN_MissingTypeInAssembly = 1684,
            WRN_MultiplePredefTypes = 1685,
            ERR_LocalCantBeFixedAndHoisted = 1686,
            WRN_TooManyLinesForDebugger = 1687,
            ERR_CantConvAnonMethNoParams = 1688,
            ERR_ConditionalOnNonAttributeClass = 1689,
            WRN_CallOnNonAgileField = 1690,
            WRN_BadWarningNumber = 1691,
            WRN_InvalidNumber = 1692,
            WRN_FileNameTooLong = 1694, //unused but preserving for the sake of existing code using /nowarn:1694
            WRN_IllegalPPChecksum = 1695,
            WRN_EndOfPPLineExpected = 1696,
            WRN_ConflictingChecksum = 1697,
            WRN_AssumedMatchThis = 1698,
            WRN_UseSwitchInsteadOfAttribute = 1699,     // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1699"" is specified.
            WRN_InvalidAssemblyName = 1700,             // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1700"" is specified.
            WRN_UnifyReferenceMajMin = 1701,
            WRN_UnifyReferenceBldRev = 1702,
            ERR_DuplicateImport = 1703,
            ERR_DuplicateImportSimple = 1704,
            ERR_AssemblyMatchBadVersion = 1705,
            ERR_AnonMethNotAllowed = 1706,
            WRN_DelegateNewMethBind = 1707,             // This error code is unused, but we still need to retain it to suppress CS1691 when ""/nowarn:1707"" is specified.
            ERR_FixedNeedsLvalue = 1708,
            WRN_EmptyFileName = 1709,
            WRN_DuplicateTypeParamTag = 1710,
            WRN_UnmatchedTypeParamTag = 1711,
            WRN_MissingTypeParamTag = 1712,
            //FTL_TypeNameBuilderError = 1713,
            ERR_ImportBadBase = 1714,
            ERR_CantChangeTypeOnOverride = 1715,
            ERR_DoNotUseFixedBufferAttr = 1716,
            WRN_AssignmentToSelf = 1717,
            WRN_ComparisonToSelf = 1718,
            ERR_CantOpenWin32Res = 1719,
            WRN_DotOnDefault = 1720,
            ERR_NoMultipleInheritance = 1721,
            ERR_BaseClassMustBeFirst = 1722,
            WRN_BadXMLRefTypeVar = 1723,
            //ERR_InvalidDefaultCharSetValue = 1724,    Not used in Roslyn.
            ERR_FriendAssemblyBadArgs = 1725,
            ERR_FriendAssemblySNReq = 1726,
            //ERR_WatsonSendNotOptedIn = 1727,            We're not doing any custom Watson processing in Roslyn. In modern OSs, Watson behavior is configured with machine policy settings.
            ERR_DelegateOnNullable = 1728,
            ERR_BadCtorArgCount = 1729,
            ERR_GlobalAttributesNotFirst = 1730,
            ERR_CantConvAnonMethReturnsNoDelegate = 1731,
            //ERR_ParameterExpected = 1732,             Not used in Roslyn.
            ERR_ExpressionExpected = 1733,
            WRN_UnmatchedParamRefTag = 1734,
            WRN_UnmatchedTypeParamRefTag = 1735,
            ERR_DefaultValueMustBeConstant = 1736,
            ERR_DefaultValueBeforeRequiredValue = 1737,
            ERR_NamedArgumentSpecificationBeforeFixedArgument = 1738,
            ERR_BadNamedArgument = 1739,
            ERR_DuplicateNamedArgument = 1740,
            ERR_RefOutDefaultValue = 1741,
            ERR_NamedArgumentForArray = 1742,
            ERR_DefaultValueForExtensionParameter = 1743,
            ERR_NamedArgumentUsedInPositional = 1744,
            ERR_DefaultValueUsedWithAttributes = 1745,
            ERR_BadNamedArgumentForDelegateInvoke = 1746,
            ERR_NoPIAAssemblyMissingAttribute = 1747,
            ERR_NoCanonicalView = 1748,
            //ERR_TypeNotFoundForNoPIA = 1749,
            ERR_NoConversionForDefaultParam = 1750,
            ERR_DefaultValueForParamsParameter = 1751,
            ERR_NewCoClassOnLink = 1752,
            ERR_NoPIANestedType = 1754,
            //ERR_InvalidTypeIdentifierConstructor = 1755,
            ERR_InteropTypeMissingAttribute = 1756,
            ERR_InteropStructContainsMethods = 1757,
            ERR_InteropTypesWithSameNameAndGuid = 1758,
            ERR_NoPIAAssemblyMissingAttributes = 1759,
            ERR_AssemblySpecifiedForLinkAndRef = 1760,
            ERR_LocalTypeNameClash = 1761,
            WRN_ReferencedAssemblyReferencesLinkedPIA = 1762,
            ERR_NotNullRefDefaultParameter = 1763,
            ERR_FixedLocalInLambda = 1764,
            WRN_TypeNotFoundForNoPIAWarning = 1765,
            ERR_MissingMethodOnSourceInterface = 1766,
            ERR_MissingSourceInterface = 1767,
            ERR_GenericsUsedInNoPIAType = 1768,
            ERR_GenericsUsedAcrossAssemblies = 1769,
            ERR_NoConversionForNubDefaultParam = 1770,
            //ERR_MemberWithGenericsUsedAcrossAssemblies = 1771,
            //ERR_GenericsUsedInBaseTypeAcrossAssemblies = 1772,
            ERR_BadSubsystemVersion = 1773,
            ERR_InteropMethodWithBody = 1774,
            ERR_BadWarningLevel = 1900,
            ERR_BadDebugType = 1902,
            //ERR_UnknownTestSwitch = 1903,
            ERR_BadResourceVis = 1906,
            ERR_DefaultValueTypeMustMatch = 1908,
            //ERR_DefaultValueBadParamType = 1909, // Replaced by ERR_DefaultValueBadValueType in Roslyn.
            ERR_DefaultValueBadValueType = 1910,
            ERR_MemberAlreadyInitialized = 1912,
            ERR_MemberCannotBeInitialized = 1913,
            ERR_StaticMemberInObjectInitializer = 1914,
            ERR_ReadonlyValueTypeInObjectInitializer = 1917,
            ERR_ValueTypePropertyInObjectInitializer = 1918,
            ERR_UnsafeTypeInObjectCreation = 1919,
            ERR_EmptyElementInitializer = 1920,
            ERR_InitializerAddHasWrongSignature = 1921,
            ERR_CollectionInitRequiresIEnumerable = 1922,
            ERR_InvalidCollectionInitializerType = 1925,
            ERR_CantOpenWin32Manifest = 1926,
            WRN_CantHaveManifestForModule = 1927,
            ERR_BadExtensionArgTypes = 1928,
            ERR_BadInstanceArgType = 1929,
            ERR_QueryDuplicateRangeVariable = 1930,
            ERR_QueryRangeVariableOverrides = 1931,
            ERR_QueryRangeVariableAssignedBadValue = 1932,
            ERR_QueryNotAllowed = 1933,
            ERR_QueryNoProviderCastable = 1934,
            ERR_QueryNoProviderStandard = 1935,
            ERR_QueryNoProvider = 1936,
            ERR_QueryOuterKey = 1937,
            ERR_QueryInnerKey = 1938,
            ERR_QueryOutRefRangeVariable = 1939,
            ERR_QueryMultipleProviders = 1940,
            ERR_QueryTypeInferenceFailedMulti = 1941,
            ERR_QueryTypeInferenceFailed = 1942,
            ERR_QueryTypeInferenceFailedSelectMany = 1943,
            ERR_ExpressionTreeContainsPointerOp = 1944,
            ERR_ExpressionTreeContainsAnonymousMethod = 1945,
            ERR_AnonymousMethodToExpressionTree = 1946,
            ERR_QueryRangeVariableReadOnly = 1947,
            ERR_QueryRangeVariableSameAsTypeParam = 1948,
            ERR_TypeVarNotFoundRangeVariable = 1949,
            ERR_BadArgTypesForCollectionAdd = 1950,
            ERR_ByRefParameterInExpressionTree = 1951,
            ERR_VarArgsInExpressionTree = 1952,
            ERR_MemGroupInExpressionTree = 1953,
            ERR_InitializerAddHasParamModifiers = 1954,
            ERR_NonInvocableMemberCalled = 1955,
            WRN_MultipleRuntimeImplementationMatches = 1956,
            WRN_MultipleRuntimeOverrideMatches = 1957,
            ERR_ObjectOrCollectionInitializerWithDelegateCreation = 1958,
            ERR_InvalidConstantDeclarationType = 1959,
            ERR_IllegalVarianceSyntax = 1960,
            ERR_UnexpectedVariance = 1961,
            ERR_BadDynamicTypeof = 1962,
            ERR_ExpressionTreeContainsDynamicOperation = 1963,
            ERR_BadDynamicConversion = 1964,
            ERR_DeriveFromDynamic = 1965,
            ERR_DeriveFromConstructedDynamic = 1966,
            ERR_DynamicTypeAsBound = 1967,
            ERR_ConstructedDynamicTypeAsBound = 1968,
            ERR_DynamicRequiredTypesMissing = 1969,
            ERR_ExplicitDynamicAttr = 1970,
            ERR_NoDynamicPhantomOnBase = 1971,
            ERR_NoDynamicPhantomOnBaseIndexer = 1972,
            ERR_BadArgTypeDynamicExtension = 1973,
            WRN_DynamicDispatchToConditionalMethod = 1974,
            ERR_NoDynamicPhantomOnBaseCtor = 1975,
            ERR_BadDynamicMethodArgMemgrp = 1976,
            ERR_BadDynamicMethodArgLambda = 1977,
            ERR_BadDynamicMethodArg = 1978,
            ERR_BadDynamicQuery = 1979,
            ERR_DynamicAttributeMissing = 1980,
            WRN_IsDynamicIsConfusing = 1981,
            ERR_DynamicNotAllowedInAttribute = 1982,                    // Replaced by ERR_BadAttributeParamType in Roslyn.
            ERR_BadAsyncReturn = 1983,
            ERR_BadAwaitInFinally = 1984,
            ERR_BadAwaitInCatch = 1985,
            ERR_BadAwaitArg = 1986,
            ERR_BadAsyncArgType = 1988,
            ERR_BadAsyncExpressionTree = 1989,
            ERR_WindowsRuntimeTypesMissing = 1990,
            ERR_MixingWinRTEventWithRegular = 1991,
            ERR_BadAwaitWithoutAsync = 1992,
            ERR_MissingAsyncTypes = 1993,
            ERR_BadAsyncLacksBody = 1994,
            ERR_BadAwaitInQuery = 1995,
            ERR_BadAwaitInLock = 1996,
            ERR_TaskRetNoObjectRequired = 1997,
            WRN_AsyncLacksAwaits = 1998,
            ERR_FileNotFound = 2001,
            WRN_FileAlreadyIncluded = 2002,
            ERR_DuplicateResponseFile = 2003,
            ERR_NoFileSpec = 2005,
            ERR_SwitchNeedsString = 2006,
            ERR_BadSwitch = 2007,
            WRN_NoSources = 2008,
            ERR_OpenResponseFile = 2011,
            ERR_CantOpenFileWrite = 2012,
            ERR_BadBaseNumber = 2013,
            WRN_UseNewSwitch = 2014,    //unused but preserved to keep compat with /nowarn:2014
            ERR_BinaryFile = 2015,
            FTL_BadCodepage = 2016,
            ERR_NoMainOnDLL = 2017,
            //FTL_NoMessagesDLL = 2018,
            FTL_InvalidTarget = 2019,
            //ERR_BadTargetForSecondInputSet = 2020,    Roslyn doesn't support building two binaries at once!
            FTL_InvalidInputFileName = 2021,
            //ERR_NoSourcesInLastInputSet = 2022,       Roslyn doesn't support building two binaries at once!
            WRN_NoConfigNotOnCommandLine = 2023,
            ERR_BadFileAlignment = 2024,
            //ERR_NoDebugSwitchSourceMap = 2026,    no sourcemap support in Roslyn.
            //ERR_SourceMapFileBinary = 2027,
            WRN_DefineIdentifierRequired = 2029,
            //ERR_InvalidSourceMap = 2030,
            //ERR_NoSourceMapFile = 2031,
            ERR_IllegalOptionChar = 2032,
            FTL_OutputFileExists = 2033,
            ERR_OneAliasPerReference = 2034,
            ERR_SwitchNeedsNumber = 2035,
            ERR_MissingDebugSwitch = 2036,
            ERR_ComRefCallInExpressionTree = 2037,
            WRN_BadUILang = 2038,
            WRN_CLS_NoVarArgs = 3000,
            WRN_CLS_BadArgType = 3001,
            WRN_CLS_BadReturnType = 3002,
            WRN_CLS_BadFieldPropType = 3003,
            WRN_CLS_BadUnicode = 3004, //unused but preserved to keep compat with /nowarn:3004
            WRN_CLS_BadIdentifierCase = 3005,
            WRN_CLS_OverloadRefOut = 3006,
            WRN_CLS_OverloadUnnamed = 3007,
            WRN_CLS_BadIdentifier = 3008,
            WRN_CLS_BadBase = 3009,
            WRN_CLS_BadInterfaceMember = 3010,
            WRN_CLS_NoAbstractMembers = 3011,
            WRN_CLS_NotOnModules = 3012,
            WRN_CLS_ModuleMissingCLS = 3013,
            WRN_CLS_AssemblyNotCLS = 3014,
            WRN_CLS_BadAttributeType = 3015,
            WRN_CLS_ArrayArgumentToAttribute = 3016,
            WRN_CLS_NotOnModules2 = 3017,
            WRN_CLS_IllegalTrueInFalse = 3018,
            WRN_CLS_MeaninglessOnPrivateType = 3019,
            WRN_CLS_AssemblyNotCLS2 = 3021,
            WRN_CLS_MeaninglessOnParam = 3022,
            WRN_CLS_MeaninglessOnReturn = 3023,
            WRN_CLS_BadTypeVar = 3024,
            WRN_CLS_VolatileField = 3026,
            WRN_CLS_BadInterface = 3027,

            // Errors introduced in C# 5 are in the range 4000-4999
            // 4000 unused
            ERR_BadAwaitArgIntrinsic = 4001,
            // 4002 unused
            ERR_BadAwaitAsIdentifier = 4003,
            ERR_AwaitInUnsafeContext = 4004,
            ERR_UnsafeAsyncArgType = 4005,
            ERR_VarargsAsync = 4006,
            ERR_ByRefTypeAndAwait = 4007,
            ERR_BadAwaitArgVoidCall = 4008,
            ERR_MainCantBeAsync = 4009,
            ERR_CantConvAsyncAnonFuncReturns = 4010,
            ERR_BadAwaiterPattern = 4011,
            ERR_BadSpecialByRefLocal = 4012,
            ERR_SpecialByRefInLambda = 4013,
            WRN_UnobservedAwaitableExpression = 4014,
            ERR_SynchronizedAsyncMethod = 4015,
            ERR_BadAsyncReturnExpression = 4016,
            ERR_NoConversionForCallerLineNumberParam = 4017,
            ERR_NoConversionForCallerFilePathParam = 4018,
            ERR_NoConversionForCallerMemberNameParam = 4019,
            ERR_BadCallerLineNumberParamWithoutDefaultValue = 4020,
            ERR_BadCallerFilePathParamWithoutDefaultValue = 4021,
            ERR_BadCallerMemberNameParamWithoutDefaultValue = 4022,
            ERR_BadPrefer32OnLib = 4023,
            WRN_CallerLineNumberParamForUnconsumedLocation = 4024,
            WRN_CallerFilePathParamForUnconsumedLocation = 4025,
            WRN_CallerMemberNameParamForUnconsumedLocation = 4026,
            ERR_DoesntImplementAwaitInterface = 4027,
            ERR_BadAwaitArg_NeedSystem = 4028,
            ERR_CantReturnVoid = 4029,
            ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync = 4030,
            ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct = 4031,
            ERR_BadAwaitWithoutAsyncMethod = 4032,
            ERR_BadAwaitWithoutVoidAsyncMethod = 4033,
            ERR_BadAwaitWithoutAsyncLambda = 4034,
            // ERR_BadAwaitWithoutAsyncAnonMeth = 4035,         Merged with ERR_BadAwaitWithoutAsyncLambda in Roslyn
            ERR_NoSuchMemberOrExtensionNeedUsing = 4036,


            WRN_UnknownOption = 5000,   //unused in Roslyn
            ERR_NoEntryPoint = 5001,

            // There is space in the range of error codes from 7000-8999 that
            // we can use for new errors in post-Dev10.

            ERR_UnexpectedAliasedName = 7000,
            ERR_UnexpectedGenericName = 7002,
            ERR_UnexpectedUnboundGenericName = 7003,
            ERR_GlobalStatement = 7006,
            ERR_BadUsingType = 7007,
            ERR_ReservedAssemblyName = 7008,
            ERR_PPReferenceFollowsToken = 7009,
            ERR_ExpectedPPFile = 7010,
            ERR_ReferenceDirectiveOnlyAllowedInScripts = 7011,
            ERR_NameNotInContextPossibleMissingReference = 7012,
            WRN_MetadataNameTooLong = 7013,
            ERR_AttributesNotAllowed = 7014,
            ERR_ExternAliasNotAllowed = 7015,
            ERR_ConflictingAliasAndDefinition = 7016,
            ERR_GlobalDefinitionOrStatementExpected = 7017,
            ERR_ExpectedSingleScript = 7018,
            ERR_RecursivelyTypedVariable = 7019,
            ERR_ReturnNotAllowedInScript = 7020,
            ERR_NamespaceNotAllowedInScript = 7021,
            WRN_MainIgnored = 7022,
            ERR_StaticInAsOrIs = 7023,
            ERR_InvalidDelegateType = 7024,
            ERR_BadVisEventType = 7025,
            ERR_GlobalAttributesNotAllowed = 7026,
            ERR_PublicKeyFileFailure = 7027,
            ERR_PublicKeyContainerFailure = 7028,
            ERR_FriendRefSigningMismatch = 7029,
            ERR_CannotPassNullForFriendAssembly = 7030,
            ERR_SignButNoPrivateKey = 7032,
            WRN_DelaySignButNoKey = 7033,
            ERR_InvalidVersionFormat = 7034,
            WRN_InvalidVersionFormat = 7035,
            ERR_NoCorrespondingArgument = 7036,
            // Moot: WRN_DestructorIsNotFinalizer = 7037,
            ERR_ModuleEmitFailure = 7038,
            ERR_NameIllegallyOverrides2 = 7039,
            ERR_NameIllegallyOverrides3 = 7040,
            ERR_ResourceFileNameNotUnique = 7041,
            ERR_DllImportOnGenericMethod = 7042,
            ERR_LibraryMethodNotFound = 7043,
            ERR_LibraryMethodNotUnique = 7044,
            ERR_ParameterNotValidForType = 7045,
            ERR_AttributeParameterRequired1 = 7046,
            ERR_AttributeParameterRequired2 = 7047,
            ERR_SecurityAttributeMissingAction = 7048,
            ERR_SecurityAttributeInvalidAction = 7049,
            ERR_SecurityAttributeInvalidActionAssembly = 7050,
            ERR_SecurityAttributeInvalidActionTypeOrMethod = 7051,
            ERR_PrincipalPermissionInvalidAction = 7052,
            ERR_FeatureNotValidInExpressionTree = 7053,
            ERR_MarshalUnmanagedTypeNotValidForFields = 7054,
            ERR_MarshalUnmanagedTypeOnlyValidForFields = 7055,
            ERR_PermissionSetAttributeInvalidFile = 7056,
            ERR_PermissionSetAttributeFileReadError = 7057,
            ERR_InvalidVersionFormat2 = 7058,
            ERR_InvalidAssemblyCultureForExe = 7059,
            ERR_AsyncBeforeVersionFive = 7060,
            ERR_DuplicateAttributeInNetModule = 7061,
            //WRN_PDBConstantStringValueTooLong = 7063,     gave up on this warning
            ERR_CantOpenIcon = 7064,
            ERR_ErrorBuildingWin32Resources = 7065,
            ERR_IteratorInInteractive = 7066,
            ERR_BadAttributeParamDefaultArgument = 7067,
            ERR_MissingTypeInSource = 7068,
            ERR_MissingTypeInAssembly = 7069,
            ERR_SecurityAttributeInvalidTarget = 7070,
            ERR_InvalidAssemblyName = 7071,
            ERR_PartialTypesBeforeVersionTwo = 7072,
            ERR_PartialMethodsBeforeVersionThree = 7073,
            ERR_QueryBeforeVersionThree = 7074,
            ERR_AnonymousTypeBeforeVersionThree = 7075,
            ERR_ImplicitArrayBeforeVersionThree = 7076,
            ERR_ObjectInitializerBeforeVersionThree = 7077,
            ERR_LambdaBeforeVersionThree = 7078,
            ERR_NoTypeDefFromModule = 7079,
            WRN_CallerFilePathPreferredOverCallerMemberName = 7080,
            WRN_CallerLineNumberPreferredOverCallerMemberName = 7081,
            WRN_CallerLineNumberPreferredOverCallerFilePath = 7082,
            ERR_InvalidDynamicCondition = 7083,
            ERR_WinRtEventPassedByRef = 7084,
            ERR_ByRefReturnUnsupported = 7085,
            ERR_NetModuleNameMismatch = 7086,
            ERR_BadCompilationOption = 7087,
            ERR_BadCompilationOptionValue = 7088,
            ERR_BadAppConfigPath = 7089,
            WRN_AssemblyAttributeFromModuleIsOverridden = 7090,
            ERR_CmdOptionConflictsSource = 7091,
            ERR_FixedBufferTooManyDimensions = 7092,
            ERR_CantReadConfigFile = 7093,

            ERR_NotYetImplementedInRoslyn = 8000,
            WRN_UnimplementedCommandLineSwitch = 8001,
            ERR_ReferencedAssemblyDoesNotHaveStrongName = 8002,
            ERR_InvalidSignaturePublicKey = 8003,
            ERR_ExportedTypeConflictsWithDeclaration = 8004,
            ERR_ExportedTypesConflict = 8005,
            ERR_ForwardedTypeConflictsWithDeclaration = 8006,
            ERR_ForwardedTypesConflict = 8007,
            ERR_ForwardedTypeConflictsWithExportedType = 8008,
            WRN_RefCultureMismatch = 8009,
            ERR_AgnosticToMachineModule = 8010,
            ERR_ConflictingMachineModule = 8011,
            WRN_ConflictingMachineAssembly = 8012,
            ERR_CryptoHashFailed = 8013,

            ERR_MissingNetModuleReference = 8014,

            // Values in the range 10000-14000 are used for ""Code Analysis"" issues previously reported by FXCop
            WRN_CA2000_DisposeObjectsBeforeLosingScope1 = 10000,
            WRN_CA2000_DisposeObjectsBeforeLosingScope2 = 10001,

            WRN_CA2202_DoNotDisposeObjectsMultipleTimes = 10002
        }


        /// <summary>
        /// Values for ErrorCode/ERRID that are used internally by the compiler but are not exposed.
        /// </summary>
        internal static class InternalErrorCode
        {
            /// <summary>
            /// The code has yet to be determined.
            /// </summary>
            public const int Unknown = -1;

            /// <summary>
            /// The code was lazily determined and does not need to be reported.
            /// </summary>
            public const int Void = -2;
        }
    }
}

";
            var compVerifier = CompileAndVerify(text);
            compVerifier.VerifyIL("ConsoleApplication24.Program.IsWarning", @"
{
  // Code size     1889 (0x761)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x32b
  IL_0006:  bgt        IL_0300
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4     0x1ad
  IL_0011:  bgt        IL_0154
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4     0xb8
  IL_001c:  bgt        IL_00a9
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.s   109
  IL_0024:  bgt.s      IL_005f
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.s   67
  IL_0029:  bgt.s      IL_0040
  IL_002b:  ldarg.0
  IL_002c:  ldc.i4.s   28
  IL_002e:  beq        IL_075d
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.s   67
  IL_0036:  beq        IL_075d
  IL_003b:  br         IL_075f
  IL_0040:  ldarg.0
  IL_0041:  ldc.i4.s   78
  IL_0043:  beq        IL_075d
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.s   105
  IL_004b:  beq        IL_075d
  IL_0050:  ldarg.0
  IL_0051:  ldc.i4.s   108
  IL_0053:  sub
  IL_0054:  ldc.i4.1
  IL_0055:  ble.un     IL_075d
  IL_005a:  br         IL_075f
  IL_005f:  ldarg.0
  IL_0060:  ldc.i4     0xa2
  IL_0065:  bgt.s      IL_007f
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.s   114
  IL_006a:  beq        IL_075d
  IL_006f:  ldarg.0
  IL_0070:  ldc.i4     0xa2
  IL_0075:  beq        IL_075d
  IL_007a:  br         IL_075f
  IL_007f:  ldarg.0
  IL_0080:  ldc.i4     0xa4
  IL_0085:  beq        IL_075d
  IL_008a:  ldarg.0
  IL_008b:  ldc.i4     0xa8
  IL_0090:  sub
  IL_0091:  ldc.i4.1
  IL_0092:  ble.un     IL_075d
  IL_0097:  ldarg.0
  IL_0098:  ldc.i4     0xb7
  IL_009d:  sub
  IL_009e:  ldc.i4.1
  IL_009f:  ble.un     IL_075d
  IL_00a4:  br         IL_075f
  IL_00a9:  ldarg.0
  IL_00aa:  ldc.i4     0x118
  IL_00af:  bgt.s      IL_00fe
  IL_00b1:  ldarg.0
  IL_00b2:  ldc.i4     0xcf
  IL_00b7:  bgt.s      IL_00d4
  IL_00b9:  ldarg.0
  IL_00ba:  ldc.i4     0xc5
  IL_00bf:  beq        IL_075d
  IL_00c4:  ldarg.0
  IL_00c5:  ldc.i4     0xcf
  IL_00ca:  beq        IL_075d
  IL_00cf:  br         IL_075f
  IL_00d4:  ldarg.0
  IL_00d5:  ldc.i4     0xdb
  IL_00da:  beq        IL_075d
  IL_00df:  ldarg.0
  IL_00e0:  ldc.i4     0xfb
  IL_00e5:  sub
  IL_00e6:  ldc.i4.2
  IL_00e7:  ble.un     IL_075d
  IL_00ec:  ldarg.0
  IL_00ed:  ldc.i4     0x116
  IL_00f2:  sub
  IL_00f3:  ldc.i4.2
  IL_00f4:  ble.un     IL_075d
  IL_00f9:  br         IL_075f
  IL_00fe:  ldarg.0
  IL_00ff:  ldc.i4     0x19e
  IL_0104:  bgt.s      IL_012c
  IL_0106:  ldarg.0
  IL_0107:  ldc.i4     0x11a
  IL_010c:  beq        IL_075d
  IL_0111:  ldarg.0
  IL_0112:  ldc.i4     0x192
  IL_0117:  beq        IL_075d
  IL_011c:  ldarg.0
  IL_011d:  ldc.i4     0x19e
  IL_0122:  beq        IL_075d
  IL_0127:  br         IL_075f
  IL_012c:  ldarg.0
  IL_012d:  ldc.i4     0x1a3
  IL_0132:  sub
  IL_0133:  ldc.i4.1
  IL_0134:  ble.un     IL_075d
  IL_0139:  ldarg.0
  IL_013a:  ldc.i4     0x1a6
  IL_013f:  beq        IL_075d
  IL_0144:  ldarg.0
  IL_0145:  ldc.i4     0x1ad
  IL_014a:  beq        IL_075d
  IL_014f:  br         IL_075f
  IL_0154:  ldarg.0
  IL_0155:  ldc.i4     0x274
  IL_015a:  bgt        IL_0224
  IL_015f:  ldarg.0
  IL_0160:  ldc.i4     0x1d9
  IL_0165:  bgt.s      IL_01db
  IL_0167:  ldarg.0
  IL_0168:  ldc.i4     0x1b8
  IL_016d:  bgt.s      IL_018c
  IL_016f:  ldarg.0
  IL_0170:  ldc.i4     0x1b3
  IL_0175:  sub
  IL_0176:  ldc.i4.2
  IL_0177:  ble.un     IL_075d
  IL_017c:  ldarg.0
  IL_017d:  ldc.i4     0x1b8
  IL_0182:  beq        IL_075d
  IL_0187:  br         IL_075f
  IL_018c:  ldarg.0
  IL_018d:  ldc.i4     0x1bc
  IL_0192:  beq        IL_075d
  IL_0197:  ldarg.0
  IL_0198:  ldc.i4     0x1ca
  IL_019d:  beq        IL_075d
  IL_01a2:  ldarg.0
  IL_01a3:  ldc.i4     0x1d0
  IL_01a8:  sub
  IL_01a9:  switch    (
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075d)
  IL_01d6:  br         IL_075f
  IL_01db:  ldarg.0
  IL_01dc:  ldc.i4     0x264
  IL_01e1:  bgt.s      IL_01fe
  IL_01e3:  ldarg.0
  IL_01e4:  ldc.i4     0x25a
  IL_01e9:  beq        IL_075d
  IL_01ee:  ldarg.0
  IL_01ef:  ldc.i4     0x264
  IL_01f4:  beq        IL_075d
  IL_01f9:  br         IL_075f
  IL_01fe:  ldarg.0
  IL_01ff:  ldc.i4     0x26a
  IL_0204:  beq        IL_075d
  IL_0209:  ldarg.0
  IL_020a:  ldc.i4     0x272
  IL_020f:  beq        IL_075d
  IL_0214:  ldarg.0
  IL_0215:  ldc.i4     0x274
  IL_021a:  beq        IL_075d
  IL_021f:  br         IL_075f
  IL_0224:  ldarg.0
  IL_0225:  ldc.i4     0x2a3
  IL_022a:  bgt.s      IL_02aa
  IL_022c:  ldarg.0
  IL_022d:  ldc.i4     0x295
  IL_0232:  bgt.s      IL_0284
  IL_0234:  ldarg.0
  IL_0235:  ldc.i4     0x282
  IL_023a:  beq        IL_075d
  IL_023f:  ldarg.0
  IL_0240:  ldc.i4     0x289
  IL_0245:  sub
  IL_0246:  switch    (
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d)
  IL_027f:  br         IL_075f
  IL_0284:  ldarg.0
  IL_0285:  ldc.i4     0x299
  IL_028a:  beq        IL_075d
  IL_028f:  ldarg.0
  IL_0290:  ldc.i4     0x2a0
  IL_0295:  beq        IL_075d
  IL_029a:  ldarg.0
  IL_029b:  ldc.i4     0x2a3
  IL_02a0:  beq        IL_075d
  IL_02a5:  br         IL_075f
  IL_02aa:  ldarg.0
  IL_02ab:  ldc.i4     0x2b5
  IL_02b0:  bgt.s      IL_02da
  IL_02b2:  ldarg.0
  IL_02b3:  ldc.i4     0x2a7
  IL_02b8:  sub
  IL_02b9:  ldc.i4.1
  IL_02ba:  ble.un     IL_075d
  IL_02bf:  ldarg.0
  IL_02c0:  ldc.i4     0x2ac
  IL_02c5:  beq        IL_075d
  IL_02ca:  ldarg.0
  IL_02cb:  ldc.i4     0x2b5
  IL_02d0:  beq        IL_075d
  IL_02d5:  br         IL_075f
  IL_02da:  ldarg.0
  IL_02db:  ldc.i4     0x2d8
  IL_02e0:  beq        IL_075d
  IL_02e5:  ldarg.0
  IL_02e6:  ldc.i4     0x329
  IL_02eb:  beq        IL_075d
  IL_02f0:  ldarg.0
  IL_02f1:  ldc.i4     0x32b
  IL_02f6:  beq        IL_075d
  IL_02fb:  br         IL_075f
  IL_0300:  ldarg.0
  IL_0301:  ldc.i4     0x7bd
  IL_0306:  bgt        IL_05d1
  IL_030b:  ldarg.0
  IL_030c:  ldc.i4     0x663
  IL_0311:  bgt        IL_0451
  IL_0316:  ldarg.0
  IL_0317:  ldc.i4     0x5f2
  IL_031c:  bgt.s      IL_038e
  IL_031e:  ldarg.0
  IL_031f:  ldc.i4     0x406
  IL_0324:  bgt.s      IL_0341
  IL_0326:  ldarg.0
  IL_0327:  ldc.i4     0x338
  IL_032c:  beq        IL_075d
  IL_0331:  ldarg.0
  IL_0332:  ldc.i4     0x406
  IL_0337:  beq        IL_075d
  IL_033c:  br         IL_075f
  IL_0341:  ldarg.0
  IL_0342:  ldc.i4     0x422
  IL_0347:  sub
  IL_0348:  switch    (
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075d)
  IL_0371:  ldarg.0
  IL_0372:  ldc.i4     0x4b0
  IL_0377:  sub
  IL_0378:  ldc.i4.4
  IL_0379:  ble.un     IL_075d
  IL_037e:  ldarg.0
  IL_037f:  ldc.i4     0x5f2
  IL_0384:  beq        IL_075d
  IL_0389:  br         IL_075f
  IL_038e:  ldarg.0
  IL_038f:  ldc.i4     0x647
  IL_0394:  bgt        IL_0429
  IL_0399:  ldarg.0
  IL_039a:  ldc.i4     0x622
  IL_039f:  sub
  IL_03a0:  switch    (
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d)
  IL_0419:  ldarg.0
  IL_041a:  ldc.i4     0x647
  IL_041f:  beq        IL_075d
  IL_0424:  br         IL_075f
  IL_0429:  ldarg.0
  IL_042a:  ldc.i4     0x64a
  IL_042f:  beq        IL_075d
  IL_0434:  ldarg.0
  IL_0435:  ldc.i4     0x650
  IL_043a:  beq        IL_075d
  IL_043f:  ldarg.0
  IL_0440:  ldc.i4     0x661
  IL_0445:  sub
  IL_0446:  ldc.i4.2
  IL_0447:  ble.un     IL_075d
  IL_044c:  br         IL_075f
  IL_0451:  ldarg.0
  IL_0452:  ldc.i4     0x6c7
  IL_0457:  bgt        IL_057b
  IL_045c:  ldarg.0
  IL_045d:  ldc.i4     0x67b
  IL_0462:  bgt.s      IL_0481
  IL_0464:  ldarg.0
  IL_0465:  ldc.i4     0x66d
  IL_046a:  beq        IL_075d
  IL_046f:  ldarg.0
  IL_0470:  ldc.i4     0x67a
  IL_0475:  sub
  IL_0476:  ldc.i4.1
  IL_0477:  ble.un     IL_075d
  IL_047c:  br         IL_075f
  IL_0481:  ldarg.0
  IL_0482:  ldc.i4     0x684
  IL_0487:  sub
  IL_0488:  switch    (
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d)
  IL_0541:  ldarg.0
  IL_0542:  ldc.i4     0x6b5
  IL_0547:  sub
  IL_0548:  switch    (
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075f,
        IL_075f,
        IL_075d)
  IL_0569:  ldarg.0
  IL_056a:  ldc.i4     0x6c6
  IL_056f:  sub
  IL_0570:  ldc.i4.1
  IL_0571:  ble.un     IL_075d
  IL_0576:  br         IL_075f
  IL_057b:  ldarg.0
  IL_057c:  ldc.i4     0x787
  IL_0581:  bgt.s      IL_05a9
  IL_0583:  ldarg.0
  IL_0584:  ldc.i4     0x6e2
  IL_0589:  beq        IL_075d
  IL_058e:  ldarg.0
  IL_058f:  ldc.i4     0x6e5
  IL_0594:  beq        IL_075d
  IL_0599:  ldarg.0
  IL_059a:  ldc.i4     0x787
  IL_059f:  beq        IL_075d
  IL_05a4:  br         IL_075f
  IL_05a9:  ldarg.0
  IL_05aa:  ldc.i4     0x7a4
  IL_05af:  sub
  IL_05b0:  ldc.i4.1
  IL_05b1:  ble.un     IL_075d
  IL_05b6:  ldarg.0
  IL_05b7:  ldc.i4     0x7b6
  IL_05bc:  beq        IL_075d
  IL_05c1:  ldarg.0
  IL_05c2:  ldc.i4     0x7bd
  IL_05c7:  beq        IL_075d
  IL_05cc:  br         IL_075f
  IL_05d1:  ldarg.0
  IL_05d2:  ldc.i4     0xfba
  IL_05d7:  bgt        IL_06e3
  IL_05dc:  ldarg.0
  IL_05dd:  ldc.i4     0x7e7
  IL_05e2:  bgt.s      IL_062d
  IL_05e4:  ldarg.0
  IL_05e5:  ldc.i4     0x7d2
  IL_05ea:  bgt.s      IL_0607
  IL_05ec:  ldarg.0
  IL_05ed:  ldc.i4     0x7ce
  IL_05f2:  beq        IL_075d
  IL_05f7:  ldarg.0
  IL_05f8:  ldc.i4     0x7d2
  IL_05fd:  beq        IL_075d
  IL_0602:  br         IL_075f
  IL_0607:  ldarg.0
  IL_0608:  ldc.i4     0x7d8
  IL_060d:  beq        IL_075d
  IL_0612:  ldarg.0
  IL_0613:  ldc.i4     0x7de
  IL_0618:  beq        IL_075d
  IL_061d:  ldarg.0
  IL_061e:  ldc.i4     0x7e7
  IL_0623:  beq        IL_075d
  IL_0628:  br         IL_075f
  IL_062d:  ldarg.0
  IL_062e:  ldc.i4     0x7f6
  IL_0633:  bgt.s      IL_0650
  IL_0635:  ldarg.0
  IL_0636:  ldc.i4     0x7ed
  IL_063b:  beq        IL_075d
  IL_0640:  ldarg.0
  IL_0641:  ldc.i4     0x7f6
  IL_0646:  beq        IL_075d
  IL_064b:  br         IL_075f
  IL_0650:  ldarg.0
  IL_0651:  ldc.i4     0xbb8
  IL_0656:  sub
  IL_0657:  switch    (
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075d,
        IL_075f,
        IL_075d,
        IL_075d)
  IL_06cc:  ldarg.0
  IL_06cd:  ldc.i4     0xfae
  IL_06d2:  beq        IL_075d
  IL_06d7:  ldarg.0
  IL_06d8:  ldc.i4     0xfb8
  IL_06dd:  sub
  IL_06de:  ldc.i4.2
  IL_06df:  ble.un.s   IL_075d
  IL_06e1:  br.s       IL_075f
  IL_06e3:  ldarg.0
  IL_06e4:  ldc.i4     0x1b7b
  IL_06e9:  bgt.s      IL_071f
  IL_06eb:  ldarg.0
  IL_06ec:  ldc.i4     0x1b65
  IL_06f1:  bgt.s      IL_0705
  IL_06f3:  ldarg.0
  IL_06f4:  ldc.i4     0x1388
  IL_06f9:  beq.s      IL_075d
  IL_06fb:  ldarg.0
  IL_06fc:  ldc.i4     0x1b65
  IL_0701:  beq.s      IL_075d
  IL_0703:  br.s       IL_075f
  IL_0705:  ldarg.0
  IL_0706:  ldc.i4     0x1b6e
  IL_070b:  beq.s      IL_075d
  IL_070d:  ldarg.0
  IL_070e:  ldc.i4     0x1b79
  IL_0713:  beq.s      IL_075d
  IL_0715:  ldarg.0
  IL_0716:  ldc.i4     0x1b7b
  IL_071b:  beq.s      IL_075d
  IL_071d:  br.s       IL_075f
  IL_071f:  ldarg.0
  IL_0720:  ldc.i4     0x1f41
  IL_0725:  bgt.s      IL_0743
  IL_0727:  ldarg.0
  IL_0728:  ldc.i4     0x1ba8
  IL_072d:  sub
  IL_072e:  ldc.i4.2
  IL_072f:  ble.un.s   IL_075d
  IL_0731:  ldarg.0
  IL_0732:  ldc.i4     0x1bb2
  IL_0737:  beq.s      IL_075d
  IL_0739:  ldarg.0
  IL_073a:  ldc.i4     0x1f41
  IL_073f:  beq.s      IL_075d
  IL_0741:  br.s       IL_075f
  IL_0743:  ldarg.0
  IL_0744:  ldc.i4     0x1f49
  IL_0749:  beq.s      IL_075d
  IL_074b:  ldarg.0
  IL_074c:  ldc.i4     0x1f4c
  IL_0751:  beq.s      IL_075d
  IL_0753:  ldarg.0
  IL_0754:  ldc.i4     0x2710
  IL_0759:  sub
  IL_075a:  ldc.i4.2
  IL_075b:  bgt.un.s   IL_075f
  IL_075d:  ldc.i4.1
  IL_075e:  ret
  IL_075f:  ldc.i4.0
  IL_0760:  ret
}");
        }

        [Fact]
        public void StringSwitch()
        {
            var text = @"using System;

public class Test
{
    public static void Main()
    {
        Console.WriteLine(M(""Orange""));
    }
    public static int M(string s)
    {
        switch (s)
        {
            case ""Black"": return 0;
            case ""Brown"": return 1;
            case ""Red"": return 2;
            case ""Orange"": return 3;
            case ""Yellow"": return 4;
            case ""Green"": return 5;
            case ""Blue"": return 6;
            case ""Violet"": return 7;
            case ""Grey"": case ""Gray"": return 8;
            case ""White"": return 9;
            default: throw new ArgumentException(s);
        }
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "3");
            compVerifier.VerifyIL("Test.M", @"
{
  // Code size      374 (0x176)
  .maxstack  2
  .locals init (uint V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_016f
  IL_0006:  ldarg.0
  IL_0007:  call       ""ComputeStringHash""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4     0x6ceb2d06
  IL_0013:  bgt.un.s   IL_005b
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4     0x2b043744
  IL_001b:  bgt.un.s   IL_0035
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4     0x2b8b9cf
  IL_0023:  beq        IL_00b8
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4     0x2b043744
  IL_002e:  beq.s      IL_00a3
  IL_0030:  br         IL_016f
  IL_0035:  ldloc.0
  IL_0036:  ldc.i4     0x32bdf8c6
  IL_003b:  beq        IL_012d
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4     0x3ac6ffba
  IL_0046:  beq        IL_013c
  IL_004b:  ldloc.0
  IL_004c:  ldc.i4     0x6ceb2d06
  IL_0051:  beq        IL_014b
  IL_0056:  br         IL_016f
  IL_005b:  ldloc.0
  IL_005c:  ldc.i4     0xa953c75c
  IL_0061:  bgt.un.s   IL_0083
  IL_0063:  ldloc.0
  IL_0064:  ldc.i4     0x727b390b
  IL_0069:  beq.s      IL_00e2
  IL_006b:  ldloc.0
  IL_006c:  ldc.i4     0xa37f187c
  IL_0071:  beq.s      IL_00cd
  IL_0073:  ldloc.0
  IL_0074:  ldc.i4     0xa953c75c
  IL_0079:  beq        IL_0100
  IL_007e:  br         IL_016f
  IL_0083:  ldloc.0
  IL_0084:  ldc.i4     0xd9cdec69
  IL_0089:  beq.s      IL_00f1
  IL_008b:  ldloc.0
  IL_008c:  ldc.i4     0xe9dd1fed
  IL_0091:  beq.s      IL_010f
  IL_0093:  ldloc.0
  IL_0094:  ldc.i4     0xf03bdf12
  IL_0099:  beq        IL_011e
  IL_009e:  br         IL_016f
  IL_00a3:  ldarg.0
  IL_00a4:  ldstr      ""Black""
  IL_00a9:  call       ""bool string.op_Equality(string, string)""
  IL_00ae:  brtrue     IL_015a
  IL_00b3:  br         IL_016f
  IL_00b8:  ldarg.0
  IL_00b9:  ldstr      ""Brown""
  IL_00be:  call       ""bool string.op_Equality(string, string)""
  IL_00c3:  brtrue     IL_015c
  IL_00c8:  br         IL_016f
  IL_00cd:  ldarg.0
  IL_00ce:  ldstr      ""Red""
  IL_00d3:  call       ""bool string.op_Equality(string, string)""
  IL_00d8:  brtrue     IL_015e
  IL_00dd:  br         IL_016f
  IL_00e2:  ldarg.0
  IL_00e3:  ldstr      ""Orange""
  IL_00e8:  call       ""bool string.op_Equality(string, string)""
  IL_00ed:  brtrue.s   IL_0160
  IL_00ef:  br.s       IL_016f
  IL_00f1:  ldarg.0
  IL_00f2:  ldstr      ""Yellow""
  IL_00f7:  call       ""bool string.op_Equality(string, string)""
  IL_00fc:  brtrue.s   IL_0162
  IL_00fe:  br.s       IL_016f
  IL_0100:  ldarg.0
  IL_0101:  ldstr      ""Green""
  IL_0106:  call       ""bool string.op_Equality(string, string)""
  IL_010b:  brtrue.s   IL_0164
  IL_010d:  br.s       IL_016f
  IL_010f:  ldarg.0
  IL_0110:  ldstr      ""Blue""
  IL_0115:  call       ""bool string.op_Equality(string, string)""
  IL_011a:  brtrue.s   IL_0166
  IL_011c:  br.s       IL_016f
  IL_011e:  ldarg.0
  IL_011f:  ldstr      ""Violet""
  IL_0124:  call       ""bool string.op_Equality(string, string)""
  IL_0129:  brtrue.s   IL_0168
  IL_012b:  br.s       IL_016f
  IL_012d:  ldarg.0
  IL_012e:  ldstr      ""Grey""
  IL_0133:  call       ""bool string.op_Equality(string, string)""
  IL_0138:  brtrue.s   IL_016a
  IL_013a:  br.s       IL_016f
  IL_013c:  ldarg.0
  IL_013d:  ldstr      ""Gray""
  IL_0142:  call       ""bool string.op_Equality(string, string)""
  IL_0147:  brtrue.s   IL_016a
  IL_0149:  br.s       IL_016f
  IL_014b:  ldarg.0
  IL_014c:  ldstr      ""White""
  IL_0151:  call       ""bool string.op_Equality(string, string)""
  IL_0156:  brtrue.s   IL_016c
  IL_0158:  br.s       IL_016f
  IL_015a:  ldc.i4.0
  IL_015b:  ret
  IL_015c:  ldc.i4.1
  IL_015d:  ret
  IL_015e:  ldc.i4.2
  IL_015f:  ret
  IL_0160:  ldc.i4.3
  IL_0161:  ret
  IL_0162:  ldc.i4.4
  IL_0163:  ret
  IL_0164:  ldc.i4.5
  IL_0165:  ret
  IL_0166:  ldc.i4.6
  IL_0167:  ret
  IL_0168:  ldc.i4.7
  IL_0169:  ret
  IL_016a:  ldc.i4.8
  IL_016b:  ret
  IL_016c:  ldc.i4.s   9
  IL_016e:  ret
  IL_016f:  ldarg.0
  IL_0170:  newobj     ""System.ArgumentException..ctor(string)""
  IL_0175:  throw
}");
        }

        #endregion

        # region "Data flow analysis tests"

        [Fact]
        public void DefiniteAssignmentOnAllControlPaths()
        {
            var text = @"using System;
class SwitchTest
{
    public static int Main()
    {
        int n = 3;
        int goo;        // unassigned goo

        switch (n)
        {
            case 1:
            case 2:
                goo = n;
                break;
            case 3:
            default:
                goo = 0;
                break;
        }
        
        Console.Write(goo);     // goo must be definitely assigned here        
        return goo;
    }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("SwitchTest.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (int V_0, //n
                int V_1) //goo
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  ldc.i4.1
  IL_0006:  ble.un.s   IL_000e
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.3
  IL_000a:  beq.s      IL_0012
  IL_000c:  br.s       IL_0012
  IL_000e:  ldloc.0
  IL_000f:  stloc.1
  IL_0010:  br.s       IL_0014
  IL_0012:  ldc.i4.0
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldloc.1
  IL_001b:  ret
}"
            );
        }

        [Fact]
        public void ComplexControlFlow_DefiniteAssignmentOnAllControlPaths()
        {
            var text = @"using System;
class SwitchTest
{
    public static int Main()
    {
        int n = 3;
        int cost = 0;
        int goo;        // unassigned goo

        switch (n)
        {
            case 1:
                cost = 1;
                goo = n;
                break;
            case 2:
                cost = 2;
                goto case 1;
            case 3:
                cost = 3;
                if(cost > n)
                {
                    goo = n - 1;
                }
                else
                {
                    goto case 2;
                }
                break;

            default:
                cost = 4;
                goo = n - 1;
                break;
        }

        if (goo != n)       // goo must be reported as definitely assigned
        {
            Console.Write(goo);
        }
        else
        {
            --cost;
        }

        Console.Write(cost);    // should output 0

        return cost;
    }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("SwitchTest.Main",
@"{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (int V_0, //n
                int V_1, //cost
                int V_2) //goo
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  sub
  IL_0007:  switch    (
        IL_001a,
        IL_0020,
        IL_0024)
  IL_0018:  br.s       IL_0030
  IL_001a:  ldc.i4.1
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  stloc.2
  IL_001e:  br.s       IL_0036
  IL_0020:  ldc.i4.2
  IL_0021:  stloc.1
  IL_0022:  br.s       IL_001a
  IL_0024:  ldc.i4.3
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldloc.0
  IL_0028:  ble.s      IL_0020
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  sub
  IL_002d:  stloc.2
  IL_002e:  br.s       IL_0036
  IL_0030:  ldc.i4.4
  IL_0031:  stloc.1
  IL_0032:  ldloc.0
  IL_0033:  ldc.i4.1
  IL_0034:  sub
  IL_0035:  stloc.2
  IL_0036:  ldloc.2
  IL_0037:  ldloc.0
  IL_0038:  beq.s      IL_0042
  IL_003a:  ldloc.2
  IL_003b:  call       ""void System.Console.Write(int)""
  IL_0040:  br.s       IL_0046
  IL_0042:  ldloc.1
  IL_0043:  ldc.i4.1
  IL_0044:  sub
  IL_0045:  stloc.1
  IL_0046:  ldloc.1
  IL_0047:  call       ""void System.Console.Write(int)""
  IL_004c:  ldloc.1
  IL_004d:  ret
}"
            );
        }

        [Fact]
        public void ComplexControlFlow_NoAssignmentOnlyOnUnreachableControlPaths()
        {
            var text = @"using System;
class SwitchTest
{
    public static int Main()
    {
        int goo;        // unassigned goo
        switch (3)
        {
            case 1:
              mylabel:
                try
                {                    
                    throw new System.ApplicationException();
                }
                catch(Exception)
                {
                    goo = 0;
                }
                break;
            case 2:
                goto mylabel;
            case 3:
                if (true)
                {
                    goto case 2;
                }
                break;
            case 4:
                break;
        }

        Console.Write(goo);    // goo should be definitely assigned here
        return goo;
    }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
            // (27,17): warning CS0162: Unreachable code detected
            //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break"),
            // (29,17): warning CS0162: Unreachable code detected
            //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break"));

            compVerifier.VerifyIL("SwitchTest.Main", @"
                {
                    // Code size       20 (0x14)
                    .maxstack  1
                    .locals init (int V_0) //goo
                    IL_0000:  nop
                    .try
                    {
                    IL_0001:  newobj     ""System.ApplicationException..ctor()""
                    IL_0006:  throw
                    }
                    catch System.Exception
                    {
                    IL_0007:  pop
                    IL_0008:  ldc.i4.0
                    IL_0009:  stloc.0
                    IL_000a:  leave.s    IL_000c
                    }
                    IL_000c:  ldloc.0
                    IL_000d:  call       ""void System.Console.Write(int)""
                    IL_0012:  ldloc.0
                    IL_0013:  ret
                }"
            );
        }

        #endregion

        #region "Control flow analysis and warning tests"

        [Fact]
        public void CS0469_NoImplicitConversionWarning()
        {
            var text = @"using System;

class A
{
    static void Goo(DayOfWeek x)
    {
        switch (x)
        {
            case DayOfWeek.Monday:
                goto case 1; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'System.DayOfWeek'
        }
    }

    static void Main() {}
}
";

            var compVerifier = CompileAndVerify(text);

            compVerifier.VerifyDiagnostics(
            // (10,17): warning CS0469: The 'goto case' value is not implicitly convertible to type 'System.DayOfWeek'
            //                 goto case 1; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'System.DayOfWeek'
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 1;").WithArguments("System.DayOfWeek"));

            compVerifier.VerifyIL("A.Goo", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  bne.un.s   IL_0006
  IL_0004:  br.s       IL_0004
  IL_0006:  ret
}"
            );
        }

        [Fact]
        public void CS0162_UnreachableCodeInSwitchCase_01()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 2;
        switch (true)
        {
            case true:
                ret = 0;
                break;
            case false:        // unreachable case label
                ret = 1;
                break;
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
            // (14,8): warning CS0162: Unreachable code detected
            // 			    ret = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ret"));

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  call       ""void System.Console.Write(int)""
  IL_000a:  ldloc.0
  IL_000b:  ret
}
"
            );
        }

        [Fact]
        public void CS0162_UnreachableCodeInSwitchCase_02()
        {
            var text = @"using System;

public class Test
{
    public static int Main(string [] args)
    {
        int ret = 1;
        switch (true)
        {
            default:        // unreachable default label
                ret = 1;
                break;
            case true: 
                ret = 0;
                break;
        }

        Console.Write(ret);
        return(ret);
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
                // (11,17): warning CS0162: Unreachable code detected
                //                 ret = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ret").WithLocation(11, 17)
                );

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0) //ret
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  call       ""void System.Console.Write(int)""
  IL_000a:  ldloc.0
  IL_000b:  ret
}"
            );
        }

        [Fact]
        public void CS0162_UnreachableCodeInSwitchCase_03()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {    
    int ret = M();
    Console.Write(ret);
    return(ret);
  }

  public static int M()
  {
    switch (1)
    {
      case 1:
        return 0;
    }
    return 1;       // unreachable code
  }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
            // (19,5): warning CS0162: Unreachable code detected
            //     return 1;       // unreachable code
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(19, 5));

            compVerifier.VerifyIL("Test.M", @"
                {
                    // Code size        2 (0x2)
                    .maxstack  1
                    IL_0000:  ldc.i4.0
                    IL_0001:  ret
                }"
            );
        }

        [Fact]
        public void CS0162_UnreachableCodeInSwitchCase_04()
        {
            var text = @"using System;

public class Test
{
  public static int Main(string [] args)
  {
    int ret = 0;
    switch (1)             // no matching case/default label
    {
      case 2:              // unreachable code
        ret = 1;
        break;
    }

    Console.Write(ret);
    return(ret);
  }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
                // (11,9): warning CS0162: Unreachable code detected
                //         ret = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ret").WithLocation(11, 9)
                );

            compVerifier.VerifyIL("Test.Main", @"
                {
                    // Code size       10 (0xa)
                    .maxstack  1
                    .locals init (int V_0) //ret
                    IL_0000:  ldc.i4.0
                    IL_0001:  stloc.0
                    IL_0002:  ldloc.0
                    IL_0003:  call       ""void System.Console.Write(int)""
                    IL_0008:  ldloc.0
                    IL_0009:  ret
                }"
            );
        }

        [Fact]
        public void CS1522_EmptySwitch()
        {
            var text = @"using System;
public class Test
{
    public static int Main(string [] args)
    {
        int ret = 0;
        switch (true) {

        }

        Console.Write(ret);
        return(0);
    }
}";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");

            compVerifier.VerifyDiagnostics(
                // (7,23): warning CS1522: Empty switch block
                //         switch (true) {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(7, 23)
                );

            compVerifier.VerifyIL("Test.Main", @"
                {
                  // Code size        8 (0x8)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  call       ""void System.Console.Write(int)""
                  IL_0006:  ldc.i4.0
                  IL_0007:  ret
                }"
            );
        }

        [WorkItem(913556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/913556")]
        [Fact]
        public void DifferentStrategiesForDifferentSwitches()
        {
            var text = @"
using System;

public class Test
{
    public static void Main(string [] args)
    {
        switch(args[0])
        {
            case ""A"": Console.Write(1); break;
        }

        switch(args[1])
        {
            case ""B"": Console.Write(2); break;
            case ""C"": Console.Write(3); break;
            case ""D"": Console.Write(4); break;
            case ""E"": Console.Write(5); break;
            case ""F"": Console.Write(6); break;
            case ""G"": Console.Write(7); break;
            case ""H"": Console.Write(8); break;
            case ""I"": Console.Write(9); break;
            case ""J"": Console.Write(10); break;
        }
    }
}";

            var comp = CreateCompilation(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size      337 (0x151)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  brfalse.s  IL_001a
  IL_0007:  ldloc.0
  IL_0008:  ldstr      ""A""
  IL_000d:  call       ""bool string.op_Equality(string, string)""
  IL_0012:  brfalse.s  IL_001a
  IL_0014:  ldc.i4.1
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.1
  IL_001c:  ldelem.ref
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  brfalse    IL_0150
  IL_0024:  ldloc.0
  IL_0025:  call       ""ComputeStringHash""
  IL_002a:  stloc.1
  IL_002b:  ldloc.1
  IL_002c:  ldc.i4     0xc30bf539
  IL_0031:  bgt.un.s   IL_0060
  IL_0033:  ldloc.1
  IL_0034:  ldc.i4     0xc10bf213
  IL_0039:  bgt.un.s   IL_004c
  IL_003b:  ldloc.1
  IL_003c:  ldc.i4     0xc00bf080
  IL_0041:  beq.s      IL_00bc
  IL_0043:  ldloc.1
  IL_0044:  ldc.i4     0xc10bf213
  IL_0049:  beq.s      IL_00ae
  IL_004b:  ret
  IL_004c:  ldloc.1
  IL_004d:  ldc.i4     0xc20bf3a6
  IL_0052:  beq        IL_00d8
  IL_0057:  ldloc.1
  IL_0058:  ldc.i4     0xc30bf539
  IL_005d:  beq.s      IL_00ca
  IL_005f:  ret
  IL_0060:  ldloc.1
  IL_0061:  ldc.i4     0xc70bfb85
  IL_0066:  bgt.un.s   IL_0079
  IL_0068:  ldloc.1
  IL_0069:  ldc.i4     0xc60bf9f2
  IL_006e:  beq.s      IL_00a0
  IL_0070:  ldloc.1
  IL_0071:  ldc.i4     0xc70bfb85
  IL_0076:  beq.s      IL_0092
  IL_0078:  ret
  IL_0079:  ldloc.1
  IL_007a:  ldc.i4     0xcc0c0364
  IL_007f:  beq.s      IL_00f4
  IL_0081:  ldloc.1
  IL_0082:  ldc.i4     0xcd0c04f7
  IL_0087:  beq.s      IL_00e6
  IL_0089:  ldloc.1
  IL_008a:  ldc.i4     0xcf0c081d
  IL_008f:  beq.s      IL_0102
  IL_0091:  ret
  IL_0092:  ldloc.0
  IL_0093:  ldstr      ""B""
  IL_0098:  call       ""bool string.op_Equality(string, string)""
  IL_009d:  brtrue.s   IL_0110
  IL_009f:  ret
  IL_00a0:  ldloc.0
  IL_00a1:  ldstr      ""C""
  IL_00a6:  call       ""bool string.op_Equality(string, string)""
  IL_00ab:  brtrue.s   IL_0117
  IL_00ad:  ret
  IL_00ae:  ldloc.0
  IL_00af:  ldstr      ""D""
  IL_00b4:  call       ""bool string.op_Equality(string, string)""
  IL_00b9:  brtrue.s   IL_011e
  IL_00bb:  ret
  IL_00bc:  ldloc.0
  IL_00bd:  ldstr      ""E""
  IL_00c2:  call       ""bool string.op_Equality(string, string)""
  IL_00c7:  brtrue.s   IL_0125
  IL_00c9:  ret
  IL_00ca:  ldloc.0
  IL_00cb:  ldstr      ""F""
  IL_00d0:  call       ""bool string.op_Equality(string, string)""
  IL_00d5:  brtrue.s   IL_012c
  IL_00d7:  ret
  IL_00d8:  ldloc.0
  IL_00d9:  ldstr      ""G""
  IL_00de:  call       ""bool string.op_Equality(string, string)""
  IL_00e3:  brtrue.s   IL_0133
  IL_00e5:  ret
  IL_00e6:  ldloc.0
  IL_00e7:  ldstr      ""H""
  IL_00ec:  call       ""bool string.op_Equality(string, string)""
  IL_00f1:  brtrue.s   IL_013a
  IL_00f3:  ret
  IL_00f4:  ldloc.0
  IL_00f5:  ldstr      ""I""
  IL_00fa:  call       ""bool string.op_Equality(string, string)""
  IL_00ff:  brtrue.s   IL_0141
  IL_0101:  ret
  IL_0102:  ldloc.0
  IL_0103:  ldstr      ""J""
  IL_0108:  call       ""bool string.op_Equality(string, string)""
  IL_010d:  brtrue.s   IL_0149
  IL_010f:  ret
  IL_0110:  ldc.i4.2
  IL_0111:  call       ""void System.Console.Write(int)""
  IL_0116:  ret
  IL_0117:  ldc.i4.3
  IL_0118:  call       ""void System.Console.Write(int)""
  IL_011d:  ret
  IL_011e:  ldc.i4.4
  IL_011f:  call       ""void System.Console.Write(int)""
  IL_0124:  ret
  IL_0125:  ldc.i4.5
  IL_0126:  call       ""void System.Console.Write(int)""
  IL_012b:  ret
  IL_012c:  ldc.i4.6
  IL_012d:  call       ""void System.Console.Write(int)""
  IL_0132:  ret
  IL_0133:  ldc.i4.7
  IL_0134:  call       ""void System.Console.Write(int)""
  IL_0139:  ret
  IL_013a:  ldc.i4.8
  IL_013b:  call       ""void System.Console.Write(int)""
  IL_0140:  ret
  IL_0141:  ldc.i4.s   9
  IL_0143:  call       ""void System.Console.Write(int)""
  IL_0148:  ret
  IL_0149:  ldc.i4.s   10
  IL_014b:  call       ""void System.Console.Write(int)""
  IL_0150:  ret
}");
        }

        [WorkItem(634404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634404")]
        [WorkItem(913556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/913556")]
        [Fact]
        public void LargeStringSwitchWithoutStringChars()
        {
            var text = @"
using System;

public class Test
{
    public static void Main(string [] args)
    {
        switch(args[0])
        {
            case ""A"": Console.Write(1); break;
            case ""B"": Console.Write(2); break;
            case ""C"": Console.Write(3); break;
            case ""D"": Console.Write(4); break;
            case ""E"": Console.Write(5); break;
            case ""F"": Console.Write(6); break;
            case ""G"": Console.Write(7); break;
            case ""H"": Console.Write(8); break;
            case ""I"": Console.Write(9); break;
        }
    }
}";

            var comp = CreateCompilation(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            // With special members available, we use a hashtable approach.
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size      313 (0x139)
  .maxstack  2
  .locals init (string V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  brfalse    IL_0138
  IL_000a:  ldloc.0
  IL_000b:  call       ""ComputeStringHash""
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4     0xc30bf539
  IL_0017:  bgt.un.s   IL_0049
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4     0xc10bf213
  IL_001f:  bgt.un.s   IL_0035
  IL_0021:  ldloc.1
  IL_0022:  ldc.i4     0xc00bf080
  IL_0027:  beq        IL_00b3
  IL_002c:  ldloc.1
  IL_002d:  ldc.i4     0xc10bf213
  IL_0032:  beq.s      IL_00a5
  IL_0034:  ret
  IL_0035:  ldloc.1
  IL_0036:  ldc.i4     0xc20bf3a6
  IL_003b:  beq        IL_00cf
  IL_0040:  ldloc.1
  IL_0041:  ldc.i4     0xc30bf539
  IL_0046:  beq.s      IL_00c1
  IL_0048:  ret
  IL_0049:  ldloc.1
  IL_004a:  ldc.i4     0xc60bf9f2
  IL_004f:  bgt.un.s   IL_0062
  IL_0051:  ldloc.1
  IL_0052:  ldc.i4     0xc40bf6cc
  IL_0057:  beq.s      IL_007b
  IL_0059:  ldloc.1
  IL_005a:  ldc.i4     0xc60bf9f2
  IL_005f:  beq.s      IL_0097
  IL_0061:  ret
  IL_0062:  ldloc.1
  IL_0063:  ldc.i4     0xc70bfb85
  IL_0068:  beq.s      IL_0089
  IL_006a:  ldloc.1
  IL_006b:  ldc.i4     0xcc0c0364
  IL_0070:  beq.s      IL_00eb
  IL_0072:  ldloc.1
  IL_0073:  ldc.i4     0xcd0c04f7
  IL_0078:  beq.s      IL_00dd
  IL_007a:  ret
  IL_007b:  ldloc.0
  IL_007c:  ldstr      ""A""
  IL_0081:  call       ""bool string.op_Equality(string, string)""
  IL_0086:  brtrue.s   IL_00f9
  IL_0088:  ret
  IL_0089:  ldloc.0
  IL_008a:  ldstr      ""B""
  IL_008f:  call       ""bool string.op_Equality(string, string)""
  IL_0094:  brtrue.s   IL_0100
  IL_0096:  ret
  IL_0097:  ldloc.0
  IL_0098:  ldstr      ""C""
  IL_009d:  call       ""bool string.op_Equality(string, string)""
  IL_00a2:  brtrue.s   IL_0107
  IL_00a4:  ret
  IL_00a5:  ldloc.0
  IL_00a6:  ldstr      ""D""
  IL_00ab:  call       ""bool string.op_Equality(string, string)""
  IL_00b0:  brtrue.s   IL_010e
  IL_00b2:  ret
  IL_00b3:  ldloc.0
  IL_00b4:  ldstr      ""E""
  IL_00b9:  call       ""bool string.op_Equality(string, string)""
  IL_00be:  brtrue.s   IL_0115
  IL_00c0:  ret
  IL_00c1:  ldloc.0
  IL_00c2:  ldstr      ""F""
  IL_00c7:  call       ""bool string.op_Equality(string, string)""
  IL_00cc:  brtrue.s   IL_011c
  IL_00ce:  ret
  IL_00cf:  ldloc.0
  IL_00d0:  ldstr      ""G""
  IL_00d5:  call       ""bool string.op_Equality(string, string)""
  IL_00da:  brtrue.s   IL_0123
  IL_00dc:  ret
  IL_00dd:  ldloc.0
  IL_00de:  ldstr      ""H""
  IL_00e3:  call       ""bool string.op_Equality(string, string)""
  IL_00e8:  brtrue.s   IL_012a
  IL_00ea:  ret
  IL_00eb:  ldloc.0
  IL_00ec:  ldstr      ""I""
  IL_00f1:  call       ""bool string.op_Equality(string, string)""
  IL_00f6:  brtrue.s   IL_0131
  IL_00f8:  ret
  IL_00f9:  ldc.i4.1
  IL_00fa:  call       ""void System.Console.Write(int)""
  IL_00ff:  ret
  IL_0100:  ldc.i4.2
  IL_0101:  call       ""void System.Console.Write(int)""
  IL_0106:  ret
  IL_0107:  ldc.i4.3
  IL_0108:  call       ""void System.Console.Write(int)""
  IL_010d:  ret
  IL_010e:  ldc.i4.4
  IL_010f:  call       ""void System.Console.Write(int)""
  IL_0114:  ret
  IL_0115:  ldc.i4.5
  IL_0116:  call       ""void System.Console.Write(int)""
  IL_011b:  ret
  IL_011c:  ldc.i4.6
  IL_011d:  call       ""void System.Console.Write(int)""
  IL_0122:  ret
  IL_0123:  ldc.i4.7
  IL_0124:  call       ""void System.Console.Write(int)""
  IL_0129:  ret
  IL_012a:  ldc.i4.8
  IL_012b:  call       ""void System.Console.Write(int)""
  IL_0130:  ret
  IL_0131:  ldc.i4.s   9
  IL_0133:  call       ""void System.Console.Write(int)""
  IL_0138:  ret
}");

            comp = CreateCompilation(text);
            comp.MakeMemberMissing(SpecialMember.System_String__Chars);

            // Can't use the hash version when String.Chars is unavailable.
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size      192 (0xc0)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  brfalse    IL_00bf
  IL_000a:  ldloc.0
  IL_000b:  ldstr      ""A""
  IL_0010:  call       ""bool string.op_Equality(string, string)""
  IL_0015:  brtrue.s   IL_0080
  IL_0017:  ldloc.0
  IL_0018:  ldstr      ""B""
  IL_001d:  call       ""bool string.op_Equality(string, string)""
  IL_0022:  brtrue.s   IL_0087
  IL_0024:  ldloc.0
  IL_0025:  ldstr      ""C""
  IL_002a:  call       ""bool string.op_Equality(string, string)""
  IL_002f:  brtrue.s   IL_008e
  IL_0031:  ldloc.0
  IL_0032:  ldstr      ""D""
  IL_0037:  call       ""bool string.op_Equality(string, string)""
  IL_003c:  brtrue.s   IL_0095
  IL_003e:  ldloc.0
  IL_003f:  ldstr      ""E""
  IL_0044:  call       ""bool string.op_Equality(string, string)""
  IL_0049:  brtrue.s   IL_009c
  IL_004b:  ldloc.0
  IL_004c:  ldstr      ""F""
  IL_0051:  call       ""bool string.op_Equality(string, string)""
  IL_0056:  brtrue.s   IL_00a3
  IL_0058:  ldloc.0
  IL_0059:  ldstr      ""G""
  IL_005e:  call       ""bool string.op_Equality(string, string)""
  IL_0063:  brtrue.s   IL_00aa
  IL_0065:  ldloc.0
  IL_0066:  ldstr      ""H""
  IL_006b:  call       ""bool string.op_Equality(string, string)""
  IL_0070:  brtrue.s   IL_00b1
  IL_0072:  ldloc.0
  IL_0073:  ldstr      ""I""
  IL_0078:  call       ""bool string.op_Equality(string, string)""
  IL_007d:  brtrue.s   IL_00b8
  IL_007f:  ret
  IL_0080:  ldc.i4.1
  IL_0081:  call       ""void System.Console.Write(int)""
  IL_0086:  ret
  IL_0087:  ldc.i4.2
  IL_0088:  call       ""void System.Console.Write(int)""
  IL_008d:  ret
  IL_008e:  ldc.i4.3
  IL_008f:  call       ""void System.Console.Write(int)""
  IL_0094:  ret
  IL_0095:  ldc.i4.4
  IL_0096:  call       ""void System.Console.Write(int)""
  IL_009b:  ret
  IL_009c:  ldc.i4.5
  IL_009d:  call       ""void System.Console.Write(int)""
  IL_00a2:  ret
  IL_00a3:  ldc.i4.6
  IL_00a4:  call       ""void System.Console.Write(int)""
  IL_00a9:  ret
  IL_00aa:  ldc.i4.7
  IL_00ab:  call       ""void System.Console.Write(int)""
  IL_00b0:  ret
  IL_00b1:  ldc.i4.8
  IL_00b2:  call       ""void System.Console.Write(int)""
  IL_00b7:  ret
  IL_00b8:  ldc.i4.s   9
  IL_00ba:  call       ""void System.Console.Write(int)""
  IL_00bf:  ret
}");
        }

        [WorkItem(947580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947580")]
        [Fact]
        public void Regress947580()
        {
            var text = @"
using System;

class Program {
    static string boo(int i) {
        switch (i) {
            case 42:
                var x = ""goo"";
                if (x != ""bar"")
                break;
            return x;
        }
        return null;
    }
    static void Main()
    {
        boo(42);
    }
}

";
            var compVerifier = CompileAndVerify(text, expectedOutput: "");
            compVerifier.VerifyIL("Program.boo",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //x
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  bne.un.s   IL_001a
  IL_0005:  ldstr      ""goo""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldstr      ""bar""
  IL_0011:  call       ""bool string.op_Inequality(string, string)""
  IL_0016:  brtrue.s   IL_001a
  IL_0018:  ldloc.0
  IL_0019:  ret
  IL_001a:  ldnull
  IL_001b:  ret
}"
            );
        }

        [WorkItem(947580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947580")]
        [Fact]
        public void Regress947580a()
        {
            var text = @"
using System;

class Program {
    static string boo(int i) {
        switch (i) {
            case 42:
                var x = ""goo"";
                if (x != ""bar"")
                break;
             break;
        }
        return null;
    }
    static void Main()
    {
        boo(42);
    }
}

";
            var compVerifier = CompileAndVerify(text, expectedOutput: "");
            compVerifier.VerifyIL("Program.boo",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  bne.un.s   IL_0015
  IL_0005:  ldstr      ""goo""
  IL_000a:  ldstr      ""bar""
  IL_000f:  call       ""bool string.op_Inequality(string, string)""
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ret
}"
            );
        }

        [WorkItem(1035228, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1035228")]
        [Fact]
        public void Regress1035228()
        {
            var text = @"
using System;

class Program {
    static bool boo(int i) {
        var ii = i;
        switch (++ii) {
            case 42:
                var x = ""goo"";
                if (x != ""bar"")
                {
                    return false;
                }
             break;
        }
        return true;
    }
    static void Main()
    {
        boo(42);
    }
}

";
            var compVerifier = CompileAndVerify(text, expectedOutput: "");
            compVerifier.VerifyIL("Program.boo",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ldc.i4.s   42
  IL_0005:  bne.un.s   IL_001a
  IL_0007:  ldstr      ""goo""
  IL_000c:  ldstr      ""bar""
  IL_0011:  call       ""bool string.op_Inequality(string, string)""
  IL_0016:  brfalse.s  IL_001a
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldc.i4.1
  IL_001b:  ret
}"
            );
        }

        [WorkItem(1035228, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1035228")]
        [Fact]
        public void Regress1035228a()
        {
            var text = @"
using System;

class Program {
    static bool boo(int i) {
        var ii = i;
        switch (ii++) {
            case 42:
                var x = ""goo"";
                if (x != ""bar"")
                {
                    return false;
                }
             break;
        }
        return true;
    }
    static void Main()
    {
        boo(42);
    }
}

";
            var compVerifier = CompileAndVerify(text, expectedOutput: "");
            compVerifier.VerifyIL("Program.boo",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  bne.un.s   IL_0018
  IL_0005:  ldstr      ""goo""
  IL_000a:  ldstr      ""bar""
  IL_000f:  call       ""bool string.op_Inequality(string, string)""
  IL_0014:  brfalse.s  IL_0018
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldc.i4.1
  IL_0019:  ret
}"
            );
        }

        [WorkItem(4701, "https://github.com/dotnet/roslyn/issues/4701")]
        [Fact]
        public void Regress4701()
        {
            var text = @"
using System;

namespace ConsoleApplication1
{
    class Program
    {
        private void SwtchTest()
        {
            int? i;

            i = 1;
            switch (i)
            {
                case null:
                    Console.WriteLine(""In Null case"");
                    i = 1;
                    break;
                default:
                    Console.WriteLine(""In DEFAULT case"");
                    i = i + 2;
                    break;
        }
    }

    static void Main(string[] args)
    {
        var p = new Program();
        p.SwtchTest();
    }
}
}

";
            var compVerifier = CompileAndVerify(text, expectedOutput: "In DEFAULT case");
            compVerifier.VerifyIL("ConsoleApplication1.Program.SwtchTest",
@"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (int? V_0, //i
                int? V_1,
                int? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""int?..ctor(int)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool int?.HasValue.get""
  IL_000f:  brtrue.s   IL_0024
  IL_0011:  ldstr      ""In Null case""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ldloca.s   V_0
  IL_001d:  ldc.i4.1
  IL_001e:  call       ""int?..ctor(int)""
  IL_0023:  ret
  IL_0024:  ldstr      ""In DEFAULT case""
  IL_0029:  call       ""void System.Console.WriteLine(string)""
  IL_002e:  ldloc.0
  IL_002f:  stloc.1
  IL_0030:  ldloca.s   V_1
  IL_0032:  call       ""bool int?.HasValue.get""
  IL_0037:  brtrue.s   IL_0044
  IL_0039:  ldloca.s   V_2
  IL_003b:  initobj    ""int?""
  IL_0041:  ldloc.2
  IL_0042:  br.s       IL_0052
  IL_0044:  ldloca.s   V_1
  IL_0046:  call       ""int int?.GetValueOrDefault()""
  IL_004b:  ldc.i4.2
  IL_004c:  add
  IL_004d:  newobj     ""int?..ctor(int)""
  IL_0052:  stloc.0
  IL_0053:  ret
}"
            );
        }

        [WorkItem(4701, "https://github.com/dotnet/roslyn/issues/4701")]
        [Fact]
        public void Regress4701a()
        {
            var text = @"
using System;

namespace ConsoleApplication1
{
    class Program
    {
        private void SwtchTest()
        {
            string i = null;

            i = ""1"";
            switch (i)
            {
                case null:
                    Console.WriteLine(""In Null case"");
                    break;
                default:
                    Console.WriteLine(""In DEFAULT case"");
                    break;
        }
    }

    static void Main(string[] args)
    {
        var p = new Program();
        p.SwtchTest();
    }
}
}

";
            var compVerifier = CompileAndVerify(text, expectedOutput: "In DEFAULT case");
            compVerifier.VerifyIL("ConsoleApplication1.Program.SwtchTest",
@"
{
  // Code size       29 (0x1d)
  .maxstack  1
  IL_0000:  ldstr      ""1""
  IL_0005:  brtrue.s   IL_0012
  IL_0007:  ldstr      ""In Null case""
  IL_000c:  call       ""void System.Console.WriteLine(string)""
  IL_0011:  ret
  IL_0012:  ldstr      ""In DEFAULT case""
  IL_0017:  call       ""void System.Console.WriteLine(string)""
  IL_001c:  ret
}"
            );
        }

        #endregion

        #region regression tests

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void BoxInPatternSwitch_01()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        switch (StringSplitOptions.RemoveEmptyEntries)
        {
            case object o:
                Console.WriteLine(o);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "RemoveEmptyEntries");
            compVerifier.VerifyIL("Program.Main",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""System.StringSplitOptions""
  IL_0006:  call       ""void System.Console.WriteLine(object)""
  IL_000b:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "RemoveEmptyEntries");
            compVerifier.VerifyIL("Program.Main",
@"{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (object V_0, //o
                System.StringSplitOptions V_1,
                System.StringSplitOptions V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.2
  IL_0003:  ldc.i4.1
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  box        ""System.StringSplitOptions""
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  br.s       IL_0010
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.WriteLine(object)""
  IL_0016:  nop
  IL_0017:  br.s       IL_0019
  IL_0019:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void ExplicitNullablePatternSwitch_02()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M(null);
        M(1);
    }
    public static void M(int? x)
    {
        switch (x)
        {
            case int i: // explicit nullable conversion
                Console.Write(i);
                break;
            case null:
                Console.Write(""null"");
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "null1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       33 (0x21)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brfalse.s  IL_0016
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""int int?.GetValueOrDefault()""
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  ret
  IL_0016:  ldstr      ""null""
  IL_001b:  call       ""void System.Console.Write(string)""
  IL_0020:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "null1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       49 (0x31)
  .maxstack  1
  .locals init (int V_0, //i
                int? V_1,
                int? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_1
  IL_0007:  call       ""bool int?.HasValue.get""
  IL_000c:  brfalse.s  IL_0023
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       ""int int?.GetValueOrDefault()""
  IL_0015:  stloc.0
  IL_0016:  br.s       IL_0018
  IL_0018:  br.s       IL_001a
  IL_001a:  ldloc.0
  IL_001b:  call       ""void System.Console.Write(int)""
  IL_0020:  nop
  IL_0021:  br.s       IL_0030
  IL_0023:  ldstr      ""null""
  IL_0028:  call       ""void System.Console.Write(string)""
  IL_002d:  nop
  IL_002e:  br.s       IL_0030
  IL_0030:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void BoxInPatternSwitch_04()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M(1);
    }
    public static void M(int x)
    {
        switch (x)
        {
            case System.IComparable i:
                Console.Write(i);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""int""
  IL_0006:  call       ""void System.Console.Write(object)""
  IL_000b:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (System.IComparable V_0, //i
                int V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  box        ""int""
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  br.s       IL_0010
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.Write(object)""
  IL_0016:  nop
  IL_0017:  br.s       IL_0019
  IL_0019:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void BoxInPatternSwitch_05()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M(1);
        M(null);
    }
    public static void M(int? x)
    {
        switch (x)
        {
            case System.IComparable i:
                Console.Write(i);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.IComparable V_0) //i
  IL_0000:  ldarg.0
  IL_0001:  box        ""int?""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  call       ""void System.Console.Write(object)""
  IL_0010:  ret
}
"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (System.IComparable V_0, //i
                int? V_1,
                int? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  box        ""int?""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0011
  IL_000f:  br.s       IL_001c
  IL_0011:  br.s       IL_0013
  IL_0013:  ldloc.0
  IL_0014:  call       ""void System.Console.Write(object)""
  IL_0019:  nop
  IL_001a:  br.s       IL_001c
  IL_001c:  ret
}
"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void UnboxInPatternSwitch_06()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M(1);
        M(null);
        M(nameof(Main));
    }
    public static void M(object x)
    {
        switch (x)
        {
            case int i:
                Console.Write(i);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""int""
  IL_0006:  brfalse.s  IL_0013
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""int""
  IL_000e:  call       ""void System.Console.Write(int)""
  IL_0013:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (int V_0, //i
                object V_1,
                object V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  isinst     ""int""
  IL_000b:  brfalse.s  IL_0021
  IL_000d:  ldloc.1
  IL_000e:  unbox.any  ""int""
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0016
  IL_0016:  br.s       IL_0018
  IL_0018:  ldloc.0
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  nop
  IL_001f:  br.s       IL_0021
  IL_0021:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void UnboxInPatternSwitch_07()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M<int>(1);
        M<int>(null);
        M<int>(10.5);
    }
    public static void M<T>(object x)
    {
        // when T is not known to be a reference type, there is an unboxing conversion from
        // the effective base class C of T to T and from any base class of C to T.
        switch (x)
        {
            case T i:
                Console.Write(i);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""T""
  IL_0006:  brfalse.s  IL_0018
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""T""
  IL_000e:  box        ""T""
  IL_0013:  call       ""void System.Console.Write(object)""
  IL_0018:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M<T>",
@"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (T V_0, //i
                object V_1,
                object V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  isinst     ""T""
  IL_000b:  brfalse.s  IL_0026
  IL_000d:  ldloc.1
  IL_000e:  unbox.any  ""T""
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0016
  IL_0016:  br.s       IL_0018
  IL_0018:  ldloc.0
  IL_0019:  box        ""T""
  IL_001e:  call       ""void System.Console.Write(object)""
  IL_0023:  nop
  IL_0024:  br.s       IL_0026
  IL_0026:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void UnboxInPatternSwitch_08()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M<int>(1);
        M<int>(null);
        M<int>(10.5);
    }
    public static void M<T>(IComparable x)
    {
        // when T is not known to be a reference type, there is an unboxing conversion from
        // any interface type to T.
        switch (x)
        {
            case T i:
                Console.Write(i);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""T""
  IL_0006:  brfalse.s  IL_0018
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""T""
  IL_000e:  box        ""T""
  IL_0013:  call       ""void System.Console.Write(object)""
  IL_0018:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M<T>",
@"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (T V_0, //i
                System.IComparable V_1,
                System.IComparable V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  isinst     ""T""
  IL_000b:  brfalse.s  IL_0026
  IL_000d:  ldloc.1
  IL_000e:  unbox.any  ""T""
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0016
  IL_0016:  br.s       IL_0018
  IL_0018:  ldloc.0
  IL_0019:  box        ""T""
  IL_001e:  call       ""void System.Console.Write(object)""
  IL_0023:  nop
  IL_0024:  br.s       IL_0026
  IL_0026:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void UnoxInPatternSwitch_09()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        M<int, object>(1);
        M<int, object>(null);
        M<int, object>(10.5);
    }
    public static void M<T, U>(U x) where T : U
    {
        // when T is not known to be a reference type, there is an unboxing conversion from
        // a type parameter U to T, provided T depends on U.
        switch (x)
        {
            case T i:
                Console.Write(i);
                break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M<T, U>",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""U""
  IL_0006:  isinst     ""T""
  IL_000b:  brfalse.s  IL_0022
  IL_000d:  ldarg.0
  IL_000e:  box        ""U""
  IL_0013:  unbox.any  ""T""
  IL_0018:  box        ""T""
  IL_001d:  call       ""void System.Console.Write(object)""
  IL_0022:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "1");
            compVerifier.VerifyIL("Program.M<T, U>",
@"{
  // Code size       49 (0x31)
  .maxstack  1
  .locals init (T V_0, //i
                U V_1,
                U V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  box        ""U""
  IL_000b:  isinst     ""T""
  IL_0010:  brfalse.s  IL_0030
  IL_0012:  ldloc.1
  IL_0013:  box        ""U""
  IL_0018:  unbox.any  ""T""
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0020
  IL_0020:  br.s       IL_0022
  IL_0022:  ldloc.0
  IL_0023:  box        ""T""
  IL_0028:  call       ""void System.Console.Write(object)""
  IL_002d:  nop
  IL_002e:  br.s       IL_0030
  IL_0030:  ret
}"
            );
        }

        [Fact, WorkItem(18859, "https://github.com/dotnet/roslyn/issues/18859")]
        public void BoxInPatternIf_02()
        {
            var source = @"using System;

public class Program
{
    public static void Main()
    {
        if (StringSplitOptions.RemoveEmptyEntries is object o)
        {
            Console.WriteLine(o);
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "RemoveEmptyEntries");
            compVerifier.VerifyIL("Program.Main",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""System.StringSplitOptions""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""void System.Console.WriteLine(object)""
  IL_000d:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "RemoveEmptyEntries");
            compVerifier.VerifyIL("Program.Main",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (object V_0, //o
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""System.StringSplitOptions""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  IL_000b:  brfalse.s  IL_0016
  IL_000d:  nop
  IL_000e:  ldloc.0
  IL_000f:  call       ""void System.Console.WriteLine(object)""
  IL_0014:  nop
  IL_0015:  nop
  IL_0016:  ret
}"
            );
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_01()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.Write(M1<int>(2));
        Console.Write(M2<int>(3));
        Console.Write(M1<int>(1.1));
        Console.Write(M2<int>(1.1));
    }
    public static T M1<T>(ValueType o)
    {
        return o is T t ? t : default(T);
    }
    public static T M2<T>(ValueType o)
    {
        switch (o)
        {
            case T t:
                return t;
            default:
                return default(T);
        }
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (13,21): error CS8413: An expression of type 'ValueType' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         return o is T t ? t : default(T);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("System.ValueType", "T", "7.0", "7.1").WithLocation(13, 21),
                // (19,18): error CS8413: An expression of type 'ValueType' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("System.ValueType", "T", "7.0", "7.1").WithLocation(19, 18)
                );
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                parseOptions: TestOptions.Regular7_1,
                expectedOutput: "2300");
            compVerifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (T V_0, //t
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""T""
  IL_0006:  brfalse.s  IL_0016
  IL_0008:  ldarg.0
  IL_0009:  isinst     ""T""
  IL_000e:  unbox.any  ""T""
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0020
  IL_0016:  ldloca.s   V_1
  IL_0018:  initobj    ""T""
  IL_001e:  ldloc.1
  IL_001f:  ret
  IL_0020:  ldloc.0
  IL_0021:  ret
}"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                parseOptions: TestOptions.Regular7_1,
                expectedOutput: "2300");
            compVerifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (T V_0, //t
                T V_1,
                T V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""T""
  IL_0007:  brfalse.s  IL_0017
  IL_0009:  ldarg.0
  IL_000a:  isinst     ""T""
  IL_000f:  unbox.any  ""T""
  IL_0014:  stloc.0
  IL_0015:  br.s       IL_0022
  IL_0017:  ldloca.s   V_1
  IL_0019:  initobj    ""T""
  IL_001f:  ldloc.1
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.0
  IL_0023:  stloc.2
  IL_0024:  br.s       IL_0026
  IL_0026:  ldloc.2
  IL_0027:  ret
}"
            );
            compVerifier.VerifyIL("Program.M2<T>",
@"{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (T V_0, //t
                System.ValueType V_1,
                System.ValueType V_2,
                T V_3,
                T V_4)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  isinst     ""T""
  IL_000b:  brfalse.s  IL_0021
  IL_000d:  ldloc.1
  IL_000e:  isinst     ""T""
  IL_0013:  unbox.any  ""T""
  IL_0018:  stloc.0
  IL_0019:  br.s       IL_001b
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.0
  IL_001e:  stloc.3
  IL_001f:  br.s       IL_002e
  IL_0021:  ldloca.s   V_4
  IL_0023:  initobj    ""T""
  IL_0029:  ldloc.s    V_4
  IL_002b:  stloc.3
  IL_002c:  br.s       IL_002e
  IL_002e:  ldloc.3
  IL_002f:  ret
}"
            );
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_02()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.Write(M1(2));
        Console.Write(M2(3));
        Console.Write(M1(1.1));
        Console.Write(M2(1.1));
    }
    public static int M1<T>(T o)
    {
        return o is int t ? t : default(int);
    }
    public static int M2<T>(T o)
    {
        switch (o)
        {
            case int t:
                return t;
            default:
                return default(int);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (13,21): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'int' in C# 7.0. Please use language version 7.1 or greater.
                //         return o is int t ? t : default(int);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "int").WithArguments("T", "int", "7.0", "7.1").WithLocation(13, 21),
                // (19,18): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'int' in C# 7.0. Please use language version 7.1 or greater.
                //             case int t:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "int").WithArguments("T", "int", "7.0", "7.1").WithLocation(19, 18)
                );
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                parseOptions: TestOptions.Regular7_1,
                expectedOutput: "2300");
            compVerifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (int V_0) //t
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  isinst     ""int""
  IL_000b:  brfalse.s  IL_0020
  IL_000d:  ldarg.0
  IL_000e:  box        ""T""
  IL_0013:  isinst     ""int""
  IL_0018:  unbox.any  ""int""
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0022
  IL_0020:  ldc.i4.0
  IL_0021:  ret
  IL_0022:  ldloc.0
  IL_0023:  ret
}
"
            );
            compVerifier.VerifyIL("Program.M2<T>",
@"{
  // Code size       32 (0x20)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  isinst     ""int""
  IL_000b:  brfalse.s  IL_001e
  IL_000d:  ldarg.0
  IL_000e:  box        ""T""
  IL_0013:  isinst     ""int""
  IL_0018:  unbox.any  ""int""
  IL_001d:  ret
  IL_001e:  ldc.i4.0
  IL_001f:  ret
}
"
            );
            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                parseOptions: TestOptions.Regular7_1,
                expectedOutput: "2300");
            compVerifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init (int V_0, //t
                int V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  isinst     ""int""
  IL_000c:  brfalse.s  IL_0021
  IL_000e:  ldarg.0
  IL_000f:  box        ""T""
  IL_0014:  isinst     ""int""
  IL_0019:  unbox.any  ""int""
  IL_001e:  stloc.0
  IL_001f:  br.s       IL_0024
  IL_0021:  ldc.i4.0
  IL_0022:  br.s       IL_0025
  IL_0024:  ldloc.0
  IL_0025:  stloc.1
  IL_0026:  br.s       IL_0028
  IL_0028:  ldloc.1
  IL_0029:  ret
}
"
            );
            compVerifier.VerifyIL("Program.M2<T>",
@"{
  // Code size       49 (0x31)
  .maxstack  1
  .locals init (int V_0, //t
                T V_1,
                T V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  box        ""T""
  IL_000b:  isinst     ""int""
  IL_0010:  brfalse.s  IL_002b
  IL_0012:  ldloc.1
  IL_0013:  box        ""T""
  IL_0018:  isinst     ""int""
  IL_001d:  unbox.any  ""int""
  IL_0022:  stloc.0
  IL_0023:  br.s       IL_0025
  IL_0025:  br.s       IL_0027
  IL_0027:  ldloc.0
  IL_0028:  stloc.3
  IL_0029:  br.s       IL_002f
  IL_002b:  ldc.i4.0
  IL_002c:  stloc.3
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.3
  IL_0030:  ret
}
"
            );
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_03()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.Write(M1<int>(2));
        Console.Write(M2<int>(3));
        Console.Write(M1<int>(1.1));
        Console.Write(M2<int>(1.1));
    }
    public static T M1<T>(ValueType o) where T : struct
    {
        return o is T t ? t : default(T);
    }
    public static T M2<T>(ValueType o) where T : struct
    {
        switch (o)
        {
            case T t:
                return t;
            default:
                return default(T);
        }
    }
}
";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "2300");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_04()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        var x = new X();
        Console.Write(M1<B>(new A()) ?? x);
        Console.Write(M2<B>(new A()) ?? x);
        Console.Write(M1<B>(new B()) ?? x);
        Console.Write(M2<B>(new B()) ?? x);
    }
    public static T M1<T>(A o) where T : class
    {
        return o is T t ? t : default(T);
    }
    public static T M2<T>(A o) where T : class
    {
        switch (o)
        {
            case T t:
                return t;
            default:
                return default(T);
        }
    }
}
class A
{
}
class B : A
{
}
class X : B { }
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (14,21): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         return o is T t ? t : default(T);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(14, 21),
                // (20,18): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(20, 18)
                );
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                parseOptions: TestOptions.Regular7_1,
                expectedOutput: "XXBB");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_05()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        var x = new X();
        Console.Write(M1<B>(new A()) ?? x);
        Console.Write(M2<B>(new A()) ?? x);
        Console.Write(M1<B>(new B()) ?? x);
        Console.Write(M2<B>(new B()) ?? x);
    }
    public static T M1<T>(A o) where T : I1
    {
        return o is T t ? t : default(T);
    }
    public static T M2<T>(A o) where T : I1
    {
        switch (o)
        {
            case T t:
                return t;
            default:
                return default(T);
        }
    }
}
interface I1
{
}
class A : I1
{
}
class B : A
{
}
class X : B { }
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (14,21): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         return o is T t ? t : default(T);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(14, 21),
                // (20,18): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(20, 18)
                );
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                parseOptions: TestOptions.Regular7_1,
                expectedOutput: "XXBB");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_06()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.Write(M1<B>(new A()));
        Console.Write(M2<B>(new A()));
        Console.Write(M1<A>(new A()));
        Console.Write(M2<A>(new A()));
    }
    public static bool M1<T>(A o) where T : I1
    {
        return o is T t;
    }
    public static bool M2<T>(A o) where T : I1
    {
        switch (o)
        {
            case T t:
                return true;
            default:
                return false;
        }
    }
}
interface I1
{
}
struct A : I1
{
}
struct B : I1
{
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (13,21): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         return o is T t;
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(13, 21),
                // (19,18): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(19, 18)
                );
            var compilation = CreateCompilation(source,
                    options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                    parseOptions: TestOptions.Regular7_1)
                .VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation,
                expectedOutput: "FalseFalseTrueTrue");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestMatchWithTypeParameter_07()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.Write(M1<B>(new A()));
        Console.Write(M2<B>(new A()));
        Console.Write(M1<A>(new A()));
        Console.Write(M2<A>(new A()));
    }
    public static bool M1<T>(A o) where T : new()
    {
        return o is T t;
    }
    public static bool M2<T>(A o) where T : new()
    {
        switch (o)
        {
            case T t:
                return true;
            default:
                return false;
        }
    }
}
struct A
{
}
struct B
{
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (13,21): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         return o is T t;
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(13, 21),
                // (19,18): error CS8413: An expression of type 'A' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("A", "T", "7.0", "7.1").WithLocation(19, 18)
                );
            var compilation = CreateCompilation(source,
                    options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                    parseOptions: TestOptions.Regular7_1)
                .VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation,
                expectedOutput: "FalseFalseTrueTrue");
            compVerifier.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/31269")]
        [WorkItem(16195, "https://github.com/dotnet/roslyn/issues/31269")]
        public void TestIgnoreDynamicVsObjectAndTupleElementNames_01()
        {
            var source =
@"public class Generic<T>
{
    public enum Color { Red, Blue }
}
class Program
{
    public static void Main(string[] args)
    {
    }
    public static void M2(object o)
    {
        switch (o)
        {
            case Generic<long>.Color c:
            case Generic<object>.Color.Red:
            case Generic<(int x, int y)>.Color.Blue:
            case Generic<string>.Color.Red:
            case Generic<dynamic>.Color.Red: // error: duplicate case
            case Generic<(int z, int w)>.Color.Blue: // error: duplicate case
            case Generic<(int z, long w)>.Color.Blue:
            default:
                break;
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (18,13): error CS8120: The switch case has already been handled by a previous case.
                //             case Generic<dynamic>.Color.Red: // error: duplicate case
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "case Generic<dynamic>.Color.Red:").WithLocation(18, 13),
                // (19,13): error CS8120: The switch case has already been handled by a previous case.
                //             case Generic<(int z, int w)>.Color.Blue: // error: duplicate case
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "case Generic<(int z, int w)>.Color.Blue:").WithLocation(19, 13)
                );
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TestIgnoreDynamicVsObjectAndTupleElementNames_02()
        {
            var source =
@"using System;
public class Generic<T>
{
    public enum Color { X0, X1, X2, Green, Blue, Red }
}
class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(M1<Generic<int>.Color>(Generic<long>.Color.Red));                      // False
        Console.WriteLine(M1<Generic<string>.Color>(Generic<object>.Color.Red));                 // False
        Console.WriteLine(M1<Generic<(int x, int y)>.Color>(Generic<(int z, int w)>.Color.Red)); // True
        Console.WriteLine(M1<Generic<object>.Color>(Generic<dynamic>.Color.Blue));               // True

        Console.WriteLine(M2(Generic<long>.Color.Red));    // Generic<long>.Color.Red
        Console.WriteLine(M2(Generic<object>.Color.Blue)); // Generic<dynamic>.Color.Blue
        Console.WriteLine(M2(Generic<int>.Color.Red));     // None
        Console.WriteLine(M2(Generic<dynamic>.Color.Red)); // Generic<object>.Color.Red
    }
    public static bool M1<T>(object o)
    {
        return o is T t;
    }
    public static string M2(object o)
    {
        switch (o)
        {
            case Generic<long>.Color c:
                return ""Generic<long>.Color."" + c;
            case Generic<object>.Color.Red:
                return ""Generic<object>.Color.Red"";
            case Generic<dynamic>.Color.Blue:
                return ""Generic<dynamic>.Color.Blue"";
            default:
                return ""None"";
        }
    }
}
";
            var compilation = CreateCompilation(source,
                    options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication))
                .VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation,
                expectedOutput: @"False
False
True
True
Generic<long>.Color.Red
Generic<dynamic>.Color.Blue
None
Generic<object>.Color.Red");
            compVerifier.VerifyIL("Program.M2",
@"{
  // Code size      115 (0x73)
  .maxstack  2
  .locals init (Generic<long>.Color V_0, //c
                object V_1,
                Generic<object>.Color V_2,
                Generic<dynamic>.Color V_3,
                object V_4,
                string V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_4
  IL_0004:  ldloc.s    V_4
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  isinst     ""Generic<long>.Color""
  IL_000d:  brfalse.s  IL_0018
  IL_000f:  ldloc.1
  IL_0010:  unbox.any  ""Generic<long>.Color""
  IL_0015:  stloc.0
  IL_0016:  br.s       IL_0038
  IL_0018:  ldloc.1
  IL_0019:  isinst     ""Generic<object>.Color""
  IL_001e:  brfalse.s  IL_0067
  IL_0020:  ldloc.1
  IL_0021:  unbox.any  ""Generic<object>.Color""
  IL_0026:  stloc.2
  IL_0027:  ldloc.2
  IL_0028:  ldc.i4.5
  IL_0029:  beq.s      IL_0055
  IL_002b:  ldloc.1
  IL_002c:  unbox.any  ""Generic<dynamic>.Color""
  IL_0031:  stloc.3
  IL_0032:  ldloc.3
  IL_0033:  ldc.i4.4
  IL_0034:  beq.s      IL_005e
  IL_0036:  br.s       IL_0067
  IL_0038:  br.s       IL_003a
  IL_003a:  ldstr      ""Generic<long>.Color.""
  IL_003f:  ldloca.s   V_0
  IL_0041:  constrained. ""Generic<long>.Color""
  IL_0047:  callvirt   ""string object.ToString()""
  IL_004c:  call       ""string string.Concat(string, string)""
  IL_0051:  stloc.s    V_5
  IL_0053:  br.s       IL_0070
  IL_0055:  ldstr      ""Generic<object>.Color.Red""
  IL_005a:  stloc.s    V_5
  IL_005c:  br.s       IL_0070
  IL_005e:  ldstr      ""Generic<dynamic>.Color.Blue""
  IL_0063:  stloc.s    V_5
  IL_0065:  br.s       IL_0070
  IL_0067:  ldstr      ""None""
  IL_006c:  stloc.s    V_5
  IL_006e:  br.s       IL_0070
  IL_0070:  ldloc.s    V_5
  IL_0072:  ret
}"
            );
        }

        [Fact, WorkItem(16129, "https://github.com/dotnet/roslyn/issues/16129")]
        public void ExactPatternMatch()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        if (TrySomething() is ValueTuple<string, bool> v && v.Item2)
        {
            System.Console.Write(v.Item1 == null);
        }
    }

    static (string Value, bool Success) TrySomething()
    {
        return (null, true);
    }
}";
            var compilation = CreateCompilation(source,
                    options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication))
                .VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation,
                expectedOutput: @"True");
            compVerifier.VerifyIL("C.Main",
@"{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (System.ValueTuple<string, bool> V_0) //v
  IL_0000:  call       ""(string Value, bool Success) C.TrySomething()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""bool System.ValueTuple<string, bool>.Item2""
  IL_000c:  brfalse.s  IL_001c
  IL_000e:  ldloc.0
  IL_000f:  ldfld      ""string System.ValueTuple<string, bool>.Item1""
  IL_0014:  ldnull
  IL_0015:  ceq
  IL_0017:  call       ""void System.Console.Write(bool)""
  IL_001c:  ret
}"
            );
        }

        [WorkItem(19280, "https://github.com/dotnet/roslyn/issues/19280")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void ShareLikeKindedTemps_01()
        {
            var source = @"using System;
public class Program
{
    public static void Main()
    {
    }
    static bool b = false;
    public static void M(object o)
    {
        switch (o)
        {
            case int i when b: break;
            case var _ when b: break;
            case int i when b: break;
            case var _ when b: break;
            case int i when b: break;
            case var _ when b: break;
            case int i when b: break;
            case var _ when b: break;
        }
    }
}";
            var compVerifier = CompileAndVerify(source,
                options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "");
            compVerifier.VerifyIL("Program.M",
@"{
  // Code size      104 (0x68)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""int""
  IL_0006:  brfalse.s  IL_001f
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""int""
  IL_000e:  stloc.0
  IL_000f:  ldsfld     ""bool Program.b""
  IL_0014:  brtrue.s   IL_0067
  IL_0016:  ldsfld     ""bool Program.b""
  IL_001b:  brtrue.s   IL_0067
  IL_001d:  br.s       IL_0028
  IL_001f:  ldsfld     ""bool Program.b""
  IL_0024:  brtrue.s   IL_0067
  IL_0026:  br.s       IL_0038
  IL_0028:  ldsfld     ""bool Program.b""
  IL_002d:  brtrue.s   IL_0067
  IL_002f:  ldsfld     ""bool Program.b""
  IL_0034:  brtrue.s   IL_0067
  IL_0036:  br.s       IL_0041
  IL_0038:  ldsfld     ""bool Program.b""
  IL_003d:  brtrue.s   IL_0067
  IL_003f:  br.s       IL_0051
  IL_0041:  ldsfld     ""bool Program.b""
  IL_0046:  brtrue.s   IL_0067
  IL_0048:  ldsfld     ""bool Program.b""
  IL_004d:  brtrue.s   IL_0067
  IL_004f:  br.s       IL_005a
  IL_0051:  ldsfld     ""bool Program.b""
  IL_0056:  brtrue.s   IL_0067
  IL_0058:  br.s       IL_0061
  IL_005a:  ldsfld     ""bool Program.b""
  IL_005f:  brtrue.s   IL_0067
  IL_0061:  ldsfld     ""bool Program.b""
  IL_0066:  pop
  IL_0067:  ret
}"
            );

            compVerifier = CompileAndVerify(source,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication),
                expectedOutput: "");
            compVerifier.VerifyIL(qualifiedMethodName: "Program.M", sequencePoints: "Program.M", source: source,
expectedIL: @"{
  // Code size      149 (0x95)
  .maxstack  1
  .locals init (int V_0, //i
                int V_1, //i
                int V_2, //i
                int V_3, //i
                object V_4,
                object V_5)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (o)
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_5
  // sequence point: <hidden>
  IL_0004:  ldloc.s    V_5
  IL_0006:  stloc.s    V_4
  // sequence point: <hidden>
  IL_0008:  ldloc.s    V_4
  IL_000a:  isinst     ""int""
  IL_000f:  brfalse.s  IL_002f
  IL_0011:  ldloc.s    V_4
  IL_0013:  unbox.any  ""int""
  IL_0018:  stloc.0
  // sequence point: <hidden>
  IL_0019:  br.s       IL_001b
  // sequence point: when b
  IL_001b:  ldsfld     ""bool Program.b""
  IL_0020:  brtrue.s   IL_0024
  // sequence point: <hidden>
  IL_0022:  br.s       IL_0026
  // sequence point: break;
  IL_0024:  br.s       IL_0094
  // sequence point: when b
  IL_0026:  ldsfld     ""bool Program.b""
  IL_002b:  brtrue.s   IL_0038
  // sequence point: <hidden>
  IL_002d:  br.s       IL_003a
  // sequence point: when b
  IL_002f:  ldsfld     ""bool Program.b""
  IL_0034:  brtrue.s   IL_0038
  // sequence point: <hidden>
  IL_0036:  br.s       IL_0050
  // sequence point: break;
  IL_0038:  br.s       IL_0094
  // sequence point: <hidden>
  IL_003a:  ldloc.0
  IL_003b:  stloc.1
  // sequence point: when b
  IL_003c:  ldsfld     ""bool Program.b""
  IL_0041:  brtrue.s   IL_0045
  // sequence point: <hidden>
  IL_0043:  br.s       IL_0047
  // sequence point: break;
  IL_0045:  br.s       IL_0094
  // sequence point: when b
  IL_0047:  ldsfld     ""bool Program.b""
  IL_004c:  brtrue.s   IL_0059
  // sequence point: <hidden>
  IL_004e:  br.s       IL_005b
  // sequence point: when b
  IL_0050:  ldsfld     ""bool Program.b""
  IL_0055:  brtrue.s   IL_0059
  // sequence point: <hidden>
  IL_0057:  br.s       IL_0071
  // sequence point: break;
  IL_0059:  br.s       IL_0094
  // sequence point: <hidden>
  IL_005b:  ldloc.0
  IL_005c:  stloc.2
  // sequence point: when b
  IL_005d:  ldsfld     ""bool Program.b""
  IL_0062:  brtrue.s   IL_0066
  // sequence point: <hidden>
  IL_0064:  br.s       IL_0068
  // sequence point: break;
  IL_0066:  br.s       IL_0094
  // sequence point: when b
  IL_0068:  ldsfld     ""bool Program.b""
  IL_006d:  brtrue.s   IL_007a
  // sequence point: <hidden>
  IL_006f:  br.s       IL_007c
  // sequence point: when b
  IL_0071:  ldsfld     ""bool Program.b""
  IL_0076:  brtrue.s   IL_007a
  // sequence point: <hidden>
  IL_0078:  br.s       IL_0089
  // sequence point: break;
  IL_007a:  br.s       IL_0094
  // sequence point: <hidden>
  IL_007c:  ldloc.0
  IL_007d:  stloc.3
  // sequence point: when b
  IL_007e:  ldsfld     ""bool Program.b""
  IL_0083:  brtrue.s   IL_0087
  // sequence point: <hidden>
  IL_0085:  br.s       IL_0089
  // sequence point: break;
  IL_0087:  br.s       IL_0094
  // sequence point: when b
  IL_0089:  ldsfld     ""bool Program.b""
  IL_008e:  brtrue.s   IL_0092
  // sequence point: <hidden>
  IL_0090:  br.s       IL_0094
  // sequence point: break;
  IL_0092:  br.s       IL_0094
  // sequence point: }
  IL_0094:  ret
}"
            );
            compVerifier.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""Program"" methodName=""Main"" />
  <methods>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""M"" parameterNames=""o"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""55"" />
          <slot kind=""0"" offset=""133"" />
          <slot kind=""0"" offset=""211"" />
          <slot kind=""0"" offset=""289"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""19"" document=""1"" />
        <entry offset=""0x4"" hidden=""true"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" startLine=""12"" startColumn=""24"" endLine=""12"" endColumn=""30"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x24"" startLine=""12"" startColumn=""32"" endLine=""12"" endColumn=""38"" document=""1"" />
        <entry offset=""0x26"" startLine=""13"" startColumn=""24"" endLine=""13"" endColumn=""30"" document=""1"" />
        <entry offset=""0x2d"" hidden=""true"" document=""1"" />
        <entry offset=""0x2f"" startLine=""13"" startColumn=""24"" endLine=""13"" endColumn=""30"" document=""1"" />
        <entry offset=""0x36"" hidden=""true"" document=""1"" />
        <entry offset=""0x38"" startLine=""13"" startColumn=""32"" endLine=""13"" endColumn=""38"" document=""1"" />
        <entry offset=""0x3a"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""14"" startColumn=""24"" endLine=""14"" endColumn=""30"" document=""1"" />
        <entry offset=""0x43"" hidden=""true"" document=""1"" />
        <entry offset=""0x45"" startLine=""14"" startColumn=""32"" endLine=""14"" endColumn=""38"" document=""1"" />
        <entry offset=""0x47"" startLine=""15"" startColumn=""24"" endLine=""15"" endColumn=""30"" document=""1"" />
        <entry offset=""0x4e"" hidden=""true"" document=""1"" />
        <entry offset=""0x50"" startLine=""15"" startColumn=""24"" endLine=""15"" endColumn=""30"" document=""1"" />
        <entry offset=""0x57"" hidden=""true"" document=""1"" />
        <entry offset=""0x59"" startLine=""15"" startColumn=""32"" endLine=""15"" endColumn=""38"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0x5d"" startLine=""16"" startColumn=""24"" endLine=""16"" endColumn=""30"" document=""1"" />
        <entry offset=""0x64"" hidden=""true"" document=""1"" />
        <entry offset=""0x66"" startLine=""16"" startColumn=""32"" endLine=""16"" endColumn=""38"" document=""1"" />
        <entry offset=""0x68"" startLine=""17"" startColumn=""24"" endLine=""17"" endColumn=""30"" document=""1"" />
        <entry offset=""0x6f"" hidden=""true"" document=""1"" />
        <entry offset=""0x71"" startLine=""17"" startColumn=""24"" endLine=""17"" endColumn=""30"" document=""1"" />
        <entry offset=""0x78"" hidden=""true"" document=""1"" />
        <entry offset=""0x7a"" startLine=""17"" startColumn=""32"" endLine=""17"" endColumn=""38"" document=""1"" />
        <entry offset=""0x7c"" hidden=""true"" document=""1"" />
        <entry offset=""0x7e"" startLine=""18"" startColumn=""24"" endLine=""18"" endColumn=""30"" document=""1"" />
        <entry offset=""0x85"" hidden=""true"" document=""1"" />
        <entry offset=""0x87"" startLine=""18"" startColumn=""32"" endLine=""18"" endColumn=""38"" document=""1"" />
        <entry offset=""0x89"" startLine=""19"" startColumn=""24"" endLine=""19"" endColumn=""30"" document=""1"" />
        <entry offset=""0x90"" hidden=""true"" document=""1"" />
        <entry offset=""0x92"" startLine=""19"" startColumn=""32"" endLine=""19"" endColumn=""38"" document=""1"" />
        <entry offset=""0x94"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x95"">
        <scope startOffset=""0x1b"" endOffset=""0x26"">
          <local name=""i"" il_index=""0"" il_start=""0x1b"" il_end=""0x26"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x3a"" endOffset=""0x47"">
          <local name=""i"" il_index=""1"" il_start=""0x3a"" il_end=""0x47"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x5b"" endOffset=""0x68"">
          <local name=""i"" il_index=""2"" il_start=""0x5b"" il_end=""0x68"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x7c"" endOffset=""0x89"">
          <local name=""i"" il_index=""3"" il_start=""0x7c"" il_end=""0x89"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Program"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""27"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(19280, "https://github.com/dotnet/roslyn/issues/19280")]
        public void TestSignificanceOfDynamicVersusObjectAndTupleNamesInUniquenessOfPatternMatchingTemps()
        {
            var source =
@"using System;
public class Generic<T,U>
{
}
class Program
{
    public static void Main(string[] args)
    {
        var g = new Generic<object, (int, int)>();
        M2(g, true, false, false);
        M2(g, false, true, false);
        M2(g, false, false, true);
    }
    public static void M2(object o, bool b1, bool b2, bool b3)
    {
        switch (o)
        {
            case Generic<object, (int a, int b)> g when b1: Console.Write(""a""); break;
            case var _ when b2: Console.Write(""b""); break;
            case Generic<dynamic, (int x, int y)> g when b3: Console.Write(""c""); break;
        }
    }
}
";
            var compilation = CreateCompilation(source,
                    options: TestOptions.DebugDll.WithOutputKind(OutputKind.ConsoleApplication))
                .VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "abc");
            compVerifier.VerifyIL("Program.M2",
@"{
  // Code size       86 (0x56)
  .maxstack  1
  .locals init (Generic<object, (int a, int b)> V_0, //g
                Generic<dynamic, (int x, int y)> V_1, //g
                object V_2,
                object V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.3
  IL_0003:  ldloc.3
  IL_0004:  stloc.2
  IL_0005:  ldloc.2
  IL_0006:  isinst     ""Generic<object, (int a, int b)>""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_001a
  IL_000f:  br.s       IL_0031
  IL_0011:  ldloc.2
  IL_0012:  castclass  ""Generic<dynamic, (int x, int y)>""
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_0043
  IL_001a:  ldarg.1
  IL_001b:  brtrue.s   IL_001f
  IL_001d:  br.s       IL_002c
  IL_001f:  ldstr      ""a""
  IL_0024:  call       ""void System.Console.Write(string)""
  IL_0029:  nop
  IL_002a:  br.s       IL_0055
  IL_002c:  ldarg.2
  IL_002d:  brtrue.s   IL_0036
  IL_002f:  br.s       IL_0011
  IL_0031:  ldarg.2
  IL_0032:  brtrue.s   IL_0036
  IL_0034:  br.s       IL_0055
  IL_0036:  ldstr      ""b""
  IL_003b:  call       ""void System.Console.Write(string)""
  IL_0040:  nop
  IL_0041:  br.s       IL_0055
  IL_0043:  ldarg.3
  IL_0044:  brtrue.s   IL_0048
  IL_0046:  br.s       IL_0055
  IL_0048:  ldstr      ""c""
  IL_004d:  call       ""void System.Console.Write(string)""
  IL_0052:  nop
  IL_0053:  br.s       IL_0055
  IL_0055:  ret
}"
            );
        }

        [Fact]
        [WorkItem(39564, "https://github.com/dotnet/roslyn/issues/39564")]
        public void OrderOfEvaluationOfTupleAsSwitchExpressionArgument()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        using var sr = new System.IO.StringReader(""foo\nbar"");
        var r = (sr.ReadLine(), sr.ReadLine()) switch
        {
            (""foo"", ""bar"") => ""Yep, all good!"",
            var (a, b) => $""Wait, what? I got ({a}, {b})!"",
        };
        Console.WriteLine(r);
    }
}
";
            var compilation = CreateCompilation(source,
                    options: TestOptions.DebugExe.WithOutputKind(OutputKind.ConsoleApplication))
                .VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "Yep, all good!");
            compVerifier.VerifyIL("Program.Main",
@"    {
      // Code size      142 (0x8e)
      .maxstack  4
      .locals init (System.IO.StringReader V_0, //sr
                    string V_1, //r
                    string V_2, //a
                    string V_3, //b
                    string V_4)
      IL_0000:  nop
      IL_0001:  ldstr      ""foo
    bar""
      IL_0006:  newobj     ""System.IO.StringReader..ctor(string)""
      IL_000b:  stloc.0
      .try
      {
        IL_000c:  ldloc.0
        IL_000d:  callvirt   ""string System.IO.TextReader.ReadLine()""
        IL_0012:  stloc.2
        IL_0013:  ldloc.0
        IL_0014:  callvirt   ""string System.IO.TextReader.ReadLine()""
        IL_0019:  stloc.3
        IL_001a:  ldloc.2
        IL_001b:  brfalse.s  IL_0045
        IL_001d:  ldloc.2
        IL_001e:  ldstr      ""foo""
        IL_0023:  call       ""bool string.op_Equality(string, string)""
        IL_0028:  brfalse.s  IL_0045
        IL_002a:  ldloc.3
        IL_002b:  brfalse.s  IL_0045
        IL_002d:  ldloc.3
        IL_002e:  ldstr      ""bar""
        IL_0033:  call       ""bool string.op_Equality(string, string)""
        IL_0038:  brtrue.s   IL_003c
        IL_003a:  br.s       IL_0045
        IL_003c:  ldstr      ""Yep, all good!""
        IL_0041:  stloc.s    V_4
        IL_0043:  br.s       IL_0076
        IL_0045:  br.s       IL_0047
        IL_0047:  ldc.i4.5
        IL_0048:  newarr     ""string""
        IL_004d:  dup
        IL_004e:  ldc.i4.0
        IL_004f:  ldstr      ""Wait, what? I got (""
        IL_0054:  stelem.ref
        IL_0055:  dup
        IL_0056:  ldc.i4.1
        IL_0057:  ldloc.2
        IL_0058:  stelem.ref
        IL_0059:  dup
        IL_005a:  ldc.i4.2
        IL_005b:  ldstr      "", ""
        IL_0060:  stelem.ref
        IL_0061:  dup
        IL_0062:  ldc.i4.3
        IL_0063:  ldloc.3
        IL_0064:  stelem.ref
        IL_0065:  dup
        IL_0066:  ldc.i4.4
        IL_0067:  ldstr      "")!""
        IL_006c:  stelem.ref
        IL_006d:  call       ""string string.Concat(params string[])""
        IL_0072:  stloc.s    V_4
        IL_0074:  br.s       IL_0076
        IL_0076:  ldloc.s    V_4
        IL_0078:  stloc.1
        IL_0079:  ldloc.1
        IL_007a:  call       ""void System.Console.WriteLine(string)""
        IL_007f:  nop
        IL_0080:  leave.s    IL_008d
      }
      finally
      {
        IL_0082:  ldloc.0
        IL_0083:  brfalse.s  IL_008c
        IL_0085:  ldloc.0
        IL_0086:  callvirt   ""void System.IDisposable.Dispose()""
        IL_008b:  nop
        IL_008c:  endfinally
      }
      IL_008d:  ret
    }
");
            compilation = CreateCompilation(source,
                    options: TestOptions.ReleaseExe.WithOutputKind(OutputKind.ConsoleApplication))
                .VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: "Yep, all good!");
            compVerifier.VerifyIL("Program.Main",
@"    {
      // Code size      128 (0x80)
      .maxstack  4
      .locals init (System.IO.StringReader V_0, //sr
                    string V_1, //a
                    string V_2, //b
                    string V_3)
      IL_0000:  ldstr      ""foo
    bar""
      IL_0005:  newobj     ""System.IO.StringReader..ctor(string)""
      IL_000a:  stloc.0
      .try
      {
        IL_000b:  ldloc.0
        IL_000c:  callvirt   ""string System.IO.TextReader.ReadLine()""
        IL_0011:  stloc.1
        IL_0012:  ldloc.0
        IL_0013:  callvirt   ""string System.IO.TextReader.ReadLine()""
        IL_0018:  stloc.2
        IL_0019:  ldloc.1
        IL_001a:  brfalse.s  IL_0041
        IL_001c:  ldloc.1
        IL_001d:  ldstr      ""foo""
        IL_0022:  call       ""bool string.op_Equality(string, string)""
        IL_0027:  brfalse.s  IL_0041
        IL_0029:  ldloc.2
        IL_002a:  brfalse.s  IL_0041
        IL_002c:  ldloc.2
        IL_002d:  ldstr      ""bar""
        IL_0032:  call       ""bool string.op_Equality(string, string)""
        IL_0037:  brfalse.s  IL_0041
        IL_0039:  ldstr      ""Yep, all good!""
        IL_003e:  stloc.3
        IL_003f:  br.s       IL_006d
        IL_0041:  ldc.i4.5
        IL_0042:  newarr     ""string""
        IL_0047:  dup
        IL_0048:  ldc.i4.0
        IL_0049:  ldstr      ""Wait, what? I got (""
        IL_004e:  stelem.ref
        IL_004f:  dup
        IL_0050:  ldc.i4.1
        IL_0051:  ldloc.1
        IL_0052:  stelem.ref
        IL_0053:  dup
        IL_0054:  ldc.i4.2
        IL_0055:  ldstr      "", ""
        IL_005a:  stelem.ref
        IL_005b:  dup
        IL_005c:  ldc.i4.3
        IL_005d:  ldloc.2
        IL_005e:  stelem.ref
        IL_005f:  dup
        IL_0060:  ldc.i4.4
        IL_0061:  ldstr      "")!""
        IL_0066:  stelem.ref
        IL_0067:  call       ""string string.Concat(params string[])""
        IL_006c:  stloc.3
        IL_006d:  ldloc.3
        IL_006e:  call       ""void System.Console.WriteLine(string)""
        IL_0073:  leave.s    IL_007f
      }
      finally
      {
        IL_0075:  ldloc.0
        IL_0076:  brfalse.s  IL_007e
        IL_0078:  ldloc.0
        IL_0079:  callvirt   ""void System.IDisposable.Dispose()""
        IL_007e:  endfinally
      }
      IL_007f:  ret
    }
");
        }

        #endregion "regression tests"
    }
}
