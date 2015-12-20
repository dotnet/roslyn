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

        [WorkItem(542298, "DevDiv")]
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
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (int V_0, //status
  string V_1) //str
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""string""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  sub
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  ret
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
                    // Code size       18 (0x12)
                    .maxstack  2
                    .locals init (int V_0, //ret
                    int V_1) //value
                    IL_0000:  ldc.i4.0
                    IL_0001:  stloc.0
                    IL_0002:  ldc.i4.1
                    IL_0003:  stloc.1
                    IL_0004:  ldloc.1
                    IL_0005:  ldc.i4.2
                    IL_0006:  bne.un.s   IL_000a
                    IL_0008:  ldc.i4.1
                    IL_0009:  stloc.0
                    IL_000a:  ldloc.0
                    IL_000b:  call       ""void System.Console.Write(int)""
                    IL_0010:  ldloc.0
                    IL_0011:  ret
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
                    // Code size       24 (0x18)
                    .maxstack  2
                    .locals init (int V_0, //ret
                    int V_1) //value
                    IL_0000:  ldc.i4.1
                    IL_0001:  stloc.0
                    IL_0002:  ldc.i4.s   23
                    IL_0004:  stloc.1
                    IL_0005:  ldloc.1
                    IL_0006:  ldc.i4.s   23
                    IL_0008:  bne.un.s   IL_000e
                    IL_000a:  ldc.i4.0
                    IL_000b:  stloc.0
                    IL_000c:  br.s       IL_0010
                    IL_000e:  ldc.i4.1
                    IL_000f:  stloc.0
                    IL_0010:  ldloc.0
                    IL_0011:  call       ""void System.Console.Write(int)""
                    IL_0016:  ldloc.0
                    IL_0017:  ret
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
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
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  switch    (
  IL_0030,
  IL_0030,
  IL_0030)
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4     0x3e9
  IL_001c:  sub
  IL_001d:  switch    (
  IL_0032,
  IL_0032,
  IL_0032)
  IL_002e:  br.s       IL_0034
  IL_0030:  ldc.i4.1
  IL_0031:  ret
  IL_0032:  ldc.i4.2
  IL_0033:  ret
  IL_0034:  ldc.i4.0
  IL_0035:  ret
}
"
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
  // Code size       50 (0x32)
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
  IL_0007:  switch    (
  IL_001a,
  IL_001a,
  IL_001e)
  IL_0018:  br.s       IL_001e
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.1
  IL_001c:  sub
  IL_001d:  stloc.0
  IL_001e:  ldloc.1
  IL_001f:  ldc.i4.1
  IL_0020:  beq.s      IL_002a
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4.3
  IL_0024:  beq.s      IL_002a
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.1
  IL_0028:  sub
  IL_0029:  stloc.0
  IL_002a:  ldloc.0
  IL_002b:  call       ""void System.Console.Write(int)""
  IL_0030:  ldloc.0
  IL_0031:  ret
}
"
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
}
"
            );
        }

        [WorkItem(740058, "DevDiv")]
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
}
"
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
}
"
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
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (int V_0, //ret
  Test.eTypes V_1) //e
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  ldc.i4.m1
  IL_0006:  sub
  IL_0007:  switch    (
  IL_0022,
  IL_0022,
  IL_001e,
  IL_001e)
  IL_001c:  br.s       IL_0022
  IL_001e:  ldloc.0
  IL_001f:  ldc.i4.1
  IL_0020:  sub
  IL_0021:  stloc.0
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4.m1
  IL_0024:  beq.s      IL_002f
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.s   100
  IL_0029:  beq.s      IL_002f
  IL_002b:  ldloc.0
  IL_002c:  ldc.i4.1
  IL_002d:  sub
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  call       ""void System.Console.Write(int)""
  IL_0035:  ldloc.0
  IL_0036:  ret
}
"
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
  // Code size      127 (0x7f)
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
  IL_0011:  brfalse.s  IL_0027
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""Test.eTypes Test.eTypes?.GetValueOrDefault()""
  IL_001a:  stloc.2
  IL_001b:  ldloc.2
  IL_001c:  ldc.i4.1
  IL_001d:  beq.s      IL_0023
  IL_001f:  ldloc.2
  IL_0020:  ldc.i4.2
  IL_0021:  bne.un.s   IL_0027
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.1
  IL_0025:  sub
  IL_0026:  stloc.0
  IL_0027:  ldloca.s   V_1
  IL_0029:  call       ""bool Test.eTypes?.HasValue.get""
  IL_002e:  brfalse.s  IL_0040
  IL_0030:  ldloca.s   V_1
  IL_0032:  call       ""Test.eTypes Test.eTypes?.GetValueOrDefault()""
  IL_0037:  stloc.2
  IL_0038:  ldloc.2
  IL_0039:  ldc.i4.m1
  IL_003a:  beq.s      IL_0040
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.1
  IL_003e:  sub
  IL_003f:  stloc.0
  IL_0040:  ldloca.s   V_1
  IL_0042:  initobj    ""Test.eTypes?""
  IL_0048:  ldloca.s   V_1
  IL_004a:  call       ""bool Test.eTypes?.HasValue.get""
  IL_004f:  brfalse.s  IL_0073
  IL_0051:  ldloca.s   V_1
  IL_0053:  call       ""Test.eTypes Test.eTypes?.GetValueOrDefault()""
  IL_0058:  stloc.2
  IL_0059:  ldloc.2
  IL_005a:  ldc.i4.m1
  IL_005b:  sub
  IL_005c:  switch    (
  IL_0077,
  IL_0077,
  IL_0077,
  IL_0077)
  IL_0071:  br.s       IL_0077
  IL_0073:  ldloc.0
  IL_0074:  ldc.i4.1
  IL_0075:  sub
  IL_0076:  stloc.0
  IL_0077:  ldloc.0
  IL_0078:  call       ""void System.Console.Write(int)""
  IL_007d:  ldloc.0
  IL_007e:  ret
}
");
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
@"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (int V_0, //ret
  int V_1) //i
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_002c
  IL_0006:  ldloc.1
  IL_0007:  switch    (
  IL_001a,
  IL_001a,
  IL_001a)
  IL_0018:  br.s       IL_0020
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.1
  IL_001c:  sub
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0028
  IL_0020:  ldc.i4.1
  IL_0021:  call       ""void System.Console.Write(int)""
  IL_0026:  ldc.i4.1
  IL_0027:  ret
  IL_0028:  ldloc.1
  IL_0029:  ldc.i4.1
  IL_002a:  add
  IL_002b:  stloc.1
  IL_002c:  ldloc.1
  IL_002d:  ldc.i4.3
  IL_002e:  blt.s      IL_0006
  IL_0030:  ldloc.0
  IL_0031:  call       ""void System.Console.Write(int)""
  IL_0036:  ldloc.0
  IL_0037:  ret
}
"
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
                      // Code size       10 (0xa)
                      .maxstack  2
                      .locals init (int V_0) //i
                      IL_0000:  ldc.i4.5
                      IL_0001:  stloc.0
                      IL_0002:  ldloc.0
                      IL_0003:  ldc.i4.1
                      IL_0004:  bne.un.s   IL_0008
                      IL_0006:  ldc.i4.1
                      IL_0007:  ret
                      IL_0008:  ldc.i4.0
                      IL_0009:  ret
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
                        // Code size       10 (0xa)
                        .maxstack  2
                        .locals init (int V_0) //i
                        IL_0000:  ldc.i4.5
                        IL_0001:  stloc.0
                        IL_0002:  ldloc.0
                        IL_0003:  ldc.i4.5
                        IL_0004:  bne.un.s   IL_0008
                        IL_0006:  ldc.i4.0
                        IL_0007:  ret
                        IL_0008:  ldc.i4.1
                        IL_0009:  ret
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
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (int V_0, //j
                int V_1) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.3
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  brtrue.s   IL_0014
  IL_0007:  nop
  .try
  {
    .try
    {
      IL_0008:  ldc.i4.0
      IL_0009:  stloc.1
      IL_000a:  leave.s    IL_0016
    }
    catch object
    {
      IL_000c:  pop
      IL_000d:  ldc.i4.1
      IL_000e:  stloc.1
      IL_000f:  leave.s    IL_0016
    }
  }
  finally
  {
    IL_0011:  ldc.i4.2
    IL_0012:  stloc.0
    IL_0013:  endfinally
  }
  IL_0014:  ldc.i4.2
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ldloc.1
  IL_001d:  ret
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
		case 6:			// start here
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
}
"
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
}
"
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
}
"
            );
        }

        [WorkItem(542398, "DevDiv")]
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

        [WorkItem(543967, "DevDiv")]
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

        Foo(1);
    }

    static void Foo(sbyte? p)
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
  // Code size       66 (0x42)
  .maxstack  2
  .locals init (sbyte? V_0, //local
  sbyte V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""sbyte?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool sbyte?.HasValue.get""
  IL_000f:  brfalse.s  IL_001f
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""sbyte sbyte?.GetValueOrDefault()""
  IL_0018:  stloc.1
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4.1
  IL_001b:  beq.s      IL_002b
  IL_001d:  br.s       IL_0035
  IL_001f:  ldstr      ""null ""
  IL_0024:  call       ""void System.Console.Write(string)""
  IL_0029:  br.s       IL_0035
  IL_002b:  ldstr      ""1 ""
  IL_0030:  call       ""void System.Console.Write(string)""
  IL_0035:  ldc.i4.1
  IL_0036:  conv.i1
  IL_0037:  newobj     ""sbyte?..ctor(sbyte)""
  IL_003c:  call       ""void Program.Foo(sbyte?)""
  IL_0041:  ret
}");
        }

        [WorkItem(543967, "DevDiv")]
        [Fact()]
        public void NullableAsSwitchExpression_02()
        {
            var text = @"using System;
class Program
{
    static void Main()
    {
        Foo(null);
        Foo(100);
    }

    static void Foo(short? p)
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
            verifier.VerifyIL("Program.Foo", @"
{
  // Code size      373 (0x175)
  .maxstack  2
  .locals init (short? V_0,
  short V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool short?.HasValue.get""
  IL_0009:  brfalse.s  IL_005a
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""short short?.GetValueOrDefault()""
  IL_0012:  stloc.1
  IL_0013:  ldloc.1
  IL_0014:  ldc.i4.1
  IL_0015:  sub
  IL_0016:  switch    (
  IL_0070,
  IL_0070,
  IL_0070)
  IL_0027:  ldloc.1
  IL_0028:  ldc.i4.s   100
  IL_002a:  sub
  IL_002b:  switch    (
  IL_0066,
  IL_0070,
  IL_0070,
  IL_0070)
  IL_0040:  ldloc.1
  IL_0041:  ldc.i4     0x3e9
  IL_0046:  sub
  IL_0047:  switch    (
  IL_0070,
  IL_0070,
  IL_0070)
  IL_0058:  br.s       IL_0070
  IL_005a:  ldstr      ""null ""
  IL_005f:  call       ""void System.Console.Write(string)""
  IL_0064:  br.s       IL_0070
  IL_0066:  ldstr      ""100 ""
  IL_006b:  call       ""void System.Console.Write(string)""
  IL_0070:  ldarg.0
  IL_0071:  stloc.0
  IL_0072:  ldloca.s   V_0
  IL_0074:  call       ""bool short?.HasValue.get""
  IL_0079:  brfalse.s  IL_00c0
  IL_007b:  ldloca.s   V_0
  IL_007d:  call       ""short short?.GetValueOrDefault()""
  IL_0082:  stloc.1
  IL_0083:  ldloc.1
  IL_0084:  ldc.i4.s   101
  IL_0086:  bgt.s      IL_00a3
  IL_0088:  ldloc.1
  IL_0089:  ldc.i4.1
  IL_008a:  sub
  IL_008b:  switch    (
  IL_00ca,
  IL_00ca,
  IL_00ca)
  IL_009c:  ldloc.1
  IL_009d:  ldc.i4.s   101
  IL_009f:  beq.s      IL_00ca
  IL_00a1:  br.s       IL_00c0
  IL_00a3:  ldloc.1
  IL_00a4:  ldc.i4.s   103
  IL_00a6:  beq.s      IL_00ca
  IL_00a8:  ldloc.1
  IL_00a9:  ldc.i4     0x3e9
  IL_00ae:  sub
  IL_00af:  switch    (
  IL_00ca,
  IL_00ca,
  IL_00ca)
  IL_00c0:  ldstr      ""default ""
  IL_00c5:  call       ""void System.Console.Write(string)""
  IL_00ca:  ldarg.0
  IL_00cb:  stloc.0
  IL_00cc:  ldloca.s   V_0
  IL_00ce:  call       ""bool short?.HasValue.get""
  IL_00d3:  brfalse    IL_0174
  IL_00d8:  ldloca.s   V_0
  IL_00da:  call       ""short short?.GetValueOrDefault()""
  IL_00df:  stloc.1
  IL_00e0:  ldloc.1
  IL_00e1:  ldc.i4.s   101
  IL_00e3:  bgt.s      IL_00ff
  IL_00e5:  ldloc.1
  IL_00e6:  ldc.i4.1
  IL_00e7:  sub
  IL_00e8:  switch    (
  IL_011d,
  IL_0128,
  IL_0133)
  IL_00f9:  ldloc.1
  IL_00fa:  ldc.i4.s   101
  IL_00fc:  beq.s      IL_013e
  IL_00fe:  ret
  IL_00ff:  ldloc.1
  IL_0100:  ldc.i4.s   103
  IL_0102:  beq.s      IL_0149
  IL_0104:  ldloc.1
  IL_0105:  ldc.i4     0x3e9
  IL_010a:  sub
  IL_010b:  switch    (
  IL_0154,
  IL_015f,
  IL_016a)
  IL_011c:  ret
  IL_011d:  ldstr      ""FAIL""
  IL_0122:  call       ""void System.Console.Write(string)""
  IL_0127:  ret
  IL_0128:  ldstr      ""FAIL""
  IL_012d:  call       ""void System.Console.Write(string)""
  IL_0132:  ret
  IL_0133:  ldstr      ""FAIL""
  IL_0138:  call       ""void System.Console.Write(string)""
  IL_013d:  ret
  IL_013e:  ldstr      ""FAIL""
  IL_0143:  call       ""void System.Console.Write(string)""
  IL_0148:  ret
  IL_0149:  ldstr      ""FAIL""
  IL_014e:  call       ""void System.Console.Write(string)""
  IL_0153:  ret
  IL_0154:  ldstr      ""FAIL""
  IL_0159:  call       ""void System.Console.Write(string)""
  IL_015e:  ret
  IL_015f:  ldstr      ""FAIL""
  IL_0164:  call       ""void System.Console.Write(string)""
  IL_0169:  ret
  IL_016a:  ldstr      ""FAIL""
  IL_016f:  call       ""void System.Console.Write(string)""
  IL_0174:  ret
}
");
        }

        [Fact]
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
@"
{
        // Code size       28 (0x1c)
        .maxstack  2
        .locals init (long? V_0,
                      long V_1)
        IL_0000:  ldarg.0
        IL_0001:  stloc.0
        IL_0002:  ldloca.s   V_0
        IL_0004:  call       ""bool long?.HasValue.get""
        IL_0009:  brfalse.s  IL_001a
        IL_000b:  ldloca.s   V_0
        IL_000d:  call       ""long long?.GetValueOrDefault()""
        IL_0012:  stloc.1
        IL_0013:  ldloc.1
        IL_0014:  ldc.i4.1
        IL_0015:  conv.i8
        IL_0016:  bne.un.s   IL_001a
        IL_0018:  ldc.i4.1
        IL_0019:  ret
        IL_001a:  ldc.i4.0
        IL_001b:  ret
}"
            );
        }

        [Fact]
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

            var expected = Diagnostic(ErrorCode.ERR_ConstantExpected, "case i:");

            compilation.VerifyDiagnostics(expected);
        }

        [Fact]
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
            case (int)Foo.X:
                return true;
            default:
                return false;
        }
    }

    public enum Foo : int { X  = 1 }

    static void Main()
    {
        System.Console.WriteLine(F(1));
    }
}";
            var compVerifier = CompileAndVerify(text, expectedOutput: "True");
            compVerifier.VerifyIL("C.F(long?)",
@"
{
        // Code size       28 (0x1c)
        .maxstack  2
        .locals init (long? V_0,
                      long V_1)
        IL_0000:  ldarg.0
        IL_0001:  stloc.0
        IL_0002:  ldloca.s   V_0
        IL_0004:  call       ""bool long?.HasValue.get""
        IL_0009:  brfalse.s  IL_001a
        IL_000b:  ldloca.s   V_0
        IL_000d:  call       ""long long?.GetValueOrDefault()""
        IL_0012:  stloc.1
        IL_0013:  ldloc.1
        IL_0014:  ldc.i4.1
        IL_0015:  conv.i8
        IL_0016:  bne.un.s   IL_001a
        IL_0018:  ldc.i4.1
        IL_0019:  ret
        IL_001a:  ldc.i4.0
        IL_001b:  ret
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
                        // Code size       46 (0x2e)
                        .maxstack  2
                        .locals init (string V_0, //s
                        int V_1) //ret
                        IL_0000:  ldnull
                        IL_0001:  stloc.0
                        IL_0002:  ldc.i4.0
                        IL_0003:  stloc.1
                        IL_0004:  ldloc.0
                        IL_0005:  ldstr      ""abc""
                        IL_000a:  call       ""bool string.op_Equality(string, string)""
                        IL_000f:  brtrue.s   IL_0020
                        IL_0011:  ldloc.0
                        IL_0012:  ldstr      ""def""
                        IL_0017:  call       ""bool string.op_Equality(string, string)""
                        IL_001c:  brtrue.s   IL_0024
                        IL_001e:  br.s       IL_0026
                        IL_0020:  ldc.i4.1
                        IL_0021:  stloc.1
                        IL_0022:  br.s       IL_0026
                        IL_0024:  ldc.i4.1
                        IL_0025:  stloc.1
                        IL_0026:  ldloc.1
                        IL_0027:  call       ""void System.Console.Write(int)""
                        IL_002c:  ldloc.1
                        IL_002d:  ret
                }"
            );

            // We shouldn't generate a string hash synthesized method if we are generating non hash string switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: false);
        }

        [Fact]
        [WorkItem(546632, "DevDiv")]
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
  // Code size      373 (0x175)
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
  IL_0022:  call       ""ComputeStringHash""
  IL_0027:  stloc.1   
  IL_0028:  ldloc.1   
  IL_0029:  ldc.i4     0xc60bf9f2
  IL_002e:  bgt.un.s   IL_0063
  IL_0030:  ldloc.1   
  IL_0031:  ldc.i4     0x390caefb
  IL_0036:  bgt.un.s   IL_004b
  IL_0038:  ldloc.1   
  IL_0039:  brfalse.s  IL_00a3
  IL_003b:  ldloc.1   
  IL_003c:  ldc.i4     0x390caefb
  IL_0041:  beq        IL_0106
  IL_0046:  br         IL_016d
  IL_004b:  ldloc.1   
  IL_004c:  ldc.i4     0x811c9dc5
  IL_0051:  beq.s      IL_00ae
  IL_0053:  ldloc.1   
  IL_0054:  ldc.i4     0xc60bf9f2
  IL_0059:  beq        IL_00f7
  IL_005e:  br         IL_016d
  IL_0063:  ldloc.1   
  IL_0064:  ldc.i4     0xc80bfd18
  IL_0069:  bgt.un.s   IL_0083
  IL_006b:  ldloc.1   
  IL_006c:  ldc.i4     0xc70bfb85
  IL_0071:  beq.s      IL_00e8
  IL_0073:  ldloc.1   
  IL_0074:  ldc.i4     0xc80bfd18
  IL_0079:  beq        IL_0124
  IL_007e:  br         IL_016d
  IL_0083:  ldloc.1   
  IL_0084:  ldc.i4     0xd10c0b43
  IL_0089:  beq        IL_0115
  IL_008e:  ldloc.1   
  IL_008f:  ldc.i4     0xd20c0cd6
  IL_0094:  beq.s      IL_00d6
  IL_0096:  ldloc.1   
  IL_0097:  ldc.i4     0xda0c196e
  IL_009c:  beq.s      IL_00c4
  IL_009e:  br         IL_016d
  IL_00a3:  ldarg.0   
  IL_00a4:  brfalse    IL_0133
  IL_00a9:  br         IL_016d
  IL_00ae:  ldarg.0   
  IL_00af:  brfalse    IL_016d
  IL_00b4:  ldarg.0   
  IL_00b5:  call       ""int string.Length.get""
  IL_00ba:  brfalse    IL_016d
  IL_00bf:  br         IL_016d
  IL_00c4:  ldarg.0   
  IL_00c5:  ldstr      ""_""
  IL_00ca:  call       ""bool string.op_Equality(string, string)""
  IL_00cf:  brtrue.s   IL_0137
  IL_00d1:  br         IL_016d
  IL_00d6:  ldarg.0   
  IL_00d7:  ldstr      ""W""
  IL_00dc:  call       ""bool string.op_Equality(string, string)""
  IL_00e1:  brtrue.s   IL_013f
  IL_00e3:  br         IL_016d
  IL_00e8:  ldarg.0   
  IL_00e9:  ldstr      ""B""
  IL_00ee:  call       ""bool string.op_Equality(string, string)""
  IL_00f3:  brtrue.s   IL_0147
  IL_00f5:  br.s       IL_016d
  IL_00f7:  ldarg.0   
  IL_00f8:  ldstr      ""C""
  IL_00fd:  call       ""bool string.op_Equality(string, string)""
  IL_0102:  brtrue.s   IL_014f
  IL_0104:  br.s       IL_016d
  IL_0106:  ldarg.0   
  IL_0107:  ldstr      ""<""
  IL_010c:  call       ""bool string.op_Equality(string, string)""
  IL_0111:  brtrue.s   IL_0157
  IL_0113:  br.s       IL_016d
  IL_0115:  ldarg.0   
  IL_0116:  ldstr      ""T""
  IL_011b:  call       ""bool string.op_Equality(string, string)""
  IL_0120:  brtrue.s   IL_015f
  IL_0122:  br.s       IL_016d
  IL_0124:  ldarg.0   
  IL_0125:  ldstr      ""M""
  IL_012a:  call       ""bool string.op_Equality(string, string)""
  IL_012f:  brtrue.s   IL_0167
  IL_0131:  br.s       IL_016d
  IL_0133:  ldnull    
  IL_0134:  stloc.0   
  IL_0135:  br.s       IL_016d
  IL_0137:  ldstr      ""_""
  IL_013c:  stloc.0   
  IL_013d:  br.s       IL_016d
  IL_013f:  ldstr      ""W""
  IL_0144:  stloc.0   
  IL_0145:  br.s       IL_016d
  IL_0147:  ldstr      ""B""
  IL_014c:  stloc.0   
  IL_014d:  br.s       IL_016d
  IL_014f:  ldstr      ""C""
  IL_0154:  stloc.0   
  IL_0155:  br.s       IL_016d
  IL_0157:  ldstr      ""<""
  IL_015c:  stloc.0   
  IL_015d:  br.s       IL_016d
  IL_015f:  ldstr      ""T""
  IL_0164:  stloc.0   
  IL_0165:  br.s       IL_016d
  IL_0167:  ldstr      ""M""
  IL_016c:  stloc.0   
  IL_016d:  ldloc.0   
  IL_016e:  ldarg.0   
  IL_016f:  call       ""bool string.op_Equality(string, string)""
  IL_0174:  ret       
}
"
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
  // Code size     1115 (0x45b)
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
  IL_001f:  call       ""ComputeStringHash""
  IL_0024:  stloc.1   
  IL_0025:  ldloc.1   
  IL_0026:  ldc.i4     0xb2f29419
  IL_002b:  bgt.un     IL_00e0
  IL_0030:  ldloc.1   
  IL_0031:  ldc.i4     0x619348d8
  IL_0036:  bgt.un.s   IL_008c
  IL_0038:  ldloc.1   
  IL_0039:  ldc.i4     0x36758e37
  IL_003e:  bgt.un.s   IL_0066
  IL_0040:  ldloc.1   
  IL_0041:  ldc.i4     0x144fd20d
  IL_0046:  beq        IL_02ae
  IL_004b:  ldloc.1   
  IL_004c:  ldc.i4     0x14ca99e2
  IL_0051:  beq        IL_025a
  IL_0056:  ldloc.1   
  IL_0057:  ldc.i4     0x36758e37
  IL_005c:  beq        IL_0206
  IL_0061:  br         IL_0453
  IL_0066:  ldloc.1   
  IL_0067:  ldc.i4     0x5398a778
  IL_006c:  beq        IL_021b
  IL_0071:  ldloc.1   
  IL_0072:  ldc.i4     0x616477cf
  IL_0077:  beq        IL_01b2
  IL_007c:  ldloc.1   
  IL_007d:  ldc.i4     0x619348d8
  IL_0082:  beq        IL_02ed
  IL_0087:  br         IL_0453
  IL_008c:  ldloc.1   
  IL_008d:  ldc.i4     0x78a826a8
  IL_0092:  bgt.un.s   IL_00ba
  IL_0094:  ldloc.1   
  IL_0095:  ldc.i4     0x65b3e3e5
  IL_009a:  beq        IL_02c3
  IL_009f:  ldloc.1   
  IL_00a0:  ldc.i4     0x7822b5bc
  IL_00a5:  beq        IL_0284
  IL_00aa:  ldloc.1   
  IL_00ab:  ldc.i4     0x78a826a8
  IL_00b0:  beq        IL_01dc
  IL_00b5:  br         IL_0453
  IL_00ba:  ldloc.1   
  IL_00bb:  ldc.i4     0x7f66da4e
  IL_00c0:  beq        IL_0356
  IL_00c5:  ldloc.1   
  IL_00c6:  ldc.i4     0xb13d374d
  IL_00cb:  beq        IL_032c
  IL_00d0:  ldloc.1   
  IL_00d1:  ldc.i4     0xb2f29419
  IL_00d6:  beq        IL_0302
  IL_00db:  br         IL_0453
  IL_00e0:  ldloc.1   
  IL_00e1:  ldc.i4     0xd59864f4
  IL_00e6:  bgt.un.s   IL_013c
  IL_00e8:  ldloc.1   
  IL_00e9:  ldc.i4     0xbf4a9f8e
  IL_00ee:  bgt.un.s   IL_0116
  IL_00f0:  ldloc.1   
  IL_00f1:  ldc.i4     0xb6e02d3a
  IL_00f6:  beq        IL_0299
  IL_00fb:  ldloc.1   
  IL_00fc:  ldc.i4     0xbaed3db3
  IL_0101:  beq        IL_0317
  IL_0106:  ldloc.1   
  IL_0107:  ldc.i4     0xbf4a9f8e
  IL_010c:  beq        IL_0230
  IL_0111:  br         IL_0453
  IL_0116:  ldloc.1   
  IL_0117:  ldc.i4     0xc6284d42
  IL_011c:  beq        IL_01f1
  IL_0121:  ldloc.1   
  IL_0122:  ldc.i4     0xd1761402
  IL_0127:  beq        IL_01c7
  IL_012c:  ldloc.1   
  IL_012d:  ldc.i4     0xd59864f4
  IL_0132:  beq        IL_026f
  IL_0137:  br         IL_0453
  IL_013c:  ldloc.1   
  IL_013d:  ldc.i4     0xeb323c73
  IL_0142:  bgt.un.s   IL_016a
  IL_0144:  ldloc.1   
  IL_0145:  ldc.i4     0xdca4b248
  IL_014a:  beq        IL_0245
  IL_014f:  ldloc.1   
  IL_0150:  ldc.i4     0xe926f470
  IL_0155:  beq        IL_036b
  IL_015a:  ldloc.1   
  IL_015b:  ldc.i4     0xeb323c73
  IL_0160:  beq        IL_02d8
  IL_0165:  br         IL_0453
  IL_016a:  ldloc.1   
  IL_016b:  ldc.i4     0xf1ea0ad5
  IL_0170:  beq        IL_0341
  IL_0175:  ldloc.1   
  IL_0176:  ldc.i4     0xfa67b44d
  IL_017b:  beq.s      IL_019d
  IL_017d:  ldloc.1   
  IL_017e:  ldc.i4     0xfea21584
  IL_0183:  bne.un     IL_0453
  IL_0188:  ldarg.0   
  IL_0189:  ldstr      ""N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9""
  IL_018e:  call       ""bool string.op_Equality(string, string)""
  IL_0193:  brtrue     IL_0380
  IL_0198:  br         IL_0453
  IL_019d:  ldarg.0   
  IL_019e:  ldstr      ""emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I""
  IL_01a3:  call       ""bool string.op_Equality(string, string)""
  IL_01a8:  brtrue     IL_038b
  IL_01ad:  br         IL_0453
  IL_01b2:  ldarg.0   
  IL_01b3:  ldstr      ""Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m""
  IL_01b8:  call       ""bool string.op_Equality(string, string)""
  IL_01bd:  brtrue     IL_0396
  IL_01c2:  br         IL_0453
  IL_01c7:  ldarg.0   
  IL_01c8:  ldstr      ""[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD""
  IL_01cd:  call       ""bool string.op_Equality(string, string)""
  IL_01d2:  brtrue     IL_03a1
  IL_01d7:  br         IL_0453
  IL_01dc:  ldarg.0   
  IL_01dd:  ldstr      ""<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]""
  IL_01e2:  call       ""bool string.op_Equality(string, string)""
  IL_01e7:  brtrue     IL_03ac
  IL_01ec:  br         IL_0453
  IL_01f1:  ldarg.0   
  IL_01f2:  ldstr      ""[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c""
  IL_01f7:  call       ""bool string.op_Equality(string, string)""
  IL_01fc:  brtrue     IL_03b7
  IL_0201:  br         IL_0453
  IL_0206:  ldarg.0   
  IL_0207:  ldstr      ""d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0""
  IL_020c:  call       ""bool string.op_Equality(string, string)""
  IL_0211:  brtrue     IL_03c2
  IL_0216:  br         IL_0453
  IL_021b:  ldarg.0   
  IL_021c:  ldstr      ""n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>""
  IL_0221:  call       ""bool string.op_Equality(string, string)""
  IL_0226:  brtrue     IL_03cd
  IL_022b:  br         IL_0453
  IL_0230:  ldarg.0   
  IL_0231:  ldstr      ""H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF""
  IL_0236:  call       ""bool string.op_Equality(string, string)""
  IL_023b:  brtrue     IL_03d5
  IL_0240:  br         IL_0453
  IL_0245:  ldarg.0   
  IL_0246:  ldstr      ""iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:""
  IL_024b:  call       ""bool string.op_Equality(string, string)""
  IL_0250:  brtrue     IL_03dd
  IL_0255:  br         IL_0453
  IL_025a:  ldarg.0   
  IL_025b:  ldstr      "">AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj""
  IL_0260:  call       ""bool string.op_Equality(string, string)""
  IL_0265:  brtrue     IL_03e5
  IL_026a:  br         IL_0453
  IL_026f:  ldarg.0   
  IL_0270:  ldstr      ""d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:""
  IL_0275:  call       ""bool string.op_Equality(string, string)""
  IL_027a:  brtrue     IL_03ed
  IL_027f:  br         IL_0453
  IL_0284:  ldarg.0   
  IL_0285:  ldstr      ""fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU""
  IL_028a:  call       ""bool string.op_Equality(string, string)""
  IL_028f:  brtrue     IL_03f5
  IL_0294:  br         IL_0453
  IL_0299:  ldarg.0   
  IL_029a:  ldstr      ""9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR""
  IL_029f:  call       ""bool string.op_Equality(string, string)""
  IL_02a4:  brtrue     IL_03fd
  IL_02a9:  br         IL_0453
  IL_02ae:  ldarg.0   
  IL_02af:  ldstr      ""A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj""
  IL_02b4:  call       ""bool string.op_Equality(string, string)""
  IL_02b9:  brtrue     IL_0405
  IL_02be:  br         IL_0453
  IL_02c3:  ldarg.0   
  IL_02c4:  ldstr      ""SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k""
  IL_02c9:  call       ""bool string.op_Equality(string, string)""
  IL_02ce:  brtrue     IL_040d
  IL_02d3:  br         IL_0453
  IL_02d8:  ldarg.0   
  IL_02d9:  ldstr      ""0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4""
  IL_02de:  call       ""bool string.op_Equality(string, string)""
  IL_02e3:  brtrue     IL_0415
  IL_02e8:  br         IL_0453
  IL_02ed:  ldarg.0   
  IL_02ee:  ldstr      ""4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O""
  IL_02f3:  call       ""bool string.op_Equality(string, string)""
  IL_02f8:  brtrue     IL_041d
  IL_02fd:  br         IL_0453
  IL_0302:  ldarg.0   
  IL_0303:  ldstr      ""EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe""
  IL_0308:  call       ""bool string.op_Equality(string, string)""
  IL_030d:  brtrue     IL_0425
  IL_0312:  br         IL_0453
  IL_0317:  ldarg.0   
  IL_0318:  ldstr      ""?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM""
  IL_031d:  call       ""bool string.op_Equality(string, string)""
  IL_0322:  brtrue     IL_042d
  IL_0327:  br         IL_0453
  IL_032c:  ldarg.0   
  IL_032d:  ldstr      ""HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W""
  IL_0332:  call       ""bool string.op_Equality(string, string)""
  IL_0337:  brtrue     IL_0435
  IL_033c:  br         IL_0453
  IL_0341:  ldarg.0   
  IL_0342:  ldstr      ""XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D""
  IL_0347:  call       ""bool string.op_Equality(string, string)""
  IL_034c:  brtrue     IL_043d
  IL_0351:  br         IL_0453
  IL_0356:  ldarg.0   
  IL_0357:  ldstr      ""MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9""
  IL_035c:  call       ""bool string.op_Equality(string, string)""
  IL_0361:  brtrue     IL_0445
  IL_0366:  br         IL_0453
  IL_036b:  ldarg.0   
  IL_036c:  ldstr      ""L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4""
  IL_0371:  call       ""bool string.op_Equality(string, string)""
  IL_0376:  brtrue     IL_044d
  IL_037b:  br         IL_0453
  IL_0380:  ldstr      ""N?_2hBEJa_klm0=BRoM]mBSY3l=Zm<Aj:mBNm9[9""
  IL_0385:  stloc.0   
  IL_0386:  br         IL_0453
  IL_038b:  ldstr      ""emoYDC`E3JS]IU[X55VKF<e5CjkZb0S0VYQlcS]I""
  IL_0390:  stloc.0   
  IL_0391:  br         IL_0453
  IL_0396:  ldstr      ""Ye]@FRVZi8Rbn0;43c8lo5`W]1CK;cfa2485N45m""
  IL_039b:  stloc.0   
  IL_039c:  br         IL_0453
  IL_03a1:  ldstr      ""[Q0V3M_N2;9jTP=79iBK6<edbYXh;`FcaEGD0RhD""
  IL_03a6:  stloc.0   
  IL_03a7:  br         IL_0453
  IL_03ac:  ldstr      ""<9Ria992H`W:DNX7lm]LV]9LUnJKDXcCo6Zd_FM]""
  IL_03b1:  stloc.0   
  IL_03b2:  br         IL_0453
  IL_03b7:  ldstr      ""[Z`j:cCFgh2cd3:>1Z@T0o<Q<0o_;11]nMd3bP9c""
  IL_03bc:  stloc.0   
  IL_03bd:  br         IL_0453
  IL_03c2:  ldstr      ""d2U5RWR:j0RS9MZZP3[f@NPgKFS9mQi:na@4Z_G0""
  IL_03c7:  stloc.0   
  IL_03c8:  br         IL_0453
  IL_03cd:  ldstr      ""n7AOl<DYj1]k>F7FaW^5b2Ki6UP0@=glIc@RE]3>""
  IL_03d2:  stloc.0   
  IL_03d3:  br.s       IL_0453
  IL_03d5:  ldstr      ""H==7DT_M5125HT:m@`7cgg>WbZ4HAFg`Am:Ba:fF""
  IL_03da:  stloc.0   
  IL_03db:  br.s       IL_0453
  IL_03dd:  ldstr      ""iEj07Ik=?G35AfEf?8@5[@4OGYeXIHYH]CZlHY7:""
  IL_03e2:  stloc.0   
  IL_03e3:  br.s       IL_0453
  IL_03e5:  ldstr      "">AcFS3V9Y@g<55K`=QnYTS=B^CS@kg6:Hc_UaRTj""
  IL_03ea:  stloc.0   
  IL_03eb:  br.s       IL_0453
  IL_03ed:  ldstr      ""d1QZgJ_jT]UeL^UF2XWS@I?Hdi1MTm9Z3mdV7]0:""
  IL_03f2:  stloc.0   
  IL_03f3:  br.s       IL_0453
  IL_03f5:  ldstr      ""fVObMkcK:_AQae0VY4N]bDXXI_KkoeNZ9ohT?gfU""
  IL_03fa:  stloc.0   
  IL_03fb:  br.s       IL_0453
  IL_03fd:  ldstr      ""9o4i04]a4g2PRLBl@`]OaoY]1<h3on[5=I3U[9RR""
  IL_0402:  stloc.0   
  IL_0403:  br.s       IL_0453
  IL_0405:  ldstr      ""A1>CNg1bZTYE64G<Adn;aE957eWjEcaXZUf<TlGj""
  IL_040a:  stloc.0   
  IL_040b:  br.s       IL_0453
  IL_040d:  ldstr      ""SK`1T7]RZZR]lkZ`nFcm]k0RJlcF>eN5=jEi=A^k""
  IL_0412:  stloc.0   
  IL_0413:  br.s       IL_0453
  IL_0415:  ldstr      ""0@U=MkSf3niYF;8aC0U]IX=X[Y]Kjmj<4CR5:4R4""
  IL_041a:  stloc.0   
  IL_041b:  br.s       IL_0453
  IL_041d:  ldstr      ""4g1JY?VRdh5RYS[Z;ElS=5I`7?>OKlD3mF1;]M<O""
  IL_0422:  stloc.0   
  IL_0423:  br.s       IL_0453
  IL_0425:  ldstr      ""EH=noQ6]]@Vj5PDW;KFeEE7j>I<Q>4243W`AGHAe""
  IL_042a:  stloc.0   
  IL_042b:  br.s       IL_0453
  IL_042d:  ldstr      ""?k3Amd3aFf3_4S<bJ9;UdR7WYVmbZLh[2ekHKdTM""
  IL_0432:  stloc.0   
  IL_0433:  br.s       IL_0453
  IL_0435:  ldstr      ""HR9nATB9C[FY7B]9iI6IbodSencFWSVlhL879C:W""
  IL_043a:  stloc.0   
  IL_043b:  br.s       IL_0453
  IL_043d:  ldstr      ""XPTnWmDfL^AIH];Ek6l1AV9J020j<W:V6SU9VA@D""
  IL_0442:  stloc.0   
  IL_0443:  br.s       IL_0453
  IL_0445:  ldstr      ""MXO]7S@eM`o>LUXfLTk^m3eP2NbAj8N^[]J7PCh9""
  IL_044a:  stloc.0   
  IL_044b:  br.s       IL_0453
  IL_044d:  ldstr      ""L=FTZJ_V59eFjg_REMagg4n0Sng1]3mOgEAQ]EL4""
  IL_0452:  stloc.0   
  IL_0453:  ldloc.0   
  IL_0454:  ldarg.0   
  IL_0455:  call       ""bool string.op_Equality(string, string)""
  IL_045a:  ret       
}
"
            );

            // Verify string hash synthesized method for hash table switch
            VerifySynthesizedStringHashMethod(compVerifier, expected: true);
        }

        [WorkItem(544322, "DevDiv")]
        [Fact]
        public void StringSwitch_HashTableSwitch_03()
        {
            var text = @"
using System;

class Foo
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

        [WorkItem(543602, "DevDiv")]
        [WorkItem(543660, "DevDiv")]
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

        [WorkItem(543660, "DevDiv")]
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

        [WorkItem(543660, "DevDiv")]
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

        [WorkItem(543673, "DevDiv")]
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

        [WorkItem(543673, "DevDiv")]
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

        [WorkItem(543673, "DevDiv")]
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

        [WorkItem(543673, "DevDiv")]
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

        [WorkItem(634404, "DevDiv")]
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
            var comp = CreateCompilation(
                trees: new[] { Parse(text) },
                references: new[] { AacorlibRef });


            var verifier = CompileAndVerify(comp, verify: false);
            verifier.VerifyIL("Program.Main", @"
{
  // Code size      223 (0xdf)
  .maxstack  2
  .locals init (string V_0) //s
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""Hi""
  IL_000c:  call       ""bool string.op_Equality(string, string)""
  IL_0011:  brtrue.s   IL_007c
  IL_0013:  ldloc.0
  IL_0014:  ldstr      ""Bye""
  IL_0019:  call       ""bool string.op_Equality(string, string)""
  IL_001e:  brtrue.s   IL_0087
  IL_0020:  ldloc.0
  IL_0021:  ldstr      ""qwe""
  IL_0026:  call       ""bool string.op_Equality(string, string)""
  IL_002b:  brtrue.s   IL_0092
  IL_002d:  ldloc.0
  IL_002e:  ldstr      ""ert""
  IL_0033:  call       ""bool string.op_Equality(string, string)""
  IL_0038:  brtrue.s   IL_009d
  IL_003a:  ldloc.0
  IL_003b:  ldstr      ""asd""
  IL_0040:  call       ""bool string.op_Equality(string, string)""
  IL_0045:  brtrue.s   IL_00a8
  IL_0047:  ldloc.0
  IL_0048:  ldstr      ""hello""
  IL_004d:  call       ""bool string.op_Equality(string, string)""
  IL_0052:  brtrue.s   IL_00b3
  IL_0054:  ldloc.0
  IL_0055:  ldstr      ""qrs""
  IL_005a:  call       ""bool string.op_Equality(string, string)""
  IL_005f:  brtrue.s   IL_00be
  IL_0061:  ldloc.0
  IL_0062:  ldstr      ""tuv""
  IL_0067:  call       ""bool string.op_Equality(string, string)""
  IL_006c:  brtrue.s   IL_00c9
  IL_006e:  ldloc.0
  IL_006f:  ldstr      ""wxy""
  IL_0074:  call       ""bool string.op_Equality(string, string)""
  IL_0079:  brtrue.s   IL_00d4
  IL_007b:  ret
  IL_007c:  ldstr      "" Hi ""
  IL_0081:  stsfld     ""string Program.d""
  IL_0086:  ret
  IL_0087:  ldstr      "" Bye ""
  IL_008c:  stsfld     ""string Program.d""
  IL_0091:  ret
  IL_0092:  ldstr      "" qwe ""
  IL_0097:  stsfld     ""string Program.d""
  IL_009c:  ret
  IL_009d:  ldstr      "" ert ""
  IL_00a2:  stsfld     ""string Program.d""
  IL_00a7:  ret
  IL_00a8:  ldstr      "" asd ""
  IL_00ad:  stsfld     ""string Program.d""
  IL_00b2:  ret
  IL_00b3:  ldstr      "" hello ""
  IL_00b8:  stsfld     ""string Program.d""
  IL_00bd:  ret
  IL_00be:  ldstr      "" qrs ""
  IL_00c3:  stsfld     ""string Program.d""
  IL_00c8:  ret
  IL_00c9:  ldstr      "" tuv ""
  IL_00ce:  stsfld     ""string Program.d""
  IL_00d3:  ret
  IL_00d4:  ldstr      "" wxy ""
  IL_00d9:  stsfld     ""string Program.d""
  IL_00de:  ret
}
");
        }

        [WorkItem(642186, "DevDiv")]
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
            ERR_OverloadRefOut = 663,
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
            ERR_OverloadRefOutCtor = 851,
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
            FTL_InputFileNameTooLong = 2021,
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
  // Code size     2072 (0x818)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x406
  IL_0006:  bgt        IL_035f
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4     0x1b8
  IL_0011:  bgt        IL_01ac
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4     0xb7
  IL_001c:  bgt        IL_00bb
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.s   114
  IL_0024:  bgt.s      IL_0072
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.s   67
  IL_0029:  bgt.s      IL_0040
  IL_002b:  ldarg.0
  IL_002c:  ldc.i4.s   28
  IL_002e:  beq        IL_0814
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.s   67
  IL_0036:  beq        IL_0814
  IL_003b:  br         IL_0816
  IL_0040:  ldarg.0
  IL_0041:  ldc.i4.s   78
  IL_0043:  beq        IL_0814
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.s   105
  IL_004b:  sub
  IL_004c:  switch    (
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814)
  IL_0065:  ldarg.0
  IL_0066:  ldc.i4.s   114
  IL_0068:  beq        IL_0814
  IL_006d:  br         IL_0816
  IL_0072:  ldarg.0
  IL_0073:  ldc.i4     0xa4
  IL_0078:  bgt.s      IL_0095
  IL_007a:  ldarg.0
  IL_007b:  ldc.i4     0xa2
  IL_0080:  beq        IL_0814
  IL_0085:  ldarg.0
  IL_0086:  ldc.i4     0xa4
  IL_008b:  beq        IL_0814
  IL_0090:  br         IL_0816
  IL_0095:  ldarg.0
  IL_0096:  ldc.i4     0xa8
  IL_009b:  beq        IL_0814
  IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4     0xa9
  IL_00a6:  beq        IL_0814
  IL_00ab:  ldarg.0
  IL_00ac:  ldc.i4     0xb7
  IL_00b1:  beq        IL_0814
  IL_00b6:  br         IL_0816
  IL_00bb:  ldarg.0
  IL_00bc:  ldc.i4     0xfd
  IL_00c1:  bgt.s      IL_0119
  IL_00c3:  ldarg.0
  IL_00c4:  ldc.i4     0xc5
  IL_00c9:  bgt.s      IL_00e6
  IL_00cb:  ldarg.0
  IL_00cc:  ldc.i4     0xb8
  IL_00d1:  beq        IL_0814
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4     0xc5
  IL_00dc:  beq        IL_0814
  IL_00e1:  br         IL_0816
  IL_00e6:  ldarg.0
  IL_00e7:  ldc.i4     0xcf
  IL_00ec:  beq        IL_0814
  IL_00f1:  ldarg.0
  IL_00f2:  ldc.i4     0xdb
  IL_00f7:  beq        IL_0814
  IL_00fc:  ldarg.0
  IL_00fd:  ldc.i4     0xfb
  IL_0102:  sub
  IL_0103:  switch    (
  IL_0814,
  IL_0814,
  IL_0814)
  IL_0114:  br         IL_0816
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4     0x19e
  IL_011f:  bgt.s      IL_015c
  IL_0121:  ldarg.0
  IL_0122:  ldc.i4     0x116
  IL_0127:  sub
  IL_0128:  switch    (
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814)
  IL_0141:  ldarg.0
  IL_0142:  ldc.i4     0x192
  IL_0147:  beq        IL_0814
  IL_014c:  ldarg.0
  IL_014d:  ldc.i4     0x19e
  IL_0152:  beq        IL_0814
  IL_0157:  br         IL_0816
  IL_015c:  ldarg.0
  IL_015d:  ldc.i4     0x1a3
  IL_0162:  sub
  IL_0163:  switch    (
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814)
  IL_0178:  ldarg.0
  IL_0179:  ldc.i4     0x1ad
  IL_017e:  beq        IL_0814
  IL_0183:  ldarg.0
  IL_0184:  ldc.i4     0x1b3
  IL_0189:  sub
  IL_018a:  switch    (
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814)
  IL_01a7:  br         IL_0816
  IL_01ac:  ldarg.0
  IL_01ad:  ldc.i4     0x299
  IL_01b2:  bgt        IL_02ba
  IL_01b7:  ldarg.0
  IL_01b8:  ldc.i4     0x264
  IL_01bd:  bgt.s      IL_0231
  IL_01bf:  ldarg.0
  IL_01c0:  ldc.i4     0x1ca
  IL_01c5:  bgt.s      IL_01e2
  IL_01c7:  ldarg.0
  IL_01c8:  ldc.i4     0x1bc
  IL_01cd:  beq        IL_0814
  IL_01d2:  ldarg.0
  IL_01d3:  ldc.i4     0x1ca
  IL_01d8:  beq        IL_0814
  IL_01dd:  br         IL_0816
  IL_01e2:  ldarg.0
  IL_01e3:  ldc.i4     0x1d0
  IL_01e8:  sub
  IL_01e9:  switch    (
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814)
  IL_0216:  ldarg.0
  IL_0217:  ldc.i4     0x25a
  IL_021c:  beq        IL_0814
  IL_0221:  ldarg.0
  IL_0222:  ldc.i4     0x264
  IL_0227:  beq        IL_0814
  IL_022c:  br         IL_0816
  IL_0231:  ldarg.0
  IL_0232:  ldc.i4     0x274
  IL_0237:  bgt.s      IL_025f
  IL_0239:  ldarg.0
  IL_023a:  ldc.i4     0x26a
  IL_023f:  beq        IL_0814
  IL_0244:  ldarg.0
  IL_0245:  ldc.i4     0x272
  IL_024a:  beq        IL_0814
  IL_024f:  ldarg.0
  IL_0250:  ldc.i4     0x274
  IL_0255:  beq        IL_0814
  IL_025a:  br         IL_0816
  IL_025f:  ldarg.0
  IL_0260:  ldc.i4     0x282
  IL_0265:  beq        IL_0814
  IL_026a:  ldarg.0
  IL_026b:  ldc.i4     0x289
  IL_0270:  sub
  IL_0271:  switch    (
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814)
  IL_02aa:  ldarg.0
  IL_02ab:  ldc.i4     0x299
  IL_02b0:  beq        IL_0814
  IL_02b5:  br         IL_0816
  IL_02ba:  ldarg.0
  IL_02bb:  ldc.i4     0x2ac
  IL_02c0:  bgt.s      IL_030b
  IL_02c2:  ldarg.0
  IL_02c3:  ldc.i4     0x2a3
  IL_02c8:  bgt.s      IL_02e5
  IL_02ca:  ldarg.0
  IL_02cb:  ldc.i4     0x2a0
  IL_02d0:  beq        IL_0814
  IL_02d5:  ldarg.0
  IL_02d6:  ldc.i4     0x2a3
  IL_02db:  beq        IL_0814
  IL_02e0:  br         IL_0816
  IL_02e5:  ldarg.0
  IL_02e6:  ldc.i4     0x2a7
  IL_02eb:  beq        IL_0814
  IL_02f0:  ldarg.0
  IL_02f1:  ldc.i4     0x2a8
  IL_02f6:  beq        IL_0814
  IL_02fb:  ldarg.0
  IL_02fc:  ldc.i4     0x2ac
  IL_0301:  beq        IL_0814
  IL_0306:  br         IL_0816
  IL_030b:  ldarg.0
  IL_030c:  ldc.i4     0x329
  IL_0311:  bgt.s      IL_0339
  IL_0313:  ldarg.0
  IL_0314:  ldc.i4     0x2b5
  IL_0319:  beq        IL_0814
  IL_031e:  ldarg.0
  IL_031f:  ldc.i4     0x2d8
  IL_0324:  beq        IL_0814
  IL_0329:  ldarg.0
  IL_032a:  ldc.i4     0x329
  IL_032f:  beq        IL_0814
  IL_0334:  br         IL_0816
  IL_0339:  ldarg.0
  IL_033a:  ldc.i4     0x32b
  IL_033f:  beq        IL_0814
  IL_0344:  ldarg.0
  IL_0345:  ldc.i4     0x338
  IL_034a:  beq        IL_0814
  IL_034f:  ldarg.0
  IL_0350:  ldc.i4     0x406
  IL_0355:  beq        IL_0814
  IL_035a:  br         IL_0816
  IL_035f:  ldarg.0
  IL_0360:  ldc.i4     0x7b6
  IL_0365:  bgt        IL_064b
  IL_036a:  ldarg.0
  IL_036b:  ldc.i4     0x67a
  IL_0370:  bgt        IL_04ce
  IL_0375:  ldarg.0
  IL_0376:  ldc.i4     0x647
  IL_037b:  bgt        IL_0478
  IL_0380:  ldarg.0
  IL_0381:  ldc.i4     0x4b4
  IL_0386:  bgt.s      IL_03dd
  IL_0388:  ldarg.0
  IL_0389:  ldc.i4     0x422
  IL_038e:  sub
  IL_038f:  switch    (
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0814)
  IL_03b8:  ldarg.0
  IL_03b9:  ldc.i4     0x4b0
  IL_03be:  sub
  IL_03bf:  switch    (
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814)
  IL_03d8:  br         IL_0816
  IL_03dd:  ldarg.0
  IL_03de:  ldc.i4     0x5f2
  IL_03e3:  beq        IL_0814
  IL_03e8:  ldarg.0
  IL_03e9:  ldc.i4     0x622
  IL_03ee:  sub
  IL_03ef:  switch    (
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814)
  IL_0468:  ldarg.0
  IL_0469:  ldc.i4     0x647
  IL_046e:  beq        IL_0814
  IL_0473:  br         IL_0816
  IL_0478:  ldarg.0
  IL_0479:  ldc.i4     0x650
  IL_047e:  bgt.s      IL_049b
  IL_0480:  ldarg.0
  IL_0481:  ldc.i4     0x64a
  IL_0486:  beq        IL_0814
  IL_048b:  ldarg.0
  IL_048c:  ldc.i4     0x650
  IL_0491:  beq        IL_0814
  IL_0496:  br         IL_0816
  IL_049b:  ldarg.0
  IL_049c:  ldc.i4     0x661
  IL_04a1:  sub
  IL_04a2:  switch    (
  IL_0814,
  IL_0814,
  IL_0814)
  IL_04b3:  ldarg.0
  IL_04b4:  ldc.i4     0x66d
  IL_04b9:  beq        IL_0814
  IL_04be:  ldarg.0
  IL_04bf:  ldc.i4     0x67a
  IL_04c4:  beq        IL_0814
  IL_04c9:  br         IL_0816
  IL_04ce:  ldarg.0
  IL_04cf:  ldc.i4     0x6c7
  IL_04d4:  bgt        IL_05f7
  IL_04d9:  ldarg.0
  IL_04da:  ldc.i4     0x6b0
  IL_04df:  bgt        IL_05b4
  IL_04e4:  ldarg.0
  IL_04e5:  ldc.i4     0x67b
  IL_04ea:  beq        IL_0814
  IL_04ef:  ldarg.0
  IL_04f0:  ldc.i4     0x684
  IL_04f5:  sub
  IL_04f6:  switch    (
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814)
  IL_05af:  br         IL_0816
  IL_05b4:  ldarg.0
  IL_05b5:  ldc.i4     0x6b5
  IL_05ba:  sub
  IL_05bb:  switch    (
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0816,
  IL_0816,
  IL_0814)
  IL_05dc:  ldarg.0
  IL_05dd:  ldc.i4     0x6c6
  IL_05e2:  beq        IL_0814
  IL_05e7:  ldarg.0
  IL_05e8:  ldc.i4     0x6c7
  IL_05ed:  beq        IL_0814
  IL_05f2:  br         IL_0816
  IL_05f7:  ldarg.0
  IL_05f8:  ldc.i4     0x787
  IL_05fd:  bgt.s      IL_0625
  IL_05ff:  ldarg.0
  IL_0600:  ldc.i4     0x6e2
  IL_0605:  beq        IL_0814
  IL_060a:  ldarg.0
  IL_060b:  ldc.i4     0x6e5
  IL_0610:  beq        IL_0814
  IL_0615:  ldarg.0
  IL_0616:  ldc.i4     0x787
  IL_061b:  beq        IL_0814
  IL_0620:  br         IL_0816
  IL_0625:  ldarg.0
  IL_0626:  ldc.i4     0x7a4
  IL_062b:  beq        IL_0814
  IL_0630:  ldarg.0
  IL_0631:  ldc.i4     0x7a5
  IL_0636:  beq        IL_0814
  IL_063b:  ldarg.0
  IL_063c:  ldc.i4     0x7b6
  IL_0641:  beq        IL_0814
  IL_0646:  br         IL_0816
  IL_064b:  ldarg.0
  IL_064c:  ldc.i4     0xfba
  IL_0651:  bgt        IL_0779
  IL_0656:  ldarg.0
  IL_0657:  ldc.i4     0x7de
  IL_065c:  bgt.s      IL_06a7
  IL_065e:  ldarg.0
  IL_065f:  ldc.i4     0x7ce
  IL_0664:  bgt.s      IL_0681
  IL_0666:  ldarg.0
  IL_0667:  ldc.i4     0x7bd
  IL_066c:  beq        IL_0814
  IL_0671:  ldarg.0
  IL_0672:  ldc.i4     0x7ce
  IL_0677:  beq        IL_0814
  IL_067c:  br         IL_0816
  IL_0681:  ldarg.0
  IL_0682:  ldc.i4     0x7d2
  IL_0687:  beq        IL_0814
  IL_068c:  ldarg.0
  IL_068d:  ldc.i4     0x7d8
  IL_0692:  beq        IL_0814
  IL_0697:  ldarg.0
  IL_0698:  ldc.i4     0x7de
  IL_069d:  beq        IL_0814
  IL_06a2:  br         IL_0816
  IL_06a7:  ldarg.0
  IL_06a8:  ldc.i4     0x7f6
  IL_06ad:  bgt.s      IL_06d5
  IL_06af:  ldarg.0
  IL_06b0:  ldc.i4     0x7e7
  IL_06b5:  beq        IL_0814
  IL_06ba:  ldarg.0
  IL_06bb:  ldc.i4     0x7ed
  IL_06c0:  beq        IL_0814
  IL_06c5:  ldarg.0
  IL_06c6:  ldc.i4     0x7f6
  IL_06cb:  beq        IL_0814
  IL_06d0:  br         IL_0816
  IL_06d5:  ldarg.0
  IL_06d6:  ldc.i4     0xbb8
  IL_06db:  sub
  IL_06dc:  switch    (
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0814,
  IL_0816,
  IL_0814,
  IL_0814)
  IL_0751:  ldarg.0
  IL_0752:  ldc.i4     0xfae
  IL_0757:  beq        IL_0814
  IL_075c:  ldarg.0
  IL_075d:  ldc.i4     0xfb8
  IL_0762:  sub
  IL_0763:  switch    (
  IL_0814,
  IL_0814,
  IL_0814)
  IL_0774:  br         IL_0816
  IL_0779:  ldarg.0
  IL_077a:  ldc.i4     0x1b7b
  IL_077f:  bgt.s      IL_07b8
  IL_0781:  ldarg.0
  IL_0782:  ldc.i4     0x1b65
  IL_0787:  bgt.s      IL_079e
  IL_0789:  ldarg.0
  IL_078a:  ldc.i4     0x1388
  IL_078f:  beq        IL_0814
  IL_0794:  ldarg.0
  IL_0795:  ldc.i4     0x1b65
  IL_079a:  beq.s      IL_0814
  IL_079c:  br.s       IL_0816
  IL_079e:  ldarg.0
  IL_079f:  ldc.i4     0x1b6e
  IL_07a4:  beq.s      IL_0814
  IL_07a6:  ldarg.0
  IL_07a7:  ldc.i4     0x1b79
  IL_07ac:  beq.s      IL_0814
  IL_07ae:  ldarg.0
  IL_07af:  ldc.i4     0x1b7b
  IL_07b4:  beq.s      IL_0814
  IL_07b6:  br.s       IL_0816
  IL_07b8:  ldarg.0
  IL_07b9:  ldc.i4     0x1f41
  IL_07be:  bgt.s      IL_07ea
  IL_07c0:  ldarg.0
  IL_07c1:  ldc.i4     0x1ba8
  IL_07c6:  sub
  IL_07c7:  switch    (
  IL_0814,
  IL_0814,
  IL_0814)
  IL_07d8:  ldarg.0
  IL_07d9:  ldc.i4     0x1bb2
  IL_07de:  beq.s      IL_0814
  IL_07e0:  ldarg.0
  IL_07e1:  ldc.i4     0x1f41
  IL_07e6:  beq.s      IL_0814
  IL_07e8:  br.s       IL_0816
  IL_07ea:  ldarg.0
  IL_07eb:  ldc.i4     0x1f49
  IL_07f0:  beq.s      IL_0814
  IL_07f2:  ldarg.0
  IL_07f3:  ldc.i4     0x1f4c
  IL_07f8:  beq.s      IL_0814
  IL_07fa:  ldarg.0
  IL_07fb:  ldc.i4     0x2710
  IL_0800:  sub
  IL_0801:  switch    (
  IL_0814,
  IL_0814,
  IL_0814)
  IL_0812:  br.s       IL_0816
  IL_0814:  ldc.i4.1
  IL_0815:  ret
  IL_0816:  ldc.i4.0
  IL_0817:  ret
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
        int foo;        // unassigned foo

        switch (n)
        {
            case 1:
            case 2:
                foo = n;
                break;
            case 3:
            default:
                foo = 0;
                break;
        }
        
        Console.Write(foo);     // foo must be definitely assigned here        
        return foo;
    }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "0");
            compVerifier.VerifyIL("SwitchTest.Main", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (int V_0, //n
  int V_1) //foo
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  switch    (
  IL_0018,
  IL_0018,
  IL_001c)
  IL_0016:  br.s       IL_001c
  IL_0018:  ldloc.0
  IL_0019:  stloc.1
  IL_001a:  br.s       IL_001e
  IL_001c:  ldc.i4.0
  IL_001d:  stloc.1
  IL_001e:  ldloc.1
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ldloc.1
  IL_0025:  ret
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
        int foo;        // unassigned foo

        switch (n)
        {
            case 1:
                cost = 1;
                foo = n;
                break;
            case 2:
                cost = 2;
                goto case 1;
            case 3:
                cost = 3;
                if(cost > n)
                {
                    foo = n - 1;
                }
                else
                {
                    goto case 2;
                }
                break;

            default:
                cost = 4;
                foo = n - 1;
                break;
        }

        if (foo != n)       // foo must be reported as definitely assigned
        {
            Console.Write(foo);
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
@"
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (int V_0, //n
  int V_1, //cost
  int V_2) //foo
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
}
"
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
        int foo;        // unassigned foo
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
                    foo = 0;
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

        Console.Write(foo);    // foo should be definitely assigned here
        return foo;
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
                    .locals init (int V_0) //foo
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
    static void Foo(DayOfWeek x)
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

            compVerifier.VerifyIL("A.Foo", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  bne.un.s   IL_0006
  IL_0004:  br.s       IL_0004
  IL_0006:  ret
}
"
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
                }"
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
            // (11,8): warning CS0162: Unreachable code detected
            // 			    ret = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ret"));

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
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ret"));

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
            // (6,17): warning CS1522: Empty switch block
            // 		switch (true) {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{"));

            compVerifier.VerifyIL("Test.Main", @"
                {
                    // Code size       10 (0xa)
                    .maxstack  1
                    .locals init (int V_0) //ret
                    IL_0000:  ldc.i4.0
                    IL_0001:  stloc.0
                    IL_0002:  ldloc.0
                    IL_0003:  call       ""void System.Console.Write(int)""
                    IL_0008:  ldc.i4.0
                    IL_0009:  ret
                }"
            );
        }

        [WorkItem(913556, "DevDiv")]
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

            var comp = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size      328 (0x148)
  .maxstack  2
  .locals init (string V_0,
  uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldstr      ""A""
  IL_000a:  call       ""bool string.op_Equality(string, string)""
  IL_000f:  brfalse.s  IL_0017
  IL_0011:  ldc.i4.1
  IL_0012:  call       ""void System.Console.Write(int)""
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.1
  IL_0019:  ldelem.ref
  IL_001a:  stloc.0
  IL_001b:  ldloc.0
  IL_001c:  call       ""ComputeStringHash""
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0xc30bf539
  IL_0028:  bgt.un.s   IL_0057
  IL_002a:  ldloc.1
  IL_002b:  ldc.i4     0xc10bf213
  IL_0030:  bgt.un.s   IL_0043
  IL_0032:  ldloc.1
  IL_0033:  ldc.i4     0xc00bf080
  IL_0038:  beq.s      IL_00b3
  IL_003a:  ldloc.1
  IL_003b:  ldc.i4     0xc10bf213
  IL_0040:  beq.s      IL_00a5
  IL_0042:  ret
  IL_0043:  ldloc.1
  IL_0044:  ldc.i4     0xc20bf3a6
  IL_0049:  beq        IL_00cf
  IL_004e:  ldloc.1
  IL_004f:  ldc.i4     0xc30bf539
  IL_0054:  beq.s      IL_00c1
  IL_0056:  ret
  IL_0057:  ldloc.1
  IL_0058:  ldc.i4     0xc70bfb85
  IL_005d:  bgt.un.s   IL_0070
  IL_005f:  ldloc.1
  IL_0060:  ldc.i4     0xc60bf9f2
  IL_0065:  beq.s      IL_0097
  IL_0067:  ldloc.1
  IL_0068:  ldc.i4     0xc70bfb85
  IL_006d:  beq.s      IL_0089
  IL_006f:  ret
  IL_0070:  ldloc.1
  IL_0071:  ldc.i4     0xcc0c0364
  IL_0076:  beq.s      IL_00eb
  IL_0078:  ldloc.1
  IL_0079:  ldc.i4     0xcd0c04f7
  IL_007e:  beq.s      IL_00dd
  IL_0080:  ldloc.1
  IL_0081:  ldc.i4     0xcf0c081d
  IL_0086:  beq.s      IL_00f9
  IL_0088:  ret
  IL_0089:  ldloc.0
  IL_008a:  ldstr      ""B""
  IL_008f:  call       ""bool string.op_Equality(string, string)""
  IL_0094:  brtrue.s   IL_0107
  IL_0096:  ret
  IL_0097:  ldloc.0
  IL_0098:  ldstr      ""C""
  IL_009d:  call       ""bool string.op_Equality(string, string)""
  IL_00a2:  brtrue.s   IL_010e
  IL_00a4:  ret
  IL_00a5:  ldloc.0
  IL_00a6:  ldstr      ""D""
  IL_00ab:  call       ""bool string.op_Equality(string, string)""
  IL_00b0:  brtrue.s   IL_0115
  IL_00b2:  ret
  IL_00b3:  ldloc.0
  IL_00b4:  ldstr      ""E""
  IL_00b9:  call       ""bool string.op_Equality(string, string)""
  IL_00be:  brtrue.s   IL_011c
  IL_00c0:  ret
  IL_00c1:  ldloc.0
  IL_00c2:  ldstr      ""F""
  IL_00c7:  call       ""bool string.op_Equality(string, string)""
  IL_00cc:  brtrue.s   IL_0123
  IL_00ce:  ret
  IL_00cf:  ldloc.0
  IL_00d0:  ldstr      ""G""
  IL_00d5:  call       ""bool string.op_Equality(string, string)""
  IL_00da:  brtrue.s   IL_012a
  IL_00dc:  ret
  IL_00dd:  ldloc.0
  IL_00de:  ldstr      ""H""
  IL_00e3:  call       ""bool string.op_Equality(string, string)""
  IL_00e8:  brtrue.s   IL_0131
  IL_00ea:  ret
  IL_00eb:  ldloc.0
  IL_00ec:  ldstr      ""I""
  IL_00f1:  call       ""bool string.op_Equality(string, string)""
  IL_00f6:  brtrue.s   IL_0138
  IL_00f8:  ret
  IL_00f9:  ldloc.0
  IL_00fa:  ldstr      ""J""
  IL_00ff:  call       ""bool string.op_Equality(string, string)""
  IL_0104:  brtrue.s   IL_0140
  IL_0106:  ret
  IL_0107:  ldc.i4.2
  IL_0108:  call       ""void System.Console.Write(int)""
  IL_010d:  ret
  IL_010e:  ldc.i4.3
  IL_010f:  call       ""void System.Console.Write(int)""
  IL_0114:  ret
  IL_0115:  ldc.i4.4
  IL_0116:  call       ""void System.Console.Write(int)""
  IL_011b:  ret
  IL_011c:  ldc.i4.5
  IL_011d:  call       ""void System.Console.Write(int)""
  IL_0122:  ret
  IL_0123:  ldc.i4.6
  IL_0124:  call       ""void System.Console.Write(int)""
  IL_0129:  ret
  IL_012a:  ldc.i4.7
  IL_012b:  call       ""void System.Console.Write(int)""
  IL_0130:  ret
  IL_0131:  ldc.i4.8
  IL_0132:  call       ""void System.Console.Write(int)""
  IL_0137:  ret
  IL_0138:  ldc.i4.s   9
  IL_013a:  call       ""void System.Console.Write(int)""
  IL_013f:  ret
  IL_0140:  ldc.i4.s   10
  IL_0142:  call       ""void System.Console.Write(int)""
  IL_0147:  ret
}
");
        }

        [WorkItem(634404, "DevDiv")]
        [WorkItem(913556, "DevDiv")]
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

            var comp = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            // With special members available, we use a hashtable approach.
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size      307 (0x133)
  .maxstack  2
  .locals init (string V_0,
  uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  call       ""ComputeStringHash""
  IL_000a:  stloc.1
  IL_000b:  ldloc.1
  IL_000c:  ldc.i4     0xc30bf539
  IL_0011:  bgt.un.s   IL_0043
  IL_0013:  ldloc.1
  IL_0014:  ldc.i4     0xc10bf213
  IL_0019:  bgt.un.s   IL_002f
  IL_001b:  ldloc.1
  IL_001c:  ldc.i4     0xc00bf080
  IL_0021:  beq        IL_00ad
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4     0xc10bf213
  IL_002c:  beq.s      IL_009f
  IL_002e:  ret
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4     0xc20bf3a6
  IL_0035:  beq        IL_00c9
  IL_003a:  ldloc.1
  IL_003b:  ldc.i4     0xc30bf539
  IL_0040:  beq.s      IL_00bb
  IL_0042:  ret
  IL_0043:  ldloc.1
  IL_0044:  ldc.i4     0xc60bf9f2
  IL_0049:  bgt.un.s   IL_005c
  IL_004b:  ldloc.1
  IL_004c:  ldc.i4     0xc40bf6cc
  IL_0051:  beq.s      IL_0075
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4     0xc60bf9f2
  IL_0059:  beq.s      IL_0091
  IL_005b:  ret
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4     0xc70bfb85
  IL_0062:  beq.s      IL_0083
  IL_0064:  ldloc.1
  IL_0065:  ldc.i4     0xcc0c0364
  IL_006a:  beq.s      IL_00e5
  IL_006c:  ldloc.1
  IL_006d:  ldc.i4     0xcd0c04f7
  IL_0072:  beq.s      IL_00d7
  IL_0074:  ret
  IL_0075:  ldloc.0
  IL_0076:  ldstr      ""A""
  IL_007b:  call       ""bool string.op_Equality(string, string)""
  IL_0080:  brtrue.s   IL_00f3
  IL_0082:  ret
  IL_0083:  ldloc.0
  IL_0084:  ldstr      ""B""
  IL_0089:  call       ""bool string.op_Equality(string, string)""
  IL_008e:  brtrue.s   IL_00fa
  IL_0090:  ret
  IL_0091:  ldloc.0
  IL_0092:  ldstr      ""C""
  IL_0097:  call       ""bool string.op_Equality(string, string)""
  IL_009c:  brtrue.s   IL_0101
  IL_009e:  ret
  IL_009f:  ldloc.0
  IL_00a0:  ldstr      ""D""
  IL_00a5:  call       ""bool string.op_Equality(string, string)""
  IL_00aa:  brtrue.s   IL_0108
  IL_00ac:  ret
  IL_00ad:  ldloc.0
  IL_00ae:  ldstr      ""E""
  IL_00b3:  call       ""bool string.op_Equality(string, string)""
  IL_00b8:  brtrue.s   IL_010f
  IL_00ba:  ret
  IL_00bb:  ldloc.0
  IL_00bc:  ldstr      ""F""
  IL_00c1:  call       ""bool string.op_Equality(string, string)""
  IL_00c6:  brtrue.s   IL_0116
  IL_00c8:  ret
  IL_00c9:  ldloc.0
  IL_00ca:  ldstr      ""G""
  IL_00cf:  call       ""bool string.op_Equality(string, string)""
  IL_00d4:  brtrue.s   IL_011d
  IL_00d6:  ret
  IL_00d7:  ldloc.0
  IL_00d8:  ldstr      ""H""
  IL_00dd:  call       ""bool string.op_Equality(string, string)""
  IL_00e2:  brtrue.s   IL_0124
  IL_00e4:  ret
  IL_00e5:  ldloc.0
  IL_00e6:  ldstr      ""I""
  IL_00eb:  call       ""bool string.op_Equality(string, string)""
  IL_00f0:  brtrue.s   IL_012b
  IL_00f2:  ret
  IL_00f3:  ldc.i4.1
  IL_00f4:  call       ""void System.Console.Write(int)""
  IL_00f9:  ret
  IL_00fa:  ldc.i4.2
  IL_00fb:  call       ""void System.Console.Write(int)""
  IL_0100:  ret
  IL_0101:  ldc.i4.3
  IL_0102:  call       ""void System.Console.Write(int)""
  IL_0107:  ret
  IL_0108:  ldc.i4.4
  IL_0109:  call       ""void System.Console.Write(int)""
  IL_010e:  ret
  IL_010f:  ldc.i4.5
  IL_0110:  call       ""void System.Console.Write(int)""
  IL_0115:  ret
  IL_0116:  ldc.i4.6
  IL_0117:  call       ""void System.Console.Write(int)""
  IL_011c:  ret
  IL_011d:  ldc.i4.7
  IL_011e:  call       ""void System.Console.Write(int)""
  IL_0123:  ret
  IL_0124:  ldc.i4.8
  IL_0125:  call       ""void System.Console.Write(int)""
  IL_012a:  ret
  IL_012b:  ldc.i4.s   9
  IL_012d:  call       ""void System.Console.Write(int)""
  IL_0132:  ret
}
");

            comp = CreateCompilationWithMscorlib(text);
            comp.MakeMemberMissing(SpecialMember.System_String__Chars);

            // Can't use the hash version when String.Chars is unavailable.
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size      186 (0xba)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldstr      ""A""
  IL_000a:  call       ""bool string.op_Equality(string, string)""
  IL_000f:  brtrue.s   IL_007a
  IL_0011:  ldloc.0
  IL_0012:  ldstr      ""B""
  IL_0017:  call       ""bool string.op_Equality(string, string)""
  IL_001c:  brtrue.s   IL_0081
  IL_001e:  ldloc.0
  IL_001f:  ldstr      ""C""
  IL_0024:  call       ""bool string.op_Equality(string, string)""
  IL_0029:  brtrue.s   IL_0088
  IL_002b:  ldloc.0
  IL_002c:  ldstr      ""D""
  IL_0031:  call       ""bool string.op_Equality(string, string)""
  IL_0036:  brtrue.s   IL_008f
  IL_0038:  ldloc.0
  IL_0039:  ldstr      ""E""
  IL_003e:  call       ""bool string.op_Equality(string, string)""
  IL_0043:  brtrue.s   IL_0096
  IL_0045:  ldloc.0
  IL_0046:  ldstr      ""F""
  IL_004b:  call       ""bool string.op_Equality(string, string)""
  IL_0050:  brtrue.s   IL_009d
  IL_0052:  ldloc.0
  IL_0053:  ldstr      ""G""
  IL_0058:  call       ""bool string.op_Equality(string, string)""
  IL_005d:  brtrue.s   IL_00a4
  IL_005f:  ldloc.0
  IL_0060:  ldstr      ""H""
  IL_0065:  call       ""bool string.op_Equality(string, string)""
  IL_006a:  brtrue.s   IL_00ab
  IL_006c:  ldloc.0
  IL_006d:  ldstr      ""I""
  IL_0072:  call       ""bool string.op_Equality(string, string)""
  IL_0077:  brtrue.s   IL_00b2
  IL_0079:  ret
  IL_007a:  ldc.i4.1
  IL_007b:  call       ""void System.Console.Write(int)""
  IL_0080:  ret
  IL_0081:  ldc.i4.2
  IL_0082:  call       ""void System.Console.Write(int)""
  IL_0087:  ret
  IL_0088:  ldc.i4.3
  IL_0089:  call       ""void System.Console.Write(int)""
  IL_008e:  ret
  IL_008f:  ldc.i4.4
  IL_0090:  call       ""void System.Console.Write(int)""
  IL_0095:  ret
  IL_0096:  ldc.i4.5
  IL_0097:  call       ""void System.Console.Write(int)""
  IL_009c:  ret
  IL_009d:  ldc.i4.6
  IL_009e:  call       ""void System.Console.Write(int)""
  IL_00a3:  ret
  IL_00a4:  ldc.i4.7
  IL_00a5:  call       ""void System.Console.Write(int)""
  IL_00aa:  ret
  IL_00ab:  ldc.i4.8
  IL_00ac:  call       ""void System.Console.Write(int)""
  IL_00b1:  ret
  IL_00b2:  ldc.i4.s   9
  IL_00b4:  call       ""void System.Console.Write(int)""
  IL_00b9:  ret
}
");
        }

        [WorkItem(947580, "DevDiv")]
        [Fact]
        public void Regress947580()
        {
            var text = @"
using System;

class Program {
    static string boo(int i) {
        switch (i) {
            case 42:
                var x = ""foo"";
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
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //x
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  bne.un.s   IL_001a
  IL_0005:  ldstr      ""foo""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldstr      ""bar""
  IL_0011:  call       ""bool string.op_Inequality(string, string)""
  IL_0016:  brtrue.s   IL_001a
  IL_0018:  ldloc.0
  IL_0019:  ret
  IL_001a:  ldnull
  IL_001b:  ret
}
"
            );
        }

        [WorkItem(947580, "DevDiv")]
        [Fact]
        public void Regress947580a()
        {
            var text = @"
using System;

class Program {
    static string boo(int i) {
        switch (i) {
            case 42:
                var x = ""foo"";
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
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  bne.un.s   IL_0015
  IL_0005:  ldstr      ""foo""
  IL_000a:  ldstr      ""bar""
  IL_000f:  call       ""bool string.op_Inequality(string, string)""
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ret
}
"
            );
        }

        [WorkItem(1035228, "DevDiv")]
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
                var x = ""foo"";
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
@"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.s   42
  IL_0007:  bne.un.s   IL_001c
  IL_0009:  ldstr      ""foo""
  IL_000e:  ldstr      ""bar""
  IL_0013:  call       ""bool string.op_Inequality(string, string)""
  IL_0018:  brfalse.s  IL_001c
  IL_001a:  ldc.i4.0
  IL_001b:  ret
  IL_001c:  ldc.i4.1
  IL_001d:  ret
}
"
            );
        }

        [WorkItem(1035228, "DevDiv")]
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
                var x = ""foo"";
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
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.s   42
  IL_0005:  bne.un.s   IL_001a
  IL_0007:  ldstr      ""foo""
  IL_000c:  ldstr      ""bar""
  IL_0011:  call       ""bool string.op_Inequality(string, string)""
  IL_0016:  brfalse.s  IL_001a
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldc.i4.1
  IL_001b:  ret
}
"
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
  // Code size       96 (0x60)
  .maxstack  2
  .locals init (int? V_0, //i
                int V_1,
                int? V_2,
                int? V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""int?..ctor(int)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool int?.HasValue.get""
  IL_000f:  brfalse.s  IL_001b
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""int int?.GetValueOrDefault()""
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_002e
  IL_001b:  ldstr      ""In Null case""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldc.i4.1
  IL_0028:  call       ""int?..ctor(int)""
  IL_002d:  ret
  IL_002e:  ldstr      ""In DEFAULT case""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ldloc.0
  IL_0039:  stloc.2
  IL_003a:  ldc.i4.2
  IL_003b:  stloc.1
  IL_003c:  ldloca.s   V_2
  IL_003e:  call       ""bool int?.HasValue.get""
  IL_0043:  brtrue.s   IL_0050
  IL_0045:  ldloca.s   V_3
  IL_0047:  initobj    ""int?""
  IL_004d:  ldloc.3
  IL_004e:  br.s       IL_005e
  IL_0050:  ldloca.s   V_2
  IL_0052:  call       ""int int?.GetValueOrDefault()""
  IL_0057:  ldloc.1
  IL_0058:  add
  IL_0059:  newobj     ""int?..ctor(int)""
  IL_005e:  stloc.0
  IL_005f:  ret
}
"
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
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (string V_0) //i
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""1""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldstr      ""In Null case""
  IL_0010:  call       ""void System.Console.WriteLine(string)""
  IL_0015:  ret
  IL_0016:  ldstr      ""In DEFAULT case""
  IL_001b:  call       ""void System.Console.WriteLine(string)""
  IL_0020:  ret
}
"
            );
        }


        #endregion
    }
}
