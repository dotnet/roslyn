// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenUtf8Tests : CSharpTestBase
    {
        [Fact]
        public void TestUtf8Literal001()
        {
            var source = @"

class Program
{
    private static System.Text.Utf8.Utf8String ut8;
    static void Main()
    {
        ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "hello utf8");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  3
  IL_0000:  ldc.i4.s   10
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.329D7DB2011B5BC1330304286AC52613D8603CDA""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  newobj     ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_0017:  stsfld     ""System.Text.Utf8.Utf8String Program.ut8""
  IL_001c:  ldsflda    ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0021:  constrained. ""System.Text.Utf8.Utf8String""
  IL_0027:  callvirt   ""string object.ToString()""
  IL_002c:  call       ""void System.Console.WriteLine(string)""
  IL_0031:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal001i()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "hello utf8");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (System.Text.Utf8.Utf8String V_0) //ut8
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   10
  IL_0004:  newarr     ""byte""
  IL_0009:  dup
  IL_000a:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.329D7DB2011B5BC1330304286AC52613D8603CDA""
  IL_000f:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0014:  call       ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_0019:  ldloca.s   V_0
  IL_001b:  constrained. ""System.Text.Utf8.Utf8String""
  IL_0021:  callvirt   ""string object.ToString()""
  IL_0026:  call       ""void System.Console.WriteLine(string)""
  IL_002b:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal002
    ()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
    // (7,19): error CS0518: Predefined type 'System.Text.Utf8.Utf8String' is not defined or imported
    //         var ut8 = "hello utf8"u8;
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello utf8""u8").WithArguments("System.Text.Utf8.Utf8String").WithLocation(7, 19),
    // (7,19): error CS0656: Missing compiler required member 'System.Text.Utf8.Utf8String..ctor'
    //         var ut8 = "hello utf8"u8;
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""hello utf8""u8").WithArguments("System.Text.Utf8.Utf8String", ".ctor").WithLocation(7, 19)
);
        }

        [Fact]
        public void TestUtf8Literal003()
        {
            var source = @"

class Program
{
    private static System.Text.Utf8.Utf8String ut8;
    static void Main()
    {
        ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "hello utf8");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.329D7DB2011B5BC1330304286AC52613D8603CDA""
  IL_0005:  ldc.i4.s   10
  IL_0007:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_000c:  stsfld     ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0011:  ldsflda    ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0016:  constrained. ""System.Text.Utf8.Utf8String""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal003i()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "hello utf8");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (System.Text.Utf8.Utf8String V_0) //ut8
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.329D7DB2011B5BC1330304286AC52613D8603CDA""
  IL_0007:  ldc.i4.s   10
  IL_0009:  call       ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_000e:  ldloca.s   V_0
  IL_0010:  constrained. ""System.Text.Utf8.Utf8String""
  IL_0016:  callvirt   ""string object.ToString()""
  IL_001b:  call       ""void System.Console.WriteLine(string)""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal004()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = """"u8;
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (System.Text.Utf8.Utf8String V_0) //ut8
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  newarr     ""byte""
  IL_0008:  call       ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_000d:  ldloca.s   V_0
  IL_000f:  constrained. ""System.Text.Utf8.Utf8String""
  IL_0015:  callvirt   ""string object.ToString()""
  IL_001a:  call       ""void System.Console.WriteLine(string)""
  IL_001f:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal005()
        {
            var source = @"

class Program
{
    private static System.Text.Utf8.Utf8String ut8;
    static void Main()
    {
        ut8 = new System.Text.Utf8.Utf8String(new byte[]{40, 41, 42, 43});
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "()*+");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldtoken    ""int <PrivateImplementationDetails>.98DD58387B215F552C9B542371285C87F8275069""
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_000b:  stsfld     ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0010:  ldsflda    ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0015:  constrained. ""System.Text.Utf8.Utf8String""
  IL_001b:  callvirt   ""string object.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal005i()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = new System.Text.Utf8.Utf8String(new byte[]{40, 41, 42, 43});
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "()*+");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (System.Text.Utf8.Utf8String V_0) //ut8
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldtoken    ""int <PrivateImplementationDetails>.98DD58387B215F552C9B542371285C87F8275069""
  IL_0007:  ldc.i4.4
  IL_0008:  call       ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_000d:  ldloca.s   V_0
  IL_000f:  constrained. ""System.Text.Utf8.Utf8String""
  IL_0015:  callvirt   ""string object.ToString()""
  IL_001a:  call       ""void System.Console.WriteLine(string)""
  IL_001f:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal006()
        {
            var source = @"

class Program
{
    private static byte notConst = 42;
    private static System.Text.Utf8.Utf8String ut8;

    static void Main()
    {
        ut8 = new System.Text.Utf8.Utf8String(new byte[]{40, 41, notConst, 43});
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "()*+");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""int <PrivateImplementationDetails>.98A9B5A70472E5BCDFB5F6AC4B9AFEF961505AD0""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  ldsfld     ""byte Program.notConst""
  IL_0018:  stelem.i1
  IL_0019:  newobj     ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_001e:  stsfld     ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0023:  ldsflda    ""System.Text.Utf8.Utf8String Program.ut8""
  IL_0028:  constrained. ""System.Text.Utf8.Utf8String""
  IL_002e:  callvirt   ""string object.ToString()""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ret
}
");
        }
        [Fact]

        public void TestUtf8Literal006i()
        {
            var source = @"

class Program
{
    private static byte notConst = 42;

    static void Main()
    {
        var ut8 = new System.Text.Utf8.Utf8String(new byte[]{40, 41, notConst, 43});
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "()*+");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  5
  .locals init (System.Text.Utf8.Utf8String V_0) //ut8
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.4
  IL_0003:  newarr     ""byte""
  IL_0008:  dup
  IL_0009:  ldtoken    ""int <PrivateImplementationDetails>.98A9B5A70472E5BCDFB5F6AC4B9AFEF961505AD0""
  IL_000e:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  ldsfld     ""byte Program.notConst""
  IL_001a:  stelem.i1
  IL_001b:  call       ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_0020:  ldloca.s   V_0
  IL_0022:  constrained. ""System.Text.Utf8.Utf8String""
  IL_0028:  callvirt   ""string object.ToString()""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
}
");
        }

        public void TestUtf8LiteralArray()
        {
            var source = @"

class Program
{
        public static object[][] LengthTestCases = new object[][] {
            new object[] { 0, """"u8 },
            new object[] { 4, ""1258""u8 },
            new object[] { 3, ""\uABCD""u8 },
            new object[] { 3, ""\uABEE""u8 },
            new object[] { 4, ""a\uABEE""u8 },
            new object[] { 5, ""a\uABEEa""u8 },
            new object[] { 8, ""a\uABEE\uABCDa""u8 }
        };

        static void Main()
    {
        foreach(var s in LengthTestCases)
        {
            foreach(var o in s)
            {
                System.Console.Write(o);
                System.Console.Write(""__""u8);
            }
            System.Console.WriteLine();
        }
    }
}

namespace System.Text.Utf8
{
    struct Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public Utf8String(RuntimeFieldHandle utf8Data, int length) 
            : this(CreateArrayFromFieldHandle(utf8Data, length))
        {
        }

        static byte[] CreateArrayFromFieldHandle(RuntimeFieldHandle utf8Data, int length)
        {
            var array = new byte[length];
            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array, utf8Data);
            return array;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput:@"
0____
4__1258__
3__ꯍ__
3__꯮__
4__a꯮__
5__a꯮a__
8__a꯮ꯍa__");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program..cctor", @"
{
  // Code size      271 (0x10f)
  .maxstack  8
  IL_0000:  ldc.i4.7
  IL_0001:  newarr     ""object[]""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.2
  IL_0009:  newarr     ""object""
  IL_000e:  dup
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.0
  IL_0011:  box        ""int""
  IL_0016:  stelem.ref
  IL_0017:  dup
  IL_0018:  ldc.i4.1
  IL_0019:  ldc.i4.0
  IL_001a:  newarr     ""byte""
  IL_001f:  newobj     ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_0024:  box        ""System.Text.Utf8.Utf8String""
  IL_0029:  stelem.ref
  IL_002a:  stelem.ref
  IL_002b:  dup
  IL_002c:  ldc.i4.1
  IL_002d:  ldc.i4.2
  IL_002e:  newarr     ""object""
  IL_0033:  dup
  IL_0034:  ldc.i4.0
  IL_0035:  ldc.i4.4
  IL_0036:  box        ""int""
  IL_003b:  stelem.ref
  IL_003c:  dup
  IL_003d:  ldc.i4.1
  IL_003e:  ldtoken    ""int <PrivateImplementationDetails>.CC7B8755A2A153285A26A7568C30B88A27217F0F""
  IL_0043:  ldc.i4.4
  IL_0044:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_0049:  box        ""System.Text.Utf8.Utf8String""
  IL_004e:  stelem.ref
  IL_004f:  stelem.ref
  IL_0050:  dup
  IL_0051:  ldc.i4.2
  IL_0052:  ldc.i4.2
  IL_0053:  newarr     ""object""
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldc.i4.3
  IL_005b:  box        ""int""
  IL_0060:  stelem.ref
  IL_0061:  dup
  IL_0062:  ldc.i4.1
  IL_0063:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.4D8B9EC8201EC2A98722C46B12494E1AF71C4CFE""
  IL_0068:  ldc.i4.3
  IL_0069:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_006e:  box        ""System.Text.Utf8.Utf8String""
  IL_0073:  stelem.ref
  IL_0074:  stelem.ref
  IL_0075:  dup
  IL_0076:  ldc.i4.3
  IL_0077:  ldc.i4.2
  IL_0078:  newarr     ""object""
  IL_007d:  dup
  IL_007e:  ldc.i4.0
  IL_007f:  ldc.i4.3
  IL_0080:  box        ""int""
  IL_0085:  stelem.ref
  IL_0086:  dup
  IL_0087:  ldc.i4.1
  IL_0088:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.ACDFE4250914F3F325B93791AA77D3EDFB89AAD9""
  IL_008d:  ldc.i4.3
  IL_008e:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_0093:  box        ""System.Text.Utf8.Utf8String""
  IL_0098:  stelem.ref
  IL_0099:  stelem.ref
  IL_009a:  dup
  IL_009b:  ldc.i4.4
  IL_009c:  ldc.i4.2
  IL_009d:  newarr     ""object""
  IL_00a2:  dup
  IL_00a3:  ldc.i4.0
  IL_00a4:  ldc.i4.4
  IL_00a5:  box        ""int""
  IL_00aa:  stelem.ref
  IL_00ab:  dup
  IL_00ac:  ldc.i4.1
  IL_00ad:  ldtoken    ""int <PrivateImplementationDetails>.D6E4FED9D7711DCF7F7906DD3081903F9E3F74E5""
  IL_00b2:  ldc.i4.4
  IL_00b3:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_00b8:  box        ""System.Text.Utf8.Utf8String""
  IL_00bd:  stelem.ref
  IL_00be:  stelem.ref
  IL_00bf:  dup
  IL_00c0:  ldc.i4.5
  IL_00c1:  ldc.i4.2
  IL_00c2:  newarr     ""object""
  IL_00c7:  dup
  IL_00c8:  ldc.i4.0
  IL_00c9:  ldc.i4.5
  IL_00ca:  box        ""int""
  IL_00cf:  stelem.ref
  IL_00d0:  dup
  IL_00d1:  ldc.i4.1
  IL_00d2:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.7F1F7694457423F4AB357000C508CBC1612472F8""
  IL_00d7:  ldc.i4.5
  IL_00d8:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_00dd:  box        ""System.Text.Utf8.Utf8String""
  IL_00e2:  stelem.ref
  IL_00e3:  stelem.ref
  IL_00e4:  dup
  IL_00e5:  ldc.i4.6
  IL_00e6:  ldc.i4.2
  IL_00e7:  newarr     ""object""
  IL_00ec:  dup
  IL_00ed:  ldc.i4.0
  IL_00ee:  ldc.i4.8
  IL_00ef:  box        ""int""
  IL_00f4:  stelem.ref
  IL_00f5:  dup
  IL_00f6:  ldc.i4.1
  IL_00f7:  ldtoken    ""long <PrivateImplementationDetails>.747B78487FF58CA6FF0331B84A428BB43180ED2F""
  IL_00fc:  ldc.i4.8
  IL_00fd:  newobj     ""System.Text.Utf8.Utf8String..ctor(System.RuntimeFieldHandle, int)""
  IL_0102:  box        ""System.Text.Utf8.Utf8String""
  IL_0107:  stelem.ref
  IL_0108:  stelem.ref
  IL_0109:  stsfld     ""object[][] Program.LengthTestCases""
  IL_010e:  ret
}
");
        }

    }
}
