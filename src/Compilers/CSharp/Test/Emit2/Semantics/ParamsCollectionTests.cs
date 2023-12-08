// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ParamsCollectionTests : CompilingTestBase
    {
        private static string ExpectedOutput(string output)
        {
            return ExecutionConditionUtil.IsMonoOrCoreClr ? output : null;
        }

        [Fact]
        public void Span()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params System.Span<long> a)
    {
        if (a.Length == 0)
        {
            System.Console.WriteLine(a.Length);
        }
        else
        {
            System.Console.WriteLine(""{0}: {1} ... {2}"", a.Length, a[0], a[^1]);
        }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Take(3).ToArray();

            Assert.Equal("Test()", nodes[0].ToString());
            comp.VerifyOperationTree(nodes[0], expectedOperationTree: """
IInvocationOperation (void Program.Test(params System.Span<System.Int64> a)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test()')
Instance Receiver:
  null
Arguments(1):
    IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand:
          ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test()')
            Elements(0)
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            Assert.Equal("Test(1)", nodes[1].ToString());
            comp.VerifyOperationTree(nodes[1], expectedOperationTree: """
IInvocationOperation (void Program.Test(params System.Span<System.Int64> a)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(1)')
Instance Receiver:
  null
Arguments(1):
    IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(1)')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test(1)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand:
          ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test(1)')
            Elements(1):
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");
            Assert.Equal("Test(2, 3)", nodes[2].ToString());
            comp.VerifyOperationTree(nodes[2], expectedOperationTree: """
IInvocationOperation (void Program.Test(params System.Span<System.Int64> a)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(2, 3)')
Instance Receiver:
  null
Arguments(1):
    IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(2, 3)')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test(2, 3)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand:
          ICollectionExpressionOperation (2 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test(2, 3)')
            Elements(2):
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 2, IsImplicit) (Syntax: '2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 3, IsImplicit) (Syntax: '3')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            var argument = nodes[1].ArgumentList.Arguments[0].Expression;
            var model = comp.GetSemanticModel(tree);

            var typeInfo = model.GetTypeInfo(argument);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(argument).IsNumeric);
        }

        [Fact]
        public void String()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test();
        Test('a');
        Test('b', 'c');
    }

    static void Test(params string a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // PROTOTYPE(ParamsCollections): Note, there is no error at the declaration site, because
            //                               according to https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md#conversions,
            //                               there is a conversion from a collection expression consisting of `char`s to a `string` type.
            //                               Even though `string` lacks APIs needed to perform the conversion.
            //                               Similar situation can happen with other types. Are we fine with this behavior, or should we
            //                               enforce existence of at least some APIs at the declaration site?
            comp.VerifyDiagnostics(
                // (6,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         Test();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test()").WithArguments("string", "0").WithLocation(6, 9),
                // (7,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         Test('a');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('a')").WithArguments("string", "0").WithLocation(7, 9),
                // (7,14): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         Test('a');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(7, 14),
                // (8,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('b', 'c')").WithArguments("string", "0").WithLocation(8, 9),
                // (8,14): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'b'").WithArguments("string", "Add").WithLocation(8, 14),
                // (8,19): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'c'").WithArguments("string", "Add").WithLocation(8, 19)
                );
        }

        [Fact]
        public void CreateMethod()
        {
            var src = """
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection : IEnumerable<long>
{
    public long[] Array;
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => new MyCollection() { Array = items.ToArray() };
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection a)
    {
        if (a.Array.Length == 0)
        {
            System.Console.WriteLine(a.Array.Length);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Length, a.Array[0], a.Array[^1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterNess_01_ElementType()
        {
            var src = @"
class Program
{
    static void Main()
    {
        int x = 1;
        Test(x);

        byte y = 1;
        Test(y);
    }

    static void Test(params System.Span<long> a)
    {
        System.Console.WriteLine(""long"");
    }

    static void Test(params System.Span<int> a)
    {
        System.Console.WriteLine(""int"");
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
int
int")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterNess_02_ElementType()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test(new C2());

        Test(new C3());
    }

    static void Test(params System.Span<C1> a)
    {
        System.Console.WriteLine(""C1"");
    }

    static void Test(params System.Span<C2> a)
    {
        System.Console.WriteLine(""C2"");
    }
}

class C1 {}
class C2 : C1 {}
class C3 : C2 {}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
C2
C2")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterNess_03_ElementType()
        {
            var src = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<char>
{
    public static void M1(params C1 x)
    {
    }
    public static void M1(params ushort[] x)
    {
    }

    void Test()
    {
        M1('a', 'b');
        M2('a', 'b');
    }

    public static void M2(params ushort[] x)
    {
    }

    IEnumerator<char> IEnumerable<char>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (16,12): error CS1061: 'C1' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'C1' could be found (are you missing a using directive or an assembly reference?)
                //         M1('a', 'b');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("C1", "Add").WithLocation(16, 12),
                // (16,17): error CS1061: 'C1' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'C1' could be found (are you missing a using directive or an assembly reference?)
                //         M1('a', 'b');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'b'").WithArguments("C1", "Add").WithLocation(16, 17)
                );
        }
    }
}
