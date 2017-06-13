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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (13,9): error CS1510: A ref or out value must be an assignable variable
                //         5.PrintValue();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "5").WithLocation(13, 9));
        }

        [Fact]
        public void ExtensionMethods_LValues_Ref_Allowed()
        {
            var code = @"
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
}";

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "55");
        }

        [Fact]
        public void ExtensionMethods_RValues_RefReadOnly_Allowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this int p)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "55");
        }

        [Fact]
        public void ExtensionMethods_LValues_RefReadOnly_Allowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this int p)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "55");
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (10,25): error CS0023: Operator '?' cannot be applied to operand of type 'TestType'
                //         var value1 = obj?.GetValue();        // This should be an error
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "TestType").WithLocation(10, 25));
        }

        [Fact]
        public void ExtensionMethods_NullConditionalOperator_RefReadOnly_NotAllowed()
        {
            var code = @"
public struct TestType
{
    public int GetValue() => 0;
}
public static class Extensions
{
    public static void Call(ref readonly this TestType obj)
    {
        var value1 = obj?.GetValue();        // This should be an error
        var value2 = obj.GetValue();         // This should be OK
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (10,25): error CS0023: Operator '?' cannot be applied to operand of type 'TestType'
                //         var value1 = obj?.GetValue();        // This should be an error
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "TestType").WithLocation(10, 25));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ValueTypes_Allowed()
        {
            var code = @"
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
}";

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "55");
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8414: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8414: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_StructConstrainedGenericTypes_Allowed()
        {
            var code = @"
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
}";

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "55");
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8414: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8414: The first parameter of the reference extension method 'PrintValue' must be a value type or a generic type constrained to struct.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_ValueTypes_Allowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this int p)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "55");
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_ReferenceTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this string p)
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8415: The first parameter of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_UnconstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref readonly this T p)
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8415: The first parameter of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_StructConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref readonly this T p) where T : struct
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8415: The first parameter of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref readonly this T p) where T : class
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8415: The first parameter of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref readonly this T p) where T : System.IComparable
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
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (4,24): error CS8415: The first parameter of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyErrorsArePropagatedThroughExtensionMethods()
        {
            var code = @"
public static class Extensions
{
    public static void Modify(ref readonly this int p)
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
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
        public void RefReadOnlyExtensionMethods_CodeGen()
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (14,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref long)' requires a receiver of type 'ref long'
                //         intValue.Print();       // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intValue").WithArguments("int", "Print", "Extensions.Print(ref long)", "ref long").WithLocation(14, 9));
        }

        [Fact]
        public void Conversions_Numeric_RefReadOnlyExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref readonly this long x)
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
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

            CreateCompilationWithMscorlibAndSystemCore(code, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
                // (16,9): error CS1929: '(int intValue1, int intValue2)' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref (long, long))' requires a receiver of type 'ref (long, long)'
                //         intTuple.Print();                       // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intTuple").WithArguments("(int intValue1, int intValue2)", "Print", "Extensions.Print(ref (long, long))", "ref (long, long)").WithLocation(16, 9));
        }

        [Fact]
        public void Conversion_Tuples_RefReadOnlyExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref readonly this (long, long) x)
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

            CreateCompilationWithMscorlibAndSystemCore(code, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (13,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref int?)' requires a receiver of type 'ref int?'
                //         0.Print();                  // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "0").WithArguments("int", "Print", "Extensions.Print(ref int?)", "ref int?").WithLocation(13, 9),
                // (16,9): error CS1929: 'int' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref int?)' requires a receiver of type 'ref int?'
                //         intValue.Print();           // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "intValue").WithArguments("int", "Print", "Extensions.Print(ref int?)", "ref int?").WithLocation(16, 9));
        }

        [Fact]
        public void Conversions_Nullables_RefReadOnlyExtensionMethods_NotAllowed()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref readonly this int? x)
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (16,9): error CS1929: 'string' does not contain a definition for 'Print' and the best extension method overload 'Extensions.Print(ref Test)' requires a receiver of type 'ref Test'
                //         stringValue.Print();            // Should be an error
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "stringValue").WithArguments("string", "Print", "Extensions.Print(ref Test)", "ref Test").WithLocation(16, 9));
        }

        [Fact]
        public void Conversions_ImplicitOperators_RefReadOnlyExtensionMethods_NotAllowed()
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "Instance,Extension,Static");
        }

        [Fact]
        public void ColorColorCasesShouldBeResolvedCorrectly_RefReadOnlyExtensionMethods()
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
    public static void Extension(ref readonly this Color x)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "Instance,Extension,Static");
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "54321");
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "7");
        }

        [Fact]
        public void RecursiveCalling_RefReadOnlyExtensionMethods()
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
    public static void PrintValue(ref readonly this S1 obj)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "54321");
        }

        [Fact]
        public void AmbiguousRefnessForExtensionMethods_RValue()
        {
            var code = @"
public static class Ext1
{
    public static void Print(ref int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Ext2
{
    public static void Print(ref readonly int p)
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (20,11): error CS1061: 'int' does not contain a definition for 'Print' and no extension method 'Print' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         0.Print();                  // Error
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Print").WithArguments("int", "Print").WithLocation(20, 11));
        }

        [Fact]
        public void AmbiguousRefnessForExtensionMethods_LValue()
        {
            var code = @"
public static class Ext1
{
    public static void Print(ref int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Ext2
{
    public static void Print(ref readonly int p)
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

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (21,15): error CS1061: 'int' does not contain a definition for 'Print' and no extension method 'Print' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         value.Print();              // Error
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Print").WithArguments("int", "Print").WithLocation(21, 15));
        }

        [Fact]
        public void ReadOnlynessPreservedThroughMultipleCalls()
        {
            var code = @"
public static class Ext
{
    public static void ReadOnly(ref readonly int p)
    {
        Ref(ref p);     // Should be an error
    }
    public static void Ref(ref int p)
    {
        Ref(ref p);     // Should be OK
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (6,17): error CS8406: Cannot use variable 'in int' as a ref or out value because it is a readonly variable
                //         Ref(ref p);     // Should be an error
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "p").WithArguments("variable", "in int").WithLocation(6, 17));
        }

        [Fact]
        public void SemanticModelPreservesRefness_RefExtensionMethods()
        {
            var code = @"
public static class Ext
{
    public static void Method(ref int p) { }
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var parameter = tree.FindNodeOrTokenByKind(SyntaxKind.Parameter);
            Assert.True(parameter.IsNode);

            var model = comp.GetSemanticModel(tree);
            var symbol = (ParameterSymbol)model.GetDeclaredSymbolForNode(parameter.AsNode());
            Assert.Equal(RefKind.Ref, symbol.RefKind);
        }

        [Fact]
        public void SemanticModelPreservesRefness_RefReadOnlyExtensionMethods()
        {
            var code = @"
public static class Ext
{
    public static void Method(ref readonly int p) { }
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var parameter = tree.FindNodeOrTokenByKind(SyntaxKind.Parameter);
            Assert.True(parameter.IsNode);

            var model = comp.GetSemanticModel(tree);
            var symbol = (ParameterSymbol)model.GetDeclaredSymbolForNode(parameter.AsNode());
            Assert.Equal(RefKind.RefReadOnly, symbol.RefKind);
        }
    }
}
