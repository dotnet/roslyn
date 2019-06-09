// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RefExtensionMethodsTests : CSharpTestBase
    {
        [Fact]
        public void ExtensionMethods_RValues_Ref_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        5.PrintValue();
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (13,9): error CS1510: A ref or out value must be an assignable variable
                //         5.PrintValue();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "5").WithLocation(13, 9));
        }

        [Fact]
        [WorkItem(25862, "https://github.com/dotnet/roslyn/issues/25862")]
        public void ExtensionMethods_StructCollectionInitializer()
        {
            var code = @"
public struct MyStruct : System.Collections.IEnumerable
{
    public int i;
    public System.Collections.IEnumerator GetEnumerator() => throw new System.NotImplementedException();
}

public static class MyStructExtension
{
    public static void Add(ref this MyStruct s, int i)
    {
        s.i += i;
    }
}

public static class Program
{
    public static void Main()
    {
        var s = new MyStruct { 1 };
        System.Console.Write(s.i);
    }
}";
            CompileAndVerify(code, expectedOutput: "1");
        }

        [Fact]
        [WorkItem(25862, "https://github.com/dotnet/roslyn/issues/25862")]
        public void ExtensionMethods_StructCollectionInitializerInParam()
        {
            var code = @"
public struct MyStruct : System.Collections.IEnumerable
{
    public int i;
    public System.Collections.IEnumerator GetEnumerator() => throw new System.NotImplementedException();
}

public static class MyStructExtension
{
    public static void Add(ref this MyStruct s, in MyStruct other)
    {
        s.i += other.i;
    }
}

public static class Program
{
    public static void Main()
    {
        var other = new MyStruct { i = 2 };
        var s = new MyStruct { other };
        System.Console.Write(s.i);
    }
}";
            var verifier = CompileAndVerify(code, expectedOutput: "2");
            verifier.VerifyIL("Program.Main", @"{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (MyStruct V_0, //other
                MyStruct V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""MyStruct""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int MyStruct.i""
  IL_0010:  ldloc.1
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_1
  IL_0014:  initobj    ""MyStruct""
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""void MyStructExtension.Add(ref MyStruct, in MyStruct)""
  IL_0023:  ldloc.1
  IL_0024:  ldfld      ""int MyStruct.i""
  IL_0029:  call       ""void System.Console.Write(int)""
  IL_002e:  ret
}");
        }

        [Fact]
        [WorkItem(25862, "https://github.com/dotnet/roslyn/issues/25862")]
        public void ExtensionMethods_StructCollectionInitializerInParamImplicitTempArg()
        {
            var code = @"
public struct MyStruct : System.Collections.IEnumerable
{
    public int i;
    public System.Collections.IEnumerator GetEnumerator() => throw new System.NotImplementedException();
}

public static class MyStructExtension
{
    public static void Add(ref this MyStruct s, in MyStruct other)
    {
        s.i += other.i;
    }
}

public static class Program
{
    public static void Main()
    {
        var s = new MyStruct { new MyStruct { i = 2 } };
        System.Console.Write(s.i);
    }
}";
            var verifier = CompileAndVerify(code, expectedOutput: "2");
            verifier.VerifyIL("Program.Main", @"{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (MyStruct V_0,
                MyStruct V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""MyStruct""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldloca.s   V_1
  IL_000c:  initobj    ""MyStruct""
  IL_0012:  ldloca.s   V_1
  IL_0014:  ldc.i4.2
  IL_0015:  stfld      ""int MyStruct.i""
  IL_001a:  ldloca.s   V_1
  IL_001c:  call       ""void MyStructExtension.Add(ref MyStruct, in MyStruct)""
  IL_0021:  ldloc.0
  IL_0022:  ldfld      ""int MyStruct.i""
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ret
}");
        }

        [Fact]
        [WorkItem(25862, "https://github.com/dotnet/roslyn/issues/25862")]
        public void ExtensionMethods_StructCollectionInitializerRefThisRefElement()
        {
            var code = @"
public struct MyStruct : System.Collections.IEnumerable
{
    public int i;
    public System.Collections.IEnumerator GetEnumerator() => throw new System.NotImplementedException();
}

public static class MyStructExtension
{
    public static void Add(ref this MyStruct s, ref int i)
    {
        s.i += i;
    }
}

public static class Program
{
    public static void Main()
    {
        var s = new MyStruct { 1 };
        System.Console.Write(s.i);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (20,32): error CS1954: The best overloaded method match 'MyStructExtension.Add(ref MyStruct, ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var s = new MyStruct { 1 };
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("MyStructExtension.Add(ref MyStruct, ref int)").WithLocation(20, 32));
        }

        [Fact]
        [WorkItem(25862, "https://github.com/dotnet/roslyn/issues/25862")]
        public void ExtensionMethods_StructCollectionInitializerInThisRefElement()
        {
            var code = @"
public struct MyStruct : System.Collections.IEnumerable
{
    public int i;
    public System.Collections.IEnumerator GetEnumerator() => throw new System.NotImplementedException();
}

public static class MyStructExtension
{
    public static void Add(in this MyStruct s, ref int i)
    {
    }
}

public static class Program
{
    public static void Main()
    {
        var s = new MyStruct { 1 };
        System.Console.Write(s.i);
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (19,32): error CS1954: The best overloaded method match 'MyStructExtension.Add(in MyStruct, ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var s = new MyStruct { 1 };
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("MyStructExtension.Add(in MyStruct, ref int)").WithLocation(19, 32));
        }

        [Fact]
        public void ExtensionMethods_LValues_Ref_Allowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref this int p)
    {
        System.Console.Write(++p);
    }
}
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CompileAndVerify(code, expectedOutput: "67");
        }

        [Fact]
        public void ExtensionMethods_RValues_In_Allowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(in this int p)
    {
        System.Console.Write(p);
    }
}
public static class Program
{
    public static void Main()
    {
        5.PrintValue();
        Extensions.PrintValue(5);
    }
}";

            CompileAndVerify(code, expectedOutput: "55");
        }

        [Fact]
        public void ExtensionMethods_LValues_In_Allowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(in this int p)
    {
        System.Console.Write(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CompileAndVerify(code, expectedOutput: "55");
        }

        [Fact]
        public void ExtensionMethods_NullConditionalOperator_Ref_NotAllowed()
        {
            var code = @"
public struct TestType
{
    public int GetValue() => 0;
}
public static class Extensions
{
    public static void Call(ref this TestType obj)
    {
        var value1 = obj?.GetValue();        // This should be an error
        var value2 = obj.GetValue();         // This should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (10,25): error CS0023: Operator '?' cannot be applied to operand of type 'TestType'
                //         var value1 = obj?.GetValue();        // This should be an error
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "TestType").WithLocation(10, 25));
        }

        [Fact]
        public void ExtensionMethods_NullConditionalOperator_In_NotAllowed()
        {
            var code = @"
public struct TestType
{
    public int GetValue() => 0;
}
public static class Extensions
{
    public static void Call(in this TestType obj)
    {
        var value1 = obj?.GetValue();        // This should be an error
        var value2 = obj.GetValue();         // This should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (10,25): error CS0023: Operator '?' cannot be applied to operand of type 'TestType'
                //         var value1 = obj?.GetValue();        // This should be an error
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "TestType").WithLocation(10, 25));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ValueTypes_Allowed()
        {
            var reference = CreateCompilationWithMscorlib40AndSystemCore(@"
public static class Extensions
{
    public static void PrintValue(ref this int p)
    {
        System.Console.Write(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", options: TestOptions.ReleaseExe);

            CompileAndVerify(reference, expectedOutput: "55");

            var code = @"
public static class Program2
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "55");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "55");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ReferenceTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref this string p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8337: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_InterfaceTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref this System.IComparable p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        System.IComparable x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8337: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue(ref this System.IComparable p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'IComparable' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'IComparable' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("System.IComparable", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        System.IComparable x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'IComparable' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'IComparable' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("System.IComparable", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_UnconstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref this T p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8337: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue<T>(ref this T p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_StructConstrainedGenericTypes_Allowed()
        {
            var reference = CreateCompilationWithMscorlib40AndSystemCore(@"
public static class Extensions
{
    public static void PrintValue<T>(ref this T p) where T : struct
    {
        System.Console.Write(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", options: TestOptions.ReleaseExe);

            CompileAndVerify(reference, expectedOutput: "55");

            var code = @"
public static class Program2
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "55");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "55");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref this T p) where T : class
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8337: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue<T>(ref this T p) where T : class
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref this T p) where T : System.IComparable
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8337: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue<T>(ref this T p) where T : System.IComparable
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ValueTypes_Allowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue(int32& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(int32)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "55");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ReferenceTypes_NotAllowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue(string& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0003:  call       void [mscorlib]System.Console::Write(string)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_InterfaceTypes_NotAllowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue(class [mscorlib]System.IComparable& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0003:  call       void [mscorlib]System.Console::Write(string)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        System.IComparable x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'IComparable' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'IComparable' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("System.IComparable", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_UnconstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_StructConstrainedGenericTypes_Allowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<valuetype .ctor ([mscorlib]System.ValueType) T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000: nop
    IL_0001: ldarg.0
    IL_0002: ldobj !!T
    IL_0007: box !!T
    IL_000c: call void [mscorlib]System.Console::Write(object)
    IL_0011: nop
    IL_0012: ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "55");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<class T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(@"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<([mscorlib]System.IComparable) T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(ref x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_ValueTypes_Allowed()
        {
            var reference = CreateCompilationWithMscorlib40AndSystemCore(@"
public static class Extensions
{
    public static void PrintValue(in this int p)
    {
        System.Console.Write(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", options: TestOptions.ReleaseExe);

            CompileAndVerify(reference, expectedOutput: "55");

            var code = @"
public static class Program2
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "55");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "55");
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_ReferenceTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(in this string p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8338: The first parameter of the 'in' extension method 'PrintValue' must be a concrete (non-generic) value type.
                //     public static void PrintValue(in this string p)
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_InterfaceTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(in this System.IComparable p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        System.IComparable x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8338: The first parameter of the 'in' extension method 'PrintValue' must be a concrete (non-generic) value type.
                //     public static void PrintValue(in this System.IComparable p)
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'IComparable' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'IComparable' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("System.IComparable", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        System.IComparable x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'IComparable' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'IComparable' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("System.IComparable", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_UnconstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(in this T p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8338: The first parameter of the 'in' extension method 'PrintValue' must be a concrete (non-generic) value type.
                //     public static void PrintValue<T>(in this T p)
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_StructConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(in this T p) where T : struct
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8338: The first parameter of the 'in' extension method 'PrintValue' must be a concrete (non-generic) value type.
                //     public static void PrintValue<T>(in this T p) where T : struct
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'int' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("int", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'int' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("int", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(in this T p) where T : class
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8338: The first parameter of the 'in' extension method 'PrintValue' must be a concrete (non-generic) value type.
                //     public static void PrintValue<T>(in this T p) where T : class
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(in this T p) where T : System.IComparable
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            var reference = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8338: The first parameter of the 'in' extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(in this T p) where T : System.IComparable
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24),
                // (14,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(14, 11));

            CreateCompilation(@"
public static class Program2
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}", references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_ValueTypes_Allowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue(int32& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(int32)
    IL_0008:  nop
    IL_0009:  ret
  }
}");


            var code = @"
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "55");
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_ReferenceTypes_NotAllowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue(string& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(string)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_InterfaceTypes_NotAllowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue(class [mscorlib]System.IComparable& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(string)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        System.IComparable x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'IComparable' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'IComparable' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("System.IComparable", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_UnconstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_StructConstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<valuetype .ctor ([mscorlib]System.ValueType) T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        int x = 5;
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'int' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("int", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<class T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes_NotAllowed_IL()
        {
            var reference = CompileIL(ExtraRefReadOnlyIL + @"
.class public abstract auto ansi sealed beforefieldinit Extensions extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig static void  PrintValue<([mscorlib]System.IComparable) T>(!!T& p) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::Write(object)
    IL_0008:  nop
    IL_0009:  ret
  }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        string x = ""test"";
        x.PrintValue();
        Extensions.PrintValue(x);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,11): error CS1061: 'string' does not contain a definition for 'PrintValue' and no extension method 'PrintValue' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         x.PrintValue();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "PrintValue").WithArguments("string", "PrintValue").WithLocation(7, 11));
        }

        [Fact]
        public void InErrorsArePropagatedThroughExtensionMethods()
        {
            var code = @"
public static class Extensions
{
    public static void Modify(in this int p)
    {
        p++;
    }
}
public static class Program
{
    public static void Main()
    {
        int value = 0;
        value.Modify();
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (6,9): error CS8408: Cannot assign to variable 'in int' because it is a readonly variable
                //         p++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "p").WithArguments("variable", "in int").WithLocation(6, 9));
        }

        [Fact]
        public void RefExtensionMethods_CodeGen()
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

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code, options: TestOptions.ReleaseExe);
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
        public void InExtensionMethods_CodeGen()
        {
            var code = @"
public static class Extensions
{
    public static void Print(in this int x)
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

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code, options: TestOptions.ReleaseExe);
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

        [Fact]
        public void Conversions_Numeric_RefExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref this long x)
    {
        System.Console.WriteLine(x);
    }
}
public class Test
{
    public static void Main()
    {
        int intValue = 0;
        intValue.Print();       // Should be an error

        long longValue = 0;
        longValue.Print();      // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (14,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref long)' requires a receiver of type 'ref long'
                //         intValue.Print();       // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intValue").WithArguments("int", "Print", "Extensions.Print(ref long)", "ref long").WithLocation(14, 9));
        }

        [Fact]
        public void Conversions_Numeric_InExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(in this long x)
    {
        System.Console.WriteLine(x);
    }
}
public class Test
{
    public static void Main()
    {
        int intValue = 0;
        intValue.Print();       // Should be an error

        long longValue = 0;
        longValue.Print();      // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (14,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(in long)' requires a receiver of type 'in long'
                //         intValue.Print();       // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intValue").WithArguments("int", "Print", "Extensions.Print(in long)", "in long").WithLocation(14, 9));
        }

        [Fact]
        public void Conversion_Tuples_RefExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref this (long, long) x)
    {
        System.Console.WriteLine(x);
    }
}
public class Test
{
    public static void Main()
    {
        int intValue1 = 0;
        int intValue2 = 0;
        var intTuple = (intValue1, intValue2);
        intTuple.Print();                       // Should be an error

        long longValue1 = 0;
        long longValue2 = 0;
        var longTuple = (longValue1, longValue2);
        longTuple.Print();                      // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
                // (16,9): error CS1929: '(int intValue1, int intValue2)' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref (long, long))' requires a receiver of type 'ref (long, long)'
                //         intTuple.Print();                       // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intTuple").WithArguments("(int intValue1, int intValue2)", "Print", "Extensions.Print(ref (long, long))", "ref (long, long)").WithLocation(16, 9));
        }

        [Fact]
        public void Conversion_Tuples_InExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(in this (long, long) x)
    {
        System.Console.WriteLine(x);
    }
}
public class Test
{
    public static void Main()
    {
        int intValue1 = 0;
        int intValue2 = 0;
        var intTuple = (intValue1, intValue2);
        intTuple.Print();                       // Should be an error

        long longValue1 = 0;
        long longValue2 = 0;
        var longTuple = (longValue1, longValue2);
        longTuple.Print();                      // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
                // (16,9): error CS1929: '(int intValue1, int intValue2)' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(in (long, long))' requires a receiver of type 'in (long, long)'
                //         intTuple.Print();                       // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intTuple").WithArguments("(int intValue1, int intValue2)", "Print", "Extensions.Print(in (long, long))", "in (long, long)").WithLocation(16, 9));
        }

        [Fact]
        public void Conversions_Nullables_RefExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref this int? x)
    {
        System.Console.WriteLine(x.Value);
    }
}
public class Test
{
    public static void Main()
    {
        0.Print();                  // Should be an error

        int intValue = 0;
        intValue.Print();           // Should be an error

        int? nullableValue = intValue;
        nullableValue.Print();      // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (13,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref int?)' requires a receiver of type 'ref int?'
                //         0.Print();                  // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "0").WithArguments("int", "Print", "Extensions.Print(ref int?)", "ref int?").WithLocation(13, 9),
                // (16,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref int?)' requires a receiver of type 'ref int?'
                //         intValue.Print();           // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intValue").WithArguments("int", "Print", "Extensions.Print(ref int?)", "ref int?").WithLocation(16, 9));
        }

        [Fact]
        public void Conversions_Nullables_InExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(in this int? x)
    {
        System.Console.WriteLine(x.Value);
    }
}
public class Test
{
    public static void Main()
    {
        0.Print();                  // Should be an error

        int intValue = 0;
        intValue.Print();           // Should be an error

        int? nullableValue = intValue;
        nullableValue.Print();      // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (13,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(in int?)' requires a receiver of type 'in int?'
                //         0.Print();                  // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "0").WithArguments("int", "Print", "Extensions.Print(in int?)", "in int?").WithLocation(13, 9),
                // (16,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(in int?)' requires a receiver of type 'in int?'
                //         intValue.Print();           // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intValue").WithArguments("int", "Print", "Extensions.Print(in int?)", "in int?").WithLocation(16, 9));
        }

        [Fact]
        public void Conversions_ImplicitOperators_RefExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref this Test x)
    {
        System.Console.WriteLine(x);
    }
}
public struct Test
{
    public static implicit operator Test(string value) => default(Test);

    public void TryMethod()
    {
        string stringValue = ""test"";
        stringValue.Print();            // Should be an error

        Test testValue = stringValue;
        testValue.Print();              // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (16,9): error CS1929: 'string' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref Test)' requires a receiver of type 'ref Test'
                //         stringValue.Print();            // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "stringValue").WithArguments("string", "Print", "Extensions.Print(ref Test)", "ref Test").WithLocation(16, 9));
        }

        [Fact]
        public void Conversions_ImplicitOperators_InExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref this Test x)
    {
        System.Console.WriteLine(x);
    }
}
public struct Test
{
    public static implicit operator Test(string value) => default(Test);

    public void TryMethod()
    {
        string stringValue = ""test"";
        stringValue.Print();            // Should be an error

        Test testValue = stringValue;
        testValue.Print();              // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (16,9): error CS1929: 'string' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref Test)' requires a receiver of type 'ref Test'
                //         stringValue.Print();            // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "stringValue").WithArguments("string", "Print", "Extensions.Print(ref Test)", "ref Test").WithLocation(16, 9));
        }

        [Fact]
        public void ColorColorCasesShouldBeResolvedCorrectly_RefExtensionMethods()
        {
            var code = @"
public struct Color
{
    public void Instance()
    {
        System.Console.Write(""Instance"");
    }
    public static void Static()
    {
        System.Console.Write(""Static"");
    }
}
public static class Extensions
{
    public static void Extension(ref this Color x)
    {
        System.Console.Write(""Extension"");
    }
}
public class Test
{
    private static Color Color = new Color();

    public static void Main()
    {
        Color.Instance();
        System.Console.Write("","");
        Color.Extension();
        System.Console.Write("","");
        Color.Static();
    }
}";

            CompileAndVerify(code, expectedOutput: "Instance,Extension,Static");
        }

        [Fact]
        public void ColorColorCasesShouldBeResolvedCorrectly_InExtensionMethods()
        {
            var code = @"
public struct Color
{
    public void Instance()
    {
        System.Console.Write(""Instance"");
    }
    public static void Static()
    {
        System.Console.Write(""Static"");
    }
}
public static class Extensions
{
    public static void Extension(in this Color x)
    {
        System.Console.Write(""Extension"");
    }
}
public class Test
{
    private static Color Color = new Color();

    public static void Main()
    {
        Color.Instance();
        System.Console.Write("","");
        Color.Extension();
        System.Console.Write("","");
        Color.Static();
    }
}";

            CompileAndVerify(code, expectedOutput: "Instance,Extension,Static");
        }

        [Fact]
        public void RecursiveCalling_RefExtensionMethods()
        {
            var code = @"
public struct S1
{
    public int i;

    public void Mutate()
    {
        System.Console.Write(i--);
    }
}
public static class Extensions
{
    public static void PrintValue(ref this S1 obj)
    {
        if (obj.i > 0)
        {
            obj.Mutate();
            obj.PrintValue();
        }
    }
}
public class Program
{
    public static void Main()
    {
        var obj = new S1 { i = 5 };
        obj.PrintValue();
    }
}";

            CompileAndVerify(code, expectedOutput: "54321");
        }

        [Fact]
        public void MutationIsObserved_RefExtensionMethods()
        {
            var code = @"
public static class Extensions
{
    public static void Decrement(ref this int p)
    {
        p--;
    }
}
public class Program
{
    public static void Main()
    {
        int p = 8;
        p.Decrement();
        System.Console.WriteLine(p);
    }
}";

            CompileAndVerify(code, expectedOutput: "7");
        }

        [Fact]
        public void RecursiveCalling_InExtensionMethods()
        {
            var code = @"
public struct S1
{
    public int i;

    public void Mutate()
    {
        System.Console.Write(i--);
    }
}
public static class Extensions
{
    public static void PrintValue(in this S1 obj, S1 other)
    {
        if (other.i > 0)
        {
            // should not mutate
            obj.Mutate();

            // should mutate
            other.Mutate();

            obj.PrintValue(other);
        }
    }
}
public class Program
{
    public static void Main()
    {
        var obj = new S1 { i = 5 };
        obj.PrintValue(obj);
    }
}";

            CompileAndVerify(code, expectedOutput: "5554535251");
        }

        [Fact]
        public void AmbiguousRefnessForExtensionMethods_RValue()
        {
            var code = @"
public static class Ext1
{
    public static void Print(ref this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Ext2
{
    public static void Print(in this int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        0.Print();                  // Error
        Ext2.Print(0);              // Ok
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (20,11): error CS0121: The call is ambiguous between the following methods or properties: 'Ext1.Print(ref int)' and 'Ext2.Print(in int)'
                //         0.Print();                  // Error
                Diagnostic(ErrorCode.ERR_AmbigCall, "Print").WithArguments("Ext1.Print(ref int)", "Ext2.Print(in int)").WithLocation(20, 11));
        }

        [Fact]
        public void AmbiguousRefnessForExtensionMethods_LValue()
        {
            var code = @"
public static class Ext1
{
    public static void Print(ref this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Ext2
{
    public static void Print(in this int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        int value = 0;
        value.Print();              // Error
        
        Ext1.Print(ref value);      // Ok
        Ext2.Print(value);          // Ok
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (21,15): error CS0121: The call is ambiguous between the following methods or properties: 'Ext1.Print(ref int)' and 'Ext2.Print(in int)'
                //         value.Print();              // Error
                Diagnostic(ErrorCode.ERR_AmbigCall, "Print").WithArguments("Ext1.Print(ref int)", "Ext2.Print(in int)").WithLocation(21, 15));
        }

        [Fact]
        public void ReadOnlynessPreservedThroughMultipleCalls()
        {
            var code = @"
public static class Ext
{
    public static void ReadOnly(in int p)
    {
        Ref(ref p);     // Should be an error
    }
    public static void Ref(ref int p)
    {
        Ref(ref p);     // Should be OK
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (6,17): error CS8406: Cannot use variable 'in int' as a ref or out value because it is a readonly variable
                //         Ref(ref p);     // Should be an error
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "in int").WithLocation(6, 17));
        }

        [Fact]
        public void ParameterSymbolsRetrievedThroughSemanticModel_RefExtensionMethods()
        {
            var code = @"
public static class Ext
{
    public static void Method(ref this int p) { }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var parameter = tree.FindNodeOrTokenByKind(SyntaxKind.Parameter);
            Assert.True(parameter.IsNode);

            var model = comp.GetSemanticModel(tree);
            var symbol = (ParameterSymbol)model.GetDeclaredSymbolForNode(parameter.AsNode());
            Assert.Equal(RefKind.Ref, symbol.RefKind);
        }

        [Fact]
        public void ParameterSymbolsRetrievedThroughSemanticModel_InExtensionMethods()
        {
            var code = @"
public static class Ext
{
    public static void Method(in this int p) { }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var parameter = tree.FindNodeOrTokenByKind(SyntaxKind.Parameter);
            Assert.True(parameter.IsNode);

            var model = comp.GetSemanticModel(tree);
            var symbol = (ParameterSymbol)model.GetDeclaredSymbolForNode(parameter.AsNode());
            Assert.Equal(RefKind.In, symbol.RefKind);
        }

        [Fact]
        public void ParameterSymbolsRetrievedThroughSemanticModel_RefExtensionMethods2()
        {
            var code = @"
public static class Ext
{
    public static void Method(this ref int p) { }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var parameter = tree.FindNodeOrTokenByKind(SyntaxKind.Parameter);
            Assert.True(parameter.IsNode);

            var model = comp.GetSemanticModel(tree);
            var symbol = (ParameterSymbol)model.GetDeclaredSymbolForNode(parameter.AsNode());
            Assert.Equal(RefKind.Ref, symbol.RefKind);
        }

        [Fact]
        public void ParameterSymbolsRetrievedThroughSemanticModel_InExtensionMethods2()
        {
            var code = @"
public static class Ext
{
    public static void Method(this in int p) { }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var parameter = tree.FindNodeOrTokenByKind(SyntaxKind.Parameter);
            Assert.True(parameter.IsNode);

            var model = comp.GetSemanticModel(tree);
            var symbol = (ParameterSymbol)model.GetDeclaredSymbolForNode(parameter.AsNode());
            Assert.Equal(RefKind.In, symbol.RefKind);
        }

        [Fact]
        public void BadContainingType_ThisWithRef()
        {
            var test = @"
using System;
public static class GenExtensions<X> where X : struct
{
    //No type parameters
    public static void Goo1(ref this int i) {}
    public static void Goo1(ref this X x) {}
    public static void Goo2(this ref int i) {}
    public static void Goo2(this ref X x) {}
    //Single type parameter
    public static void Goo1<T>(ref this T t) where T : struct {}
    public static void Goo1<T>(ref this X x) {}
    public static void Goo2<T>(this ref T t) where T : struct {}
    public static void Goo2<T>(this ref X x) {}
    //Multiple type parameters
    public static void Goo1<T,U,V>(ref this U u) where U : struct {}
    public static void Goo1<T,U,V>(ref this X x) {}
    public static void Goo2<T,U,V>(this ref U u) where U : struct {}
    public static void Goo2<T,U,V>(this ref X x) {}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21),
                // (3,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X> where X : struct
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(3, 21));
        }

        [Fact]
        public void UsingRefExtensionMethodsBeforeVersion7_2ProducesDiagnostics_RefSyntax_SameCompilation()
        {
            var code = @"
public static class Ext
{
    public static void Print(ref this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int p = 5;
        p.Print();
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (4,30): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public static void Print(ref this int p)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "ref").WithArguments("ref extension methods", "7.2").WithLocation(4, 30),
                // (14,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(14, 9)
            );

            CompileAndVerify(code, expectedOutput: "5");
        }

        [Fact]
        public void UsingRefExtensionMethodsBeforeVersion7_2ProducesDiagnostics_RefSyntax_DifferentCompilation()
        {
            var reference = CreateCompilationWithMscorlib40AndSystemCore(@"
public static class Ext
{
    public static void Print(ref this int p)
    {
        System.Console.WriteLine(p);
    }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        int p = 5;
        p.Print();
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(
                source: code,
                parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1),
                references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(7, 9));

            CreateCompilationWithMscorlib40AndSystemCore(
                source: code,
                parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1),
                references: new[] { reference.EmitToImageReference() }).VerifyDiagnostics(
                // (7,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(7, 9));

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void UsingRefExtensionMethodsBeforeVersion7_2ProducesDiagnostics_InSyntax_SameCompilation()
        {
            var code = @"
public static class Ext
{
    public static void Print(in this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int p = 5;
        p.Print();
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (4,30): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public static void Print(in this int p)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("readonly references", "7.2").WithLocation(4, 30),
                // (4,30): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public static void Print(in this int p)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("ref extension methods", "7.2").WithLocation(4, 30),
                // (14,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(14, 9)
            );

            CompileAndVerify(code, expectedOutput: "5");
        }

        [Fact]
        public void UsingRefExtensionMethodsBeforeVersion7_2ProducesDiagnostics_InSyntax_DifferentCompilation()
        {
            var reference = CreateCompilationWithMscorlib40AndSystemCore(@"
public static class Ext
{
    public static void Print(in this int p)
    {
        System.Console.WriteLine(p);
    }
}");

            var code = @"
public static class Program
{
    public static void Main()
    {
        int p = 5;
        p.Print();
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(
                source: code,
                parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1),
                references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (7,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(7, 9));

            CreateCompilationWithMscorlib40AndSystemCore(
                source: code,
                parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1),
                references: new[] { reference.EmitToImageReference() }).VerifyDiagnostics(
                // (7,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(7, 9));

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        private const string ExtraRefReadOnlyIL = @"
.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}";
    }
}
