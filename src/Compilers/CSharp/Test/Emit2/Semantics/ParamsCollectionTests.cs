// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ParamsCollectionTests : CompilingTestBase
    {
        private const string ParamCollectionAttributeSource = @"
namespace System.Runtime.CompilerServices
{
    public sealed class ParamCollectionAttribute : Attribute
    {
        public ParamCollectionAttribute() { }
    }
}
";

        private static void VerifyParamsAndAttribute(ParameterSymbol parameter, bool isParamArray = false, bool isParamCollection = false)
        {
            VerifyParams(parameter, isParamArray: isParamArray, isParamCollection: isParamCollection);

            var peParameter = (PEParameterSymbol)parameter;
            PEModule module = ((PEModuleSymbol)peParameter.ContainingModule).Module;

            Assert.Equal(isParamArray, module.HasParamArrayAttribute(peParameter.Handle));
            Assert.Equal(isParamCollection, module.HasParamCollectionAttribute(peParameter.Handle));
        }

        private static void VerifyParams(ParameterSymbol parameter, bool isParamArray = false, bool isParamCollection = false)
        {
            Assert.Equal(isParamArray, parameter.IsParamsArray);
            Assert.Equal(isParamCollection, parameter.IsParamsCollection);
            Assert.Equal(isParamArray | isParamCollection, parameter.IsParams);

            IParameterSymbol iParameter = parameter.GetPublicSymbol();
            Assert.Equal(isParamArray, iParameter.IsParamsArray);
            Assert.Equal(isParamCollection, iParameter.IsParamsCollection);
            Assert.Equal(isParamArray | isParamCollection, iParameter.IsParams);
        }

        private static void VerifyParams(IParameterSymbol parameter, bool isParamArray = false, bool isParamCollection = false)
        {
            VerifyParams(parameter.GetSymbol<ParameterSymbol>(), isParamArray: isParamArray, isParamCollection: isParamCollection);
        }

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

    static void Test2()
    {
        Test([2, 3]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                sourceSymbolValidator: static (m) =>
                {
                    VerifyParams(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                symbolValidator: static (m) =>
                {
                    VerifyParamsAndAttribute(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
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
    IArgumentOperation (ArgumentKind.ParamCollection, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test()')
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
    IArgumentOperation (ArgumentKind.ParamCollection, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(1)')
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
    IArgumentOperation (ArgumentKind.ParamCollection, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(2, 3)')
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
        public void Span_InAttribute()
        {
            var src = @"
[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params System.Span<long> a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params Span<long>)'
                // [Test()]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params System.Span<long>)").WithLocation(2, 2),
                // (5,7): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Span<long>'
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Span<long>").WithLocation(5, 7),
                // (8,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test(2, 3)").WithArguments("Test", "2").WithLocation(8, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                Assert.Empty(attributeData1.ConstructorArguments);
            }
        }

        [Fact]
        public void ReadOnlySpan()
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

    static void Test(params System.ReadOnlySpan<long> a)
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

    static void Test2()
    {
        Test([2, 3]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                sourceSymbolValidator: static (m) =>
                {
                    VerifyParams(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                symbolValidator: static (m) =>
                {
                    VerifyParamsAndAttribute(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlySpan_InAttribute()
        {
            var src = @"
[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params System.ReadOnlySpan<long> a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params ReadOnlySpan<long>)'
                // [Test()]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params System.ReadOnlySpan<long>)").WithLocation(2, 2),
                // (5,7): error CS1503: Argument 1: cannot convert from 'int' to 'params System.ReadOnlySpan<long>'
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.ReadOnlySpan<long>").WithLocation(5, 7),
                // (8,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test(2, 3)").WithArguments("Test", "2").WithLocation(8, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                Assert.Empty(attributeData1.ConstructorArguments);
            }
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

            comp.VerifyDiagnostics(
                // (6,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params string)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params string)").WithLocation(6, 9),
                // (7,14): error CS1503: Argument 1: cannot convert from 'char' to 'params string'
                //         Test('a');
                Diagnostic(ErrorCode.ERR_BadArgType, "'a'").WithArguments("1", "char", "params string").WithLocation(7, 14),
                // (8,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(8, 9),
                // (11,22): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void Test(params string a)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string a").WithArguments("string", "0").WithLocation(11, 22)
                );
        }

        [Fact]
        public void String_InAttribute()
        {
            var src = @"
[Test()]
class C1;

[Test('1')]
class C2;

[Test('2', '3')]
class C3;

class Test : System.Attribute
{
    public Test(params string a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                    // (2,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params string)'
                    // [Test()]
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params string)").WithLocation(2, 2),
                    // (5,7): error CS1503: Argument 1: cannot convert from 'char' to 'params string'
                    // [Test('1')]
                    Diagnostic(ErrorCode.ERR_BadArgType, "'1'").WithArguments("1", "char", "params string").WithLocation(5, 7),
                    // (8,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                    // [Test('2', '3')]
                    Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('2', '3')").WithArguments("Test", "2").WithLocation(8, 2),
                    // (13,17): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                    //     public Test(params string a) {}
                    Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string a").WithArguments("string", "0").WithLocation(13, 17)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Char", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Char", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);
                Assert.Empty(attributeData1.ConstructorArguments);
            }
        }

        [Fact]
        public void CreateMethod_01()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection
{
    public long[] Array;
    public IEnumerator<long> GetEnumerator() => throw null;
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

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                sourceSymbolValidator: static (m) =>
                {
                    VerifyParams(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                symbolValidator: static (m) =>
                {
                    VerifyParamsAndAttribute(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CreateMethod_02_InAttribute(bool asStruct)
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
" + (asStruct ? "struct" : "class") + @" MyCollection
{
    public IEnumerator<long> GetEnumerator() => throw new InvalidOperationException();
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => new MyCollection();
}

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params MyCollection a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (16,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params MyCollection)'
                // [Test()]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params MyCollection)").WithLocation(16, 2),
                // (19,7): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(19, 7),
                // (22,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test(2, 3)").WithArguments("Test", "2").WithLocation(22, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                Assert.Empty(attributeData1.ConstructorArguments);
            }
        }

        [Fact]
        public void CreateMethod_03_NoElementType()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection
{
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => null;
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
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (18,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(18, 9),
                // (19,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(19, 14),
                // (20,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(20, 9),
                // (23,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(23, 22)
                );
        }

        [Fact]
        public void CreateMethod_04_NoElementType_ExtensionGetEnumerator()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection
{
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => null;
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
        new MyCollection().GetEnumerator();
    }
    static void Test(params MyCollection a)
    {
        foreach (var x in a)
        {
            long y = x;
        }
    }
}

static class Ext
{
    public static IEnumerator<long> GetEnumerator(this MyCollection x) => throw null;
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (18,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(18, 9),
                // (19,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(19, 14),
                // (20,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(20, 9),
                // (23,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(23, 22)
                );
        }

        [Fact]
        public void CreateMethod_05_Missing()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
public class MyCollection
{
    public IEnumerator<long> GetEnumerator() => throw null;
}
public class MyCollectionBuilder
{
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (18,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test();
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test()").WithArguments("Create", "long", "MyCollection").WithLocation(18, 9),
                // (19,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test(1)").WithArguments("Create", "long", "MyCollection").WithLocation(19, 9),
                // (20,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test(2, 3)").WithArguments("Create", "long", "MyCollection").WithLocation(20, 9),
                // (23,29): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection a").WithArguments("Create", "long", "MyCollection").WithLocation(23, 29)
                );
        }

        [Fact]
        public void CreateMethod_06_InconsistentAccessibility()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
public class MyCollection
{
    public IEnumerator<long> GetEnumerator() => throw null;
}
public class MyCollectionBuilder
{
    internal static MyCollection Create(ReadOnlySpan<long> items) => null;
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (24,29): error CS9224: Method 'MyCollectionBuilder.Create(ReadOnlySpan<long>)' cannot be less visible than the member with params collection 'Program.Test(params MyCollection)'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection a").WithArguments("MyCollectionBuilder.Create(System.ReadOnlySpan<long>)", "Program.Test(params MyCollection)").WithLocation(24, 29)
                );
        }

        [Fact]
        public void CreateMethod_07_InconsistentAccessibility()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder1), nameof(MyCollectionBuilder1.Create))]
public class MyCollection1
{
    public IEnumerator<long> GetEnumerator() => throw null;
}
internal class MyCollectionBuilder1
{
    public static MyCollection1 Create(ReadOnlySpan<long> items) => null;
}

[CollectionBuilder(typeof(MyCollectionBuilder2), nameof(MyCollectionBuilder2.Create))]
public class MyCollection2
{
    public IEnumerator<long> GetEnumerator() => throw null;
}
internal class MyCollectionBuilder2
{
    public static MyCollection2 Create(ReadOnlySpan<long> items) => null;
}

public class Program
{
    static void Main()
    {
        Test1();
        Test1(1);
        Test1(2, 3);

        Test2();
        Test2(1);
        Test2(2, 3);
    }

    public static void Test1(params MyCollection1 a)
    {
    }

    internal static void Test2(params MyCollection2 a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (38,30): error CS9224: Method 'MyCollectionBuilder1.Create(ReadOnlySpan<long>)' cannot be less visible than the member with params collection 'Program.Test1(params MyCollection1)'.
                //     public static void Test1(params MyCollection1 a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection1 a").WithArguments("MyCollectionBuilder1.Create(System.ReadOnlySpan<long>)", "Program.Test1(params MyCollection1)").WithLocation(38, 30)
                );
        }

        [Fact]
        public void CreateMethod_08_Inaccessible()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
public class MyCollection
{
    public IEnumerator<long> GetEnumerator() => throw null;
}
public class MyCollectionBuilder
{
    protected static MyCollection Create(ReadOnlySpan<long> items) => null;
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (19,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test();
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test()").WithArguments("Create", "long", "MyCollection").WithLocation(19, 9),
                // (20,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test(1)").WithArguments("Create", "long", "MyCollection").WithLocation(20, 9),
                // (21,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test(2, 3)").WithArguments("Create", "long", "MyCollection").WithLocation(21, 9),
                // (24,29): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection a").WithArguments("Create", "long", "MyCollection").WithLocation(24, 29)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71854")]
        public void CreateMethod_09_InDifferentAssembly()
        {
            var myCollection_v0Source = """
using System.Collections.Generic;

public class MyCollection
{
    public long[] Array;
    public IEnumerator<long> GetEnumerator() => throw null;
}
""";

            var myCollection_v0 = CreateCompilation(myCollection_v0Source, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, assemblyName: "Collection");
            myCollection_v0.VerifyDiagnostics();

            var builderSource = """
using System;

public class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => new MyCollection() { Array = items.ToArray() };
}
""";

            var builder = CreateCompilation(builderSource, references: [myCollection_v0.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            builder.VerifyDiagnostics();

            var myCollectionSource = """
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
public class MyCollection
{
    public long[] Array;
    public IEnumerator<long> GetEnumerator() => throw null;
}
""";

            var myCollection = CreateCompilation(myCollectionSource, references: [builder.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, assemblyName: "Collection");
            myCollection.VerifyDiagnostics();
            var myCollectionRef = myCollection.EmitToImageReference();

            var src = """
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
            var comp = CreateCompilation(src, references: [myCollectionRef, builder.EmitToImageReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();

            comp = CreateCompilation(src, references: [myCollectionRef], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // The error improvement is tracked by https://github.com/dotnet/roslyn/issues/71854
            comp.VerifyDiagnostics(
                // (5,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test();
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test()").WithArguments("Create", "long", "MyCollection").WithLocation(5, 9),
                // (6,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test(1)").WithArguments("Create", "long", "MyCollection").WithLocation(6, 9),
                // (7,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Test(2, 3)").WithArguments("Create", "long", "MyCollection").WithLocation(7, 9),
                // (10,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<long>' and return type 'MyCollection'.
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection a").WithArguments("Create", "long", "MyCollection").WithLocation(10, 22)
                );
        }

        [Fact]
        public void CreateMethod_10_NoElementType_GetEnumeratorWithParams()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection
{
    public IEnumerator<long> GetEnumerator(params MyCollection x) => throw null;
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => null;
    public static MyCollection Create(ReadOnlySpan<int> items) => null;
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
        foreach (var x in a)
        {
            long y = x;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (8,44): error CS0225: The params parameter must have a valid collection type
                //     public IEnumerator<long> GetEnumerator(params MyCollection x) => throw null;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(8, 44),
                // (20,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(20, 9),
                // (21,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(21, 14),
                // (22,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(22, 9),
                // (24,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(24, 22),
                // (26,27): error CS1579: foreach statement cannot operate on variables of type 'MyCollection' because 'MyCollection' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var x in a)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "a").WithArguments("MyCollection", "GetEnumerator").WithLocation(26, 27)
                );
        }

        [Fact]
        public void CreateMethod_11_NoElementType_MoveNextWithParams()
        {
            var src = """
using System;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection
{
    public Enumerator GetEnumerator() => throw null;

    public class Enumerator
    {
        public bool MoveNext(params MyCollection x) => false;
        public long Current => 0;
    }
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => null;
    public static MyCollection Create(ReadOnlySpan<int> items) => null;
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
        foreach (var x in a)
        {
            long y = x;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (11,30): error CS0225: The params parameter must have a valid collection type
                //         public bool MoveNext(params MyCollection x) => false;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(11, 30),
                // (25,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(25, 9),
                // (26,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(26, 14),
                // (27,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(27, 9),
                // (29,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(29, 22),
                // (31,27): error CS0202: foreach requires that the return type 'MyCollection.Enumerator' of 'MyCollection.GetEnumerator()' must have a suitable public 'MoveNext' method and public 'Current' property
                //         foreach (var x in a)
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "a").WithArguments("MyCollection.Enumerator", "MyCollection.GetEnumerator()").WithLocation(31, 27)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_01()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);
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
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                sourceSymbolValidator: static (m) =>
                {
                    VerifyParams(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                symbolValidator: static (m) =>
                {
                    VerifyParamsAndAttribute(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerableT_02()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public IEnumerator<string> GetEnumerator() => throw null; 

    public void Add(long l) => throw null; 
    public void Add(string l) => throw null; 
}

class Program
{
    static void Main()
    {
        Test("2", 3);
        Test(["2", 3]);
        Test("2");
        Test(["2"]);
        Test(3);
        Test([3]);

        MyCollection x1 = ["2"];
        MyCollection x2 = [3];
    }

    static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (19,19): error CS1503: Argument 2: cannot convert from 'int' to 'string'
                //         Test("2", 3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("2", "int", "string").WithLocation(19, 19),
                // (20,14): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'string'
                //         Test(["2", 3]);
                Diagnostic(ErrorCode.ERR_BadArgType, @"[""2"", 3]").WithArguments("1", "collection expressions", "string").WithLocation(20, 14),
                // (23,14): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         Test(3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("1", "int", "string").WithLocation(23, 14),
                // (24,14): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'string'
                //         Test([3]);
                Diagnostic(ErrorCode.ERR_BadArgType, "[3]").WithArguments("1", "collection expressions", "string").WithLocation(24, 14),
                // (27,28): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         MyCollection x2 = [3];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "3").WithArguments("int", "string").WithLocation(27, 28)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ImplementsIEnumerableT_03_InAttribute(bool asStruct)
        {
            var src = @"
using System;
using System.Collections;
using System.Collections.Generic;

" + (asStruct ? "struct" : "class") + @" MyCollection : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw new InvalidOperationException();
    IEnumerator IEnumerable.GetEnumerator() => throw new InvalidOperationException();

    public void Add(long l) {}
}

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params MyCollection a) {}
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (14,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params MyCollection)'
                // [Test()]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params MyCollection)").WithLocation(14, 2),
                // (17,7): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(17, 7),
                // (20,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test(2, 3)").WithArguments("Test", "2").WithLocation(20, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                Assert.Empty(attributeData1.ConstructorArguments);
            }
        }

        [Fact]
        public void ImplementsIEnumerableT_04_MissingConstructor()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    public MyCollection(int x){}
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (18,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(18, 9),
                // (19,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(19, 14),
                // (20,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(20, 9),
                // (23,22): error CS9228: Non-array params collection type must have an applicable constructor that can be called with no arguments.
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsCollectionMissingConstructor, "params MyCollection a").WithLocation(23, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_05_InaccessibleConstructor()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    protected MyCollection(){}
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (18,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(18, 9),
                // (19,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(19, 14),
                // (20,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(20, 9),
                // (23,22): error CS0122: 'MyCollection.MyCollection()' is inaccessible due to its protection level
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyCollection a").WithArguments("MyCollection.MyCollection()").WithLocation(23, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_06_LessAccessibleConstructor()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

public class MyCollection : IEnumerable<long>
{
    internal MyCollection(){}
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (23,29): error CS9224: Method 'MyCollection.MyCollection()' cannot be less visible than the member with params collection 'Program.Test(params MyCollection)'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection a").WithArguments("MyCollection.MyCollection()", "Program.Test(params MyCollection)").WithLocation(23, 29)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_07_MissingAdd()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (15,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(15, 9),
                // (16,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(16, 14),
                // (17,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(17, 9),
                // (20,22): error CS0117: 'MyCollection' does not contain a definition for 'Add'
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyCollection a").WithArguments("MyCollection", "Add").WithLocation(20, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_08_InaccessibleAdd()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    protected void Add(long l) => Array.Add(l);
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (17,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(17, 9),
                // (18,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(18, 14),
                // (19,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(19, 9),
                // (22,22): error CS0122: 'MyCollection.Add(long)' is inaccessible due to its protection level
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyCollection a").WithArguments("MyCollection.Add(long)").WithLocation(22, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_09_LessAccessibleAdd()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

public class MyCollection : IEnumerable<long>
{
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    internal void Add(long l) => Array.Add(l);
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (22,29): error CS9224: Method 'MyCollection.Add(long)' cannot be less visible than the member with params collection 'Program.Test(params MyCollection)'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection a").WithArguments("MyCollection.Add(long)", "Program.Test(params MyCollection)").WithLocation(22, 29)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_10_MissingAdd_Dynamic()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(20, 9),
                // (21,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(21, 14),
                // (22,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(22, 9),
                // (25,22): error CS0117: 'MyCollection' does not contain a definition for 'Add'
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyCollection a").WithArguments("MyCollection", "Add").WithLocation(25, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_11_InaccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    protected void Add(long x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (22,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(22, 9),
                // (23,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(23, 14),
                // (24,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(24, 9),
                // (27,22): error CS0122: 'MyCollection.Add(long)' is inaccessible due to its protection level
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyCollection a").WithArguments("MyCollection.Add(long)").WithLocation(27, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_12_InaccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    protected void Add(long x){}
    protected void Add(int x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (23,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(23, 9),
                // (24,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(24, 14),
                // (25,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(25, 9),
                // (28,22): error CS0122: 'MyCollection.Add(long)' is inaccessible due to its protection level
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyCollection a").WithArguments("MyCollection.Add(long)").WithLocation(28, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_13_LessAccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;

public class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    internal void Add(long x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (26,29): error CS9224: Method 'MyCollection.Add(long)' cannot be less visible than the member with params collection 'Program.Test(params MyCollection)'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection a").WithArguments("MyCollection.Add(long)", "Program.Test(params MyCollection)").WithLocation(26, 29)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_14_LessAccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

public class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    protected void Add(long x){}
    internal void Add(int x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (28,29): error CS9224: Method 'MyCollection.Add(int)' cannot be less visible than the member with params collection 'Program.Test(params MyCollection)'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection a").WithArguments("MyCollection.Add(int)", "Program.Test(params MyCollection)").WithLocation(28, 29)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_15_LessAccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;

public class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    internal void Add(long x){}
    internal void Add(int x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (27,29): error CS9224: Method 'MyCollection.Add(long)' cannot be less visible than the member with params collection 'Program.Test(params MyCollection)'.
                //     public static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection a").WithArguments("MyCollection.Add(long)", "Program.Test(params MyCollection)").WithLocation(27, 29)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_16_LessAccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;

public class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long x){}
    internal void Add(int x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerableT_17_LessAccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;

public class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    internal void Add(long x){}
    public void Add(int x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerableT_18_LessAccessibleAdd_Dynamic()
        {
            var src = """
using System.Collections;

public class MyCollection : IEnumerable
{
    public Enumerator GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    internal void Add(long x){}
    public void Add(int x){}
    internal void Add(byte x){}

    public class Enumerator
    {
        public bool MoveNext() => throw null;
        public dynamic Current => null;
    }
}

public class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    public static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerableT_19_NoElementType()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>, IEnumerable<int>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => throw null;
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (17,9): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params MyCollection)'
                //         Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "Program.Test(params MyCollection)").WithLocation(17, 9),
                // (18,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                //         Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(18, 14),
                // (19,9): error CS1501: No overload for method 'Test' takes 2 arguments
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(19, 9),
                // (22,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(22, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_20_LessAccessibleConstructorAndAdd_NoError_In_LambdaOrLocalFunction()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

public class MyCollection : IEnumerable<string>
{
    internal MyCollection() { }
    internal void Add(string s) { }

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

public class Program
{
    public void Test1()
    {
        local();
        local("a");

        void local(params MyCollection collection) { }

        var x = (params MyCollection collection) => { };
    }

    public void Test2(params MyCollection collection)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (25,23): error CS9224: Method 'MyCollection.MyCollection()' cannot be less visible than the member with params collection 'Program.Test2(params MyCollection)'.
                //     public void Test2(params MyCollection collection)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection collection").WithArguments("MyCollection.MyCollection()", "Program.Test2(params MyCollection)").WithLocation(25, 23),
                // (25,23): error CS9224: Method 'MyCollection.Add(string)' cannot be less visible than the member with params collection 'Program.Test2(params MyCollection)'.
                //     public void Test2(params MyCollection collection)
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection collection").WithArguments("MyCollection.Add(string)", "Program.Test2(params MyCollection)").WithLocation(25, 23)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_21_AddIsNotAnExtension()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

static class Ext
{
    public static void Add(this MyCollection c, long l) {}
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
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,22): error CS9227: 'MyCollection' does not contain a definition for a suitable instance 'Add' method
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.ERR_ParamsCollectionExtensionAddMethod, "params MyCollection a").WithArguments("MyCollection").WithLocation(24, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_22_ObsoleteConstructor()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    [System.Obsolete()]
    public MyCollection(){}
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
#line 100
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection a)
    {
    }

    [System.Obsolete()]
    static void Test2(params MyCollection a)
    {
        Test2();
        Test2(1);
        Test2(2, 3);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (100,9): warning CS0612: 'MyCollection.MyCollection()' is obsolete
                //         Test();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Test()").WithArguments("MyCollection.MyCollection()").WithLocation(100, 9),
                // (101,9): warning CS0612: 'MyCollection.MyCollection()' is obsolete
                //         Test(1);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Test(1)").WithArguments("MyCollection.MyCollection()").WithLocation(101, 9),
                // (102,9): warning CS0612: 'MyCollection.MyCollection()' is obsolete
                //         Test(2, 3);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Test(2, 3)").WithArguments("MyCollection.MyCollection()").WithLocation(102, 9),
                // (105,22): warning CS0612: 'MyCollection.MyCollection()' is obsolete
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "params MyCollection a").WithArguments("MyCollection.MyCollection()").WithLocation(105, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerableT_23_ObsoleteAdd()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    [System.Obsolete()]
    public void Add(long l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
#line 100
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection a)
    {
    }

    [System.Obsolete()]
    static void Test2(params MyCollection a)
    {
        Test2();
        Test2(1);
        Test2(2, 3);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (101,14): warning CS1064: The best overloaded Add method 'MyCollection.Add(long)' for the collection initializer element is obsolete.
                //         Test(1);
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAdd, "1").WithArguments("MyCollection.Add(long)").WithLocation(101, 14),
                // (102,14): warning CS1064: The best overloaded Add method 'MyCollection.Add(long)' for the collection initializer element is obsolete.
                //         Test(2, 3);
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAdd, "2").WithArguments("MyCollection.Add(long)").WithLocation(102, 14),
                // (102,17): warning CS1064: The best overloaded Add method 'MyCollection.Add(long)' for the collection initializer element is obsolete.
                //         Test(2, 3);
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAdd, "3").WithArguments("MyCollection.Add(long)").WithLocation(102, 17),
                // (105,22): warning CS1064: The best overloaded Add method 'MyCollection.Add(long)' for the collection initializer element is obsolete.
                //     static void Test(params MyCollection a)
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAdd, "params MyCollection a").WithArguments("MyCollection.Add(long)").WithLocation(105, 22)
                );
        }

        [Fact]
        public void ImplementsIEnumerable_01()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array = new List<object>();
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(object l) => Array.Add(l);
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
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(
                comp,
                sourceSymbolValidator: static (m) =>
                {
                    VerifyParams(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                symbolValidator: static (m) =>
                {
                    VerifyParamsAndAttribute(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerable_02()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public IEnumerator<string> GetEnumerator() => throw null; 
    public void Add(object l) => throw null;
}

class Program
{
    static void Main()
    {
        Test("2", 3);
        Test(["2", 3]);
    }

    static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (16,19): error CS1503: Argument 2: cannot convert from 'int' to 'string'
                //         Test("2", 3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("2", "int", "string").WithLocation(16, 19),
                // (17,14): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'string'
                //         Test(["2", 3]);
                Diagnostic(ErrorCode.ERR_BadArgType, @"[""2"", 3]").WithArguments("1", "collection expressions", "string").WithLocation(17, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ImplementsIEnumerable_03_InAttribute(bool asStruct)
        {
            var src = @"
using System;
using System.Collections;

" + (asStruct ? "struct" : "class") + @" MyCollection : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw new InvalidOperationException();

    public void Add(object l) {}
}

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params MyCollection a) {}
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params MyCollection)'
                // [Test()]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params MyCollection)").WithLocation(12, 2),
                // (15,7): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection'
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params MyCollection").WithLocation(15, 7),
                // (18,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test(2, 3)").WithArguments("Test", "2").WithLocation(18, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                Assert.Empty(attributeData1.ConstructorArguments);
            }
        }

        [Theory]
        [InlineData("IEnumerable<long>")]
        [InlineData("IReadOnlyCollection<long>")]
        [InlineData("IReadOnlyList<long>")]
        [InlineData("ICollection<long>")]
        [InlineData("IList<long>")]
        public void ArrayInterfaces(string @interface)
        {
            var src = """
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params 
""" +
                            @interface +
"""
                                         a)
    {
        var array = a.ToArray();
        if (array.Length == 0)
        {
            System.Console.WriteLine(array.Length);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", array.Length, array[0], array[^1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                sourceSymbolValidator: static (m) =>
                {
                    VerifyParams(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                symbolValidator: static (m) =>
                {
                    VerifyParamsAndAttribute(m.GlobalNamespace.GetMember<MethodSymbol>("Program.Test").Parameters.Last(), isParamCollection: true);
                },
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Theory]
        [InlineData("IEnumerable")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlyList")]
        [InlineData("ICollection")]
        [InlineData("IList")]
        public void ArrayInterfaces_InAttribute(string @interface)
        {
            var src = @"
using System.Collections.Generic;

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params " + @interface + @"<long> a) {}
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,2): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Test.Test(params IReadOnlyList<long>)'
                // [Test()]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test()").WithArguments("a", "Test.Test(params System.Collections.Generic." + @interface + "<long>)").WithLocation(4, 2),
                // (7,7): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Collections.Generic.IReadOnlyList<long>'
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Collections.Generic." + @interface + "<long>").WithLocation(7, 7),
                // (10,2): error CS1729: 'Test' does not contain a constructor that takes 2 arguments
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test(2, 3)").WithArguments("Test", "2").WithLocation(10, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                Assert.Empty(attributeData1.ConstructorArguments);
            }
        }

        [Fact]
        public void IEnumerable()
        {
            var src = """
using System.Collections;

class Program
{
    static void Test(params IEnumerable a)
    {
        Test(new object());
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (5,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params IEnumerable a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(5, 22),
                // (7,14): error CS1503: Argument 1: cannot convert from 'object' to 'params System.Collections.IEnumerable'
                //         Test(new object());
                Diagnostic(ErrorCode.ERR_BadArgType, "new object()").WithArguments("1", "object", "params System.Collections.IEnumerable").WithLocation(7, 14)
                );
        }

        [Fact]
        public void WRN_ParamsArrayInLambdaOnly_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        System.Action<IEnumerable<long>> l = (params IEnumerable<long> x) => {};
        l(null);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor);

            CompileAndVerify(
                comp,
                symbolValidator: (m) =>
                {
                    MethodSymbol l1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<>c.<Main>b__0_0");
                    AssertEx.Equal("void Program.<>c.<Main>b__0_0(System.Collections.Generic.IEnumerable<System.Int64> x)", l1.ToTestDisplayString());
                    VerifyParamsAndAttribute(l1.Parameters.Last());

                    Assert.Empty(((NamespaceSymbol)m.GlobalNamespace.GetMember("System.Runtime.CompilerServices")).GetMembers("ParamCollectionAttribute"));
                }).VerifyDiagnostics(
                    // (7,72): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                    //         System.Action<IEnumerable<long>> l = (params IEnumerable<long> x) => {};
                    Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(7, 72)
                    );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single());
            AssertEx.Equal("params System.Collections.Generic.IEnumerable<System.Int64> x", parameter.ToTestDisplayString());
            VerifyParams(parameter, isParamCollection: true);

            var src2 = """
class Program
{
    static void Main()
    {
        System.Action<long[]> l = (params long[] x) => {};
        l(null);
    }
}
""";

            comp = CreateCompilation(src2, options: TestOptions.ReleaseExe);

            comp.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor);

            comp.VerifyDiagnostics(
                // (5,36): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                //         System.Action<long[]> l = (params long[] x) => {};
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(5, 36),
                // (5,50): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                //         System.Action<long[]> l = (params long[] x) => {};
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(5, 50)
                );

            comp = CreateCompilation(src2, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            tree = comp.SyntaxTrees.Single();
            model = comp.GetSemanticModel(tree);

            parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single());
            AssertEx.Equal("params System.Int64[] x", parameter.ToTestDisplayString());
            VerifyParams(parameter, isParamArray: true);

            CompileAndVerify(
                comp,
                symbolValidator: (m) =>
                {
                    MethodSymbol l1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<>c.<Main>b__0_0");
                    AssertEx.Equal("void Program.<>c.<Main>b__0_0(System.Int64[] x)", l1.ToTestDisplayString());
                    VerifyParamsAndAttribute(l1.Parameters.Last());
                }).VerifyDiagnostics(
                    // (5,50): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                    //         System.Action<long[]> l = (params long[] x) => {};
                    Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(5, 50)
                    );
        }

        [Fact]
        public void WRN_ParamsArrayInLambdaOnly_02()
        {
            // public delegate void D1([ParamArrayAttribute] IEnumerable<int> args);
            // public delegate void D2([ParamCollectionAttribute] int[] args);
            var il = @"
.class public auto ansi sealed D1
    extends [mscorlib]System.MulticastDelegate
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            object 'object',
            native int 'method'
        ) runtime managed 
    {
    }

    .method public hidebysig newslot virtual 
        instance void Invoke (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int32> args
        ) runtime managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
    }
}

.class public auto ansi sealed D2
    extends [mscorlib]System.MulticastDelegate
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            object 'object',
            native int 'method'
        ) runtime managed 
    {
    }

    .method public hidebysig newslot virtual 
        instance void Invoke (
            int32[] args
        ) runtime managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        D1 l1 = (params IEnumerable<int> x) => {};
        D2 l2 = (params int[] x) => {};
        l1 = (IEnumerable<int> x) => {};
        l2 = (int[] x) => {};
        l1(null);
        l2(null);
    }
}
""";
            CreateCompilationWithIL(src, il).VerifyEmitDiagnostics();
        }

        [Fact]
        public void WRN_ParamsArrayInLambdaOnly_03()
        {
            var src = """
using System.Collections.Generic;

public delegate void D1(params IEnumerable<int> args);
public delegate void D2(params int[] args);

class Program
{
    static void Main()
    {
        D1 l1 = (params IEnumerable<int> x) => {};
        D2 l2 = (params int[] x) => {};
        l1 = (IEnumerable<int> x) => {};
        l2 = (int[] x) => {};
        l1(null);
        l2(null);
    }
}
""";
            CreateCompilation(src).VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParamCollectionInLocalFunctionOnly()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        void local (params IEnumerable<long> x) {};
        local(1);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor);

            CompileAndVerify(
                comp,
                symbolValidator: (m) =>
                {
                    MethodSymbol l1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>g__local|0_0");
                    AssertEx.Equal("void Program.<Main>g__local|0_0(System.Collections.Generic.IEnumerable<System.Int64> x)", l1.ToTestDisplayString());
                    VerifyParamsAndAttribute(l1.Parameters.Last());

                    Assert.Empty(((NamespaceSymbol)m.GlobalNamespace.GetMember("System.Runtime.CompilerServices")).GetMembers("ParamCollectionAttribute"));
                }).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single());
            AssertEx.Equal("params System.Collections.Generic.IEnumerable<System.Int64> x", parameter.ToTestDisplayString());
            VerifyParams(parameter, isParamCollection: true);

            var src2 = """
class Program
{
    static void Main()
    {
        void local (params long[] x) {};
        local(1);
    }
}
""";

            comp = CreateCompilation(src2, options: TestOptions.ReleaseExe);

            comp.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor);

            comp.VerifyDiagnostics(
                // (5,21): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                //         void local (params long[] x) {};
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params long[] x").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(5, 21)
                );

            comp = CreateCompilation(src2, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));

            tree = comp.SyntaxTrees.Single();
            model = comp.GetSemanticModel(tree);

            parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single());
            AssertEx.Equal("params System.Int64[] x", parameter.ToTestDisplayString());
            VerifyParams(parameter, isParamArray: true);

            CompileAndVerify(
                comp,
                symbolValidator: (m) =>
                {
                    MethodSymbol l1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>g__local|0_0");
                    AssertEx.Equal("void Program.<Main>g__local|0_0(System.Int64[] x)", l1.ToTestDisplayString());
                    VerifyParamsAndAttribute(l1.Parameters.Last());
                }).VerifyDiagnostics();
        }

        [Theory]
        [InlineData(@"$""Literal{1}""")]
        [InlineData(@"$""Literal"" + $""{1}""")]
        public void ConversionInParamsArguments_InterpolatedStringHandler(string expression)
        {
            var code = @"
using System;
using System.Linq;

M(" + expression + ", " + expression + @");

void M(params System.ReadOnlySpan<CustomHandler> handlers)
{
    Console.WriteLine(string.Join(Environment.NewLine, handlers.ToArray().Select(h => h.ToString())));
}
";

            var verifier = CompileAndVerify(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false, includeOneTimeHelpers: false) }, targetFramework: TargetFramework.Net80,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsReadOnlySpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x11 }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
literal:Literal
value:1
alignment:0
format:

literal:Literal
value:1
alignment:0
format:
"));

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size      122 (0x7a)
  .maxstack  5
  .locals init (<>y__InlineArray2<CustomHandler> V_0,
                CustomHandler V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""<>y__InlineArray2<CustomHandler>""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""ref CustomHandler <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<CustomHandler>, CustomHandler>(ref <>y__InlineArray2<CustomHandler>, int)""
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.7
  IL_0013:  ldc.i4.1
  IL_0014:  call       ""CustomHandler..ctor(int, int)""
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldstr      ""Literal""
  IL_0020:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldc.i4.1
  IL_0028:  box        ""int""
  IL_002d:  ldc.i4.0
  IL_002e:  ldnull
  IL_002f:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0034:  ldloc.1
  IL_0035:  stobj      ""CustomHandler""
  IL_003a:  ldloca.s   V_0
  IL_003c:  ldc.i4.1
  IL_003d:  call       ""ref CustomHandler <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<CustomHandler>, CustomHandler>(ref <>y__InlineArray2<CustomHandler>, int)""
  IL_0042:  ldloca.s   V_1
  IL_0044:  ldc.i4.7
  IL_0045:  ldc.i4.1
  IL_0046:  call       ""CustomHandler..ctor(int, int)""
  IL_004b:  ldloca.s   V_1
  IL_004d:  ldstr      ""Literal""
  IL_0052:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0057:  ldloca.s   V_1
  IL_0059:  ldc.i4.1
  IL_005a:  box        ""int""
  IL_005f:  ldc.i4.0
  IL_0060:  ldnull
  IL_0061:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0066:  ldloc.1
  IL_0067:  stobj      ""CustomHandler""
  IL_006c:  ldloca.s   V_0
  IL_006e:  ldc.i4.2
  IL_006f:  call       ""System.ReadOnlySpan<CustomHandler> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<CustomHandler>, CustomHandler>(in <>y__InlineArray2<CustomHandler>, int)""
  IL_0074:  call       ""void Program.<<Main>$>g__M|0_0(scoped System.ReadOnlySpan<CustomHandler>)""
  IL_0079:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_01_NamedArguments()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class Program
{
    static void Main()
    {
        Test(b: GetB(), c: GetC(), a: GetA());
    }

    static void Test(int a, int b, params MyCollection c)
    {
    }

    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
GetB
Create
GetC
Add
GetA
").VerifyDiagnostics();

            // Note, the collection is created after the lexically previous argument is evaluated, 
            // but before the lexically following argument is evaluated. This differs from params
            // array case, which is created right before the target methos is invoked, after all
            // arguments are evaluated in their lexical order, which can be observed in a unit-test
            // Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenTests.NamedParamsOptimizationAndParams002​
            verifier.VerifyIL("Program.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  call       ""int Program.GetB()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""MyCollection..ctor()""
  IL_000b:  dup
  IL_000c:  call       ""int Program.GetC()""
  IL_0011:  callvirt   ""void MyCollection.Add(int)""
  IL_0016:  stloc.1
  IL_0017:  call       ""int Program.GetA()""
  IL_001c:  ldloc.0
  IL_001d:  ldloc.1
  IL_001e:  call       ""void Program.Test(int, int, params MyCollection)""
  IL_0023:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_02_CompoundAssignment()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class Program
{
    private MyCollection _c;

    static void Main()
    {
        System.Console.WriteLine("---Test1");
        Test1(new Program());
        System.Console.WriteLine("---Test2");
        Test2(new Program());
        System.Console.WriteLine("---Test3");
        Test3(new Program());
    }

    static void Test1(Program p)
    {
        p[GetA()]++;
    }

    static void Test2(Program p)
    {
        p[GetA(), GetC()]++;
    }

    static void Test3(Program p)
    {
        p[GetA(), GetB(), GetC()]++;
    }

    int this[int a, params MyCollection c]
    {
        get
        {
            System.Console.WriteLine("Get_this {0}", c is not null);
            _c = c;
            return 0;
        }
        set
        {
            System.Console.WriteLine("Set_this {0}", (object)_c == c);
        }
    }


    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
---Test1
GetA
Create
Get_this True
Set_this True
---Test2
GetA
Create
GetC
Add
Get_this True
Set_this True
---Test3
GetA
Create
GetB
Add
GetC
Add
Get_this True
Set_this True
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (int V_0,
            MyCollection V_1,
            int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.GetA()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""MyCollection..ctor()""
  IL_000c:  stloc.1
  IL_000d:  dup
  IL_000e:  ldloc.0
  IL_000f:  ldloc.1
  IL_0010:  callvirt   ""int Program.this[int, params MyCollection].get""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  callvirt   ""void Program.this[int, params MyCollection].set""
  IL_0020:  ret

}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       44 (0x2c)
  .maxstack  5
  .locals init (int V_0,
            MyCollection V_1,
            int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.GetA()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""MyCollection..ctor()""
  IL_000c:  dup
  IL_000d:  call       ""int Program.GetC()""
  IL_0012:  callvirt   ""void MyCollection.Add(int)""
  IL_0017:  stloc.1
  IL_0018:  dup
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  callvirt   ""int Program.this[int, params MyCollection].get""
  IL_0020:  stloc.2
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  ldloc.2
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  callvirt   ""void Program.this[int, params MyCollection].set""
  IL_002b:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       55 (0x37)
  .maxstack  5
  .locals init (int V_0,
            MyCollection V_1,
            int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.GetA()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""MyCollection..ctor()""
  IL_000c:  dup
  IL_000d:  call       ""int Program.GetB()""
  IL_0012:  callvirt   ""void MyCollection.Add(int)""
  IL_0017:  dup
  IL_0018:  call       ""int Program.GetC()""
  IL_001d:  callvirt   ""void MyCollection.Add(int)""
  IL_0022:  stloc.1
  IL_0023:  dup
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  callvirt   ""int Program.this[int, params MyCollection].get""
  IL_002b:  stloc.2
  IL_002c:  ldloc.0
  IL_002d:  ldloc.1
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  callvirt   ""void Program.this[int, params MyCollection].set""
  IL_0036:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_03_ObjectInitializer()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class C1
{
    public int F1;
    public int F2;
}

class Program
{
    private MyCollection _c;

    static void Main()
    {
        System.Console.WriteLine("---Test1");
        Test1();
        System.Console.WriteLine("---Test2");
        Test2();
        System.Console.WriteLine("---Test3");
        Test3();
    }

    static void Test1()
    {
        _ = new Program() { [GetA()] = { F1 = GetF1(), F2 = GetF2() } };
    }

    static void Test2()
    {
        _ = new Program() { [GetA(), GetC()] = { F1 = GetF1(), F2 = GetF2() } };
    }

    static void Test3()
    {
        _ = new Program() { [GetA(), GetB(), GetC()] = { F1 = GetF1(), F2 = GetF2() } };
    }

    C1 this[int a, params MyCollection c]
    {
        get
        {
            System.Console.WriteLine("Get_this {0}", c is not null && (_c is null || (object)_c == c));
            _c = c;
            return new C1();
        }
        set
        {
            System.Console.WriteLine("Set_this {0}", (object)_c == c);
        }
    }


    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }

    static int GetF1()
    {
        System.Console.WriteLine("GetF1");
        return 0;
    }

    static int GetF2()
    {
        System.Console.WriteLine("GetF2");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
---Test1
GetA
Create
Get_this True
GetF1
Get_this True
GetF2
---Test2
GetA
Create
GetC
Add
Get_this True
GetF1
Get_this True
GetF2
---Test3
GetA
Create
GetB
Add
GetC
Add
Get_this True
GetF1
Get_this True
GetF2
").VerifyDiagnostics();

            // Note, the collection is created once and that same instance is used across multiple invocation of the indexer.
            // With params arrays, however, only individual elements are cached and each invocation of the indexer is getting
            // a new instance of an array (with the same values inside though). This can be observed in a unit-test
            // Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.ObjectAndCollectionInitializerTests.DictionaryInitializerTestSideeffects001param
            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""int Program.GetA()""
  IL_000a:  stloc.0
  IL_000b:  newobj     ""MyCollection..ctor()""
  IL_0010:  stloc.1
  IL_0011:  dup
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0019:  call       ""int Program.GetF1()""
  IL_001e:  stfld      ""int C1.F1""
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_002a:  call       ""int Program.GetF2()""
  IL_002f:  stfld      ""int C1.F2""
  IL_0034:  ret
}
");

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "Test1").Single();

            VerifyFlowGraph(comp, node, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program ... GetF2() } }')
              Value:
                IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program ... GetF2() } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetA()')
                  Value:
                    IInvocationOperation (System.Int32 Program.GetA()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetA()')
                      Instance Receiver:
                        null
                      Arguments(0)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[GetA()]')
                  Value:
                    ICollectionExpressionOperation (0 elements, ConstructMethod: MyCollection..ctor()) (OperationKind.CollectionExpression, Type: MyCollection, IsImplicit) (Syntax: '[GetA()]')
                      Elements(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'F1 = GetF1()')
                  Left:
                    IFieldReferenceOperation: System.Int32 C1.F1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F1')
                      Instance Receiver:
                        IPropertyReferenceOperation: C1 Program.this[System.Int32 a, params MyCollection c] { get; set; } (OperationKind.PropertyReference, Type: C1) (Syntax: '[GetA()]')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Program, IsImplicit) (Syntax: 'new Program ... GetF2() } }')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'GetA()')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetA()')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.ParamCollection, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[GetA()]')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyCollection, IsImplicit) (Syntax: '[GetA()]')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    IInvocationOperation (System.Int32 Program.GetF1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetF1()')
                      Instance Receiver:
                        null
                      Arguments(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'F2 = GetF2()')
                  Left:
                    IFieldReferenceOperation: System.Int32 C1.F2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F2')
                      Instance Receiver:
                        IPropertyReferenceOperation: C1 Program.this[System.Int32 a, params MyCollection c] { get; set; } (OperationKind.PropertyReference, Type: C1) (Syntax: '[GetA()]')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Program, IsImplicit) (Syntax: 'new Program ... GetF2() } }')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'GetA()')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetA()')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.ParamCollection, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[GetA()]')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyCollection, IsImplicit) (Syntax: '[GetA()]')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    IInvocationOperation (System.Int32 Program.GetF2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetF2()')
                      Instance Receiver:
                        null
                      Arguments(0)
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new Pro ... etF2() } };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Program) (Syntax: '_ = new Pro ... GetF2() } }')
                  Left:
                    IDiscardOperation (Symbol: Program _) (OperationKind.Discard, Type: Program) (Syntax: '_')
                  Right:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Program, IsImplicit) (Syntax: 'new Program ... GetF2() } }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
""");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       64 (0x40)
  .maxstack  4
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""int Program.GetA()""
  IL_000a:  stloc.0
  IL_000b:  newobj     ""MyCollection..ctor()""
  IL_0010:  dup
  IL_0011:  call       ""int Program.GetC()""
  IL_0016:  callvirt   ""void MyCollection.Add(int)""
  IL_001b:  stloc.1
  IL_001c:  dup
  IL_001d:  ldloc.0
  IL_001e:  ldloc.1
  IL_001f:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0024:  call       ""int Program.GetF1()""
  IL_0029:  stfld      ""int C1.F1""
  IL_002e:  ldloc.0
  IL_002f:  ldloc.1
  IL_0030:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0035:  call       ""int Program.GetF2()""
  IL_003a:  stfld      ""int C1.F2""
  IL_003f:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       75 (0x4b)
  .maxstack  4
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""int Program.GetA()""
  IL_000a:  stloc.0
  IL_000b:  newobj     ""MyCollection..ctor()""
  IL_0010:  dup
  IL_0011:  call       ""int Program.GetB()""
  IL_0016:  callvirt   ""void MyCollection.Add(int)""
  IL_001b:  dup
  IL_001c:  call       ""int Program.GetC()""
  IL_0021:  callvirt   ""void MyCollection.Add(int)""
  IL_0026:  stloc.1
  IL_0027:  dup
  IL_0028:  ldloc.0
  IL_0029:  ldloc.1
  IL_002a:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_002f:  call       ""int Program.GetF1()""
  IL_0034:  stfld      ""int C1.F1""
  IL_0039:  ldloc.0
  IL_003a:  ldloc.1
  IL_003b:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0040:  call       ""int Program.GetF2()""
  IL_0045:  stfld      ""int C1.F2""
  IL_004a:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_04_ObjectInitializer()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class C1
{
}

class Program
{
    private MyCollection _c;

    static void Main()
    {
        System.Console.WriteLine("---Test1");
        Test1();
        System.Console.WriteLine("---Test2");
        Test2();
        System.Console.WriteLine("---Test3");
        Test3();
    }

    static void Test1()
    {
        _ = new Program() { [GetA()] = { } };
    }

    static void Test2()
    {
        _ = new Program() { [GetA(), GetC()] = { } };
    }

    static void Test3()
    {
        _ = new Program() { [GetA(), GetB(), GetC()] = { } };
    }

    C1 this[int a, params MyCollection c]
    {
        get
        {
            System.Console.WriteLine("Get_this {0}", c is not null && (_c is null || (object)_c == c));
            _c = c;
            return new C1();
        }
        set
        {
            System.Console.WriteLine("Set_this {0}", (object)_c == c);
        }
    }


    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
---Test1
GetA
Create
---Test2
GetA
Create
GetC
Add
---Test3
GetA
Create
GetB
Add
GetC
Add
").VerifyDiagnostics();

            // Note, the collection is created even though the getter is never invoked
            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  pop
  IL_0006:  call       ""int Program.GetA()""
  IL_000b:  pop
  IL_000c:  newobj     ""MyCollection..ctor()""
  IL_0011:  pop
  IL_0012:  ret
}
");

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "Test1").Single();

            VerifyFlowGraph(comp, node, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program ... ()] = { } }')
              Value:
                IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program ... ()] = { } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Int32 Program.GetA()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetA()')
              Instance Receiver:
                null
              Arguments(0)
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyCollection..ctor()) (OperationKind.CollectionExpression, Type: MyCollection, IsImplicit) (Syntax: '[GetA()]')
              Elements(0)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new Pro ... )] = { } };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Program) (Syntax: '_ = new Pro ... ()] = { } }')
                  Left:
                    IDiscardOperation (Symbol: Program _) (OperationKind.Discard, Type: Program) (Syntax: '_')
                  Right:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Program, IsImplicit) (Syntax: 'new Program ... ()] = { } }')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  pop
  IL_0006:  call       ""int Program.GetA()""
  IL_000b:  pop
  IL_000c:  newobj     ""MyCollection..ctor()""
  IL_0011:  call       ""int Program.GetC()""
  IL_0016:  callvirt   ""void MyCollection.Add(int)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  pop
  IL_0006:  call       ""int Program.GetA()""
  IL_000b:  pop
  IL_000c:  newobj     ""MyCollection..ctor()""
  IL_0011:  dup
  IL_0012:  call       ""int Program.GetB()""
  IL_0017:  callvirt   ""void MyCollection.Add(int)""
  IL_001c:  call       ""int Program.GetC()""
  IL_0021:  callvirt   ""void MyCollection.Add(int)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void LanguageVersion_01_Declaration()
        {
            var src = @"
class Program
{
    static void Test1(params System.ReadOnlySpan<long> a) {}
    static void Test2(params long[] a) {}

    void Test()
    {
        Test1(1);
        Test2(2);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (4,23): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static void Test1(params System.ReadOnlySpan<long> a) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> a").WithArguments("params collections").WithLocation(4, 23)
                );
        }

        [Fact]
        public void LanguageVersion_02_CallSite()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.ReadOnlySpan<long> a) {}
    static public void Test2(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    void Test()
    {
        Params.Test1(1);
        Params.Test2(2);

        Params.Test1();
        Params.Test2();

        Params.Test1([1]);
        Params.Test2([2]);
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
                comp2.VerifyDiagnostics(
                    // (6,9): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Params.Test1(1);
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1(1)").WithArguments("params collections").WithLocation(6, 9),
                    // (9,9): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Params.Test1();
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1()").WithArguments("params collections").WithLocation(9, 9)
                    );
            }
        }

        [Fact]
        public void LanguageVersion_03_DelegateNaturalType()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.ReadOnlySpan<long> a) {}
    static public void Test2(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    void Test()
    {
        var x1 = Params.Test1;
        var x2 = Params.Test2;

        x1(1);
        x2(2);

        x1();
        x2();

        x1([1]);
        x2([2]);
    }
}
";
            var comp = CreateCompilation(src2 + src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (22,30): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static public void Test1(params System.ReadOnlySpan<long> a) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> a").WithArguments("params collections").WithLocation(22, 30)
                );

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
                comp2.VerifyDiagnostics(
                    // (6,18): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         var x1 = Params.Test1;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1").WithArguments("params collections").WithLocation(6, 18)
                    );
            }
        }

        [Fact]
        public void LanguageVersion_04_DelegateNaturalType()
        {
            var src = @"
class Program
{
    void Test()
    {
        var x1 = (params System.ReadOnlySpan<long> a) => {};
        var x2 = (params long[] a) => {};

        x1(1);
        x2(2);

        x1();
        x2();

        x1([1]);
        x2([2]);

        M1(x1);
        M1(x2);

        M1((params System.ReadOnlySpan<long> b) => {});
        M1((params long[] b) => {});
    }

    static void M1<T>(T t) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (6,19): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var x1 = (params System.ReadOnlySpan<long> a) => {};
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> a").WithArguments("params collections").WithLocation(6, 19),
                // (21,13): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         M1((params System.ReadOnlySpan<long> b) => {});
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> b").WithArguments("params collections").WithLocation(21, 13)
                );
        }

        [Fact]
        public void LanguageVersion_05_DelegateNaturalType()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.Collections.Generic.IEnumerable<long> a) {}
    static public void Test2(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    void Test1()
    {
        var a = Params.Test1;
        M1(a); // See DelegateNaturalType_03 unit-test for an observable effect that 'params' modifier has for this invocation. 
        M1(Params.Test1);
    }

    static void M1<T>(T t) {}

    void Test2()
    {
        var b = Params.Test2;
        M1(b);
        M1(Params.Test2);
    }
}
";
            var comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (23,30): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static public void Test1(params System.Collections.Generic.IEnumerable<long> a) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.Collections.Generic.IEnumerable<long> a").WithArguments("params collections").WithLocation(23, 30)
                );

            var comp1 = CreateCompilation(src1, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
                comp2.VerifyDiagnostics(
                    // (6,17): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         var a = Params.Test1;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1").WithArguments("params collections").WithLocation(6, 17),
                    // (8,12): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         M1(Params.Test1);
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1").WithArguments("params collections").WithLocation(8, 12)
                    );
            }
        }

        [Fact]
        public void LanguageVersion_06_LambdaForDelegateWithParams()
        {
            var src1 = @"
public class Params
{
    static public void Test1(D1 d) {}
    static public void Test2(D2 d) {}
}

public delegate void D1(params System.Collections.Generic.IEnumerable<long> a);
public delegate void D2(params long[] a);
";
            var src2 = @"
class Program1
{
    void Test1()
    {
        Params.Test1(e1 => { });
    }
}
class Program2
{
    void Test2()
    {
        Params.Test2(e2 => { });
    }
}
";
            var comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (23,25): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // public delegate void D1(params System.Collections.Generic.IEnumerable<long> a);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.Collections.Generic.IEnumerable<long> a").WithArguments("params collections").WithLocation(23, 25)
                );

            var comp1 = CreateCompilation(src1, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular12);

                var tree = comp2.SyntaxTrees.Single();
                var model = comp2.GetSemanticModel(tree);

                var parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().First());
                AssertEx.Equal("System.Collections.Generic.IEnumerable<System.Int64> e1", parameter.ToTestDisplayString());
                Assert.False(parameter.IsParams);
                Assert.False(parameter.IsParamsArray);
                Assert.False(parameter.IsParamsCollection);

                parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Skip(1).First());
                AssertEx.Equal("System.Int64[] e2", parameter.ToTestDisplayString());
                Assert.False(parameter.IsParams);
                Assert.False(parameter.IsParamsArray);
                Assert.False(parameter.IsParamsCollection);

                CompileAndVerify(comp2,
                    symbolValidator: (m) =>
                    {
                        var lambda = m.GlobalNamespace.GetMember<MethodSymbol>("Program1.<>c.<Test1>b__0_0");
                        ParameterSymbol parameter = lambda.Parameters.Single();

                        VerifyParamsAndAttribute(parameter);
                        Assert.Equal("System.Collections.Generic.IEnumerable<System.Int64> e1", parameter.ToTestDisplayString());

                        lambda = m.GlobalNamespace.GetMember<MethodSymbol>("Program2.<>c.<Test2>b__0_0");
                        parameter = lambda.Parameters.Single();

                        VerifyParamsAndAttribute(parameter);
                        Assert.Equal("System.Int64[] e2", parameter.ToTestDisplayString());
                    }
                    ).VerifyDiagnostics(); // No language version diagnostics as expected. The 'params' modifier doesn't even make it to symbol and metadata.
            }
        }

        [Fact]
        public void DelegateNaturalType_01()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.ReadOnlySpan<long> a) { System.Console.WriteLine(a.Length); }
    static public void Test2(params long[] a) { System.Console.WriteLine(a.Length); }
}
";
            var src2 = @"
class Program
{
    static void Main()
    {
        var x1 = Params.Test1;
        var x2 = Params.Test2;

        x1(1);
        x2(2);

        x1();
        x2();
    }
}
";
            var comp = CreateCompilation(src1 + src2, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            verify(comp, attributeIsEmbedded: true);

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            verify(comp2, attributeIsEmbedded: true);

            var comp3 = CreateCompilation(src1 + ParamCollectionAttributeSource, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var comp4 = CreateCompilation(src2, references: [comp3.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            verify(comp4, attributeIsEmbedded: false);

            var comp5 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseModule);
            comp5.VerifyDiagnostics();

            void verify(CSharpCompilation comp, bool attributeIsEmbedded)
            {
                // We want to test attribute embedding 
                Assert.Equal(attributeIsEmbedded, comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor) is null);

                CompileAndVerify(
                    comp,
                    verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                    symbolValidator: (m) =>
                    {
                        MethodSymbol delegateInvokeMethod1 = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate0").DelegateInvokeMethod;
                        AssertEx.Equal("void <>f__AnonymousDelegate0.Invoke(params System.ReadOnlySpan<System.Int64> arg)", delegateInvokeMethod1.ToTestDisplayString());
                        VerifyParamsAndAttribute(delegateInvokeMethod1.Parameters.Last(), isParamCollection: true);

                        MethodSymbol delegateInvokeMethod2 = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate1`1").DelegateInvokeMethod;
                        AssertEx.Equal("void <>f__AnonymousDelegate1<T1>.Invoke(params T1[] arg)", delegateInvokeMethod2.ToTestDisplayString());
                        VerifyParamsAndAttribute(delegateInvokeMethod2.Parameters.Last(), isParamArray: true);

                        if (attributeIsEmbedded)
                        {
                            Assert.NotNull(m.GlobalNamespace.GetMember("System.Runtime.CompilerServices.ParamCollectionAttribute"));
                        }
                        else
                        {
                            Assert.Empty(m.GlobalNamespace.GetMembers("System"));
                        }
                    },
                    expectedOutput: ExpectedOutput(@"
1
1
0
0
")).VerifyDiagnostics();
            }
        }

        [Fact]
        public void DelegateNaturalType_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var x1 = (params System.ReadOnlySpan<long> a) => System.Console.WriteLine(a.Length);
        var x2 = (params long[] a) => System.Console.WriteLine(a.Length);

        x1(1);
        x2(2);

        x1();
        x2();
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp, attributeIsEmbedded: true);

            var comp1 = CreateCompilation(ParamCollectionAttributeSource, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var comp2 = CreateCompilation(src, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp2, attributeIsEmbedded: false);

            void verify(CSharpCompilation comp, bool attributeIsEmbedded)
            {
                // We want to test attribute embedding 
                Assert.Equal(attributeIsEmbedded, comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor) is null);

                CompileAndVerify(
                    comp,
                    verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                    symbolValidator: (m) =>
                    {
                        MethodSymbol delegateInvokeMethod1 = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate0").DelegateInvokeMethod;
                        AssertEx.Equal("void <>f__AnonymousDelegate0.Invoke(params System.ReadOnlySpan<System.Int64> arg)", delegateInvokeMethod1.ToTestDisplayString());
                        VerifyParamsAndAttribute(delegateInvokeMethod1.Parameters.Last(), isParamCollection: true);

                        MethodSymbol delegateInvokeMethod2 = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate1`1").DelegateInvokeMethod;
                        AssertEx.Equal("void <>f__AnonymousDelegate1<T1>.Invoke(params T1[] arg)", delegateInvokeMethod2.ToTestDisplayString());
                        VerifyParamsAndAttribute(delegateInvokeMethod2.Parameters.Last(), isParamArray: true);

                        // Note, no attributes on lambdas

                        MethodSymbol l1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<>c.<Main>b__0_0");
                        AssertEx.Equal("void Program.<>c.<Main>b__0_0(scoped System.ReadOnlySpan<System.Int64> a)", l1.ToTestDisplayString());
                        VerifyParamsAndAttribute(l1.Parameters.Last());

                        MethodSymbol l2 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<>c.<Main>b__0_1");
                        AssertEx.Equal("void Program.<>c.<Main>b__0_1(System.Int64[] a)", l2.ToTestDisplayString());
                        VerifyParamsAndAttribute(l2.Parameters.Last());

                        if (attributeIsEmbedded)
                        {
                            Assert.NotNull(m.GlobalNamespace.GetMember("System.Runtime.CompilerServices.ParamCollectionAttribute"));
                        }
                        else
                        {
                            Assert.Empty(m.GlobalNamespace.GetMembers("System"));
                        }
                    },
                    expectedOutput: ExpectedOutput(@"
1
1
0
0
")).VerifyDiagnostics();
            }
        }

        [Fact]
        public void DelegateNaturalType_03()
        {
            var src = @"
class Program
{
    static public void Test1(System.Collections.Generic.IEnumerable<long> a) { System.Console.WriteLine("" {0}"", a is not null); }
    static public void Test2(params System.Collections.Generic.IEnumerable<long> a) { System.Console.WriteLine("" {0}"", a is not null); }
    static public void Test3(params System.Collections.Generic.List<long> a) { System.Console.WriteLine("" {0}"", a is not null); }
    static public void Test4(params long[] a) { System.Console.WriteLine("" {0}"", a is not null); }

    static void Main()
    {
        DoTest1();
        DoTest21();
        DoTest22();
        DoTest3();
        DoTest4();
    }

    static void DoTest1()
    {
        var a1 = Test1;
        M(a1);
    }

    static void DoTest21()
    {
        var a2 = Test2;
        M(a2)();
    }

    static void DoTest22()
    {
        var a2 = Test2;
        M(a2)();
    }

    static void DoTest3()
    {
        var a3 = Test3;
        M(a3)();
    }

    static void DoTest4()
    {
        var a4 = Test4;
        M(a4)();
    }

    static T M<T>(T t) { System.Console.WriteLine(typeof(T)); return t; }
    static void M(System.Action<System.Collections.Generic.IEnumerable<long>> t) => System.Console.WriteLine(""Action"");
    static void M(System.Action<System.Collections.Generic.List<long>> t) => System.Console.WriteLine(""Action"");
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                symbolValidator: (m) =>
                {
                    AssertEx.Equal("void <>f__AnonymousDelegate0.Invoke(params System.Collections.Generic.IEnumerable<System.Int64> arg)", m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate0").DelegateInvokeMethod.ToTestDisplayString());
                    AssertEx.Equal("void <>f__AnonymousDelegate1.Invoke(params System.Collections.Generic.List<System.Int64> arg)", m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate1").DelegateInvokeMethod.ToTestDisplayString());
                    AssertEx.Equal("void <>f__AnonymousDelegate2<T1>.Invoke(params T1[] arg)", m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate2`1").DelegateInvokeMethod.ToTestDisplayString());
                },
                expectedOutput: ExpectedOutput(@"
Action
<>f__AnonymousDelegate0
 True
<>f__AnonymousDelegate0
 True
<>f__AnonymousDelegate1
 True
<>f__AnonymousDelegate2`1[System.Int64]
 True
")).VerifyDiagnostics();
        }

        [Fact]
        public void DelegateNaturalType_04()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var x1 = Test1;
        var x2 = Test2;

        x1(1);
        x2(2);

        x1();
        x2();

        static void Test1(params System.ReadOnlySpan<long> a) { System.Console.WriteLine(a.Length); }
        static void Test2(params long[] a) { System.Console.WriteLine(a.Length); }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp, attributeIsEmbedded: true);

            var comp1 = CreateCompilation(ParamCollectionAttributeSource, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var comp2 = CreateCompilation(src, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp2, attributeIsEmbedded: false);

            void verify(CSharpCompilation comp, bool attributeIsEmbedded)
            {
                // We want to test attribute embedding 
                Assert.Equal(attributeIsEmbedded, comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor) is null);

                CompileAndVerify(
                    comp,
                    verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                    symbolValidator: (m) =>
                    {
                        MethodSymbol delegateInvokeMethod1 = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate0").DelegateInvokeMethod;
                        AssertEx.Equal("void <>f__AnonymousDelegate0.Invoke(params System.ReadOnlySpan<System.Int64> arg)", delegateInvokeMethod1.ToTestDisplayString());
                        VerifyParamsAndAttribute(delegateInvokeMethod1.Parameters.Last(), isParamCollection: true);

                        MethodSymbol delegateInvokeMethod2 = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate1`1").DelegateInvokeMethod;
                        AssertEx.Equal("void <>f__AnonymousDelegate1<T1>.Invoke(params T1[] arg)", delegateInvokeMethod2.ToTestDisplayString());
                        VerifyParamsAndAttribute(delegateInvokeMethod2.Parameters.Last(), isParamArray: true);

                        // Note, no attributes on local functions

                        MethodSymbol l1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>g__Test1|0_0");
                        AssertEx.Equal("void Program.<Main>g__Test1|0_0(scoped System.ReadOnlySpan<System.Int64> a)", l1.ToTestDisplayString());
                        VerifyParamsAndAttribute(l1.Parameters.Last());

                        MethodSymbol l2 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>g__Test2|0_1");
                        AssertEx.Equal("void Program.<Main>g__Test2|0_1(System.Int64[] a)", l2.ToTestDisplayString());
                        VerifyParamsAndAttribute(l2.Parameters.Last());

                        if (attributeIsEmbedded)
                        {
                            Assert.NotNull(m.GlobalNamespace.GetMember("System.Runtime.CompilerServices.ParamCollectionAttribute"));
                        }
                        else
                        {
                            Assert.Empty(m.GlobalNamespace.GetMembers("System"));
                        }
                    },
                    expectedOutput: ExpectedOutput(@"
1
1
0
0
")).VerifyDiagnostics();
            }
        }

        [Fact]
        public void DelegateNaturalType_05()
        {
            var src1 = @"
using System.Collections.Generic;

public class Params
{
    static public void Test1(params IEnumerable<long> a) {}
}
";
            var src2 = @"
class Program
{
    static void Main()
    {
        var x1 = Params.Test1;

        x1(1);
        x1();
    }
}
";

            var comp1 = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);

            comp2.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            comp2.VerifyEmitDiagnostics();
        }

        [Fact]
        public void DelegateNaturalType_06()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    static void Main()
    {
        var x1 = Params.Test1;

        x1(1);
        x1();
    }
}
";

            var comp1 = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);

            comp2.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            comp2.VerifyDiagnostics(
                // (6,18): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                //         var x1 = Params.Test1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "Params.Test1").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(6, 18)
                );
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
                // (7,27): error CS0117: 'C1' does not contain a definition for 'Add'
                //     public static void M1(params C1 x)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params C1 x").WithArguments("C1", "Add").WithLocation(7, 27)
                );
        }

        [Theory]
        [InlineData("System.Span<T>", "T[]", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IEnumerable<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IReadOnlyCollection<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IReadOnlyList<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.ICollection<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IList<T>", "System.Span<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "T[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IEnumerable<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IReadOnlyCollection<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IReadOnlyList<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.ICollection<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IList<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.HashSet<T>", null)] // rule requires array or array interface

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Span<T>", "System.ReadOnlySpan<object>", "System.Span<System.Int32>")] // cannot convert from object to int

        [InlineData("RefStructCollection<T>", "T[]", null, new[] { CollectionExpressionTests.example_RefStructCollection })] // rule requires span

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<T>", "RefStructCollection<object>", "RefStructCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection })] // rule requires span
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<int>", "GenericClassCollection<object>", "RefStructCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection, CollectionExpressionTests.example_GenericClassCollection })] // rule requires span
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<object>", "GenericClassCollection<int>", "GenericClassCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection, CollectionExpressionTests.example_GenericClassCollection })] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<int>", "NonGenericClassCollection", "RefStructCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection, CollectionExpressionTests.example_NonGenericClassCollection })] // rule requires span

        [InlineData("GenericClassCollection<T>", "T[]", null, new[] { CollectionExpressionTests.example_GenericClassCollection })] // rule requires span
        [InlineData("NonGenericClassCollection", "object[]", null, new[] { CollectionExpressionTests.example_NonGenericClassCollection })] // rule requires span
        [InlineData("System.ReadOnlySpan<T>", "object[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "long[]", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.ReadOnlySpan<T>", "short[]", "System.ReadOnlySpan<System.Int32>")] // cannot convert int to short
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("System.ReadOnlySpan<long>", "T[]", "System.Int32[]")] // cannot convert long to int
        // Ambiguous for inline collection expression, but 'long' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "long[]", "System.Int64[]")] // cannot convert object to long

        [InlineData("System.ReadOnlySpan<long>", "object[]", "System.ReadOnlySpan<System.Int64>")]
        [InlineData("System.ReadOnlySpan<long>", "string[]", "System.ReadOnlySpan<System.Int64>")]
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<string>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Span<T>", "System.ReadOnlySpan<System.Int32>")]

        // Inline collection expression picks "System.ReadOnlySpan<System.Int32>", but that params candidate is worse because it is generic
        [InlineData("System.ReadOnlySpan<T>", "System.Span<int>", "System.Span<System.Int32>")]

        [InlineData("System.ReadOnlySpan<T>", "System.Span<object>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.ReadOnlySpan<T>", "System.Span<short>", "System.ReadOnlySpan<System.Int32>")]

        [InlineData("System.ReadOnlySpan<T>", "System.ReadOnlySpan<int>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<T>", "System.ReadOnlySpan<object>", "System.ReadOnlySpan<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("System.ReadOnlySpan<T>", "System.ReadOnlySpan<long>", "System.ReadOnlySpan<System.Int32>")]

        [InlineData("System.Span<T>", "System.Span<int>", "System.Span<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Span<T>", "System.Span<object>", "System.Span<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.Span<T>", "System.Span<short>", "System.Span<System.Int32>")]

        [InlineData("System.Span<T>", "System.Span<string>", "System.Span<System.Int32>")]
        [InlineData("T[]", "int[]", "System.Int32[]")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("T[]", "object[]", "System.Int32[]")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("T[]", "int?[]", "System.Int32[]")]

        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.ICollection<int>", "System.Collections.Generic.ICollection<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.ICollection<object>", "System.Collections.Generic.ICollection<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.ICollection<short>", "System.Collections.Generic.ICollection<System.Int32>")]

        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.IReadOnlyCollection<T>", null)]
        [InlineData("MyCollectionA<T>", "MyCollectionB<T>", "MyCollectionB<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]

        // Inline collection expression picks "MyCollectionB<System.Int32>", but that params candidate is worse because it is generic
        [InlineData("MyCollectionA<int>", "MyCollectionB<T>", "MyCollectionA<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("MyCollectionA<T>", "MyCollectionB<long>", "MyCollectionA<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("MyCollectionA<T>", "MyCollectionB<object>", "MyCollectionA<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("MyCollectionB<T>", "MyCollectionB<long>", "MyCollectionB<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]

        [InlineData("RefStructConvertibleFromArray<T>", "T[]", "System.Int32[]", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]
        [InlineData("RefStructConvertibleFromArray<T>", "int[]", "System.Int32[]", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructConvertibleFromArray<object>", "T[]", "System.Int32[]", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructConvertibleFromArray<T>", "object[]", "RefStructConvertibleFromArray<System.Int32>", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]
        public void BetterConversionFromExpression_01A(string type1, string type2, string expectedType, string[] additionalSources = null) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                class Program
                {
                    {{generateMethod("F1", type1)}}
                    {{generateMethod("F1", type2)}}
                    {{generateMethod("F2", type2)}}
                    {{generateMethod("F2", type1)}}
                    static void Main()
                    {
                        var x = F1(1, 2, 3);
                        Console.WriteLine(x.GetTypeName());
                        var y = F2(4, 5);
                        Console.WriteLine(y.GetTypeName());
                    }
                }
                """;
            var comp = CreateCompilation(
                getSources(source, additionalSources),
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            if (expectedType is { })
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($"""
                    {expectedType}
                    {expectedType}
                    """));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(ReadOnlySpan<long>)' and 'Program.F1(ReadOnlySpan<object>)'
                    //         var x = F1(1, 2, 3);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments(generateMethodSignature("F1", type1), generateMethodSignature("F1", type2)).WithLocation(10, 17),
                    // 0.cs(12,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(ReadOnlySpan<object>)' and 'Program.F2(ReadOnlySpan<long>)'
                    //         var y = F2(4, 5);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments(generateMethodSignature("F2", type2), generateMethodSignature("F2", type1)).WithLocation(12, 17));
            }

            static string getTypeParameters(string type) =>
                type.Contains("T[]") || type.Contains("<T>") ? "<T>" : "";

            static string generateMethod(string methodName, string parameterType) =>
                $"static Type {methodName}{getTypeParameters(parameterType)}(params {parameterType} value) => typeof({parameterType});";

            static string generateMethodSignature(string methodName, string parameterType) =>
                $"Program.{methodName}{getTypeParameters(parameterType)}(params {parameterType})";

            static string[] getSources(string source, string[] additionalSources)
            {
                var builder = ArrayBuilder<string>.GetInstance();
                builder.Add(source);
                builder.Add(CollectionExpressionTests.s_collectionExtensions);
                if (additionalSources is { }) builder.AddRange(additionalSources);
                return builder.ToArrayAndFree();
            }
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int?>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<object>", "System.Span<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<int?>", "System.Span<int>", null)] // cannot convert int? to int
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<object>", null)]
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<int?>", null)]
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<int?>", null)]
        [InlineData("System.Span<int>", "System.Span<object>", null)]
        [InlineData("System.Span<int>", "System.Span<int?>", null)]
        [InlineData("System.Span<object>", "System.Span<int?>", null)]
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<long>", null)]
        [InlineData("System.Span<int>", "int?[]", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IEnumerable<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyCollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyList<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.ICollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IList<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int?>", "int[]", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IEnumerable<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyCollection<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyList<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.ICollection<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IList<int>", null)] // cannot convert int? to int
        [InlineData("System.ReadOnlySpan<int>", "object[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IEnumerable<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyCollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyList<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.ICollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IList<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<object>", "int[]", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IEnumerable<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyCollection<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyList<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.ICollection<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IList<int>", null)] // cannot convert object to int
        [InlineData("System.Collections.Generic.List<int>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.List<System.Int32>")]
        [InlineData("int[]", "object[]", null)] // rule requires span
        [InlineData("int[]", "System.Collections.Generic.IReadOnlyList<object>", null)] // rule requires span
        public void BetterConversionFromExpression_01B_Empty(string type1, string type2, string expectedType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                class Program
                {
                    {{generateMethod("F1", type1)}}
                    {{generateMethod("F1", type2)}}
                    {{generateMethod("F2", type2)}}
                    {{generateMethod("F2", type1)}}
                    static void Main()
                    {
                        var a = F1();
                        Console.WriteLine(a.GetTypeName());
                        var b = F2();
                        Console.WriteLine(b.GetTypeName());
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, CollectionExpressionTests.s_collectionExtensions },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            if (expectedType is { })
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($"""
                    {expectedType}
                    {expectedType}
                    """));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[])' and 'Program.F1(object[])'
                    //         var a = F1();
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments(generateMethodSignature("F1", type1), generateMethodSignature("F1", type2)).WithLocation(10, 17),
                    // 0.cs(12,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(object[])' and 'Program.F2(int[])'
                    //         var b = F2();
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments(generateMethodSignature("F2", type2), generateMethodSignature("F2", type1)).WithLocation(12, 17));
            }

            static string generateMethod(string methodName, string parameterType) =>
                $"static Type {methodName}(params {parameterType} value) => typeof({parameterType});";

            static string generateMethodSignature(string methodName, string parameterType) =>
                $"Program.{methodName}(params {parameterType})";
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int?>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Span<int>", "System.Span<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.ReadOnlySpan<int?>", "System.Span<int>", "System.Span<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<object>", "System.ReadOnlySpan<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<int?>", "System.ReadOnlySpan<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int?' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<int?>", "System.ReadOnlySpan<System.Nullable<System.Int32>>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Span<int>", "System.Span<object>", "System.Span<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int>", "System.Span<int?>", "System.Span<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int?' is a better conversion target than 'object' in params case
        [InlineData("System.Span<object>", "System.Span<int?>", "System.Span<System.Nullable<System.Int32>>")]
        // Ambiguous for inline collection expression, but 'long' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<long>", "System.ReadOnlySpan<System.Int64>")]

        [InlineData("System.Span<int>", "int?[]", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IEnumerable<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyCollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyList<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.ICollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IList<int?>", "System.Span<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "int[]", "System.Int32[]")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.IEnumerable<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyCollection<int>", "System.Collections.Generic.IReadOnlyCollection<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyList<int>", "System.Collections.Generic.IReadOnlyList<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.ICollection<int>", "System.Collections.Generic.ICollection<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IList<int>", "System.Collections.Generic.IList<System.Int32>")] // cannot convert int? to int

        [InlineData("System.ReadOnlySpan<int>", "object[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IEnumerable<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyCollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyList<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.ICollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IList<object>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "int[]", "System.Int32[]")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.IEnumerable<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyCollection<int>", "System.Collections.Generic.IReadOnlyCollection<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyList<int>", "System.Collections.Generic.IReadOnlyList<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.ICollection<int>", "System.Collections.Generic.ICollection<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IList<int>", "System.Collections.Generic.IList<System.Int32>")] // cannot convert object to int

        [InlineData("System.Collections.Generic.List<int>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.List<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("int[]", "object[]", "System.Int32[]")] // rule requires span
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("int[]", "System.Collections.Generic.IReadOnlyList<object>", "System.Int32[]")] // rule requires span
        public void BetterConversionFromExpression_01B_NotEmpty(string type1, string type2, string expectedType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                class Program
                {
                    {{generateMethod("F1", type1)}}
                    {{generateMethod("F1", type2)}}
                    {{generateMethod("F2", type2)}}
                    {{generateMethod("F2", type1)}}
                    static void Main()
                    {
                        var c = F1(1, 2, 3);
                        Console.WriteLine(c.GetTypeName());
                        var d = F2(4, 5);
                        Console.WriteLine(d.GetTypeName());
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, CollectionExpressionTests.s_collectionExtensions },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            if (expectedType is { })
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($"""
                    {expectedType}
                    {expectedType}
                    """));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[])' and 'Program.F1(object[])'
                    //         var c = F1(1, 2, 3);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments(generateMethodSignature("F1", type1), generateMethodSignature("F1", type2)).WithLocation(10, 17),
                    // 0.cs(12,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(object[])' and 'Program.F2(int[])'
                    //         var d = F2(4, 5);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments(generateMethodSignature("F2", type2), generateMethodSignature("F2", type1)).WithLocation(12, 17));
            }

            static string generateMethod(string methodName, string parameterType) =>
                $"static Type {methodName}(params {parameterType} value) => typeof({parameterType});";

            static string generateMethodSignature(string methodName, string parameterType) =>
                $"Program.{methodName}(params {parameterType})";
        }

        [Fact]
        public void BetterConversionFromExpression_02() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string sourceA = """
                using System;
                using static System.Console;

                partial class Program
                {
                    static void Generic<T>(params Span<T> value) { WriteLine("Span<T>"); }
                    static void Generic<T>(params T[] value)     { WriteLine("T[]"); }

                    static void Identical(params Span<string> value) { WriteLine("Span<string>"); }
                    static void Identical(params string[] value)     { WriteLine("string[]"); }

                    static void SpanDerived(params Span<string> value) { WriteLine("Span<string>"); }
                    static void SpanDerived(params object[] value)     { WriteLine("object[]"); }

                    static void ArrayDerived(params Span<object> value) { WriteLine("Span<object>"); }
                    static void ArrayDerived(params string[] value)     { WriteLine("string[]"); }
                }
                """;

            string sourceB1 = """
                partial class Program
                {
                    static void Main()
                    {
                        Generic(new[] { string.Empty }); // string[]
                        Identical(new[] { string.Empty }); // string[]
                        ArrayDerived(new[] { string.Empty }); // string[]

                        Generic(string.Empty); // Span<string>
                        Identical(string.Empty); // Span<string>
                        SpanDerived(string.Empty); // Span<string>

                        // Ambiguous for inline collection expression, but 'string' is a better conversion target than 'object' in params case
                        ArrayDerived(string.Empty);
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { sourceA, sourceB1 },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput("""
                T[]
                string[]
                string[]
                Span<T>
                Span<string>
                Span<string>
                string[]
                """));
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/69634")]
        [Fact]
        public void BetterConversionFromExpression_03() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string sourceA = """
                using System;
                using static System.Console;

                partial class Program
                {
                    static void Unrelated(params Span<int> value) { WriteLine("Span<int>"); }
                    static void Unrelated(params string[] value)     { WriteLine("string[]"); }
                }
                """;

            string sourceB1 = """
                partial class Program
                {
                    static void Main()
                    {
                        Unrelated(new[] { 1 }); // Span<int>
                        Unrelated(new[] { string.Empty }); // string[]

                        Unrelated(2); // Span<int>
                        Unrelated(string.Empty); // string[]
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { sourceA, sourceB1 },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput("""
                Span<int>
                string[]
                Span<int>
                string[]
                """));

            string sourceB2 = """
                partial class Program
                {
                    static void Main()
                    {
                        Unrelated(new[] { default }); // error
                        Unrelated(default); // ambiguous
                    }
                }
                """;
            comp = CreateCompilation(
                new[] { sourceA, sourceB2 },
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 1.cs(5,19): error CS0826: No best type found for implicitly-typed array
                //         Unrelated(new[] { default }); // error
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { default }").WithLocation(5, 19),
                // 1.cs(5,19): error CS1503: Argument 1: cannot convert from '?[]' to 'int'
                //         Unrelated(new[] { default }); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "new[] { default }").WithArguments("1", "?[]", "int").WithLocation(5, 19),
                // 1.cs(6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Unrelated(params Span<int>)' and 'Program.Unrelated(params string[])'
                //         Unrelated(default); // ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "Unrelated").WithArguments("Program.Unrelated(params System.Span<int>)", "Program.Unrelated(params string[])").WithLocation(6, 9)
                );
        }

        [Fact]
        public void BetterConversionFromExpression_04() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F1(int[] x, params int[] y) { throw null; }
                    static void F1(Span<object> x, params ReadOnlySpan<int> y) { x.Report(); y.Report(); }
                    static void F2(object x, params string[] y) { throw null; }
                    static void F2(string x, params Span<object> y) { y.Report(); }
                    static void Main()
                    {
                        F1([1], 2);
                        F2("3", "4");
                    }
                }
                """;
            CreateCompilation(
                new[] { source, CollectionExpressionTests.s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // Inline collection expression works in this case.
                // For 'params' case it fails because:
                //    - For the first argument, 'int[]' and 'Span<object>' -> neither is better
                //    - For the second argument, 'int' and 'int' -> neither is better vs. 'int[]' and 'ReadOnlySpan<int>' -> ReadOnlySpan<int> for a collection expression 
                // Parameters type sequences are different, tie-breaking rules do not apply.   

                // 0.cs(10,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[], params int[])' and 'Program.F1(Span<object>, params ReadOnlySpan<int>)'
                //         F1([1], 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(int[], params int[])", "Program.F1(System.Span<object>, params System.ReadOnlySpan<int>)").WithLocation(10, 9),

                // Inline collection expression works in this case.
                // For 'params' case it fails because:
                //    - For the first argument, 'object' and 'string' -> string
                //    - For the second argument, 'string' and 'object' -> string (different direction) vs. 'string[]' and 'Span<object>' -> neither is better for a collection expression 
                // Parameters type sequences are different, tie-breaking rules do not apply.   

                // 0.cs(11,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(object, params string[])' and 'Program.F2(string, params Span<object>)'
                //         F2("3", "4");
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(object, params string[])", "Program.F2(string, params System.Span<object>)").WithLocation(11, 9)
                );
        }

        [Fact]
        public void BetterConversionFromExpression_05() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F1(Span<int> x, params int[] y)  { x.Report(); y.Report(); }
                    static void F1(int[] x, params ReadOnlySpan<int> y) { throw null; }
                    static void F2(string x, params string[] y) { y.Report(); }
                    static void F2(object x, params Span<string> y) { throw null; }
                    static void Main()
                    {
                        F1([1], 2);
                        F2("3", "4");
                    }
                }
                """;

            // Both calls are ambiguous for inline collection expressions, due to better-ness in different directions among arguments.
            // For params case, there is no difference in the target type for the second argument
            CompileAndVerify(
                new[] { source, CollectionExpressionTests.s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: ExpectedOutput("[1], [2], [4], "));
        }

        // Two ref struct collection types, with an implicit conversion from one to the other.
        [Fact]
        public void BetterConversionFromExpression_06() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create1))]
                ref struct MyCollection1<T>
                {
                    private readonly List<T> _list;
                    public MyCollection1(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    public static implicit operator MyCollection2<T>(MyCollection1<T> c) => new(c._list);
                }
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create2))]
                ref struct MyCollection2<T>
                {
                    private readonly List<T> _list;
                    public MyCollection2(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                }
                static class MyCollectionBuilder
                {
                    public static MyCollection1<T> Create1<T>(scoped ReadOnlySpan<T> items)
                    {
                        return new MyCollection1<T>(new List<T>(items.ToArray()));
                    }
                    public static MyCollection2<T> Create2<T>(scoped ReadOnlySpan<T> items)
                    {
                        return new MyCollection2<T>(new List<T>(items.ToArray()));
                    }
                }
                class Program
                {
                    static void F1<T>(params MyCollection1<T> c) { Console.WriteLine("MyCollection1<T>"); }
                    static void F1<T>(params MyCollection2<T> c) { Console.WriteLine("MyCollection2<T>"); }
                    static void F2(params MyCollection2<object> c) { Console.WriteLine("MyCollection2<object>"); }
                    static void F2(params MyCollection1<object> c) { Console.WriteLine("MyCollection1<object>"); }
                    static void Main()
                    {
                        F1(1, 2, 3);
                        F2(4, null);
                        F1((MyCollection1<object>)[6]);
                        F1((MyCollection2<int>)[7]);
                        F2((MyCollection2<object>)[8]);
                    }
                }
                """;
            CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: ExpectedOutput("""
                    MyCollection1<T>
                    MyCollection1<object>
                    MyCollection1<T>
                    MyCollection2<T>
                    MyCollection2<object>
                    """));
        }

        [Fact]
        public void BetterConversionFromExpression_07() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F1(params ReadOnlySpan<int> value) { Console.WriteLine("int"); }
                    static void F1(params ReadOnlySpan<object> value) { }
                    static void F2(params Span<string> value) { Console.WriteLine("string"); }
                    static void F2(params Span<object> value) { }
                    static void Main()
                    {
                        F1(1, 2, 3);
                        F2("a", "b");
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);

            // Ambiguity in case of inline collection expression
            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
int
string
")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterConversionFromExpression_08A() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                class Program
                {
                    static void F1(params int[] value) { System.Console.WriteLine("int"); }
                    static void F1(params object[] value) { }
                    static void Main()
                    {
                        F1(1, 2, 3);
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            // No behavior change (param arrays). Ambiguity in case of inline collection expression
            CompileAndVerify(
                comp,
                expectedOutput: @"int").VerifyDiagnostics();
        }

        [Fact]
        public void BetterConversionFromExpression_08B() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F2(params string[] value) { Console.WriteLine("string[]"); }
                    static void F2(params object[] value) { Console.WriteLine("object[]"); }
                    static void Main()
                    {
                        F2("a", "b");
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: "string[]");
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<char>")]
        [InlineData("System.Span<char>")]
        public void BetterConversionFromExpression_String_01(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                        F1('a', 'b', 'c');
                        F2('1', '2', '3');
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F1(params string value) { WriteLine("F1(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(7, 20),
                // (8,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F2(params string value) { WriteLine("F2(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(8, 20)
                );
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.ReadOnlySpan<object>")]
        [InlineData("System.Span<object>")]
        public void BetterConversionFromExpression_String_02_Empty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (7,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F1(params string value) { WriteLine("F1(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(7, 20),
                // (8,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F2(params string value) { WriteLine("F2(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(8, 20)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var f1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "F1").Single();
            Assert.NotEqual(SpecialType.System_String, model.GetSymbolInfo(f1).Symbol.GetParameters().Single().Type.SpecialType);
            var f2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "F2").Single();
            Assert.NotEqual(SpecialType.System_String, model.GetSymbolInfo(f2).Symbol.GetParameters().Single().Type.SpecialType);
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.ReadOnlySpan<object>")]
        [InlineData("System.Span<object>")]
        public void BetterConversionFromExpression_String_02_NotEmpty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1('a', 'b', 'c');
                        F2('1', '2', '3');
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);

            comp.VerifyEmitDiagnostics(
                // (7,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F1(params string value) { WriteLine("F1(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(7, 20),
                // (8,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F2(params string value) { WriteLine("F2(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(8, 20)
                );
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<byte>")]
        [InlineData("System.Span<byte>")]
        public void BetterConversionFromExpression_String_03(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (7,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F1(params string value) { WriteLine("F1(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(7, 20),
                // (8,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F2(params string value) { WriteLine("F2(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(8, 20)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var f1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "F1").Single();
            Assert.NotEqual(SpecialType.System_String, model.GetSymbolInfo(f1).Symbol.GetParameters().Single().Type.SpecialType);
            var f2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "F2").Single();
            Assert.NotEqual(SpecialType.System_String, model.GetSymbolInfo(f2).Symbol.GetParameters().Single().Type.SpecialType);
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<MyChar>")]
        [InlineData("System.Span<MyChar>")]
        public void BetterConversionFromExpression_String_04_Empty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class MyChar
                {
                    private readonly int _i;
                    public MyChar(int i) { _i = i; }
                    public static implicit operator MyChar(int i) => new MyChar(i);
                    public static implicit operator char(MyChar c) => (char)c._i;
                }

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (15,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F1(params string value) { WriteLine("F1(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(15, 20),
                // (16,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F2(params string value) { WriteLine("F2(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(16, 20)
                );
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<MyChar>")]
        [InlineData("System.Span<MyChar>")]
        public void BetterConversionFromExpression_String_04_NotEmpty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using static System.Console;

                class MyChar
                {
                    private readonly int _i;
                    public MyChar(int i) { _i = i; }
                    public static implicit operator MyChar(int i) => new MyChar(i);
                    public static implicit operator char(MyChar c) => (char)c._i;
                }

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1('a', 'b', 'c');
                        F2('1', '2', '3');
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (14,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F1(params string value) { WriteLine("F1(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(14, 20),
                // (15,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F2(params string value) { WriteLine("F2(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(15, 20)
                );
        }

        [Fact]
        public void BetterConversionFromExpression_String_05() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using System.Collections.Generic;
                using static System.Console;

                class Program
                {
                    static void F(params IEnumerable<char> value) { WriteLine("F(IEnumerable<char>)"); }
                    static void F(params string value) { WriteLine("F(string)"); }

                    static void Main()
                    {
                        F();
                        F('a');
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,19): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //     static void F(params string value) { WriteLine("F(string)"); }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "params string value").WithArguments("string", "0").WithLocation(8, 19)
                );
        }

        [Fact]
        public void BetterOverload_01_BetterParameterPassing()
        {
            // the better parameter-passing choice (https://github.com/dotnet/csharpstandard/blob/draft-v9/standard/expressions.md#12644-better-parameter-passing-mode)
            // should come before collection better-ness, but after argument conversion better-ness.
            // Expected output below matches legacy behavior of param arrays.

            var src = """
class Program
{
    static void Main()
    {
        Test(1);
        Test(1, new C2());
    }

    static void Test(in int x, params C2[] y)
    {
        System.Console.Write("In");
    }

    static void Test(int x, params C1[] y)
    {
        System.Console.Write("Val");
    }
}

class C1 {}
class C2 : C1 {}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: ExpectedOutput(@"ValIn")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterOverload_02_NotSameCollectionElements()
        {
            var src = """
class Program
{
    static void Main()
    {
        Test(x: 1, y: 2);
    }

    static void Test(int x, params System.ReadOnlySpan<int> y)
    {
        System.Console.Write("ReadOnlySpan");
    }

    static void Test(int y, params System.Span<int> x)
    {
        System.Console.Write("Span");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(int, params ReadOnlySpan<int>)' and 'Program.Test(int, params Span<int>)'
                //         Test(x: 1, y: 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(int, params System.ReadOnlySpan<int>)", "Program.Test(int, params System.Span<int>)").WithLocation(5, 9)
                );
        }

        [Fact]
        public void BetterOverload_03_NotSameCollectionElements()
        {
            var src = """
class Program
{
    static void Main()
    {
        Test(x: 1, y: 2);
    }

    static void Test(long x, params System.ReadOnlySpan<int> y)
    {
        System.Console.Write("ReadOnlySpan");
    }

    static void Test(int y, params long[] x)
    {
        System.Console.Write("Span");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(long, params ReadOnlySpan<int>)' and 'Program.Test(int, params long[])'
                //         Test(x: 1, y: 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(long, params System.ReadOnlySpan<int>)", "Program.Test(int, params long[])").WithLocation(5, 9)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72696")]
        public void BetterOverload_04_Ambiguity()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection<T> : IEnumerable<T>
{
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

static class ExtensionsA
{
    public static void Add<T>(this MyCollection<T> collection, params string[] args) { }
}

static class ExtensionsB
{
    public static void Add<T>(this MyCollection<T> collection, params string[] args) { }
}

class Program
{
    static void Main()
    {
        var x = new MyCollection<object>();
        x.Add("");

        var y = new MyCollection<object> { "" };
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (25,11): error CS0121: The call is ambiguous between the following methods or properties: 'ExtensionsA.Add<T>(MyCollection<T>, params string[])' and 'ExtensionsB.Add<T>(MyCollection<T>, params string[])'
                //         x.Add("");
                Diagnostic(ErrorCode.ERR_AmbigCall, "Add").WithArguments("ExtensionsA.Add<T>(MyCollection<T>, params string[])", "ExtensionsB.Add<T>(MyCollection<T>, params string[])").WithLocation(25, 11),
                // (27,44): error CS0121: The call is ambiguous between the following methods or properties: 'ExtensionsA.Add<T>(MyCollection<T>, params string[])' and 'ExtensionsB.Add<T>(MyCollection<T>, params string[])'
                //         var y = new MyCollection<object> { "" };
                Diagnostic(ErrorCode.ERR_AmbigCall, @"""""").WithArguments("ExtensionsA.Add<T>(MyCollection<T>, params string[])", "ExtensionsB.Add<T>(MyCollection<T>, params string[])").WithLocation(27, 44)
                );
        }

        [Fact]
        public void GenericInference()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        long l = 1;
        int i = 2;
        byte b = 3;
        Test(i, b, l);
    }

    static void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write(typeof(T));
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: ExpectedOutput(@"System.Int64")).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);
        Test(d, 1);
        Test(d, 2, 3);
        Test(2, d, 3);
        Test(2, 3, d);
        Test(d, [3, 4]);

        Test2(d, d);
        Test2(d, 1);
        Test2(d, 2, 3);
        Test2(2, d, 3);
        Test2(2, 3, d);

        Test2<int>(d);
        Test2<int>(d, d);
        Test2<int>(d, 1);
        Test2<int>(d, 2, 3);
        Test2<int>(2, d, 3);
        Test2<int>(2, 3, d);
        Test2<int>(d, [3, 4]);
    }

    static void Test(int a, params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }

    static void Test2<T>(int a, params T[] b)
    {
        System.Console.Write("Called2");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);
    }

    static void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,14): error CS9219: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Program.Test(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         Test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Program.Test(params System.Collections.Generic.IEnumerable<int>)").WithLocation(8, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_03_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        Test1(d1);                  // Called2
        
        dynamic d2 = new[] { 1 };
        Test1(d2);                  // Called1
        Test2(1, d1);               // Called3
        Test2(1, d2);               // Called5
        
        int x = 1;
        Test2(x, d1);               // Called3
        Test2(x, d2);               // Called4

        dynamic d3 = (byte)1;
        Test3(d3, 1, 2);            // Called7
        Test3(d3, x, x);            // Called6

        dynamic d4 = x;
        Test4((byte)d3, x, x);      // Called8
        Test4(d3, x, x);            // Called9
        Test4(d3, d4, d4);          // Called9
    }

    static void Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
    static void Test1(System.DateTime b) => System.Console.Write("Called2");

    static void Test2(int x, System.DateTime b) => System.Console.Write("Called3");
    static void Test2(long x, IEnumerable<int> b) => System.Console.Write("Called4");
    static void Test2(byte x, params IEnumerable<int> b) => System.Console.Write("Called5");

    static void Test3(byte x, params IEnumerable<int> b) => System.Console.Write("Called6");
    static void Test3(byte x, byte y, byte z) => System.Console.Write("Called7");

    static void Test4(byte x, params IEnumerable<int> b) => System.Console.Write("Called8");
    static void Test4(byte x, long y, long z) => System.Console.Write("Called9");
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called2Called1Called3Called5Called3Called4Called7Called6Called8Called9Called9").
            VerifyDiagnostics(
                // (8,9): warning CS9220: One or more overloads of method 'Test1' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test1(d1);                  // Called2
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test1(d1)").WithArguments("Test1").WithLocation(8, 9),
                // (11,9): warning CS9220: One or more overloads of method 'Test1' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test1(d2);                  // Called1
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test1(d2)").WithArguments("Test1").WithLocation(11, 9),
                // (12,9): warning CS9220: One or more overloads of method 'Test2' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test2(1, d1);               // Called3
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test2(1, d1)").WithArguments("Test2").WithLocation(12, 9),
                // (13,9): warning CS9220: One or more overloads of method 'Test2' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test2(1, d2);               // Called5
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test2(1, d2)").WithArguments("Test2").WithLocation(13, 9),
                // (20,9): warning CS9220: One or more overloads of method 'Test3' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test3(d3, 1, 2);            // Called7
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test3(d3, 1, 2)").WithArguments("Test3").WithLocation(20, 9),
                // (25,9): warning CS9220: One or more overloads of method 'Test4' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test4(d3, x, x);            // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test4(d3, x, x)").WithArguments("Test4").WithLocation(25, 9),
                // (26,9): warning CS9220: One or more overloads of method 'Test4' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test4(d3, d4, d4);          // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test4(d3, d4, d4)").WithArguments("Test4").WithLocation(26, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2);
    }

    static void Test(int a, params IEnumerable<int> b)
    {
        System.Console.Write("Called {0}", b is not null);
    }

    static void Test(int a, System.DateTime b)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2, 3);
    }

    static void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_06_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2, 3);
    }

    static void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9218: The type arguments for method 'Program.Test<T>(params IEnumerable<T>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(d, 2, 3)").WithArguments("Program.Test<T>(params System.Collections.Generic.IEnumerable<T>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(d, 2, 3);
    }

    static void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_07_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(0, d, 2, 3);
    }

    static void Test<T>(T a, params IEnumerable<long> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9218: The type arguments for method 'Program.Test<T>(T, params IEnumerable<long>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(0, d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(0, d, 2, 3)").WithArguments("Program.Test<T>(T, params System.Collections.Generic.IEnumerable<long>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(0, d, 2, 3);
    }

    static void Test<T>(T a, params IEnumerable<long> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_08_HideByOverride()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test(d, 2, 3);
    }
}

class C1
{
    public virtual void Test(params IEnumerable<int> b){}
}

class C2 : C1
{
    public override void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_09_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test<int>(d, 2, 3);
    }
}

class C1
{
    public void Test<T>(params IEnumerable<T> b){}
}

class C2 : C1
{
    new public void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_10_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test(d, 2, 3);
    }
}

class C0
{
    public virtual void Test(params IEnumerable<int> b){}
}

class C1 : C0
{
    public override void Test(params IEnumerable<int> b){}
}

class C2 : C1
{
    new public void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_11_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C3().Test(d, 2, 3);
    }
}

class C1
{
    public void Test(params IEnumerable<int> b){}
}

class C2 : C1
{
    new public virtual void Test(params IEnumerable<int> b) {}
}

class C3 : C2
{
    public override void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_12_HideByName()
        {
            var src1 = """
using System.Collections.Generic;

public class C1
{
    public void Test(params IEnumerable<int> b){}
}
""";

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseDll);

            var src2 = """
Public Class C2
    Inherits C1

    Public Shadows Sub Test(ParamArray b As Long())
        System.Console.Write("Called")
    End Sub
End Class
""";

            MetadataReference comp1Ref = comp1.EmitToImageReference();
            var comp2 = CreateVisualBasicCompilation(src2, referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Standard).Concat(comp1Ref));

            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test(d, 2, 3);
    }
}
""";
            var comp = CreateCompilation(src, references: new[] { comp1Ref, comp2.EmitToImageReference() }, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_13_DoNotHideByApplicability()
        {
            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new C2().Test(d);
    }
}

class C1
{
    public void Test(long a)
    {
        System.Console.Write("long");
    }
}

class C2 : C1
{
    public void Test(int b)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"long").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new C1().Test(1, d, 2);
    }
}

class C1
{
    public void Test(long a1, params IEnumerable<long> a2)
    {
        System.Console.Write("long");
    }

    public void Test(int b1, int b2, int b3)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): warning CS9220: One or more overloads of method 'Test' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new C1().Test(1, d, 2);
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "new C1().Test(1, d, 2)").WithArguments("Test").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_15_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = 1;
        Test1((int)d1);
        try
        {
            Test1(d1);
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            System.Console.Write("Failed");
        }
    }

    static void Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
    static void Test1(System.DateTime b) => System.Console.Write("Called2");
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called1Failed").
            VerifyDiagnostics(
                // (11,13): warning CS9220: One or more overloads of method 'Test1' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //             Test1(d1);
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test1(d1)").WithArguments("Test1").WithLocation(11, 13)
                );
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);
        Test(d, 1);
        Test(d, 2, 3);
        Test(2, d, 3);
        Test(2, 3, d);
        Test(2, [3, d]);

        Test2(d);
        Test2(d, 1);
        Test2(d, 2, 3);
        Test2(2, d, 3);
        Test2(2, 3, d);
        Test2(d, [3, 4]);

        void Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }

        void Test2(int a, params int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);

        static void Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,14): error CS9219: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Test(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         Test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Test(params System.Collections.Generic.IEnumerable<int>)").WithLocation(8, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_06_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2, 3);

        void Test<T>(params IEnumerable<T> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9218: The type arguments for method 'Test<T>(params IEnumerable<T>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(d, 2, 3)").WithArguments("Test<T>(params System.Collections.Generic.IEnumerable<T>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(d, 2, 3);

        void Test<T>(params IEnumerable<T> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_07_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(0, d, 2, 3);

        void Test<T>(T a, params IEnumerable<long> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9218: The type arguments for method 'Test<T>(T, params IEnumerable<long>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(0, d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(0, d, 2, 3)").WithArguments("Test<T>(T, params System.Collections.Generic.IEnumerable<long>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(0, d, 2, 3);

        void Test<T>(T a, params IEnumerable<long> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Delegate_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        D test = Test;
        dynamic d = 1;
        test(d);
        test(d, 1);
        test(d, 2, 3);
        test(2, d, 3);
        test(2, 3, d);
        test(2, [3, d]);

        D2 test2 = Test2;
        test2(d);
        test2(d, d);
        test2(d, 1);
        test2(d, 2, 3);
        test2(2, d, 3);
        test2(2, 3, d);
        test2(d, [3, 4]);

        void Test(int a, IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
        void Test2(int a, int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}

delegate void D(int a, params IEnumerable<int> b);
delegate void D2(int a, params int[] b);
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Delegate_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        D test = Test;
        dynamic d = 1;
        test(d);

        static void Test(IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}

delegate void D(params IEnumerable<int> b);
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (9,14): error CS9219: Ambiguity between expanded and normal forms of non-array params collection parameter of 'D.Invoke(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("D.Invoke(params System.Collections.Generic.IEnumerable<int>)").WithLocation(9, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_Indexer_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        dynamic d = 1;
        _ = c1[d];
        _ = c1[d, 1];
        _ = c1[d, 2, 3];
        _ = c1[2, d, 3];
        _ = c1[2, 3, d];
        _ = c1[d, [3, 4]];

        var c2 = new C2();

        _ = c2[d];
        _ = c2[d, d];
        _ = c2[d, 1];
        _ = c2[d, 2, 3];
        _ = c2[2, d, 3];
        _ = c2[2, 3, d];
        _ = c2[d, [3, 4]];
    }
}

class C1
{
    public int this[int a, params IEnumerable<int> b] 
    {
        get
        {
            System.Console.Write("Called");
            return 0;
        }
    }
}
class C2
{
    public int this[int a, params int[] b] 
    {
        get
        {
            System.Console.Write("Called2");
            return 0;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new Program()[d];
    }

    int this[params IEnumerable<int> b]
    {
        get 
        {
            System.Console.Write("Called");
            return 0;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,27): error CS9219: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Program.this[params IEnumerable<int>]', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         _ = new Program()[d];
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Program.this[params System.Collections.Generic.IEnumerable<int>]").WithLocation(8, 27)
                );
        }

        [Fact]
        public void DynamicInvocation_Indexer_03_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        _ = new Test1()[d1];                  // Called2
        
        dynamic d2 = new[] { 1 };
        _ = new Test1()[d2];                  // Called1
        _ = new Test2()[1, d1];               // Called3
        _ = new Test2()[1, d2];               // Called5
        
        int x = 1;
        _ = new Test2()[x, d1];               // Called3
        _ = new Test2()[x, d2];               // Called4

        dynamic d3 = (byte)1;
        _ = new Test3()[d3, 1, 2];            // Called7
        _ = new Test3()[d3, x, x];            // Called6

        dynamic d4 = x;
        _ = new Test4()[(byte)d3, x, x];      // Called8
        _ = new Test4()[d3, x, x];            // Called9
        _ = new Test4()[d3, d4, d4];          // Called9
    }

    class Test1
    {
        public int this[params IEnumerable<int> b] { get { System.Console.Write("Called1"); return 0; } }
        public int this[System.DateTime b] { get { System.Console.Write("Called2"); return 0; } }
    }
    class Test2
    {
        public int this[int x, System.DateTime b] { get { System.Console.Write("Called3"); return 0; } }
        public int this[long x, IEnumerable<int> b] { get { System.Console.Write("Called4"); return 0; } }
        public int this[byte x, params IEnumerable<int> b] { get { System.Console.Write("Called5"); return 0; } }
    }
    class Test3
    {
        public int this[byte x, params IEnumerable<int> b] { get { System.Console.Write("Called6"); return 0; } }
        public int this[byte x, byte y, byte z] { get { System.Console.Write("Called7"); return 0; } }
    }
    class Test4
    {
        public int this[byte x, params IEnumerable<int> b] { get { System.Console.Write("Called8"); return 0; } }
        public int this[byte x, long y, long z] { get { System.Console.Write("Called9"); return 0; } }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called2Called1Called3Called5Called3Called4Called7Called6Called8Called9Called9").
            VerifyDiagnostics(
                // (8,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test1()[d1];                  // Called2
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test1()[d1]").WithLocation(8, 13),
                // (11,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test1()[d2];                  // Called1
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test1()[d2]").WithLocation(11, 13),
                // (12,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test2()[1, d1];               // Called3
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test2()[1, d1]").WithLocation(12, 13),
                // (13,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test2()[1, d2];               // Called5
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test2()[1, d2]").WithLocation(13, 13),
                // (20,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test3()[d3, 1, 2];            // Called7
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test3()[d3, 1, 2]").WithLocation(20, 13),
                // (25,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test4()[d3, x, x];            // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test4()[d3, x, x]").WithLocation(25, 13),
                // (26,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test4()[d3, d4, d4];          // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test4()[d3, d4, d4]").WithLocation(26, 13)
                );
        }

        [Fact]
        public void DynamicInvocation_Indexer_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new Program()[d, 2];
    }

    int this[int a, params IEnumerable<int> b]
    {
        get
        {
            System.Console.Write("Called {0}", b is not null);
            return 0;
        }
    }

    int this[int a, System.DateTime b] => 0;
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new Program()[d, 2, 3];
    }

    int this[params IEnumerable<int> b]
    {
        get
        {
            System.Console.Write("Called");
            return 0;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_08_HideByOverride()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2<int>()[d, 2, 3];
    }
}

class C1<T>
{
    public virtual T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    public override T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_09_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2<int>()[d, 2, 3];
    }
}

class C1<T>
{
    public T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    new public T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_10_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2<int>()[d, 2, 3];
    }
}

class C0<T>
{
    public virtual T this[params IEnumerable<T> b] => default;
}

class C1<T> : C0<T>
{
    public override T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    new public T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_11_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C3<int>()[d, 2, 3];
    }
}

class C1<T>
{
    public T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    new public virtual T this[params IEnumerable<T> b] => default;
}

class C3<T> : C2<T>
{
    public override T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_12_HideByName()
        {
            var src1 = """
using System.Collections.Generic;

public class C1
{
    public int this[int x, params IEnumerable<int> b] => default;
}
""";

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseDll);

            var src2 = """
Public Class C2
    Inherits C1

    Public Shadows Readonly Default Property Item(x as Integer, ParamArray b As Long()) As Integer
        Get
            System.Console.Write("Called")
            Return 0
        End Get
    End Property
End Class
""";

            MetadataReference comp1Ref = comp1.EmitToImageReference();
            var comp2 = CreateVisualBasicCompilation(src2, referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Standard).Concat(comp1Ref));

            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2()[4, d, 2, 3];
    }
}
""";
            var comp = CreateCompilation(src, references: new[] { comp1Ref, comp2.EmitToImageReference() }, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_13_DoNotHideByApplicability()
        {
            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1L;
        _ = new C2()[d];
    }
}

class C1
{
    public long this[long a]
    {
        get
        {
            System.Console.Write("long");
            return a;
        }
    }
}

class C2 : C1
{
    public int this[int b]
    {
        get
        {
            System.Console.Write("int");
            return b;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"long").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        _ = new C1()[1, d, 2];
    }
}

class C1
{
    public long this[long a1, params IEnumerable<long> a2]
    {
        get
        {
            System.Console.Write("long");
            return a1;
        }
    }

    public int this[int b1, int b2, int b3]
    {
        get
        {
            System.Console.Write("int");
            return b1;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,13): warning CS9221: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new C1()[1, d, 2];
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new C1()[1, d, 2]").WithLocation(8, 13)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d);
        new Test(d, 1);
        new Test(d, 2, 3);
        new Test(2, d, 3);
        new Test(2, 3, d);
        new Test(d, [3, 4]);

        new Test2(d);
        new Test2(d, d);
        new Test2(d, 1);
        new Test2(d, 2, 3);
        new Test2(2, d, 3);
        new Test2(2, 3, d);
        new Test2(d, [3, 4]);
    }

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }

    class Test2
    {
        public Test2(int a, params int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Constructor_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d);
    }

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,18): error CS9219: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Program.Test.Test(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         new Test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Program.Test.Test(params System.Collections.Generic.IEnumerable<int>)").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_03_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        new Test1(d1);                  // Called2
        
        dynamic d2 = new[] { 1 };
        new Test1(d2);                  // Called1
        new Test2(1, d1);               // Called3
        new Test2(1, d2);               // Called5
        
        int x = 1;
        new Test2(x, d1);               // Called3
        new Test2(x, d2);               // Called4

        dynamic d3 = (byte)1;
        new Test3(d3, 1, 2);            // Called7
        new Test3(d3, x, x);            // Called6

        dynamic d4 = x;
        new Test4((byte)d3, x, x);      // Called8
        new Test4(d3, x, x);            // Called9
        new Test4(d3, d4, d4);          // Called9
    }

    class Test1
    {
        public Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
        public Test1(System.DateTime b) => System.Console.Write("Called2");
    }

    class Test2
    {
        public Test2(int x, System.DateTime b) => System.Console.Write("Called3");
        public Test2(long x, IEnumerable<int> b) => System.Console.Write("Called4");
        public Test2(byte x, params IEnumerable<int> b) => System.Console.Write("Called5");
    }

    class Test3
    {
        public Test3(byte x, params IEnumerable<int> b) => System.Console.Write("Called6");
        public Test3(byte x, byte y, byte z) => System.Console.Write("Called7");
    }

    class Test4
    {
        public Test4(byte x, params IEnumerable<int> b) => System.Console.Write("Called8");
        public Test4(byte x, long y, long z) => System.Console.Write("Called9");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called2Called1Called3Called5Called3Called4Called7Called6Called8Called9Called9").
            VerifyDiagnostics(
                // (8,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test1(d1);                  // Called2
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test1(d1)").WithLocation(8, 9),
                // (11,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test1(d2);                  // Called1
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test1(d2)").WithLocation(11, 9),
                // (12,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test2(1, d1);               // Called3
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test2(1, d1)").WithLocation(12, 9),
                // (13,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test2(1, d2);               // Called5
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test2(1, d2)").WithLocation(13, 9),
                // (20,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test3(d3, 1, 2);            // Called7
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test3(d3, 1, 2)").WithLocation(20, 9),
                // (25,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test4(d3, x, x);            // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test4(d3, x, x)").WithLocation(25, 9),
                // (26,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test4(d3, d4, d4);          // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test4(d3, d4, d4)").WithLocation(26, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d, 2);
    }

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called {0}", b is not null);
        }

        public Test(int a, System.DateTime b)
        {
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Constructor_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d, 2, 3);
    }

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Constructor_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new Test(1, d, 2);
    }
}

class Test
{
    public Test(long a1, params IEnumerable<long> a2)
    {
        System.Console.Write("long");
    }

    public Test(int b1, int b2, int b3)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): warning CS9222: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test(1, d, 2);
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test(1, d, 2)").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_16_Abstract()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d);
    }

    abstract class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): error CS0144: Cannot create an instance of the abstract type or interface 'Program.Test'
                //         new Test(d);
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new Test(d)").WithArguments("Program.Test").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C01(d);
        new C02(d);
        new C03(d);
        new C04(d);
        new C05(d);
        new C06(d);

        new C07(d);
        new C09(d);
        new C10(d);
        new C11(d);
        new C12(d);
        new C13(d);
    }

    class C01(dynamic d) : Test(d);
    class C02(dynamic d) : Test(d, 1);
    class C03(dynamic d) : Test(d, 2, 3);
    class C04(dynamic d) : Test(2, d, 3);
    class C05(dynamic d) : Test(2, 3, d);
    class C06(dynamic d) : Test(d, [3, 4]);

    class C07(dynamic d) : Test2(d);
    class C09(dynamic d) : Test2(d, 1);
    class C10(dynamic d) : Test2(d, 2, 3);
    class C11(dynamic d) : Test2(2, d, 3);
    class C12(dynamic d) : Test2(2, 3, d);
    class C13(dynamic d) : Test2(d, [3, 4]);

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }

    class Test2
    {
        public Test2(int a, params int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C(d);
    }

    class C(dynamic d) : Test(d);

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (11,30): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C(dynamic d) : Test(d);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d)").WithLocation(11, 30)
                );
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_03_MultipleCandidates()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        new C01(d1);
        
        dynamic d2 = new[] { 1 };
        new C02(d2);
        new C03(d1);
        new C04(d2);
        
        int x = 1;
        new C05(x, d1);
        new C06(x, d2);

        dynamic d3 = (byte)1;
        new C07(d3);
        new C08(d3, x);

        dynamic d4 = x;
        new C09(d3, x);
        new C10(d3, x);
        new C11(d3, d4);
    }

    class C01(dynamic d1) : Test1(d1);
        
    class C02(dynamic d2) : Test1(d2);
    class C03(dynamic d1) : Test2(1, d1);
    class C04(dynamic d2) : Test2(1, d2);
        
    class C05(int x, dynamic d1) : Test2(x, d1);
    class C06(int x, dynamic d2) : Test2(x, d2);

    class C07(dynamic d3) : Test3(d3, 1, 2);
    class C08(int x, dynamic d3) : Test3(d3, x, x);            // Called6

    class C09(dynamic d3, int x) : Test4((byte)d3, x, x);      // Called8
    class C10(dynamic d3, int x) : Test4(d3, x, x);
    class C11(dynamic d3, dynamic d4) : Test4(d3, d4, d4);

    class Test1
    {
        public Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
        public Test1(System.DateTime b) => System.Console.Write("Called2");
    }

    class Test2
    {
        public Test2(int x, System.DateTime b) => System.Console.Write("Called3");
        public Test2(long x, IEnumerable<int> b) => System.Console.Write("Called4");
        public Test2(byte x, params IEnumerable<int> b) => System.Console.Write("Called5");
    }

    class Test3
    {
        public Test3(byte x, params IEnumerable<int> b) => System.Console.Write("Called6");
        public Test3(byte x, byte y, byte z) => System.Console.Write("Called7");
    }

    class Test4
    {
        public Test4(byte x, params IEnumerable<int> b) => System.Console.Write("Called8");
        public Test4(byte x, long y, long z) => System.Console.Write("Called9");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (29,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C01(dynamic d1) : Test1(d1);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d1)").WithLocation(29, 34),
                // (31,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C02(dynamic d2) : Test1(d2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d2)").WithLocation(31, 34),
                // (32,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C03(dynamic d1) : Test2(1, d1);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(1, d1)").WithLocation(32, 34),
                // (33,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C04(dynamic d2) : Test2(1, d2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(1, d2)").WithLocation(33, 34),
                // (35,41): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C05(int x, dynamic d1) : Test2(x, d1);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(x, d1)").WithLocation(35, 41),
                // (36,41): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C06(int x, dynamic d2) : Test2(x, d2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(x, d2)").WithLocation(36, 41),
                // (38,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C07(dynamic d3) : Test3(d3, 1, 2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d3, 1, 2)").WithLocation(38, 34),
                // (42,41): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C10(dynamic d3, int x) : Test4(d3, x, x);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d3, x, x)").WithLocation(42, 41),
                // (43,46): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C11(dynamic d3, dynamic d4) : Test4(d3, d4, d4);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d3, d4, d4)").WithLocation(43, 46)
                );
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C(d);
    }

    class C(dynamic d) : Test(d, 2);

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called {0}", b is not null);
        }

        public Test(int a, System.DateTime b)
        {
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C(d);
    }

    class C(dynamic d) : Test(d, 2, 3);

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new C(d);
    }
}

class C(dynamic d) : Test(1, d, 2);

class Test
{
    public Test(long a1, params IEnumerable<long> a2)
    {
        System.Console.Write("long");
    }

    public Test(int b1, int b2, int b3)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (12,26): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                // class C(dynamic d) : Test(1, d, 2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(1, d, 2)").WithLocation(12, 26)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_01_RegularMethod()
        {
            var template = """
{0}

public ref struct Test
{{
    public {1} Test1({2})
    {{
        {3}
        return default;
    }}

    public {4};
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test.Test1");
        }

        private void VerifyParameterRefSafetyScope(string template, string memberName)
        {
            // var template = """
            //                {0}
            //                
            //                ref struct  Test
            //                {{
            //                    public {1} Test1({2})
            //                    {{
            //                        {3}
            //                    }}
            //
            //                    {4};
            //                }}
            //                """;
            //
            // var template = """
            //                {0}
            //                
            //                class  Test
            //                {{
            //                    public {1} Test1(ref {4}, {2})
            //                    {{
            //                        {3}
            //                    }}
            //                }}
            //                """;

            // IEnumerable<T> ---------------------------------------------------------------

            CSharpCompilation comp;

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System.Collections.Generic;",
                    /* return type */ "IEnumerable<long>",
                    /* parameter   */ "params IEnumerable<long> paramsParameter",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"IEnumerable<long> F"),
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            ParameterSymbol p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            int parameterOrdinal = p.Ordinal;

            CompileAndVerify(
                comp,
                symbolValidator: (m) => verifyScopeOnImport(m, ScopedKind.None)).VerifyDiagnostics();

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System.Collections.Generic;",
                    /* return type */ "IEnumerable<long>",
                    /* parameter   */ @"
#line 100
params scoped IEnumerable<long> paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"IEnumerable<long> F"));

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.ScopedValue, p);

            comp.VerifyDiagnostics(
                // (100,1): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // params scoped IEnumerable<long> paramsParameter
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "params scoped IEnumerable<long> paramsParameter").WithLocation(100, 1)
                );

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System.Collections.Generic; using System.Diagnostics.CodeAnalysis;",
                    /* return type */ "IEnumerable<long>",
                    /* parameter   */ @"
#line 100
[UnscopedRef] params IEnumerable<long> paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"IEnumerable<long> F"),
                targetFramework: TargetFramework.Net80);

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            comp.VerifyDiagnostics(
                // (100,2): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                // [UnscopedRef] params IEnumerable<long> paramsParameter
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(100, 2)
                );

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System.Collections.Generic; using System.Diagnostics.CodeAnalysis;",
                    /* return type */ "IEnumerable<long>",
                    /* parameter   */ @"
#line 100
[UnscopedRef] params scoped IEnumerable<long> paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"IEnumerable<long> F"),
                targetFramework: TargetFramework.Net80);

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            comp.VerifyDiagnostics(
                // (100,1): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // [UnscopedRef] params scoped IEnumerable<long> paramsParameter
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "[UnscopedRef] params scoped IEnumerable<long> paramsParameter").WithLocation(100, 1),
                // (100,2): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                // [UnscopedRef] params scoped IEnumerable<long> paramsParameter
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(100, 2)
                );

            // Array --------------------------------------------------------

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "",
                    /* return type */ "long[]",
                    /* parameter   */ "params long[] paramsParameter",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"long[] F"),
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            CompileAndVerify(
                comp,
                symbolValidator: (m) => verifyScopeOnImport(m, ScopedKind.None)).VerifyDiagnostics();

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "",
                    /* return type */ "long[]",
                    /* parameter   */ @"
#line 100
params scoped long[] paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"long[] F"));

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.ScopedValue, p);

            comp.VerifyDiagnostics(
                // (100,1): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // params scoped long[] paramsParameter
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "params scoped long[] paramsParameter").WithLocation(100, 1)
                );

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System.Diagnostics.CodeAnalysis;",
                    /* return type */ "long[]",
                    /* parameter   */ @"
#line 100
[UnscopedRef] params long[] paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"long[] F"),
                targetFramework: TargetFramework.Net80);

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            comp.VerifyDiagnostics(
                // (100,2): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                // [UnscopedRef] params long[] paramsParameter
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(100, 2)
                );

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System.Diagnostics.CodeAnalysis;",
                    /* return type */ "long[]",
                    /* parameter   */ @"
#line 100
[UnscopedRef] params scoped long[] paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"long[] F"),
                targetFramework: TargetFramework.Net80);

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            comp.VerifyDiagnostics(
                // (100,1): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // [UnscopedRef] params scoped long[] paramsParameter
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "[UnscopedRef] params scoped long[] paramsParameter").WithLocation(100, 1),
                // (100,2): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                // [UnscopedRef] params scoped long[] paramsParameter
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(100, 2)
                );

            // Span ----------------------------------------------------------

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System;",
                    /* return type */ "Span<long>",
                    /* parameter   */ "params Span<long> paramsParameter",
                    /* method body */ @"",
                    /* extra       */ @"Span<long> F"),
                targetFramework: TargetFramework.Net80,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.ScopedValue, p);

            CompileAndVerify(
                comp,
                verify: Verification.Skipped,
                symbolValidator: (m) => verifyScopeOnImport(m, ScopedKind.ScopedValue)).VerifyDiagnostics();

            bool hasBody = p.ContainingSymbol is not MethodSymbol { MethodKind: MethodKind.DelegateInvoke };

            if (hasBody)
            {
                comp = CreateCompilation(
                    string.Format(
                        template,
                        /* usings      */ "using System;",
                        /* return type */ "Span<long>",
                        /* parameter   */ "params Span<long> paramsParameter",
                        /* method body */ @"
#line 100
F = paramsParameter;
",
                        /* extra       */ @"Span<long> F"),
                    targetFramework: TargetFramework.Net80);

                comp.VerifyDiagnostics(
                    // (100,5): error CS8352: Cannot use variable 'params Span<long> paramsParameter' in this context because it may expose referenced variables outside of their declaration scope
                    // F = paramsParameter;
                    Diagnostic(ErrorCode.ERR_EscapeVariable, "paramsParameter").WithArguments("params System.Span<long> paramsParameter").WithLocation(100, 5)
                    );
            }

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System;",
                    /* return type */ "Span<long>",
                    /* parameter   */ "params scoped Span<long> paramsParameter",
                    /* method body */ @"",
                    /* extra       */ @"Span<long> F"),
                targetFramework: TargetFramework.Net80,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.ScopedValue, p);

            CompileAndVerify(
                comp,
                verify: Verification.Skipped,
                symbolValidator: (m) => verifyScopeOnImport(m, ScopedKind.ScopedValue)).VerifyDiagnostics();

            if (hasBody)
            {
                comp = CreateCompilation(
                    string.Format(
                        template,
                        /* usings      */ "using System;",
                        /* return type */ "Span<long>",
                        /* parameter   */ "params scoped Span<long> paramsParameter",
                        /* method body */ @"
#line 100
F = paramsParameter;
",
                        /* extra       */ @"Span<long> F"),
                    targetFramework: TargetFramework.Net80);

                comp.VerifyDiagnostics(
                    // (100,5): error CS8352: Cannot use variable 'params Span<long> paramsParameter' in this context because it may expose referenced variables outside of their declaration scope
                    // F = paramsParameter;
                    Diagnostic(ErrorCode.ERR_EscapeVariable, "paramsParameter").WithArguments("params System.Span<long> paramsParameter").WithLocation(100, 5)
                    );
            }

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System; using System.Diagnostics.CodeAnalysis;",
                    /* return type */ "Span<long>",
                    /* parameter   */ "[UnscopedRef] params Span<long> paramsParameter",
                    /* method body */ @"
#line 100
F = paramsParameter;
",
                    /* extra       */ @"Span<long> F"),
                targetFramework: TargetFramework.Net80,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            CompileAndVerify(
                comp,
                verify: Verification.Skipped,
                symbolValidator: (m) => verifyScopeOnImport(m, ScopedKind.None, expectUnscopedRefAttribute: true)).VerifyDiagnostics();

            comp = CreateCompilation(
                string.Format(
                    template,
                    /* usings      */ "using System; using System.Diagnostics.CodeAnalysis;",
                    /* return type */ "Span<long>",
                    /* parameter   */ @"
#line 100
[UnscopedRef] params scoped Span<long> paramsParameter
",
                    /* method body */ @"
#line 200
F = paramsParameter;
",
                    /* extra       */ @"Span<long> F"),
                targetFramework: TargetFramework.Net80);

            p = GetParamsParameterForRefSafetyScopeTests(comp);
            assertScope(ScopedKind.None, p);

            comp.VerifyDiagnostics(
                // (100,2): error CS9066: UnscopedRefAttribute cannot be applied to parameters that have a 'scoped' modifier.
                // [UnscopedRef] params scoped Span<long> paramsParameter
                Diagnostic(ErrorCode.ERR_UnscopedScoped, "UnscopedRef").WithLocation(100, 2)
                );

            // Helpers ---------------------------------------------

            void verifyScopeOnImport(ModuleSymbol m, ScopedKind expected, bool expectUnscopedRefAttribute = false)
            {
                PEModule module = ((PEModuleSymbol)m).Module;

                var p1 = (PEParameterSymbol)m.GlobalNamespace.GetMember(memberName).GetParameters()[parameterOrdinal];
                assertScope(expected, p1);

                assertAttributes(expected, expectUnscopedRefAttribute, module, p1);

                if (p1.ContainingSymbol is PropertySymbol prop)
                {
                    if (prop.GetMethod is MethodSymbol getMethod)
                    {
                        assertAttributes(expected, expectUnscopedRefAttribute, module, (PEParameterSymbol)getMethod.Parameters[p1.Ordinal]);
                    }

                    if (prop.SetMethod is MethodSymbol setMethod)
                    {
                        assertAttributes(expected, expectUnscopedRefAttribute, module, (PEParameterSymbol)setMethod.Parameters[p1.Ordinal]);
                    }
                }
            }

            static void assertAttributes(ScopedKind expected, bool expectUnscopedRefAttribute, PEModule module, PEParameterSymbol p1)
            {
                switch (expected)
                {
                    case ScopedKind.None:
                        if (expectUnscopedRefAttribute)
                        {
                            Assert.Equal("System.Diagnostics.CodeAnalysis.UnscopedRefAttribute", p1.GetAttributes().Single().ToString());
                        }
                        else
                        {
                            Assert.Empty(p1.GetAttributes());
                        }

                        Assert.False(module.FindTargetAttribute(p1.Handle, AttributeDescription.ScopedRefAttribute).HasValue);
                        break;
                    case ScopedKind.ScopedValue:
                        Assert.False(expectUnscopedRefAttribute);
                        Assert.Empty(p1.GetAttributes());
                        Assert.True(module.FindTargetAttribute(p1.Handle, AttributeDescription.ScopedRefAttribute).HasValue);
                        break;
                    default:
                        Assert.False(true);
                        break;
                }
            }

            static void assertScope(ScopedKind scope, ParameterSymbol p)
            {
                Assert.Equal(scope, p.EffectiveScope);

                if (p.ContainingSymbol is PropertySymbol prop)
                {
                    if (prop.GetMethod is MethodSymbol getMethod)
                    {
                        Assert.Equal(scope, getMethod.Parameters[p.Ordinal].EffectiveScope);
                    }

                    if (prop.SetMethod is MethodSymbol setMethod)
                    {
                        Assert.Equal(scope, setMethod.Parameters[p.Ordinal].EffectiveScope);
                    }
                }
            }
        }

        private static ParameterSymbol GetParamsParameterForRefSafetyScopeTests(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var parameterDecl = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ValueText == "paramsParameter").Single();
            return model.GetDeclaredSymbol(parameterDecl).GetSymbol<ParameterSymbol>();
        }

        [Fact]
        public void ParameterRefSafetyScope_02_Constructor()
        {
            var template = """
{0}

class Test
{{
    public /*{1}*/ Test(ref {4}, {2})
    {{
        {3}
    }}
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test..ctor");
        }

        [Fact]
        public void ParameterRefSafetyScope_03_Delegate()
        {
            var template = """
{0}

delegate {1} Test({2});
/*{3}*/
/*{4}*/
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test.Invoke");
        }

        [Fact]
        public void ParameterRefSafetyScope_04_Property_get()
        {
            var template = """
{0}

public ref struct Test
{{
    public {1} this[{2}]
    {{
        get
        {{
            {3}
            return default;
        }}
    }}

    public {4};
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test." + WellKnownMemberNames.Indexer);
        }

        [Fact]
        public void ParameterRefSafetyScope_05_Property_set()
        {
            var template = """
{0}

public ref struct Test
{{
    public {1} this[{2}]
    {{
        set
        {{
            {3}
        }}
    }}

    public {4};
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test." + WellKnownMemberNames.Indexer);
        }

        [Fact]
        public void ParameterRefSafetyScope_06_Property_get_set()
        {
            var template = """
{0}

public ref struct Test
{{
    public {1} this[{2}]
    {{
        get
        {{
            {3}
            return default;
        }}
        set {{}}
    }}

    public {4};
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test." + WellKnownMemberNames.Indexer);
        }

        [Fact]
        public void ParameterRefSafetyScope_07_Lambda()
        {
            var template = """
{0}

class Test
{{
    public void Test2()
    {{
        var d = {1} (ref {4}, {2}) =>
        {{
            {3}
            return default;
        }};
    }}
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test.<>c.<Test2>b__0_0");
        }

        [Fact]
        public void ParameterRefSafetyScope_08_Lambda_Delegate()
        {
            var template = """
{0}

class Test
{{
    public void Test2()
    {{
        var d = {1} (ref {4}, {2}) =>
        {{
            {3}
            return default;
        }};
    }}
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "<>f__AnonymousDelegate0.Invoke");
        }

        [Fact]
        public void ParameterRefSafetyScope_09_LocalFunction()
        {
            var template = """
{0}

class Test
{{
    public void Test2()
    {{
        var d = local;

        {1} local (ref {4}, {2})
        {{
            {3}
            return default;
        }};
    }}
}}
""";

            VerifyParameterRefSafetyScope(template, memberName: "Test.<Test2>g__local|0_0");
        }

        [Fact]
        public void ParameterRefSafetyScope_10_Mismatch_Overriding()
        {
            var src = @"
using System;

abstract class C1
{
    public abstract Span<long> Test1(Span<long> a);
    public abstract Span<long> Test2(Span<long> a);
    public abstract Span<long> Test3(Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1(params Span<long> a)
        => throw null;

    public override Span<long> Test2(params scoped Span<long> a)
        => throw null;

    public override Span<long> Test3(scoped Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.False(comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.False(comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ParameterRefSafetyScope_11_Mismatch_Overriding()
        {
            var src = @"
using System;

abstract class C1
{
    public abstract Span<long> Test1(params Span<long> a);
    public abstract Span<long> Test2(params scoped Span<long> a);
    public abstract Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1(Span<long> a)
        => throw null;

    public override Span<long> Test2(Span<long> a)
        => throw null;

    public override Span<long> Test3(Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (13,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test1(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(13, 32),
                // (16,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test2(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(16, 32),
                // (19,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test3(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(19, 32)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_12_Mismatch_Overriding()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

abstract class C1
{
    public abstract Span<long> Test1(params Span<long> a);
    public abstract Span<long> Test2(params scoped Span<long> a);
    public abstract Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1([UnscopedRef] Span<long> a)
        => throw null;

    public override Span<long> Test2([UnscopedRef] Span<long> a)
        => throw null;

    public override Span<long> Test3([UnscopedRef] Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (14,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(14, 32),
                // (14,39): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public override Span<long> Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(14, 39),
                // (17,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(17, 32),
                // (17,39): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public override Span<long> Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(17, 39),
                // (20,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(20, 32),
                // (20,39): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public override Span<long> Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(20, 39)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_13_Mismatch_Overriding()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

abstract class C1
{
    public abstract Span<long> Test1(params Span<long> a);
    public abstract Span<long> Test2(params scoped Span<long> a);
    public abstract Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1([UnscopedRef] params Span<long> a)
        => throw null;

    public override Span<long> Test2([UnscopedRef] params Span<long> a)
        => throw null;

    public override Span<long> Test3([UnscopedRef] params Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            Assert.False(comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().IsParams);

            comp.VerifyDiagnostics(
                // (14,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test1([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(14, 32),
                // (17,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test2([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(17, 32),
                // (20,32): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public override Span<long> Test3([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(20, 32)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_14_Mismatch_Implementing()
        {
            var src = @"
using System;

interface C1
{
    Span<long> Test1(Span<long> a);
    Span<long> Test2(Span<long> a);
    Span<long> Test3(Span<long> a);
}

class C2 : C1
{
    public Span<long> Test1(params Span<long> a)
        => throw null;

    public Span<long> Test2(params scoped Span<long> a)
        => throw null;

    public Span<long> Test3(scoped Span<long> a)
        => throw null;
}

class C3 : C1
{
    Span<long> C1.Test1(params Span<long> a)
        => throw null;

    Span<long> C1.Test2(params scoped Span<long> a)
        => throw null;

    Span<long> C1.Test3(scoped Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (25,19): error CS0466: 'C3.C1.Test1(params Span<long>)' should not have a params parameter since 'C1.Test1(Span<long>)' does not
                //     Span<long> C1.Test1(params Span<long> a)
                Diagnostic(ErrorCode.ERR_ExplicitImplParams, "Test1").WithArguments("C3.C1.Test1(params System.Span<long>)", "C1.Test1(System.Span<long>)").WithLocation(25, 19),
                // (28,19): error CS0466: 'C3.C1.Test2(params Span<long>)' should not have a params parameter since 'C1.Test2(Span<long>)' does not
                //     Span<long> C1.Test2(params scoped Span<long> a)
                Diagnostic(ErrorCode.ERR_ExplicitImplParams, "Test2").WithArguments("C3.C1.Test2(params System.Span<long>)", "C1.Test2(System.Span<long>)").WithLocation(28, 19)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_15_Mismatch_Implementing()
        {
            var src = @"
using System;

interface C1
{
    Span<long> Test1(params Span<long> a);
    Span<long> Test2(params scoped Span<long> a);
    Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public Span<long> Test1(Span<long> a)
        => throw null;

    public Span<long> Test2(Span<long> a)
        => throw null;

    public Span<long> Test3(Span<long> a)
        => throw null;
}

class C3 : C1
{
    Span<long> C1.Test1(Span<long> a)
        => throw null;

    Span<long> C1.Test2(Span<long> a)
        => throw null;

    Span<long> C1.Test3(Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (13,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test1(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(13, 23),
                // (16,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test2(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(16, 23),
                // (19,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test3(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(19, 23),
                // (25,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test1(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(25, 19),
                // (28,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test2(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(28, 19),
                // (31,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test3(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(31, 19)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_16_Mismatch_Implementing()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

interface C1
{
    Span<long> Test1(params Span<long> a);
    Span<long> Test2(params scoped Span<long> a);
    Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public Span<long> Test1([UnscopedRef] Span<long> a)
        => throw null;

    public Span<long> Test2([UnscopedRef] Span<long> a)
        => throw null;

    public Span<long> Test3([UnscopedRef] Span<long> a)
        => throw null;
}

class C3 : C1
{
    Span<long> C1.Test1([UnscopedRef] Span<long> a)
        => throw null;

    Span<long> C1.Test2([UnscopedRef] Span<long> a)
        => throw null;

    Span<long> C1.Test3([UnscopedRef] Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (14,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(14, 23),
                // (14,30): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public Span<long> Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(14, 30),
                // (17,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(17, 23),
                // (17,30): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public Span<long> Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(17, 30),
                // (20,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(20, 23),
                // (20,30): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public Span<long> Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(20, 30),
                // (26,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(26, 19),
                // (26,26): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     Span<long> C1.Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(26, 26),
                // (29,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(29, 19),
                // (29,26): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     Span<long> C1.Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(29, 26),
                // (32,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(32, 19),
                // (32,26): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     Span<long> C1.Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(32, 26)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_17_Mismatch_Implementing()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

interface C1
{
    Span<long> Test1(params Span<long> a);
    Span<long> Test2(params scoped Span<long> a);
    Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public Span<long> Test1([UnscopedRef] params Span<long> a)
        => throw null;

    public Span<long> Test2([UnscopedRef] params Span<long> a)
        => throw null;

    public Span<long> Test3([UnscopedRef] params Span<long> a)
        => throw null;
}

class C3 : C1
{
    Span<long> C1.Test1([UnscopedRef] params Span<long> a)
        => throw null;

    Span<long> C1.Test2([UnscopedRef] params Span<long> a)
        => throw null;

    Span<long> C1.Test3([UnscopedRef] params Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C3.C1.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (14,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test1([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(14, 23),
                // (17,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test2([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(17, 23),
                // (20,23): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     public Span<long> Test3([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(20, 23),
                // (26,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test1([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test1").WithArguments("a").WithLocation(26, 19),
                // (29,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test2([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test2").WithArguments("a").WithLocation(29, 19),
                // (32,19): error CS0466: 'C3.C1.Test3(params Span<long>)' should not have a params parameter since 'C1.Test3(scoped Span<long>)' does not
                //     Span<long> C1.Test3([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ExplicitImplParams, "Test3").WithArguments("C3.C1.Test3(params System.Span<long>)", "C1.Test3(scoped System.Span<long>)").WithLocation(32, 19),
                // (32,19): error CS8987: The 'scoped' modifier of parameter 'a' doesn't match overridden or implemented member.
                //     Span<long> C1.Test3([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "Test3").WithArguments("a").WithLocation(32, 19)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_18_Mismatch_Delegate()
        {
            var src = @"
using System;

class C1
{
    public delegate Span<long> Test1(Span<long> a);
    public delegate Span<long> Test2(Span<long> a);
    public delegate Span<long> Test3(Span<long> a);
}

class C2
{
    void Test()
    {
        C1.Test1 d1 = Test1;
        C1.Test2 d2 = Test2;
        C1.Test3 d3 = Test3;
    }

    public Span<long> Test1(params Span<long> a)
        => throw null;

    public Span<long> Test2(params scoped Span<long> a)
        => throw null;

    public Span<long> Test3(scoped Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ParameterRefSafetyScope_19_Mismatch_Delegate()
        {
            var src = @"
using System;

class C1
{
    public delegate Span<long> Test1(params Span<long> a);
    public delegate Span<long> Test2(params scoped Span<long> a);
    public delegate Span<long> Test3(scoped Span<long> a);
}

class C2
{
    void Test()
    {
        C1.Test1 d1 = Test1;
        C1.Test2 d2 = Test2;
        C1.Test3 d3 = Test3;
    }

    public Span<long> Test1(Span<long> a)
        => throw null;

    public Span<long> Test2(Span<long> a)
        => throw null;

    public Span<long> Test3(Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (15,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test1'.
                //         C1.Test1 d1 = Test1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test1").WithArguments("a", "C1.Test1").WithLocation(15, 23),
                // (16,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test2'.
                //         C1.Test2 d2 = Test2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test2").WithArguments("a", "C1.Test2").WithLocation(16, 23),
                // (17,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test3'.
                //         C1.Test3 d3 = Test3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test3").WithArguments("a", "C1.Test3").WithLocation(17, 23)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_20_Mismatch_Delegate()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C1
{
    public delegate Span<long> Test1(params Span<long> a);
    public delegate Span<long> Test2(params scoped Span<long> a);
    public delegate Span<long> Test3(scoped Span<long> a);
}

class C2
{
    void Test()
    {
        C1.Test1 d1 = Test1;
        C1.Test2 d2 = Test2;
        C1.Test3 d3 = Test3;
    }

    public Span<long> Test1([UnscopedRef] Span<long> a)
        => throw null;

    public Span<long> Test2([UnscopedRef] Span<long> a)
        => throw null;

    public Span<long> Test3([UnscopedRef] Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (16,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test1'.
                //         C1.Test1 d1 = Test1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test1").WithArguments("a", "C1.Test1").WithLocation(16, 23),
                // (17,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test2'.
                //         C1.Test2 d2 = Test2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test2").WithArguments("a", "C1.Test2").WithLocation(17, 23),
                // (18,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test3'.
                //         C1.Test3 d3 = Test3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test3").WithArguments("a", "C1.Test3").WithLocation(18, 23),
                // (21,30): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public Span<long> Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(21, 30),
                // (24,30): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public Span<long> Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(24, 30),
                // (27,30): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     public Span<long> Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(27, 30)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_21_Mismatch_Delegate()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C1
{
    public delegate Span<long> Test1(params Span<long> a);
    public delegate Span<long> Test2(params scoped Span<long> a);
    public delegate Span<long> Test3(scoped Span<long> a);
}

class C2
{
    void Test()
    {
        C1.Test1 d1 = Test1;
        C1.Test2 d2 = Test2;
        C1.Test3 d3 = Test3;
    }

    public Span<long> Test1([UnscopedRef] params Span<long> a)
        => throw null;

    public Span<long> Test2([UnscopedRef] params Span<long> a)
        => throw null;

    public Span<long> Test3([UnscopedRef] params Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3.Invoke").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (16,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test1'.
                //         C1.Test1 d1 = Test1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test1").WithArguments("a", "C1.Test1").WithLocation(16, 23),
                // (17,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test2'.
                //         C1.Test2 d2 = Test2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test2").WithArguments("a", "C1.Test2").WithLocation(17, 23),
                // (18,23): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'C1.Test3'.
                //         C1.Test3 d3 = Test3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "Test3").WithArguments("a", "C1.Test3").WithLocation(18, 23)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_22_Mismatch_Partial()
        {
            var src = @"
using System;

partial class C1
{
    partial void Test1(Span<long> a);
    partial void Test2(Span<long> a);
    partial void Test3(Span<long> a);
}

partial class C1
{
    partial void Test1(params Span<long> a)
    {
    }

    partial void Test2(params scoped Span<long> a)
    {
    }

    partial void Test3(scoped Span<long> a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test1").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test2").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test3").PartialImplementationPart.Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (13,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test1(params Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test1").WithLocation(13, 18),
                // (13,18): error CS8988: The 'scoped' modifier of parameter 'a' doesn't match partial method declaration.
                //     partial void Test1(params Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "Test1").WithArguments("a").WithLocation(13, 18),
                // (17,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test2(params scoped Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test2").WithLocation(17, 18),
                // (17,18): error CS8988: The 'scoped' modifier of parameter 'a' doesn't match partial method declaration.
                //     partial void Test2(params scoped Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "Test2").WithArguments("a").WithLocation(17, 18),
                // (21,18): error CS8988: The 'scoped' modifier of parameter 'a' doesn't match partial method declaration.
                //     partial void Test3(scoped Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "Test3").WithArguments("a").WithLocation(21, 18)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_23_Mismatch_Partial()
        {
            var src = @"
using System;

partial class C1
{
    partial void Test1(params Span<long> a);
    partial void Test2(params scoped Span<long> a);
    partial void Test3(scoped Span<long> a);
}

partial class C1
{
    partial void Test1(Span<long> a)
    {
    }

    partial void Test2(Span<long> a)
    {
    }

    partial void Test3(Span<long> a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test1").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test2").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.NotEqual(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test3").PartialImplementationPart.Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (13,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test1(Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test1").WithLocation(13, 18),
                // (13,18): error CS8988: The 'scoped' modifier of parameter 'a' doesn't match partial method declaration.
                //     partial void Test1(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "Test1").WithArguments("a").WithLocation(13, 18),
                // (17,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test2(Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test2").WithLocation(17, 18),
                // (17,18): error CS8988: The 'scoped' modifier of parameter 'a' doesn't match partial method declaration.
                //     partial void Test2(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "Test2").WithArguments("a").WithLocation(17, 18),
                // (21,18): error CS8988: The 'scoped' modifier of parameter 'a' doesn't match partial method declaration.
                //     partial void Test3(Span<long> a)
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "Test3").WithArguments("a").WithLocation(21, 18)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_24_Mismatch_Partial()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

partial class C1
{
    partial void Test1(params Span<long> a);
    partial void Test2(params scoped Span<long> a);
    partial void Test3(scoped Span<long> a);
}

partial class C1
{
    partial void Test1([UnscopedRef] Span<long> a)
    {
    }

    partial void Test2([UnscopedRef] Span<long> a)
    {
    }

    partial void Test3([UnscopedRef] Span<long> a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            // The the [UnscopedRef] attribute is applied to the declaration as well, and it cancels the scoped modifier
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);

            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test1").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test2").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test3").PartialImplementationPart.Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (14,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test1").WithLocation(14, 18),
                // (14,25): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     partial void Test1([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(14, 25),
                // (18,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test2").WithLocation(18, 18),
                // (18,25): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     partial void Test2([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(18, 25),
                // (22,25): error CS9063: UnscopedRefAttribute cannot be applied to this parameter because it is unscoped by default.
                //     partial void Test3([UnscopedRef] Span<long> a)
                Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, "UnscopedRef").WithLocation(22, 25)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_25_Mismatch_Partial()
        {
            var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

partial class C1
{
    public partial Span<long> Test1(params Span<long> a);
    public partial Span<long> Test2(params scoped Span<long> a);
    public partial Span<long> Test3(scoped Span<long> a);
}

partial class C1
{
    public partial Span<long> Test1([UnscopedRef] params Span<long> a)
        => throw null;

    public partial Span<long> Test2([UnscopedRef] params Span<long> a)
        => throw null;

    public partial Span<long> Test3([UnscopedRef] params Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            // The the [UnscopedRef] attribute is applied to the declaration as well, and it cancels the scoped modifier
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.None, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);

            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test1").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test2").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test3").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test1").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test2").PartialImplementationPart.Parameters.Single().EffectiveScope);
            Assert.Equal(comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope, comp.GetMember<MethodSymbol>("C1.Test3").PartialImplementationPart.Parameters.Single().EffectiveScope);

            comp.VerifyDiagnostics(
                // (20,31): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     public partial Span<long> Test3([UnscopedRef] params Span<long> a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test3").WithLocation(20, 31)
                );
        }

        [Fact]
        public void ParameterRefSafetyScope_26_Mismatch_Overriding()
        {
            var src = @"
using System;

abstract class C1
{
    public abstract Span<long> Test1(params Span<long> a);
    public abstract Span<long> Test2(params scoped Span<long> a);
    public abstract Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1(params Span<long> a)
        => throw null;

    public override Span<long> Test2(params Span<long> a)
        => throw null;

    public override Span<long> Test3(params Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);
            Assert.False(comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().IsParams);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ParameterRefSafetyScope_27_Mismatch_Overriding()
        {
            var src = @"
using System;

abstract class C1
{
    public abstract Span<long> Test1(params Span<long> a);
    public abstract Span<long> Test2(params scoped Span<long> a);
    public abstract Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1(scoped Span<long> a)
        => throw null;

    public override Span<long> Test2(scoped Span<long> a)
        => throw null;

    public override Span<long> Test3(scoped Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);
            Assert.False(comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().IsParams);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ParameterRefSafetyScope_28_Mismatch_Overriding()
        {
            var src = @"
using System;

abstract class C1
{
    public abstract Span<long> Test1(params Span<long> a);
    public abstract Span<long> Test2(params scoped Span<long> a);
    public abstract Span<long> Test3(scoped Span<long> a);
}

class C2 : C1
{
    public override Span<long> Test1(params scoped Span<long> a)
        => throw null;

    public override Span<long> Test2(params scoped Span<long> a)
        => throw null;

    public override Span<long> Test3(params scoped Span<long> a)
        => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test1").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test1").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test2").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().EffectiveScope);
            Assert.True(comp.GetMember<MethodSymbol>("C2.Test2").Parameters.Single().IsParams);

            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C1.Test3").Parameters.Single().EffectiveScope);
            Assert.Equal(ScopedKind.ScopedValue, comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().EffectiveScope);
            Assert.False(comp.GetMember<MethodSymbol>("C2.Test3").Parameters.Single().IsParams);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExpressionTree()
        {
            var src = @"
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Expression<System.Action> e1 = () => Test();
        Expression<System.Action> e2 = () => Test(1);
        Expression<System.Action> e3 = () => Test(2, 3);
        Expression<System.Action> e4 = () => Test([]);

        Expression<System.Action> e5 = () => Test2();
        Expression<System.Action> e6 = () => Test2(1);
        Expression<System.Action> e7 = () => Test2(2, 3);
    }

    static void Test(params System.Collections.Generic.IEnumerable<long> a)
    {
    }

    static void Test2(params long[] a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,46): error CS9226: An expression tree may not contain an expanded form of non-array params collection parameter.
                //         Expression<System.Action> e1 = () => Test();
                Diagnostic(ErrorCode.ERR_ParamsCollectionExpressionTree, "Test()").WithLocation(8, 46),
                // (9,46): error CS9226: An expression tree may not contain an expanded form of non-array params collection parameter.
                //         Expression<System.Action> e2 = () => Test(1);
                Diagnostic(ErrorCode.ERR_ParamsCollectionExpressionTree, "Test(1)").WithLocation(9, 46),
                // (10,46): error CS9226: An expression tree may not contain an expanded form of non-array params collection parameter.
                //         Expression<System.Action> e3 = () => Test(2, 3);
                Diagnostic(ErrorCode.ERR_ParamsCollectionExpressionTree, "Test(2, 3)").WithLocation(10, 46),
                // (11,51): error CS9175: An expression tree may not contain a collection expression.
                //         Expression<System.Action> e4 = () => Test([]);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "[]").WithLocation(11, 51)
                );
        }

        [Fact]
        public void MetadataImport_01_Method()
        {
            // public class Params
            // {
            //     static public void Test1(params System.Collections.Generic.IEnumerable<long> a) { System.Console.Write("Test1"); }
            //     static public void Test2(params long[] a) { System.Console.Write("Test2"); }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params
    extends [mscorlib]System.Object
{
    .method public hidebysig static 
        void Test1 (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig static 
        void Test2 (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            var test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            VerifyParamsAndAttribute(test1, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true);

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true);

            AssertEx.Equal("System.Runtime.CompilerServices.ParamCollectionAttribute", test1.GetCustomAttributesToEmit(null).Single().ToString());
            AssertEx.Equal("System.ParamArrayAttribute", test2.GetCustomAttributesToEmit(null).Single().ToString());

            comp = CreateCompilationWithIL("", il);

            test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true);

            AssertEx.Equal("System.Runtime.CompilerServices.ParamCollectionAttribute", test1.GetCustomAttributesToEmit(null).Single().ToString());
            AssertEx.Equal("System.ParamArrayAttribute", test2.GetCustomAttributesToEmit(null).Single().ToString());

            var src = @"
class Program
{
    static void Main()
    {
        Params.Test1(1);
        Params.Test2(2);
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_02_Method()
        {
            // public class Params
            // {
            //     static public void Test1([ParamCollectionAttribute, ParamArrayAttribute] System.Collections.Generic.IEnumerable<long> a) { System.Console.Write("Test1"); }
            //     static public void Test2([ParamCollectionAttribute, ParamArrayAttribute] long[] a) { System.Console.Write("Test2"); }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params
    extends [mscorlib]System.Object
{
    .method public hidebysig static 
        void Test1 (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig static 
        void Test2 (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            var test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            VerifyParamsAndAttribute(test1, isParamArray: true, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true, isParamCollection: true);

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamArray: true, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true, isParamCollection: true);

            var attributes = new[] { "System.ParamArrayAttribute", "System.Runtime.CompilerServices.ParamCollectionAttribute" };
            AssertEx.Equal(attributes, test1.GetCustomAttributesToEmit(null).Select(a => a.ToString()));
            AssertEx.Equal(attributes, test2.GetCustomAttributesToEmit(null).Select(a => a.ToString()));

            comp = CreateCompilationWithIL("", il);

            test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamArray: true, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true, isParamCollection: true);

            AssertEx.Equal(attributes, test1.GetCustomAttributesToEmit(null).Select(a => a.ToString()));
            AssertEx.Equal(attributes, test2.GetCustomAttributesToEmit(null).Select(a => a.ToString()));

            var src = @"
class Program
{
    static void Main()
    {
        Params.Test1(1);
        Params.Test2(2);
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_03_Method()
        {
            // public class Params
            // {
            //     static public void Test1([ParamArrayAttribute, ParamCollectionAttribute] System.Collections.Generic.IEnumerable<long> a) { System.Console.Write("Test1"); }
            //     static public void Test2([ParamArrayAttribute, ParamCollectionAttribute] long[] a) { System.Console.Write("Test2"); }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params
    extends [mscorlib]System.Object
{
    .method public hidebysig static 
        void Test1 (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig static 
        void Test2 (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            var test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            VerifyParamsAndAttribute(test1, isParamArray: true, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true, isParamCollection: true);

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamArray: true, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true, isParamCollection: true);

            var attributes = new[] { "System.ParamArrayAttribute", "System.Runtime.CompilerServices.ParamCollectionAttribute" };
            AssertEx.Equal(attributes, test1.GetCustomAttributesToEmit(null).Select(a => a.ToString()));
            AssertEx.Equal(attributes, test2.GetCustomAttributesToEmit(null).Select(a => a.ToString()));

            comp = CreateCompilationWithIL("", il);

            test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamArray: true, isParamCollection: true);
            VerifyParamsAndAttribute(test2, isParamArray: true, isParamCollection: true);

            AssertEx.Equal(attributes, test1.GetCustomAttributesToEmit(null).Select(a => a.ToString()));
            AssertEx.Equal(attributes, test2.GetCustomAttributesToEmit(null).Select(a => a.ToString()));

            var src = @"
class Program
{
    static void Main()
    {
        Params.Test1(1);
        Params.Test2(2);
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_04_Method()
        {
            // public class Params
            // {
            //     static public void Test1([ParamArrayAttribute] System.Collections.Generic.IEnumerable<long> a) { System.Console.Write("Test1"); }
            //     static public void Test2([ParamCollectionAttribute] long[] a) { System.Console.Write("Test2"); }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params
    extends [mscorlib]System.Object
{
    .method public hidebysig static 
        void Test1 (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig static 
        void Test2 (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            var test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            VerifyParamsAndAttribute(test1, isParamArray: true);
            VerifyParamsAndAttribute(test2, isParamCollection: true);

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamArray: true);
            VerifyParamsAndAttribute(test2, isParamCollection: true);

            AssertEx.Equal("System.ParamArrayAttribute", test1.GetCustomAttributesToEmit(null).Single().ToString());
            AssertEx.Equal("System.Runtime.CompilerServices.ParamCollectionAttribute", test2.GetCustomAttributesToEmit(null).Single().ToString());

            comp = CreateCompilationWithIL("", il);

            test1 = comp.GetMember<MethodSymbol>("Params.Test1").Parameters.Last();
            test2 = comp.GetMember<MethodSymbol>("Params.Test2").Parameters.Last();

            Assert.Empty(test1.GetAttributes());
            Assert.Empty(test2.GetAttributes());

            VerifyParamsAndAttribute(test1, isParamArray: true);
            VerifyParamsAndAttribute(test2, isParamCollection: true);

            AssertEx.Equal("System.ParamArrayAttribute", test1.GetCustomAttributesToEmit(null).Single().ToString());
            AssertEx.Equal("System.Runtime.CompilerServices.ParamCollectionAttribute", test2.GetCustomAttributesToEmit(null).Single().ToString());

            var src = @"
class Program
{
    static void Main()
    {
        Params.Test1(1);
        Params.Test2(2);
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,22): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Collections.Generic.IEnumerable<long>'
                //         Params.Test1(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Collections.Generic.IEnumerable<long>").WithLocation(6, 22),
                // (7,22): error CS1503: Argument 1: cannot convert from 'int' to 'params long[]'
                //         Params.Test2(2);
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("1", "int", "params long[]").WithLocation(7, 22)
                );
        }

        [Fact]
        public void MetadataImport_05_Property()
        {
            // public class Params1
            // {
            //     public int this[params System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         get
            //         { System.Console.Write("Test1"); return 0; }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[params long[] a]
            //     {
            //         get
            //         { System.Console.Write("Test2"); return 0; }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .get instance int32 Params1::get_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .get instance int32 Params2::get_Item(int64[])
    }

}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamCollection: true);
            VerifyParams(test2, isParamArray: true);

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        _ = new Params2()[2];
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_06_Property()
        {
            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [ParamCollectionAttribute, ParamArrayAttribute] get
            //         { System.Console.Write("Test1"); return 0; }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [ParamCollectionAttribute, ParamArrayAttribute] get
            //         { System.Console.Write("Test2"); return 0; }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .get instance int32 Params1::get_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .get instance int32 Params2::get_Item(int64[])
    }

}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: true, isParamCollection: true);
            VerifyParams(test2, isParamArray: true, isParamCollection: true);

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        _ = new Params2()[2];
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_07_Property()
        {
            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [ParamArrayAttribute, ParamCollectionAttribute] get
            //         { System.Console.Write("Test1"); return 0; }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [ParamArrayAttribute, ParamCollectionAttribute] get
            //         { System.Console.Write("Test2"); return 0; }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .get instance int32 Params1::get_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .get instance int32 Params2::get_Item(int64[])
    }

}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: true, isParamCollection: true);
            VerifyParams(test2, isParamArray: true, isParamCollection: true);

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        _ = new Params2()[2];
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_08_Property()
        {
            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [ParamArrayAttribute] get
            //         { System.Console.Write("Test1"); return 0; }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [ParamCollectionAttribute] get
            //         { System.Console.Write("Test2"); return 0; }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .get instance int32 Params1::get_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance int32 get_Item (
            int64[] a
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .get instance int32 Params2::get_Item(int64[])
    }

}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: true);
            VerifyParams(test2, isParamCollection: true);

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        _ = new Params2()[2];
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,27): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Collections.Generic.IEnumerable<long>'
                //         _ = new Params1()[1];
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Collections.Generic.IEnumerable<long>").WithLocation(6, 27),
                // (7,27): error CS1503: Argument 1: cannot convert from 'int' to 'params long[]'
                //         _ = new Params2()[2];
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("1", "int", "params long[]").WithLocation(7, 27)
                );
        }

        [Fact]
        public void MetadataImport_09_Property()
        {
            // public class Params1
            // {
            //     public int this[params System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[params long[] a]
            //     {
            //         set
            //         { System.Console.Write("Test2"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .set instance void Params1::set_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>, int32)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            int64[] a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .set instance void Params2::set_Item(int64[], int32)
    }

}
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamCollection: true);
            VerifyParams(test2, isParamArray: true);

            var src = @"
class Program
{
    static void Main()
    {
        new Params1()[1] = 0;
        new Params2()[2] = 0;
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_10_Property()
        {
            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [ParamCollectionAttribute, ParamArrayAttribute] set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [ParamCollectionAttribute, ParamArrayAttribute] set
            //         { System.Console.Write("Test2"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .set instance void Params1::set_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>, int32)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            int64[] a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .set instance void Params2::set_Item(int64[], int32)
    }

}
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: true, isParamCollection: true);
            VerifyParams(test2, isParamArray: true, isParamCollection: true);

            var src = @"
class Program
{
    static void Main()
    {
        new Params1()[1] = 0;
        new Params2()[2] = 0;
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_11_Property()
        {
            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [ParamArrayAttribute, ParamCollectionAttribute] set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [ParamArrayAttribute, ParamCollectionAttribute] set
            //         { System.Console.Write("Test2"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .set instance void Params1::set_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>, int32)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            int64[] a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .set instance void Params2::set_Item(int64[], int32)
    }

}
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: true, isParamCollection: true);
            VerifyParams(test2, isParamArray: true, isParamCollection: true);

            var src = @"
class Program
{
    static void Main()
    {
        new Params1()[1] = 0;
        new Params2()[2] = 0;
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Test1Test2").VerifyDiagnostics();
        }

        [Fact]
        public void MetadataImport_12_Property()
        {
            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [ParamArrayAttribute] set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [ParamCollectionAttribute] set
            //         { System.Console.Write("Test2"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .set instance void Params1::set_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>, int32)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .method public hidebysig specialname 
        instance void set_Item (
            int64[] a,
            int32 'value'
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .set instance void Params2::set_Item(int64[], int32)
    }

}
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var comp = CreateCompilationWithIL("", il);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: true);
            VerifyParams(test2, isParamCollection: true);

            var src = @"
class Program
{
    static void Main()
    {
        new Params1()[1] = 0;
        new Params2()[2] = 0;
    }
}
";
            comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,23): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Collections.Generic.IEnumerable<long>'
                //         new Params1()[1] = 0;
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Collections.Generic.IEnumerable<long>").WithLocation(6, 23),
                // (7,23): error CS1503: Argument 1: cannot convert from 'int' to 'params long[]'
                //         new Params2()[2] = 0;
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("1", "int", "params long[]").WithLocation(7, 23)
                );
        }

        [Flags]
        public enum ParamsAttributes
        {
            None = 0,
            Array = 1,
            Collection = 2,
            Both = Array | Collection,
        }

        private string GetAttributesIL(ParamsAttributes attributes)
        {
            if (attributes == ParamsAttributes.None)
            {
                return "";
            }

            string result = @"        .param [1]
";

            if ((attributes & ParamsAttributes.Array) != 0)
            {
                result += @"
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
";
            }

            if ((attributes & ParamsAttributes.Collection) != 0)
            {
                result += @"
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
";
            }

            return result;
        }

        [Theory]
        [CombinatorialData]
        public void MetadataImport_13_Property(ParamsAttributes getAttributes, ParamsAttributes setAttributes)
        {
            if (getAttributes == setAttributes)
            {
                return;
            }

            var getAttributesString = GetAttributesIL(getAttributes);
            var setAttributesString = GetAttributesIL(setAttributes);

            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> a]
            //     {
            //         [getAttributes] get
            //         { System.Console.Write("Test1"); return 0; }
            //         [setAttributes] set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            // public class Params2
            // {
            //     public int this[long[] a]
            //     {
            //         [getAttributes] get
            //         { System.Console.Write("Test2"); return 0; }
            //         [setAttributes] set
            //         { System.Console.Write("Test2"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )


    .method public hidebysig specialname 
        instance int32 get_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
        ) cil managed 
    {
        " + getAttributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname 
        instance void set_Item (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a,
            int32 'value'
        ) cil managed 
    {
        " + setAttributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> a
    )
    {
        .get instance int32 Params1::get_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>)
        .set instance void Params1::set_Item(class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>, int32)
    }
}

.class public auto ansi beforefieldinit Params2
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )


    .method public hidebysig specialname 
        instance int32 get_Item (
            int64[] a
        ) cil managed 
    {
        " + getAttributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname 
        instance void set_Item (
            int64[] a,
            int32 'value'
        ) cil managed 
    {
        " + setAttributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test2""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        int64[] a
    )
    {
        .get instance int32 Params2::get_Item(int64[])
        .set instance void Params2::set_Item(int64[], int32)
    }

}
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        _ = new Params2()[2];
        new Params1()[1] = 0;
        new Params2()[2] = 0;
    }
}
";
            var comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();
            var test2 = comp.GetMember<PropertySymbol>("Params2." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: (setAttributes & ParamsAttributes.Array) != 0, isParamCollection: (setAttributes & ParamsAttributes.Collection) != 0);
            VerifyParams(test2, isParamArray: (setAttributes & ParamsAttributes.Array) != 0, isParamCollection: (setAttributes & ParamsAttributes.Collection) != 0);

            string getModifier = getAttributes == ParamsAttributes.None ? "" : "params ";
            string setModifier = setAttributes == ParamsAttributes.None ? "" : "params ";

            comp.VerifyDiagnostics(
                // (6,13): error CS1545: Property, indexer, or event 'Params1.this[params IEnumerable<long>]' is not supported by the language; try directly calling accessor methods 'Params1.get_Item(params IEnumerable<long>)' or 'Params1.set_Item(params IEnumerable<long>, int)'
                //         _ = new Params1()[1];
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "new Params1()[1]").WithArguments("Params1.this[" + setModifier + "System.Collections.Generic.IEnumerable<long>]", "Params1.get_Item(" + getModifier + "System.Collections.Generic.IEnumerable<long>)", "Params1.set_Item(" + setModifier + "System.Collections.Generic.IEnumerable<long>, int)").WithLocation(6, 13),
                // (7,13): error CS1545: Property, indexer, or event 'Params2.this[params long[]]' is not supported by the language; try directly calling accessor methods 'Params2.get_Item(params long[])' or 'Params2.set_Item(params long[], int)'
                //         _ = new Params2()[2];
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "new Params2()[2]").WithArguments("Params2.this[" + setModifier + "long[]]", "Params2.get_Item(" + getModifier + "long[])", "Params2.set_Item(" + setModifier + "long[], int)").WithLocation(7, 13),
                // (8,9): error CS1545: Property, indexer, or event 'Params1.this[params IEnumerable<long>]' is not supported by the language; try directly calling accessor methods 'Params1.get_Item(params IEnumerable<long>)' or 'Params1.set_Item(params IEnumerable<long>, int)'
                //         new Params1()[1] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "new Params1()[1]").WithArguments("Params1.this[" + setModifier + "System.Collections.Generic.IEnumerable<long>]", "Params1.get_Item(" + getModifier + "System.Collections.Generic.IEnumerable<long>)", "Params1.set_Item(" + setModifier + "System.Collections.Generic.IEnumerable<long>, int)").WithLocation(8, 9),
                // (9,9): error CS1545: Property, indexer, or event 'Params2.this[params long[]]' is not supported by the language; try directly calling accessor methods 'Params2.get_Item(params long[])' or 'Params2.set_Item(params long[], int)'
                //         new Params2()[2] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "new Params2()[2]").WithArguments("Params2.this[" + setModifier + "long[]]", "Params2.get_Item(" + getModifier + "long[])", "Params2.set_Item(" + setModifier + "long[], int)").WithLocation(9, 9)
                );
        }

        [Theory]
        [CombinatorialData]
        public void MetadataImport_14_Property(ParamsAttributes parameterType, ParamsAttributes parameterAttributes)
        {
            if (parameterAttributes == ParamsAttributes.None ||
                parameterType is not (ParamsAttributes.Array or ParamsAttributes.Collection) ||
                (parameterAttributes & parameterType) == 0)
            {
                return;
            }

            var attributesString = GetAttributesIL(parameterAttributes);
            bool isArrayType = parameterType == ParamsAttributes.Array;
            var typeString = isArrayType ? "int64[]" : "class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>";

            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> or long[] a]
            //     {
            //         [parameterAttributes] get
            //         { System.Console.Write("Test1"); return 0; }
            //         [parameterAttributes] set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )


    .method public hidebysig specialname 
        instance int32 get_Item (
            " + typeString + @" a
        ) cil managed 
    {
        " + attributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname 
        instance void set_Item (
            " + typeString + @" a,
            int32 'value'
        ) cil managed 
    {
        " + attributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        " + typeString + @" a
    )
    {
        .get instance int32 Params1::get_Item(" + typeString + @")
        .set instance void Params1::set_Item(" + typeString + @", int32)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        new Params1()[1] = 0;
    }
}
";
            var comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: (parameterAttributes & ParamsAttributes.Array) != 0, isParamCollection: (parameterAttributes & ParamsAttributes.Collection) != 0);

            CompileAndVerify(comp, expectedOutput: "Test1Test1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void MetadataImport_15_Property(ParamsAttributes parameterType, ParamsAttributes parameterAttributes)
        {
            switch (parameterType, parameterAttributes)
            {
                case (ParamsAttributes.Array, ParamsAttributes.Collection):
                case (ParamsAttributes.Collection, ParamsAttributes.Array):
                    break;
                default:
                    return;
            }

            var attributesString = GetAttributesIL(parameterAttributes);
            bool isArrayType = parameterType == ParamsAttributes.Array;
            var typeString = isArrayType ? "int64[]" : "class [mscorlib]System.Collections.Generic.IEnumerable`1<int64>";

            // public class Params1
            // {
            //     public int this[System.Collections.Generic.IEnumerable<long> or long[] a]
            //     {
            //         [parameterAttributes] get
            //         { System.Console.Write("Test1"); return 0; }
            //         [parameterAttributes] set
            //         { System.Console.Write("Test1"); }
            //     }
            // }
            string il = @"
.class public auto ansi beforefieldinit Params1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )


    .method public hidebysig specialname 
        instance int32 get_Item (
            " + typeString + @" a
        ) cil managed 
    {
        " + attributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ldc.i4.0
        IL_000b: ret
    }

    .method public hidebysig specialname 
        instance void set_Item (
            " + typeString + @" a,
            int32 'value'
        ) cil managed 
    {
        " + attributesString + @"
        .maxstack 8

        IL_0000: ldstr ""Test1""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item(
        " + typeString + @" a
    )
    {
        .get instance int32 Params1::get_Item(" + typeString + @")
        .set instance void Params1::set_Item(" + typeString + @", int32)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var src = @"
class Program
{
    static void Main()
    {
        _ = new Params1()[1];
        new Params1()[1] = 0;
    }
}
";
            var comp = CreateCompilationWithIL(src, il, options: TestOptions.ReleaseExe);

            var test1 = comp.GetMember<PropertySymbol>("Params1." + WellKnownMemberNames.Indexer).Parameters.Last();

            VerifyParams(test1, isParamArray: parameterAttributes == ParamsAttributes.Array, isParamCollection: parameterAttributes == ParamsAttributes.Collection);

            if (isArrayType)
            {
                comp.VerifyDiagnostics(
                    // (6,27): error CS1503: Argument 1: cannot convert from 'int' to 'params long[]'
                    //         _ = new Params1()[1];
                    Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params long[]").WithLocation(6, 27),
                    // (7,23): error CS1503: Argument 1: cannot convert from 'int' to 'params long[]'
                    //         new Params1()[1] = 0;
                    Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params long[]").WithLocation(7, 23)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (6,27): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Collections.Generic.IEnumerable<long>'
                    //         _ = new Params1()[1];
                    Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Collections.Generic.IEnumerable<long>").WithLocation(6, 27),
                    // (7,23): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Collections.Generic.IEnumerable<long>'
                    //         new Params1()[1] = 0;
                    Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Collections.Generic.IEnumerable<long>").WithLocation(7, 23)
                    );
            }
        }

        [Fact]
        public void UsingPatternWithParamsTest()
        {
            var source = @"
using System.Collections.Generic;

ref struct S1
{
    public void Dispose(params IEnumerable<int> args)
    {
        System.Console.Write(""Disposed"");
    }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
        S1 c1b = new S1();
        using (c1b) { }
    }
}";
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: "DisposedDisposed").VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWithParamsTest_Foreach()
        {
            var source = @"
using System.Collections.Generic;

ref struct S1
{
    public void Dispose(params IEnumerable<int> args)
    {
        System.Console.Write(""Disposed"");
    }

    public int Current => 0;
    public bool MoveNext() => false;
}

class C2
{
    public S1 GetEnumerator() => default;

    static void Main()
    {
        foreach (var i in new C2())
        {
        }
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(
                comp, expectedOutput: "Disposed",
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[GetEnumerator]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x9 }" }
                            : Verification.Passes
                ).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            comp.VerifyOperationTree(node, expectedOperationTree: """
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 i
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer:
        null
  Collection:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2, IsImplicit) (Syntax: 'new C2()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IObjectCreationOperation (Constructor: C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'new C2()')
          Arguments(0)
          Initializer:
            null
  Body:
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  NextVariables(0)
""");

            VerifyFlowGraph(comp, node.Parent.Parent, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C2()')
                Value:
                IInvocationOperation ( S1 C2.GetEnumerator()) (OperationKind.Invocation, Type: S1, IsImplicit) (Syntax: 'new C2()')
                    Instance Receiver:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2, IsImplicit) (Syntax: 'new C2()')
                        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                        Operand:
                        IObjectCreationOperation (Constructor: C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'new C2()')
                            Arguments(0)
                            Initializer:
                            null
                    Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IInvocationOperation ( System.Boolean S1.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new C2()')
                    Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new C2()')
                    Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                        Left:
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                        Right:
                        IPropertyReferenceOperation: System.Int32 S1.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                            Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new C2()')
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void S1.Dispose(params System.Collections.Generic.IEnumerable<System.Int32> args)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new C2()')
                    Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new C2()')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.ParamCollection, Matching Parameter: args) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'foreach (va ... }')
                        ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'foreach (va ... }')
                            Elements(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
""");
        }

        [Fact]
        public void ERR_ExplicitImplParams_01()
        {
            var source = @"
using System.Collections.Generic;

interface I1
{
    void M1(params IEnumerable<int> args);
    void M2(IEnumerable<int> args);
    void M3(params IEnumerable<int> args);
}
class C2 : I1
{
    void I1.M1(IEnumerable<int> args) {}
    void I1.M2(params IEnumerable<int> args) {}
    void I1.M3(params IEnumerable<int> args) {}
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,13): error CS0466: 'C2.I1.M2(params IEnumerable<int>)' should not have a params parameter since 'I1.M2(IEnumerable<int>)' does not
                //     void I1.M2(params IEnumerable<int> args) {}
                Diagnostic(ErrorCode.ERR_ExplicitImplParams, "M2").WithArguments("C2.I1.M2(params System.Collections.Generic.IEnumerable<int>)", "I1.M2(System.Collections.Generic.IEnumerable<int>)").WithLocation(13, 13)
                );
        }

        [Fact]
        public void ERR_ExplicitImplParams_02()
        {
            // public interface I1
            // {
            //     void M1([ParamArrayAttribute]  IEnumerable<int> args);
            //     void M2([ParamCollectionAttribute]  int[] args);
            // }
            var il = @"
.class interface public auto ansi abstract beforefieldinit I1
{
    .method public hidebysig newslot abstract virtual 
        instance void M1 (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int32> args
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
    }

    .method public hidebysig newslot abstract virtual 
        instance void M2 (
            int32[] args
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var source = @"
using System.Collections.Generic;

class C1 : I1
{
    void I1.M1(IEnumerable<int> args) {}
    void I1.M2(int[] args) {}
}

class C2 : I1
{
    void I1.M1(params IEnumerable<int> args) {}
    void I1.M2(params int[] args) {}
}
";
            CreateCompilationWithIL(source, il).VerifyDiagnostics();
        }

        [Fact]
        public void EmbedAttribute_01_Constructor()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    public Program(params IEnumerable<long> x)
    {
    }
}
""";
            VerifyAttributeEmbedding(
                src,
                "Program..ctor",
                "Program..ctor(params System.Collections.Generic.IEnumerable<System.Int64> x)",
                // (5,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //     public Program(params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(5, 20)
                );
        }

        private void VerifyAttributeEmbedding(string src, string memberName, string memberDisplay, params DiagnosticDescription[] moduleDiagnostic)
        {
            VerifyAttributeEmbedding(src1: src, src2: null, memberName, memberDisplay, moduleDiagnostic);
        }

        private void VerifyAttributeEmbedding(string src1, string src2, string memberName, string memberDisplay, params DiagnosticDescription[] moduleDiagnostic)
        {
            IEnumerable<MetadataReference> references = src2 is null ? [] : [CreateCompilation(src2).ToMetadataReference()];

            var comp = CreateCompilation(src1, references, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.MakeMemberMissing(WellKnownMember.System_ParamArrayAttribute__ctor);
            verify(comp, attributeIsEmbedded: true);

            var comp1 = CreateCompilation(ParamCollectionAttributeSource, options: TestOptions.ReleaseDll);
            var comp1Ref = comp1.ToMetadataReference();
            var comp2 = CreateCompilation(src1, references: references.Concat([comp1Ref]), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp2, attributeIsEmbedded: false);
            Assert.Contains(comp1Ref, comp2.GetUsedAssemblyReferences());

            var comp3 = CreateCompilation(src1, references, options: TestOptions.ReleaseModule);
            comp3.VerifyEmitDiagnostics(moduleDiagnostic);
            Assert.NotEmpty(moduleDiagnostic);

            var comp4 = CreateCompilation(src1, references: references.Concat([comp1Ref]), options: TestOptions.ReleaseModule.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp4, attributeIsEmbedded: false);
            Assert.Contains(comp1Ref, comp4.GetUsedAssemblyReferences());

            const string brokenParamCollectionAttributeSource = @"
namespace System.Runtime.CompilerServices
{
    public sealed class ParamCollectionAttribute : Attribute
    {
        public ParamCollectionAttribute(int x) { }
    }
}
";

            var comp5 = CreateCompilation(brokenParamCollectionAttributeSource, options: TestOptions.ReleaseDll);
            var comp5Ref = comp5.ToMetadataReference();
            var comp6 = CreateCompilation(src1, references: references.Concat([comp5Ref]), options: TestOptions.ReleaseDll);
            comp6.VerifyEmitDiagnostics(
                // (5,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.ParamCollectionAttribute..ctor'
                //     public Program(params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, moduleDiagnostic[0].SquiggledText).WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute", ".ctor").WithLocation(moduleDiagnostic[0].LocationLine, moduleDiagnostic[0].LocationCharacter)
                );

            var comp7 = CreateCompilation(src1, references: references.Concat([comp5Ref]), options: TestOptions.ReleaseModule);
            comp7.VerifyEmitDiagnostics(
                // (5,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.ParamCollectionAttribute..ctor'
                //     public Program(params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, moduleDiagnostic[0].SquiggledText).WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute", ".ctor").WithLocation(moduleDiagnostic[0].LocationLine, moduleDiagnostic[0].LocationCharacter)
                );

            void verify(CSharpCompilation comp, bool attributeIsEmbedded)
            {
                // We want to test attribute embedding 
                Assert.Equal(attributeIsEmbedded, comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor) is null);

                CompileAndVerify(
                    comp,
                    symbolValidator: (m) =>
                    {
                        string adjustedMemberName = memberName;
                        string adjustedMemberDisplay = memberDisplay;
                        if (comp.Options.OutputKind == OutputKind.NetModule && memberName.StartsWith("<>"))
                        {
                            adjustedMemberName = adjustedMemberName.Replace("<>", "<" + comp.SourceAssembly.Name + ">");
                            adjustedMemberDisplay = adjustedMemberDisplay.Replace("<>", "<" + comp.SourceAssembly.Name + ">");
                        }

                        MethodSymbol member = m.GlobalNamespace.GetMember<MethodSymbol>(adjustedMemberName);
                        AssertEx.Equal(adjustedMemberDisplay, member.ToTestDisplayString());
                        VerifyParamsAndAttribute(member.Parameters[0], isParamCollection: true);

                        if (member.AssociatedSymbol is PropertySymbol prop)
                        {
                            VerifyParams(prop.Parameters[0], isParamCollection: true);
                        }

                        if (attributeIsEmbedded)
                        {
                            Assert.NotNull(m.GlobalNamespace.GetMember("System.Runtime.CompilerServices.ParamCollectionAttribute"));
                        }
                        else if (!m.GlobalNamespace.GetMembers("System").IsEmpty)
                        {
                            Assert.Empty(((NamespaceSymbol)m.GlobalNamespace.GetMember("System.Runtime.CompilerServices")).GetMembers("ParamCollectionAttribute"));
                        }
                    },
                    verify: comp.Options.OutputKind != OutputKind.NetModule ?
                                Verification.Passes :
                                Verification.Fails with
                                {
                                    PEVerifyMessage = "The module  was expected to contain an assembly manifest.",
                                    ILVerifyMessage = "The format of a DLL or executable being loaded is invalid"
                                }
                    ).VerifyDiagnostics();
            }
        }

        [Fact]
        public void EmbedAttribute_02_Delegate()
        {
            var src = """
using System.Collections.Generic;

delegate void Program(params IEnumerable<long> x);
""";
            VerifyAttributeEmbedding(
                src,
                "Program.Invoke",
                "void Program.Invoke(params System.Collections.Generic.IEnumerable<System.Int64> x)",
                // (3,23): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                // delegate void Program(params IEnumerable<long> x);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(3, 23)
                );
        }

        [Fact]
        public void EmbedAttribute_03_AnonymousDelegate_FromMember()
        {
            var src1 = @"
class Program
{
    static void Main()
    {
        var x1 = Params.Test1;

        x1(1);
    }
}
";
            var src2 = @"
using System.Collections.Generic;

public class Params
{
    static public void Test1(params IEnumerable<long> a) { }
}
";
            VerifyAttributeEmbedding(
                src1,
                src2,
                "<>f__AnonymousDelegate0.Invoke",
                "void <>f__AnonymousDelegate0.Invoke(params System.Collections.Generic.IEnumerable<System.Int64> arg)",
                // (6,18): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //         var x1 = Params.Test1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Params.Test1").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(6, 18)
                );
        }

        [Fact]
        public void EmbedAttribute_04_AnonymousDelegate_FromLambda()
        {
            var src = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var x1 = (params IEnumerable<long> a) => {};

        x1(1);
    }
}
";
            VerifyAttributeEmbedding(
                src,
                "<>f__AnonymousDelegate0.Invoke",
                "void <>f__AnonymousDelegate0.Invoke(params System.Collections.Generic.IEnumerable<System.Int64> arg)",
                // (8,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //         var x1 = (params IEnumerable<long> a) => {};
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> a").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(8, 19)
                );
        }

        [Fact]
        public void EmbedAttribute_05_AnonymousDelegate_FromLambda_ExpressionTree()
        {
            var src = @"
using System.Collections.Generic;
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Test((params IEnumerable<long> a) => {});
    }
    
    static void Test<T>(Expression<T> e){}
}
";
            CreateCompilation(src).VerifyEmitDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'Program.Test<T>(Expression<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test((params IEnumerable<long> a) => {});
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Program.Test<T>(System.Linq.Expressions.Expression<T>)").WithLocation(9, 9)
                );
        }

        [Fact]
        public void EmbedAttribute_06_AnonymousDelegate_FromLocalFunction()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var x1 = Test1;

        x1(1);

        void Test1(params IEnumerable<long> a) { }
    }
}
""";
            VerifyAttributeEmbedding(
                src,
                "<>f__AnonymousDelegate0.Invoke",
                "void <>f__AnonymousDelegate0.Invoke(params System.Collections.Generic.IEnumerable<System.Int64> arg)",
                // (7,18): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //         var x1 = Test1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test1").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(7, 18)
                );
        }

        [Fact]
        public void EmbedAttribute_07_RegularMethod()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    void Test(params IEnumerable<long> x)
    {
    }
}
""";
            VerifyAttributeEmbedding(
                src,
                "Program.Test",
                "void Program.Test(params System.Collections.Generic.IEnumerable<System.Int64> x)",
                // (5,15): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //     void Test(params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(5, 15)
                );
        }

        [Fact]
        public void EmbedAttribute_08_Operator()
        {
            var src = @"
using System.Collections.Generic;

class Program
{
    public static implicit operator Program(params List<long> x)
    {
        return null;
    }

    public static Program operator +(Program x, params List<long> y)
    {
        return null;
    }
}
";
            CreateCompilation(src).VerifyEmitDiagnostics(
                // (6,45): error CS1670: params is not valid in this context
                //     public static implicit operator Program(params List<long> x)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(6, 45),
                // (11,49): error CS1670: params is not valid in this context
                //     public static Program operator +(Program x, params List<long> y)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(11, 49)
                );
        }

        [Fact]
        public void EmbedAttribute_09_Property_get()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    int this[params IEnumerable<long> x]
        => 0;
}
""";
            VerifyAttributeEmbedding(
                src,
                "Program.get_Item",
                "System.Int32 Program.this[params System.Collections.Generic.IEnumerable<System.Int64> x].get",
                // (5,14): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //     int this[params IEnumerable<long> x]
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(5, 14)
                );
        }

        [Fact]
        public void EmbedAttribute_10_Property_get()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    int this[params IEnumerable<long> x]
    { get => 0; set {} }
}
""";
            VerifyAttributeEmbedding(
                src,
                "Program.get_Item",
                "System.Int32 Program.this[params System.Collections.Generic.IEnumerable<System.Int64> x].get",
                // (5,14): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //     int this[params IEnumerable<long> x]
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(5, 14)
                );
        }

        [Fact]
        public void EmbedAttribute_11_Property_set()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    int this[params IEnumerable<long> x]
    {set{}}
}
""";
            VerifyAttributeEmbedding(
                src,
                "Program.set_Item",
                "void Program.this[params System.Collections.Generic.IEnumerable<System.Int64> x].set",
                // (5,14): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //     int this[params IEnumerable<long> x]
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(5, 14)
                );
        }

        [Fact]
        public void EmbedAttribute_12_Property_set()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    int this[params IEnumerable<long> x]
    {get => 0; set{}}
}
""";
            VerifyAttributeEmbedding(
                src,
                "Program.set_Item",
                "void Program.this[params System.Collections.Generic.IEnumerable<System.Int64> x].set",
                // (5,14): error CS0518: Predefined type 'System.Runtime.CompilerServices.ParamCollectionAttribute' is not defined or imported
                //     int this[params IEnumerable<long> x]
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "params IEnumerable<long> x").WithArguments("System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(5, 14)
                );
        }

        [Fact]
        public void EmbedAttribute_13_ParamCollectionAttributeNotPortedInNoPia()
        {
            var comAssembly = CreateCompilationWithMscorlib40(@"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
[ComImport()]
[Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
public interface Test
{
    int Method(params IEnumerable<int> x);
}");

            // We want to test attribute embedding 
            Assert.Null(comAssembly.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor));

            CompileAndVerify(comAssembly, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");
                var method = type.GetMethod("Method");
                var parameter = method.Parameters.Single();
                VerifyParamsAndAttribute(parameter, isParamCollection: true);
            });

            var code = @"
class User
{
    public void M(Test p)
    {
        p.Method(0);
    }
}";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var compilation_CompilationReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comAssembly.ToMetadataReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_CompilationReference, symbolValidator: symbolValidator);

            var compilation_BinaryReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comAssembly.EmitToImageReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_BinaryReference, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                // Attribute is not embedded
                if (!module.GlobalNamespace.GetMembers("System").IsEmpty)
                {
                    Assert.Empty(((NamespaceSymbol)module.GlobalNamespace.GetMember("System.Runtime.CompilerServices")).GetMembers("ParamCollectionAttribute"));
                }

                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");
                var method = type.GetMethod("Method");
                var parameter = method.Parameters.Single();

                // Attribute is not applied
                VerifyParamsAndAttribute(parameter, isParamCollection: false);
            }
        }

        [Fact]
        public void ParamCollectionAttributeNotPortedInNoPia_01()
        {
            var comAssembly = CreateCompilationWithMscorlib40(@"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
[ComImport()]
[Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
public interface Test
{
    int Method(params IEnumerable<int> x);
}" + ParamCollectionAttributeSource);

            Assert.NotNull(comAssembly.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor));

            CompileAndVerify(comAssembly, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");
                var method = type.GetMethod("Method");
                var parameter = method.Parameters.Single();
                VerifyParamsAndAttribute(parameter, isParamCollection: true);
            });

            var code = @"
class User
{
    public void M(Test p)
    {
        p.Method(0);
    }
}";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var compilation_CompilationReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comAssembly.ToMetadataReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_CompilationReference, symbolValidator: symbolValidator);

            var compilation_BinaryReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comAssembly.EmitToImageReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_BinaryReference, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                // Attribute is not embedded
                if (!module.GlobalNamespace.GetMembers("System").IsEmpty)
                {
                    Assert.Empty(((NamespaceSymbol)module.GlobalNamespace.GetMember("System.Runtime.CompilerServices")).GetMembers("ParamCollectionAttribute"));
                }

                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");
                var method = type.GetMethod("Method");
                var parameter = method.Parameters.Single();

                // Attribute is not applied
                VerifyParamsAndAttribute(parameter, isParamCollection: false);
            }
        }

        [Fact]
        public void ParamCollectionAttributeNotPortedInNoPia_02()
        {
            var comp1 = CreateCompilationWithMscorlib40(ParamCollectionAttributeSource, options: TestOptions.ReleaseDll);
            var comp1Ref = comp1.EmitToImageReference();

            var comAssembly = CreateCompilationWithMscorlib40(@"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
[ComImport()]
[Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
public interface Test
{
    int Method(params IEnumerable<int> x);
}", references: [comp1Ref]);

            Assert.NotNull(comAssembly.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor));

            CompileAndVerify(comAssembly, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");
                var method = type.GetMethod("Method");
                var parameter = method.Parameters.Single();
                VerifyParamsAndAttribute(parameter, isParamCollection: true);
            });

            var code = @"
class User
{
    public void M(Test p)
    {
        p.Method(0);
    }
}";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var compilation_CompilationReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comp1Ref, comAssembly.ToMetadataReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_CompilationReference, symbolValidator: symbolValidator);

            var compilation_BinaryReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comp1Ref, comAssembly.EmitToImageReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_BinaryReference, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                // Attribute is not embedded
                if (!module.GlobalNamespace.GetMembers("System").IsEmpty)
                {
                    Assert.Empty(((NamespaceSymbol)module.GlobalNamespace.GetMember("System.Runtime.CompilerServices")).GetMembers("ParamCollectionAttribute"));
                }

                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");
                var method = type.GetMethod("Method");
                var parameter = method.Parameters.Single();

                // Attribute is not applied
                VerifyParamsAndAttribute(parameter, isParamCollection: false);
            }
        }

        [Fact]
        public void ConsumeAcrossAssemblyBoundary_01_Method()
        {
            var src1 = """
using System.Collections.Generic;
using System.Linq;

public class Params
{
    public static void Test(params IEnumerable<long> a)
    {
        var array = a.ToArray();
        if (array.Length == 0)
        {
            System.Console.WriteLine(array.Length);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", array.Length, array[0], array[array.Length - 1]);
        }
    }
}
""";
            var src2 = """
class Program
{
    static void Main()
    {
        Params.Test();
        Params.Test(1);
        Params.Test(2, 3);
    }
}
""";
            var comp1 = CreateCompilation(src1);

            verify(image: true);
            verify(image: false);

            void verify(bool image)
            {
                var comp2 = CreateCompilation(src2, references: [image ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);

                CompileAndVerify(
                    comp2,
                    verify: image ? Verification.Passes : Verification.Skipped,
                    expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void ConsumeAcrossAssemblyBoundary_02_Property(bool hasSet)
        {
            var src1 = """
using System.Collections.Generic;
using System.Linq;

public class Params
{
    public int this[char c, params IEnumerable<long> a]
    {
        get
        {
            var array = a.ToArray();
            if (array.Length == 0)
            {
                System.Console.WriteLine(array.Length);
            }
            else
            {
                System.Console.WriteLine("{0}: {1} ... {2}", array.Length, array[0], array[array.Length - 1]);
            }

            return 0;
        }
""" + (hasSet ? """
        set {}
""" :
"") +
            """
    }
}
""";
            var src2 = """
class Program
{
    static void Main()
    {
        var p = new Params();
        _ = p['a'];
        _ = p['b', 1];
        _ = p['c', 2, 3];
    }
}
""";
            var comp1 = CreateCompilation(src1);

            verify(image: true);
            verify(image: false);

            void verify(bool image)
            {
                var comp2 = CreateCompilation(src2, references: [image ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);

                CompileAndVerify(
                    comp2,
                    verify: image ? Verification.Passes : Verification.Skipped,
                    expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void ConsumeAcrossAssemblyBoundary_03_Property(bool hasGet)
        {
            var src1 = """
using System.Collections.Generic;
using System.Linq;

public class Params
{
    public int this[char c, params IEnumerable<long> a]
    {
        set
        {
            var array = a.ToArray();
            if (array.Length == 0)
            {
                System.Console.WriteLine(array.Length);
            }
            else
            {
                System.Console.WriteLine("{0}: {1} ... {2}", array.Length, array[0], array[array.Length - 1]);
            }
        }
""" + (hasGet ? """
        get => 0;
""" :
"") +
            """
    }
}
""";
            var src2 = """
class Program
{
    static void Main()
    {
        var p = new Params();
        p['a'] = 0;
        p['b', 1] = 1;
        p['c', 2, 3] = 2;
    }
}
""";
            var comp1 = CreateCompilation(src1);

            verify(image: true);
            verify(image: false);

            void verify(bool image)
            {
                var comp2 = CreateCompilation(src2, references: [image ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);

                CompileAndVerify(
                    comp2,
                    verify: image ? Verification.Passes : Verification.Skipped,
                    expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
            }
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71840")]
        public void UnsafeContext_01_Constructor()
        {
            string source1 = """
using System.Collections;
using System.Collections.Generic;
                            
public class MyCollectionOfInt : IEnumerable<int>
{
    unsafe public MyCollectionOfInt(void* dummy = null){}

    public List<int> Array = new List<int>();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
                            
    public void Add(int l) => Array.Add(l);
}
""";

            var comp1 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll);
            var comp1Ref = comp1.EmitToImageReference();

            string source2 = """
class Program
{
    unsafe public static void Test(params MyCollectionOfInt a)
    {
    }
}
""";

            var comp2 = CreateCompilation(source2, references: [comp1Ref], options: TestOptions.UnsafeDebugDll);
            comp2.VerifyEmitDiagnostics();

            string source3 = """
public class Params
{
    public static void Test(params MyCollectionOfInt a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";

            var comp3 = CreateCompilation(source3, references: [comp1Ref], options: TestOptions.DebugDll);
            comp3.VerifyEmitDiagnostics();

            string source4 = """
class Program
{
    static void Main()
    {
        Params.Test();
        Params.Test(1);
        Params.Test(2, 3);
    }
}
""";

            var comp4 = CreateCompilation(source4, references: [comp1Ref, comp3.ToMetadataReference()], options: TestOptions.ReleaseExe);
            comp4.VerifyEmitDiagnostics(
                // (5,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Params.Test();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Params.Test()").WithLocation(5, 9),
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Params.Test(1);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Params.Test(1)").WithLocation(6, 9),
                // (7,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Params.Test(2, 3);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Params.Test(2, 3)").WithLocation(7, 9)
                );

            string source5 = """
class Program
{
    static unsafe void Main()
    {
        Params.Test();
        Params.Test(1);
        Params.Test(2, 3);
    }
}
""";

            var comp5 = CreateCompilation(source5, references: [comp1Ref, comp3.ToMetadataReference()], options: TestOptions.UnsafeReleaseExe);
            CompileAndVerify(
                comp5,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71840")]
        public void UnsafeContext_02_Add()
        {
            string source1 = """
using System.Collections;
using System.Collections.Generic;
                            
public class MyCollectionOfInt : IEnumerable<int>
{
    public List<int> Array = new List<int>();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
                            
    unsafe public void Add(int l, void* dummy = null) => Array.Add(l);
}
""";

            var comp1 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll);
            var comp1Ref = comp1.EmitToImageReference();

            string source2 = """
class Program
{
    unsafe public static void Test(params MyCollectionOfInt a)
    {
    }
}
""";

            var comp2 = CreateCompilation(source2, references: [comp1Ref], options: TestOptions.UnsafeDebugDll);
            comp2.VerifyEmitDiagnostics();

            string source3 = """
public class Params
{
    public static void Test(params MyCollectionOfInt a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";

            var comp3 = CreateCompilation(source3, references: [comp1Ref], options: TestOptions.DebugDll);
            comp3.VerifyEmitDiagnostics();

            string source4 = """
class Program
{
    static void Main()
    {
        Params.Test();
        Params.Test(1);
        Params.Test(2, 3);
    }
}
""";

            var comp4 = CreateCompilation(source4, references: [comp1Ref, comp3.ToMetadataReference()], options: TestOptions.ReleaseExe);
            comp4.VerifyEmitDiagnostics(
                // (6,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Params.Test(1);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "1").WithLocation(6, 21),
                // (7,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Params.Test(2, 3);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "2").WithLocation(7, 21),
                // (7,24): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Params.Test(2, 3);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "3").WithLocation(7, 24)
                );

            string source5 = """
class Program
{
    static unsafe void Main()
    {
        Params.Test();
        Params.Test(1);
        Params.Test(2, 3);
    }
}
""";

            var comp5 = CreateCompilation(source5, references: [comp1Ref, comp3.ToMetadataReference()], options: TestOptions.UnsafeReleaseExe);
            CompileAndVerify(
                comp5,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_01()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection(params MyCollection p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l, params MyCollection p) => Array.Add(l);
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,9): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test()").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(20, 9),
                // (21,9): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(1)").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(21, 9),
                // (22,9): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(2, 3)").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(22, 9)
                );
        }

        [Fact]
        public void Cycle_02()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection(params MyCollection p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
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
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,9): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test()").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(20, 9),
                // (21,9): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(1)").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(21, 9),
                // (22,9): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(2, 3)").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(22, 9)
                );
        }

        [Fact]
        public void Cycle_03()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection()
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l, params MyCollection p) => Array.Add(l);
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
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_04()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection(params MyCollection p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l, params MyCollection p) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test([]);
        Test([1]);
        Test([2, 3]);
    }

    static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,14): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[]").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(20, 14),
                // (21,14): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([1]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[1]").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(21, 14),
                // (21,15): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([1]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "1").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(21, 15),
                // (22,14): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([2, 3]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[2, 3]").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(22, 14),
                // (22,15): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([2, 3]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "2").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(22, 15),
                // (22,18): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([2, 3]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "3").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(22, 18)
                );
        }

        [Fact]
        public void Cycle_05()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection(params MyCollection p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test([]);
        Test([1]);
        Test([2, 3]);
    }

    static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,14): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[]").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(20, 14),
                // (21,14): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([1]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[1]").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(21, 14),
                // (22,14): error CS9223: Creation of params collection 'MyCollection' results in an infinite chain of invocation of constructor 'MyCollection.MyCollection(params MyCollection)'.
                //         Test([2, 3]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[2, 3]").WithArguments("MyCollection", "MyCollection.MyCollection(params MyCollection)").WithLocation(22, 14)
                );
        }

        [Fact]
        public void Cycle_06()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection()
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l, params MyCollection p) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test([]);
        Test([1]);
        Test([2, 3]);
    }

    static void Test(params MyCollection a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_07()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection<T> : IEnumerable
{
    public List<object> Array;
    public MyCollection(params MyCollection<MyCollection<T>> p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l, params MyCollection<MyCollection<T>> p) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection<int> a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,9): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)'.
                //         Test();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test()").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)").WithLocation(20, 9),
                // (21,9): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(1)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)").WithLocation(21, 9),
                // (22,9): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(2, 3)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)").WithLocation(22, 9)
                );
        }

        [Fact]
        public void Cycle_08()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection<T> : IEnumerable
{
    public List<object> Array;
    public MyCollection()
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l, params MyCollection<MyCollection<T>> p) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection<int> a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_09()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection<T> : IEnumerable
{
    public List<object> Array;
    public MyCollection(params MyCollection<MyCollection<T>> p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection<int> a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (20,9): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)'.
                //         Test();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test()").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)").WithLocation(20, 9),
                // (21,9): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(1)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)").WithLocation(21, 9),
                // (22,9): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(2, 3)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<MyCollection<T>>)").WithLocation(22, 9)
                );
        }

        [Fact]
        public void Cycle_10()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection1 : IEnumerable
{
    public List<object> Array;
    public MyCollection1(params MyCollection2 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class MyCollection2 : IEnumerable
{
    public List<object> Array;
    public MyCollection2(params MyCollection1 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class MyCollection3 : IEnumerable
{
    public List<object> Array;
    public MyCollection3(params MyCollection2 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection3 a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (44,9): error CS9223: Creation of params collection 'MyCollection3' results in an infinite chain of invocation of constructor 'MyCollection2.MyCollection2(params MyCollection1)'.
                //         Test();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test()").WithArguments("MyCollection3", "MyCollection2.MyCollection2(params MyCollection1)").WithLocation(44, 9),
                // (45,9): error CS9223: Creation of params collection 'MyCollection3' results in an infinite chain of invocation of constructor 'MyCollection2.MyCollection2(params MyCollection1)'.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(1)").WithArguments("MyCollection3", "MyCollection2.MyCollection2(params MyCollection1)").WithLocation(45, 9),
                // (46,9): error CS9223: Creation of params collection 'MyCollection3' results in an infinite chain of invocation of constructor 'MyCollection2.MyCollection2(params MyCollection1)'.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Test(2, 3)").WithArguments("MyCollection3", "MyCollection2.MyCollection2(params MyCollection1)").WithLocation(46, 9)
                );
        }

        [Fact]
        public void Cycle_11()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection1 : IEnumerable
{
    public List<object> Array;
    public MyCollection1(params int[] p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class MyCollection2 : IEnumerable
{
    public List<object> Array;
    public MyCollection2(params MyCollection1 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class MyCollection3 : IEnumerable
{
    public List<object> Array;
    public MyCollection3(params MyCollection2 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection3 a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_12()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection1 : IEnumerable
{
    public List<object> Array;
    public MyCollection1()
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class MyCollection2 : IEnumerable
{
    public List<object> Array;
    public MyCollection2(params MyCollection1 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class MyCollection3 : IEnumerable
{
    public List<object> Array;
    public MyCollection3(params MyCollection2 p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection3 a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_13()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array;
    public MyCollection(params int[] p)
    {
        Array = new List<object>();
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(object l) => Array.Add(l);
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
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void Cycle_14_ThroughCollectionBuilderAttribute()
        {
            var src = """
#pragma warning disable CS0436 // The type 'CollectionBuilderAttribute' in '' conflicts with the imported type 'CollectionBuilderAttribute' in 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. Using the type defined in ''.

using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
    public sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName, params CollectionBuilderAttribute p) { }
        public IEnumerator<long> GetEnumerator() => throw null;
    }

    public class MyCollectionBuilder
    {
        public static CollectionBuilderAttribute Create(ReadOnlySpan<long> items) => throw null;
    }

    class Program
    {
        static void Main()
        {
            Test();
            Test(1);
            Test(2, 3);
        }

        static void Test(params CollectionBuilderAttribute a)
        {
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (9,6): error CS7036: There is no argument given that corresponds to the required parameter 'p' of 'CollectionBuilderAttribute.CollectionBuilderAttribute(Type, string, params CollectionBuilderAttribute)'
                //     [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))").WithArguments("p", "System.Runtime.CompilerServices.CollectionBuilderAttribute.CollectionBuilderAttribute(System.Type, string, params System.Runtime.CompilerServices.CollectionBuilderAttribute)").WithLocation(9, 6),
                // (12,80): error CS0225: The params parameter must have a valid collection type
                //         public CollectionBuilderAttribute(Type builderType, string methodName, params CollectionBuilderAttribute p) { }
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(12, 80),
                // (25,13): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params CollectionBuilderAttribute)'
                //             Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "System.Runtime.CompilerServices.Program.Test(params System.Runtime.CompilerServices.CollectionBuilderAttribute)").WithLocation(25, 13),
                // (26,18): error CS1503: Argument 1: cannot convert from 'int' to 'params System.Runtime.CompilerServices.CollectionBuilderAttribute'
                //             Test(1);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params System.Runtime.CompilerServices.CollectionBuilderAttribute").WithLocation(26, 18),
                // (27,13): error CS1501: No overload for method 'Test' takes 2 arguments
                //             Test(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(27, 13),
                // (30,26): error CS0225: The params parameter must have a valid collection type
                //         static void Test(params CollectionBuilderAttribute a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(30, 26)
                );
        }

        [Fact]
        public void Cycle_15_ThroughCollectionBuilderAttribute()
        {
            var src = """
#pragma warning disable CS0436 // The type 'CollectionBuilderAttribute' in '' conflicts with the imported type 'CollectionBuilderAttribute' in 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. Using the type defined in ''.

using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
    public sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, params CollectionBuilderAttribute p) { }
        public IEnumerator<string> GetEnumerator() => throw null;
    }

    public class MyCollectionBuilder
    {
        public static CollectionBuilderAttribute Create(ReadOnlySpan<string> items) => throw null;
    }

    class Program
    {
        static void Main()
        {
            Test();
            Test("1");
            Test("2", "3");
        }

        static void Test(params CollectionBuilderAttribute a)
        {
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (9,53): error CS1503: Argument 2: cannot convert from 'string' to 'params System.Runtime.CompilerServices.CollectionBuilderAttribute'
                //     [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                Diagnostic(ErrorCode.ERR_BadArgType, "nameof(MyCollectionBuilder.Create)").WithArguments("2", "string", "params System.Runtime.CompilerServices.CollectionBuilderAttribute").WithLocation(9, 53),
                // (12,61): error CS0225: The params parameter must have a valid collection type
                //         public CollectionBuilderAttribute(Type builderType, params CollectionBuilderAttribute p) { }
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(12, 61),
                // (25,13): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'Program.Test(params CollectionBuilderAttribute)'
                //             Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("a", "System.Runtime.CompilerServices.Program.Test(params System.Runtime.CompilerServices.CollectionBuilderAttribute)").WithLocation(25, 13),
                // (26,18): error CS1503: Argument 1: cannot convert from 'string' to 'params System.Runtime.CompilerServices.CollectionBuilderAttribute'
                //             Test("1");
                Diagnostic(ErrorCode.ERR_BadArgType, @"""1""").WithArguments("1", "string", "params System.Runtime.CompilerServices.CollectionBuilderAttribute").WithLocation(26, 18),
                // (27,13): error CS1501: No overload for method 'Test' takes 2 arguments
                //             Test("2", "3");
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test").WithArguments("Test", "2").WithLocation(27, 13),
                // (30,26): error CS0225: The params parameter must have a valid collection type
                //         static void Test(params CollectionBuilderAttribute a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(30, 26)
                );
        }

        [Fact]
        public void InvalidParamsTypeInPartialMethod()
        {
            var src = """
partial class Program
{
    partial void Test1(params int a);

    partial void Test1(params int a)
    {
    }

    partial void Test2(int a);

    partial void Test2(params int a)
    {
    }

    partial void Test3(params int a);

    partial void Test3(int a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (3,24): error CS0225: The params parameter must have a valid collection type
                //     partial void Test1(params int a);
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(3, 24),
                // (5,24): error CS0225: The params parameter must have a valid collection type
                //     partial void Test1(params int a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(5, 24),
                // (11,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test2(params int a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test2").WithLocation(11, 18),
                // (11,24): error CS0225: The params parameter must have a valid collection type
                //     partial void Test2(params int a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(11, 24),
                // (15,24): error CS0225: The params parameter must have a valid collection type
                //     partial void Test3(params int a);
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(15, 24),
                // (17,18): error CS0758: Both partial method declarations must use a params parameter or neither may use a params parameter
                //     partial void Test3(int a)
                Diagnostic(ErrorCode.ERR_PartialMethodParamsDifference, "Test3").WithLocation(17, 18)
                );
        }

        [Fact]
        public void InvalidParamsType()
        {
            var src = """
partial class Program
{
    int this[params int a] => a;

    void Test()
    {
        var x = (params int a) => a;
        local (0);

        int local(params int a) => a; 
    }
}

delegate void D(params int a);
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (3,14): error CS0225: The params parameter must have a valid collection type
                //     int this[params int a] => a;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(3, 14),
                // (7,18): error CS0225: The params parameter must have a valid collection type
                //         var x = (params int a) => a;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(7, 18),
                // (10,19): error CS0225: The params parameter must have a valid collection type
                //         int local(params int a) => a; 
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(10, 19),
                // (14,17): error CS0225: The params parameter must have a valid collection type
                // delegate void D(params int a);
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(14, 17)
                );
        }

        [Fact]
        public void CollectionWithRequiredMember_01()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

public class MyCollection1 : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(long l) => throw null;

    public required int F;
}

public class MyCollection2 : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(long l) => throw null;
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection1 a)
    {
    }

    [System.Obsolete]
    static void Test(MyCollection2 a)
    {
    }

    static void Test2(MyCollection1 a)
    {
    }

    static void Test2()
    {
        MyCollection1 b = [1];
        Test([2, 3]);
        Test2([2, 3]);
    }
}

""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,9): error CS9035: Required member 'MyCollection1.F' must be set in the object initializer or attribute constructor.
                //         Test();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Test()").WithArguments("MyCollection1.F").WithLocation(24, 9),
                // (25,9): error CS9035: Required member 'MyCollection1.F' must be set in the object initializer or attribute constructor.
                //         Test(1);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Test(1)").WithArguments("MyCollection1.F").WithLocation(25, 9),
                // (26,9): error CS9035: Required member 'MyCollection1.F' must be set in the object initializer or attribute constructor.
                //         Test(2, 3);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Test(2, 3)").WithArguments("MyCollection1.F").WithLocation(26, 9),
                // (29,22): error CS9225: Constructor 'MyCollection1.MyCollection1()' leaves required member 'MyCollection1.F' uninitialized.
                //     static void Test(params MyCollection1 a)
                Diagnostic(ErrorCode.ERR_ParamsCollectionConstructorDoesntInitializeRequiredMember, "params").WithArguments("MyCollection1.MyCollection1()", "MyCollection1.F").WithLocation(29, 22),
                // (44,27): error CS9035: Required member 'MyCollection1.F' must be set in the object initializer or attribute constructor.
                //         MyCollection1 b = [1];
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "[1]").WithArguments("MyCollection1.F").WithLocation(44, 27),
                // (45,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(params MyCollection1)' and 'Program.Test(MyCollection2)'
                //         Test([2, 3]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(params MyCollection1)", "Program.Test(MyCollection2)").WithLocation(45, 9),
                // (46,15): error CS9035: Required member 'MyCollection1.F' must be set in the object initializer or attribute constructor.
                //         Test2([2, 3]);
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "[2, 3]").WithArguments("MyCollection1.F").WithLocation(46, 15)
                );
        }

        [Fact]
        public void CollectionWithRequiredMember_02()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class MyCollection : IEnumerable<long>
{
    [SetsRequiredMembers]
    public MyCollection(){}

    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);

    public required int F;
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
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}

""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParamsOverriding_01()
        {
            var src = @"
using System.Collections.Generic;

abstract class C1
{
    public abstract void Test(params IEnumerable<long> a);
}

class C2 : C1
{
    public override void Test(IEnumerable<long> a)
    {}
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            VerifyParams(comp.GetMember<MethodSymbol>("C1.Test").Parameters.Single(), isParamCollection: true);
            VerifyParams(comp.GetMember<MethodSymbol>("C2.Test").Parameters.Single(), isParamCollection: true);
        }

        [Fact]
        public void ParamsOverriding_02()
        {
            var src = @"
using System.Collections.Generic;

abstract class C1
{
    public abstract void Test(IEnumerable<long> a);
}

class C2 : C1
{
    public override void Test(params IEnumerable<long> a)
    {}
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            VerifyParams(comp.GetMember<MethodSymbol>("C1.Test").Parameters.Single());
            VerifyParams(comp.GetMember<MethodSymbol>("C2.Test").Parameters.Single());
        }

        [Fact]
        public void NullableAnalysis_01()
        {
            var src = """
#nullable enable

using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<string>
{
    IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw null!;
    IEnumerator IEnumerable.GetEnumerator() => throw null!;
    public void Add(string? l) => throw null!;
}

class Program
{
    static void Test_1(string? x, string y, string? z, string u, string? v, string w, string? s)
    {
        Test1();
        Test1(x);
        Test1(y);
        Test1(z, u, v);
        Test1(w, s);
    }

    static void Test_2(string? x, string y, string? z, string u, string? v, string w, string? s)
    {
        Test2();
        Test2(x);
        Test2(y);
        Test2(z, u, v);
        Test2(w, s);
    }

    static void Test1(params MyCollection paramsParameter)
    {
    }

    static void Test2(params string[] paramsParameter)
    {
    }
}
""";
            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (18,15): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test1(params MyCollection paramsParameter)'.
                //         Test1(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("paramsParameter", "void Program.Test1(params MyCollection paramsParameter)").WithLocation(18, 15),
                // (20,15): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test1(params MyCollection paramsParameter)'.
                //         Test1(z, u, v);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "z").WithArguments("paramsParameter", "void Program.Test1(params MyCollection paramsParameter)").WithLocation(20, 15),
                // (20,21): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test1(params MyCollection paramsParameter)'.
                //         Test1(z, u, v);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "v").WithArguments("paramsParameter", "void Program.Test1(params MyCollection paramsParameter)").WithLocation(20, 21),
                // (21,18): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test1(params MyCollection paramsParameter)'.
                //         Test1(w, s);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("paramsParameter", "void Program.Test1(params MyCollection paramsParameter)").WithLocation(21, 18),
                // (27,15): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test2(params string[] paramsParameter)'.
                //         Test2(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("paramsParameter", "void Program.Test2(params string[] paramsParameter)").WithLocation(27, 15),
                // (29,15): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test2(params string[] paramsParameter)'.
                //         Test2(z, u, v);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "z").WithArguments("paramsParameter", "void Program.Test2(params string[] paramsParameter)").WithLocation(29, 15),
                // (29,21): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test2(params string[] paramsParameter)'.
                //         Test2(z, u, v);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "v").WithArguments("paramsParameter", "void Program.Test2(params string[] paramsParameter)").WithLocation(29, 21),
                // (30,18): warning CS8604: Possible null reference argument for parameter 'paramsParameter' in 'void Program.Test2(params string[] paramsParameter)'.
                //         Test2(w, s);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("paramsParameter", "void Program.Test2(params string[] paramsParameter)").WithLocation(30, 18)
                );
        }

        [Fact]
        public void NullableAnalysis_02_GenericInference()
        {
            var src = """
#nullable enable

using System.Collections;
using System.Collections.Generic;

class MyCollection<T> : IEnumerable<T>
{
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null!;
    IEnumerator IEnumerable.GetEnumerator() => throw null!;
    public void Add(T? l) => throw null!;
}

class Program
{
    static void Test_1(string? x, string y, string? z, string u, string? v, string w, string? s)
    {
        Test1(x) = null;
        Test1(y) = null;
        Test1(z, u, v) = null;
        Test1(w, s) = null;
    }

    static void Test_2(string? x, string y, string? z, string u, string? v, string w, string? s)
    {
        Test2(x) = null;
        Test2(y) = null;
        Test2(z, u, v) = null;
        Test2(w, s) = null;
    }

    static ref T Test1<T>(params MyCollection<T> paramsParameter)
    {
        throw null!;
    }

    static ref T Test2<T>(params T[] paramsParameter)
    {
        throw null!;
    }
}
""";
            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (18,20): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         Test1(y) = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(18, 20),
                // (26,20): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         Test2(y) = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(26, 20)
                );
        }

        [Fact]
        public void GetAsyncEnumerator_WithParams()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

class MyCollection : IEnumerable<long>
{
    public MyCollection()
    {
        System.Console.Write(""MyCollection "");
    }

    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => throw null;
}

class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(params MyCollection x)
    {
        System.Console.Write(""GetAsyncEnumerator"");
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "MyCollection GetAsyncEnumerator").VerifyDiagnostics();
        }

        [Fact]
        public void TestMoveNextAsync_WithParamsParameter()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

public class MyCollection : IEnumerable<long>
{
    public MyCollection()
    {
        System.Console.Write(""MyCollection "");
    }

    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => throw null;
}

public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async System.Threading.Tasks.Task<bool> MoveNextAsync(params MyCollection ok)
        {
            System.Console.Write($""MoveNextAsync"");
            await System.Threading.Tasks.Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MyCollection MoveNextAsync");
        }

        [Fact]
        public void PatternBasedDisposal_WithParams()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MyCollection : IEnumerable<long>
{
    public MyCollection()
    {
        System.Console.Write(""MyCollection "");
    }

    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => throw null;
}

class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task DisposeAsync(params MyCollection s)
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync MyCollection DisposeAsync Done");
        }

        [Fact]
        public void OperatorWithParams_01()
        {
            var src = @"
using System.Collections.Generic;

class C
{
    static void Test(Program p, List<long> l, long[] a)
    {
        p = (Program)l;
        p = (Program)a;

        _ = p + l;
        _ = p + a;
    }
}

class Program
{
    public static implicit operator Program(params List<long> x)
    {
        return null;
    }

    public static Program operator +(Program x, params List<long> y)
    {
        return null;
    }

    public static implicit operator Program(params long[] x)
    {
        return null;
    }

    public static Program operator +(Program x, params long[] y)
    {
        return null;
    }
}
";
            CreateCompilation(src).VerifyEmitDiagnostics(
                // (18,45): error CS1670: params is not valid in this context
                //     public static implicit operator Program(params List<long> x)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(18, 45),
                // (23,49): error CS1670: params is not valid in this context
                //     public static Program operator +(Program x, params List<long> y)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(23, 49),
                // (28,45): error CS1670: params is not valid in this context
                //     public static implicit operator Program(params long[] x)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(28, 45),
                // (33,49): error CS1670: params is not valid in this context
                //     public static Program operator +(Program x, params long[] y)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(33, 49)
                );
        }

        [Fact]
        public void OperatorWithParams_02()
        {
            // public class Program
            // {
            //     public static implicit operator Program(params List<long> x)
            //     {
            //         return null;
            //     }
            //   
            //     public static Program operator +(Program x, params List<long> y)
            //     {
            //         return null;
            //     }
            //   
            //     public static implicit operator Program(params long[] x)
            //     {
            //         return null;
            //     }
            //   
            //     public static Program operator +(Program x, params long[] y)
            //     {
            //         return null;
            //     }
            // }
            var ilSource = """
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname static 
        class Program op_Implicit (
            class [mscorlib]System.Collections.Generic.List`1<int64> x
        ) cil managed 
    {
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldnull
        IL_0001: ret
    }

    .method public hidebysig specialname static 
        class Program op_Addition (
            class Program x,
            class [mscorlib]System.Collections.Generic.List`1<int64> y
        ) cil managed 
    {
        .param [2]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldnull
        IL_0001: ret
    }

    .method public hidebysig specialname static 
        class Program op_Implicit (
            int64[] x
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldnull
        IL_0001: ret
    }

    .method public hidebysig specialname static 
        class Program op_Addition (
            class Program x,
            int64[] y
        ) cil managed 
    {
        .param [2]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ldnull
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
""";

            var src = @"
using System.Collections.Generic;

class C
{
    static void Test(Program p, List<long> l, long[] a)
    {
        p = (Program)l;
        p = (Program)a;

        _ = p + l;
        _ = p + a;
    }
}
";
            CreateCompilationWithIL(src, ilSource).VerifyEmitDiagnostics(
                // (8,13): error CS0030: Cannot convert type 'System.Collections.Generic.List<long>' to 'Program'
                //         p = (Program)l;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Program)l").WithArguments("System.Collections.Generic.List<long>", "Program").WithLocation(8, 13),
                // (9,13): error CS0030: Cannot convert type 'long[]' to 'Program'
                //         p = (Program)a;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Program)a").WithArguments("long[]", "Program").WithLocation(9, 13),
                // (11,13): error CS0019: Operator '+' cannot be applied to operands of type 'Program' and 'List<long>'
                //         _ = p + l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p + l").WithArguments("+", "Program", "System.Collections.Generic.List<long>").WithLocation(11, 13),
                // (12,13): error CS0019: Operator '+' cannot be applied to operands of type 'Program' and 'long[]'
                //         _ = p + a;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p + a").WithArguments("+", "Program", "long[]").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ExtensionMethodWithParams_01()
        {
            var src = @"
using System.Collections.Generic;

class C
{
    static void Test(IEnumerable<long> l, long[] a)
    {
        l.M1();
        a.M2();
    }
}

static class Ext
{
    public static void M1(this params IEnumerable<long> x)
    {
    }

    public static void M2(this params long[] x)
    {
    }
}
";
            CreateCompilation(src).VerifyEmitDiagnostics(
                // (15,32): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void M1(this params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(15, 32),
                // (19,32): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void M2(this params long[] x)
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(19, 32)
                );
        }

        [Fact]
        public void ExtensionMethodWithParams_02()
        {
            // static class Ext
            // {
            //     public static void M1(this params IEnumerable<long> x)
            //     {
            //     }
            // 
            //     public static void M2(this params long[] x)
            //     {
            //     }
            // }
            var ilSource = """
.class public auto ansi abstract sealed beforefieldinit Ext
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )

    .method public hidebysig static 
        void M1 (
            class [mscorlib]System.Collections.Generic.IEnumerable`1<int64> x
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param [1]
            .custom instance void System.Runtime.CompilerServices.ParamCollectionAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ret
    }

    .method public hidebysig static 
        void M2 (
            int64[] x
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )

        .maxstack 8

        IL_0000: ret
    }
}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ParamCollectionAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
""";

            var src = @"
using System.Collections.Generic;

class C
{
    static void Test(IEnumerable<long> l, long[] a)
    {
        l.M1();
        a.M2();
    }
}
";
            CreateCompilationWithIL(src, ilSource).VerifyEmitDiagnostics(
                // (8,11): error CS1061: 'IEnumerable<long>' does not contain a definition for 'M1' and no accessible extension method 'M1' accepting a first argument of type 'IEnumerable<long>' could be found (are you missing a using directive or an assembly reference?)
                //         l.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("System.Collections.Generic.IEnumerable<long>", "M1").WithLocation(8, 11),
                // (9,11): error CS1061: 'long[]' does not contain a definition for 'M2' and no accessible extension method 'M2' accepting a first argument of type 'long[]' could be found (are you missing a using directive or an assembly reference?)
                //         a.M2();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("long[]", "M2").WithLocation(9, 11)
                );
        }

        [Fact]
        public void VarargWithParams_01()
        {
            var src = @"
using System.Collections.Generic;

class C1
{
    C1(params IEnumerable<long> x, __arglist)
    {
    }

    C1(params long[] x, __arglist)
    {
    }

    static void M1(params IEnumerable<long> x, __arglist)
    {
    }

    static void M2(params long[] x, __arglist)
    {
    }
}

class C2
{
    C2(IEnumerable<long> x, __arglist)
    {
    }

    C2(long[] x, __arglist)
    {
    }

    static void M3(IEnumerable<long> x, __arglist)
    {
    }

    static void M4(long[] x, __arglist)
    {
    }
}
";
            CreateCompilation(src).VerifyEmitDiagnostics(
                // (6,5): error CS0224: A method with vararg cannot be generic, be in a generic type, or have a params parameter
                //     C1(params IEnumerable<long> x, __arglist)
                Diagnostic(ErrorCode.ERR_BadVarargs, "C1").WithLocation(6, 5),
                // (6,8): error CS0231: A params parameter must be the last parameter in a parameter list
                //     C1(params IEnumerable<long> x, __arglist)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<long> x").WithLocation(6, 8),
                // (10,5): error CS0224: A method with vararg cannot be generic, be in a generic type, or have a params parameter
                //     C1(params long[] x, __arglist)
                Diagnostic(ErrorCode.ERR_BadVarargs, "C1").WithLocation(10, 5),
                // (10,8): error CS0231: A params parameter must be the last parameter in a parameter list
                //     C1(params long[] x, __arglist)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params long[] x").WithLocation(10, 8),
                // (14,17): error CS0224: A method with vararg cannot be generic, be in a generic type, or have a params parameter
                //     static void M1(params IEnumerable<long> x, __arglist)
                Diagnostic(ErrorCode.ERR_BadVarargs, "M1").WithLocation(14, 17),
                // (14,20): error CS0231: A params parameter must be the last parameter in a parameter list
                //     static void M1(params IEnumerable<long> x, __arglist)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<long> x").WithLocation(14, 20),
                // (18,17): error CS0224: A method with vararg cannot be generic, be in a generic type, or have a params parameter
                //     static void M2(params long[] x, __arglist)
                Diagnostic(ErrorCode.ERR_BadVarargs, "M2").WithLocation(18, 17),
                // (18,20): error CS0231: A params parameter must be the last parameter in a parameter list
                //     static void M2(params long[] x, __arglist)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params long[] x").WithLocation(18, 20)
                );
        }

        [Fact]
        public void ParamsNotLast_01()
        {
            var src = @"
using System.Collections.Generic;

class C1
{
    C1(params IEnumerable<long> x, int y)
    {
    }

    static void M1(params IEnumerable<long> x, int y)
    {
    }

    int this[params IEnumerable<long> x, int y] => 0;   
}

delegate void D1(params IEnumerable<long> x, int y);

class C2
{
    C2(IEnumerable<long> x, int y)
    {
    }

    static void M3(IEnumerable<long> x, int y)
    {
    }

    int this[IEnumerable<long> x, int y] => 0;   
}

delegate void D2(IEnumerable<long> x, int y);
";
            CreateCompilation(src).VerifyDiagnostics(
                // (6,8): error CS0231: A params parameter must be the last parameter in a parameter list
                //     C1(params IEnumerable<long> x, int y)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<long> x").WithLocation(6, 8),
                // (10,20): error CS0231: A params parameter must be the last parameter in a parameter list
                //     static void M1(params IEnumerable<long> x, int y)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<long> x").WithLocation(10, 20),
                // (14,14): error CS0231: A params parameter must be the last parameter in a parameter list
                //     int this[params IEnumerable<long> x, int y] => 0;   
                Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<long> x").WithLocation(14, 14),
                // (17,18): error CS0231: A params parameter must be the last parameter in a parameter list
                // delegate void D1(params IEnumerable<long> x, int y);
                Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<long> x").WithLocation(17, 18)
                );
        }

        [Fact]
        public void ExplicitParamCollectionAttribute()
        {
            var source = """
var lam = ([System.Runtime.CompilerServices.ParamCollectionAttribute] int[] xs) => xs.Length;
""";

            CreateCompilation([source, ParamCollectionAttributeSource]).VerifyDiagnostics(
                // (1,13): error CS0674: Do not use 'System.ParamArrayAttribute'/'System.Runtime.CompilerServices.ParamCollectionAttribute'. Use the 'params' keyword instead.
                // var lam = ([System.Runtime.CompilerServices.ParamCollectionAttribute] int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_ExplicitParamArrayOrCollection, "System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(1, 13)
                );
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ref")]
        [InlineData("ref readonly")]
        [InlineData("out")]
        public void ParamsCantBeWithModifier_01(string modifier)
        {
            var src = @"
using System.Collections.Generic;

class C1
{
    static void M1(params " + modifier + @" IEnumerable<long> x)
    {
        throw null;
    }
}
";

            if (modifier == "ref readonly")
            {
                modifier = "ref";
            }

            CreateCompilation(src).VerifyDiagnostics(
                // (6,27): error CS1611: The params parameter cannot be declared as in
                //     static void M1(params in IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, modifier).WithArguments(modifier).WithLocation(6, 27)
                );
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ref")]
        [InlineData("ref readonly")]
        [InlineData("out")]
        public void ParamsCantBeWithModifier_02(string modifier)
        {
            var src = @"
using System.Collections.Generic;

class C1
{
    static void M1(" + modifier + @"
params IEnumerable<long> x)
    {
        throw null;
    }
}
";

            if (modifier == "ref readonly")
            {
                modifier = "ref";
            }

            CreateCompilation(src).VerifyDiagnostics(
                // (7,1): error CS8328:  The parameter modifier 'params' cannot be used with 'in'
                // params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", modifier).WithLocation(7, 1)
                );
        }

        [Fact]
        public void IllegalParams_01()
        {
            var src = @"
using System.Collections.Generic;

var lam = delegate (params IEnumerable<long> xs)
{
    return 0;
};
";

            CreateCompilation(src).VerifyDiagnostics(
                // (4,21): error CS1670: params is not valid in this context
                // var lam = delegate (params IEnumerable<long> xs)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(4, 21)
                );
        }

        [Fact]
        public void ParamsWithDefault_01()
        {
            var src = @"
using System.Collections.Generic;

class C1
{
    static void M1(params IEnumerable<long> x = null)
    {
    }
}
";
            CreateCompilation(src).VerifyDiagnostics(
                // (6,20): error CS1751: Cannot specify a default value for a parameter collection
                //     static void M1(params IEnumerable<long> x = null)
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(6, 20)
                );
        }

        [Fact]
        public void NoOverloadingOnParams_01()
        {
            var src = @"
using System.Collections.Generic;

class C1
{
    static void M1(params IEnumerable<long> x)
    {
    }
    static void M1(IEnumerable<long> x)
    {
    }

    static void M2(IEnumerable<long> x)
    {
    }
    static void M2(params IEnumerable<long> x)
    {
    }
}
";
            CreateCompilation(src).VerifyDiagnostics(
                // (9,17): error CS0111: Type 'C1' already defines a member called 'M1' with the same parameter types
                //     static void M1(IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C1").WithLocation(9, 17),
                // (16,17): error CS0111: Type 'C1' already defines a member called 'M2' with the same parameter types
                //     static void M2(params IEnumerable<long> x)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "C1").WithLocation(16, 17)
                );
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/72098")]
        [Fact]
        public void AddMethod_Derived_01()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;

                class Element { }

                class ElementCollection : IEnumerable
                {
                    private readonly List<object> _list = new();
                    public IEnumerator GetEnumerator() => _list.GetEnumerator();
                    public void Add(Element element) { _list.Add(element); }
                }

                class Program
                {
                    static void Main()
                    {
                        Test(new Element(), null);
                    }

                    static void Test(params ElementCollection c)
                    {
                        c.Report();
                    }
                }
                """;
            CompileAndVerify([source, CollectionExpressionTests.s_collectionExtensions], expectedOutput: "[Element, null], ");
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/72098")]
        [Fact]
        public void AddMethod_Derived_02()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;

                class Base { }
                class Element : Base { }

                class ElementCollection : IEnumerable<Base>
                {
                    private readonly List<Base> _list = new();
                    public IEnumerator<Base> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public void Add(Element element) { _list.Add(element); }
                }

                class Program
                {
                    static void Main()
                    {
                        Test(new Element(), null);
                    }
                
                    static void Test(params ElementCollection c)
                    {
                        c.Report();
                    }
                }
                """;
            CompileAndVerify([source, CollectionExpressionTests.s_collectionExtensions], expectedOutput: "[Element, null], ");
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/71240")]
        [Fact]
        public void AddMethod_Derived_03()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;

                class Sample<T> : IEnumerable<object[]>
                {
                    private readonly List<object[]> _list = new();
                    IEnumerator<object[]> IEnumerable<object[]>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                    public void Add(T t) { if (t is object[] o) _list.Add(o); }
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        Test(["a"], ["b"], ["c"]);
                    }
                
                    static void Test(params Sample<string[]> s)
                    {
                        s.Report();
                    }
                }
                """;
            CompileAndVerify([sourceA, sourceB1, CollectionExpressionTests.s_collectionExtensions], expectedOutput: "[[a], [b], [c]], ");

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        Test("a", null);
                    }
                
                    static void Test(params Sample<string> s)
                    {
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB2]);
            comp.VerifyEmitDiagnostics(
                // (5,14): error CS1503: Argument 1: cannot convert from 'string' to 'object[]'
                //         Test("a", null);
                Diagnostic(ErrorCode.ERR_BadArgType, @"""a""").WithArguments("1", "string", "object[]").WithLocation(5, 14)
                );
        }

        [Fact]
        public void AddMethod_Generic_02()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable
                {
                    private readonly List<T> _list = new();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                    public void Add<U>(T t) { _list.Add(t); }
                }
                class Program
                {
                
                    static void Test(params MyCollection<object> z)
                    {
                    }

                    static void Main()
                    {
                        int x = 1;
                        Test(x);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,22): error CS9215: Collection expression type 'MyCollection<object>' must have an instance or extension method 'Add' that can be called with a single argument.
                //     static void Test(params MyCollection<object> z)
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "params MyCollection<object> z").WithArguments("MyCollection<object>").WithLocation(12, 22),
                // (19,14): error CS1503: Argument 1: cannot convert from 'int' to 'params MyCollection<object>'
                //         Test(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "int", "params MyCollection<object>").WithLocation(19, 14)
                );
        }
    }
}
