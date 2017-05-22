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
        public void ExtensionMethods_RValues_Ref()
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
        public void ExtensionMethods_LValues_Ref()
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
        int x = 5;
        x.PrintValue();
    }
}";

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "5");
        }

        [Fact]
        public void ExtensionMethods_RValues_RefReadOnly()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this int p)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "5");
        }

        [Fact]
        public void ExtensionMethods_LValues_RefReadOnly()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this int p)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "5");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ValueTypes()
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
        int x = 5;
        x.PrintValue();
    }
}";

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "5");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ReferenceTypes()
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
                // (4,24): error CS8414: The receiver of the reference extension method 'PrintValue' must be a value type or a generic type constrained to one.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_UnconstrainedGenericTypes()
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
                // (4,24): error CS8414: The receiver of the reference extension method 'PrintValue' must be a value type or a generic type constrained to one.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_StructConstrainedGenericTypes()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue<T>(ref this T p) where T : struct
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "5");
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes()
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
                // (4,24): error CS8414: The receiver of the reference extension method 'PrintValue' must be a value type or a generic type constrained to one.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes()
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
                // (4,24): error CS8414: The receiver of the reference extension method 'PrintValue' must be a value type or a generic type constrained to one.
                //     public static void PrintValue(ref this string p)
                Diagnostic(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_ValueTypes()
        {
            var code = @"
public static class Extensions
{
    public static void PrintValue(ref readonly this int p)
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

            CompileAndVerify(code, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "5");
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_ReferenceTypes()
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
                // (4,24): error CS8415: The receiver of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_UnconstrainedGenericTypes()
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
                // (4,24): error CS8415: The receiver of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_StructConstrainedGenericTypes()
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
                // (4,24): error CS8415: The receiver of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_ClassConstrainedGenericTypes()
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
                // (4,24): error CS8415: The receiver of the readonly reference extension method 'PrintValue' must be a value type.
                //     public static void PrintValue<T>(ref readonly this T p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyExtensionMustBeValueType, "PrintValue").WithArguments("PrintValue").WithLocation(4, 24));
        }

        [Fact]
        public void RefReadOnlyExtensionMethodsReceiverTypes_InterfaceConstrainedGenericTypes()
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
                // (4,24): error CS8415: The receiver of the readonly reference extension method 'PrintValue' must be a value type.
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
    }
}
