// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionExpressionTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

        private const string s_collectionExtensions = """
            using System;
            using System.Collections;
            using System.Linq;
            using System.Text;
            static partial class CollectionExtensions
            {
                private static void Append(StringBuilder builder, bool isFirst, object value)
                {
                    if (!isFirst) builder.Append(", ");
                    if (value is IEnumerable e && value is not string)
                    {
                        AppendCollection(builder, e);
                    }
                    else
                    {
                        builder.Append(value is null ? "null" : value.ToString());
                    }
                }
                private static void AppendCollection(StringBuilder builder, IEnumerable e)
                {
                    builder.Append("[");
                    bool isFirst = true;
                    foreach (var i in e)
                    {
                        Append(builder, isFirst, i);
                        isFirst = false;
                    }
                    builder.Append("]");
                }
                internal static void Report(this object o, bool includeType = false)
                {
                    var builder = new StringBuilder();
                    Append(builder, isFirst: true, o);
                    if (includeType) Console.Write("({0}) ", GetTypeName(o.GetType()));
                    Console.Write(builder.ToString());
                    Console.Write(", ");
                }
                private static string GetTypeName(Type type)
                {
                    if (type.IsArray)
                    {
                        return GetTypeName(type.GetElementType()) + "[]";
                    }
                    string typeName = type.Name;
                    int index = typeName.LastIndexOf('`');
                    if (index >= 0)
                    {
                        typeName = typeName.Substring(0, index);
                    }
                    if (!type.IsGenericParameter)
                    {
                        if (type.DeclaringType is { } declaringType)
                        {
                            typeName = Concat(GetTypeName(declaringType), typeName);
                        }
                        else
                        {
                            typeName = Concat(type.Namespace, typeName);
                        }
                    }
                    if (!type.IsGenericType)
                    {
                        return typeName;
                    }
                    var typeArgs = type.GetGenericArguments();
                    return $"{typeName}<{string.Join(", ", typeArgs.Select(GetTypeName))}>";
                }
                private static string Concat(string container, string name)
                {
                    return string.IsNullOrEmpty(container) ? name : container + "." + name;
                }
            }
            """;
        private const string s_collectionExtensionsWithSpan = s_collectionExtensions +
            """
            static partial class CollectionExtensions
            {
                internal static void Report<T>(this in Span<T> s)
                {
                    Report((ReadOnlySpan<T>)s);
                }
                internal static void Report<T>(this in ReadOnlySpan<T> s)
                {
                    var builder = new StringBuilder();
                    builder.Append("[");
                    bool isFirst = true;
                    foreach (var i in s)
                    {
                        Append(builder, isFirst, i);
                        isFirst = false;
                    }
                    builder.Append("]");
                    Console.Write(builder.ToString());
                    Console.Write(", ");
                }
            }
            """;

        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Preview)]
        public void LanguageVersionDiagnostics(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        object[] x = [];
                        List<object> y = [1, 2, 3];
                        List<object[]> z = [[]];
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp11)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,22): error CS9058: Feature 'collection expressions' is not available in C# 11.0. Please use language version 12.0 or greater.
                    //         object[] x = [];
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "[").WithArguments("collection expressions", "12.0").WithLocation(6, 22),
                    // (7,26): error CS9058: Feature 'collection expressions' is not available in C# 11.0. Please use language version 12.0 or greater.
                    //         List<object> y = [1, 2, 3];
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "[").WithArguments("collection expressions", "12.0").WithLocation(7, 26),
                    // (8,28): error CS9058: Feature 'collection expressions' is not available in C# 11.0. Please use language version 12.0 or greater.
                    //         List<object[]> z = [[]];
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "[").WithArguments("collection expressions", "12.0").WithLocation(8, 28),
                    // (8,29): error CS9058: Feature 'collection expressions' is not available in C# 11.0. Please use language version 12.0 or greater.
                    //         List<object[]> z = [[]];
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "[").WithArguments("collection expressions", "12.0").WithLocation(8, 29));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Fact]
        public void NaturalType_01()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [];
                        dynamic y = [];
                        var z = [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object x = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9174: Cannot initialize type 'dynamic' with a collection expression because the type is not constructible.
                //         dynamic y = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9176: There is no target type for the collection expression.
                //         var z = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 17));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
            Assert.Equal(3, collections.Length);
            VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: "System.Object", ConversionKind.NoConversion);
            VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: "dynamic", ConversionKind.NoConversion);
            VerifyTypes(model, collections[2], expectedType: null, expectedConvertedType: "?", ConversionKind.NoConversion);
        }

        [Fact]
        public void NaturalType_02()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [1];
                        dynamic y = [2];
                        var z = [3];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object x = [1];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9174: Cannot initialize type 'dynamic' with a collection expression because the type is not constructible.
                //         dynamic y = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9176: There is no target type for the collection expression.
                //         var z = [3];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[3]").WithLocation(7, 17));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
            Assert.Equal(3, collections.Length);
            VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: "System.Object", ConversionKind.NoConversion);
            VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: "dynamic", ConversionKind.NoConversion);
            VerifyTypes(model, collections[2], expectedType: null, expectedConvertedType: "?", ConversionKind.NoConversion);
        }

        [Fact]
        public void NaturalType_03()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [1, ""];
                        dynamic y = [2, ""];
                        var z = [3, ""];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object x = [1, ""];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, @"[1, """"]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9174: Cannot initialize type 'dynamic' with a collection expression because the type is not constructible.
                //         dynamic y = [2, ""];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, @"[2, """"]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9176: There is no target type for the collection expression.
                //         var z = [3, ""];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, @"[3, """"]").WithLocation(7, 17));
        }

        [Fact]
        public void NaturalType_04()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [null];
                        dynamic y = [null];
                        var z = [null];
                        int?[] w = [null];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object x = [null];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[null]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9174: Cannot initialize type 'dynamic' with a collection expression because the type is not constructible.
                //         dynamic y = [null];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[null]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9176: There is no target type for the collection expression.
                //         var z = [null];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[null]").WithLocation(7, 17));
        }

        [Fact]
        public void NaturalType_05()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var x = [1, 2, null];
                        object y = [1, 2, null];
                        dynamic z = [1, 2, null];
                        int?[] w = [1, 2, null];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS9176: There is no target type for the collection expression.
                //         var x = [1, 2, null];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, null]").WithLocation(5, 17),
                // (6,20): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object y = [1, 2, null];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2, null]").WithArguments("object").WithLocation(6, 20),
                // (7,21): error CS9174: Cannot initialize type 'dynamic' with a collection expression because the type is not constructible.
                //         dynamic z = [1, 2, null];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2, null]").WithArguments("dynamic").WithLocation(7, 21));
        }

        [Fact]
        public void NaturalType_06()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[] x = [[]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,23): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object[] x = [[]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("object").WithLocation(5, 23));
        }

        [Fact]
        public void NaturalType_07()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[] y = [[2]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,23): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object[] y = [[2]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("object").WithLocation(5, 23));
        }

        [Fact]
        public void NaturalType_08()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var z = [[3]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS9176: There is no target type for the collection expression.
                //         var z = [[3]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[[3]]").WithLocation(5, 17));
        }

        [Fact]
        public void NaturalType_09()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F([]);
                        F([[]]);
                    }
                    static T F<T>(T t) => t;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(5, 9),
                // (6,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([[]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(6, 9));
        }

        [Fact]
        public void NaturalType_10()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F([1, 2]).Report();
                        F([[3, 4]]).Report();
                    }
                    static T F<T>(T t) => t;
                }
                """;
            var comp = CreateCompilation(new[] { source, s_collectionExtensions });
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([1, 2]).Report();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(5, 9),
                // 0.cs(6,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([[3, 4]]).Report();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(6, 9));
        }

        [Fact]
        public void NaturalType_11()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        var d1 = () => [];
                        Func<int[]> d2 = () => [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = () => [];
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "() => []").WithLocation(6, 18));
        }

        [Fact]
        public void InterfaceType_01()
        {
            string source = """
                using System.Collections;
                class Program
                {
                    static void Main()
                    {
                        IEnumerable a = [1];
                        ICollection b = [2];
                        IList c = [3];
                        a.Report(includeType: true);
                        b.Report(includeType: true);
                        c.Report(includeType: true);
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, s_collectionExtensions });
            comp.VerifyEmitDiagnostics(
                // 0.cs(6,25): error CS9174: Cannot initialize type 'IEnumerable' with a collection expression because the type is not constructible.
                //         IEnumerable a = [1];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1]").WithArguments("System.Collections.IEnumerable").WithLocation(6, 25),
                // 0.cs(7,25): error CS9174: Cannot initialize type 'ICollection' with a collection expression because the type is not constructible.
                //         ICollection b = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("System.Collections.ICollection").WithLocation(7, 25),
                // 0.cs(8,19): error CS9174: Cannot initialize type 'IList' with a collection expression because the type is not constructible.
                //         IList c = [3];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[3]").WithArguments("System.Collections.IList").WithLocation(8, 19));
        }

        [Fact]
        public void InterfaceType_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IEnumerable<int> a = [];
                        ICollection<int> b = [];
                        IList<int> c = [];
                        IReadOnlyCollection<int> d = [];
                        IReadOnlyList<int> e = [];
                        a.Report(includeType: true);
                        b.Report(includeType: true);
                        c.Report(includeType: true);
                        d.Report(includeType: true);
                        e.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensions },
                expectedOutput: "(System.Int32[]) [], (System.Collections.Generic.List<System.Int32>) [], (System.Collections.Generic.List<System.Int32>) [], (System.Int32[]) [], (System.Int32[]) [], ");
        }

        [Fact]
        public void InterfaceType_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IEnumerable<int> a = [1];
                        ICollection<int> b = [2];
                        IList<int> c = [3];
                        IReadOnlyCollection<int> d = [4];
                        IReadOnlyList<int> e = [5];
                        a.Report(includeType: true);
                        b.Report(includeType: true);
                        c.Report(includeType: true);
                        d.Report(includeType: true);
                        e.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensions },
                expectedOutput: "(System.Collections.Generic.List<System.Int32>) [1], (System.Collections.Generic.List<System.Int32>) [2], (System.Collections.Generic.List<System.Int32>) [3], (System.Collections.Generic.List<System.Int32>) [4], (System.Collections.Generic.List<System.Int32>) [5], ");
        }

        [Fact]
        public void NaturalType_23()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var x = [null, 1];
                        object y = [null, 2];
                        int?[] z = [null, 3];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS9176: There is no target type for the collection expression.
                //         var x = [null, 1];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[null, 1]").WithLocation(5, 17),
                // (6,20): error CS9174: Cannot initialize type 'object' with a collection expression because the type is not constructible.
                //         object y = [null, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[null, 2]").WithArguments("object").WithLocation(6, 20));
        }

        [Fact]
        public void NaturalType_24()
        {
            string source = """
                class Program
                {
                    static void F1(int i)
                    {
                        (string, int)[] x1 = [(null, default)];
                        string[] y1 = [i switch {  _ => default }];
                        string[] z1 = [i == 0 ? null : default];
                    }
                    static void F2(int i)
                    /*<bind>*/
                    {
                        var x2 = [(null, default)];
                        var y2 = [i switch { _ => default }];
                        var z2 = [i == 0 ? null : default];
                    }
                    /*</bind>*/
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,18): error CS9176: There is no target type for the collection expression.
                //         var x2 = [(null, default)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[(null, default)]").WithLocation(12, 18),
                // (13,18): error CS9176: There is no target type for the collection expression.
                //         var y2 = [i switch { _ => default }];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[i switch { _ => default }]").WithLocation(13, 18),
                // (13,21): error CS8506: No best type was found for the switch expression.
                //         var y2 = [i switch { _ => default }];
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(13, 21),
                // (14,18): error CS9176: There is no target type for the collection expression.
                //         var z2 = [i == 0 ? null : default];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[i == 0 ? null : default]").WithLocation(14, 18),
                // (14,19): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between '<null>' and 'default'
                //         var z2 = [i == 0 ? null : default];
                Diagnostic(ErrorCode.ERR_InvalidQM, "i == 0 ? null : default").WithArguments("<null>", "default").WithLocation(14, 19));

            VerifyOperationTreeForTest<BlockSyntax>(comp,
                """
                IBlockOperation (3 statements, 3 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
                  Locals: Local_1: ? x2
                    Local_2: ? y2
                    Local_3: ? z2
                  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var x2 = [( ...  default)];')
                    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var x2 = [( ... , default)]')
                      Declarators:
                          IVariableDeclaratorOperation (Symbol: ? x2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x2 = [(null, default)]')
                            Initializer:
                              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= [(null, default)]')
                                IOperation:  (OperationKind.None, Type: ?, IsInvalid) (Syntax: '[(null, default)]')
                                  Children(1):
                                      ITupleOperation (OperationKind.Tuple, Type: null, IsInvalid) (Syntax: '(null, default)')
                                        NaturalType: null
                                        Elements(2):
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                                            IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, IsInvalid) (Syntax: 'default')
                      Initializer:
                        null
                  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var y2 = [i ... default }];')
                    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var y2 = [i ...  default }]')
                      Declarators:
                          IVariableDeclaratorOperation (Symbol: ? y2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'y2 = [i swi ...  default }]')
                            Initializer:
                              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= [i switch ...  default }]')
                                IOperation:  (OperationKind.None, Type: ?, IsInvalid) (Syntax: '[i switch { ...  default }]')
                                  Children(1):
                                      ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: ?, IsInvalid) (Syntax: 'i switch {  ... > default }')
                                        Value:
                                          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                                        Arms(1):
                                            ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => default')
                                              Pattern:
                                                IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                                              Value:
                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsInvalid, IsImplicit) (Syntax: 'default')
                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                  Operand:
                                                    IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, IsInvalid) (Syntax: 'default')
                      Initializer:
                        null
                  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var z2 = [i ... : default];')
                    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var z2 = [i ...  : default]')
                      Declarators:
                          IVariableDeclaratorOperation (Symbol: ? z2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'z2 = [i ==  ...  : default]')
                            Initializer:
                              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= [i == 0 ? ...  : default]')
                                IOperation:  (OperationKind.None, Type: ?, IsInvalid) (Syntax: '[i == 0 ? n ...  : default]')
                                  Children(1):
                                      IConditionalOperation (OperationKind.Conditional, Type: ?, IsInvalid) (Syntax: 'i == 0 ? null : default')
                                        Condition:
                                          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid) (Syntax: 'i == 0')
                                            Left:
                                              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                                            Right:
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                        WhenTrue:
                                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            Operand:
                                              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                                        WhenFalse:
                                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsInvalid, IsImplicit) (Syntax: 'default')
                                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            Operand:
                                              IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, IsInvalid) (Syntax: 'default')
                      Initializer:
                        null
                """);
        }

        [Fact]
        public void TargetType_01()
        {
            string source = """
                class Program
                {
                    static void F(bool b, object o)
                    {
                        int[] a1 = b ? [1] : [];
                        int[] a2 = b? [] : [2];
                        object[] a3 = b ? [3] : [o];
                        object[] a4 = b ? [o] : [4];
                        int?[] a5 = b ? [5] : [null];
                        int?[] a6 = b ? [null] : [6];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void TargetType_02()
        {
            string source = """
                using System;
                class Program
                {
                    static void F(bool b, object o)
                    {
                        Func<int[]> f1 = () => { if (b) return [1]; return []; };
                        Func<int[]> f2 = () => { if (b) return []; return [2]; };
                        Func<object[]> f3 = () => { if (b) return [3]; return [o]; };
                        Func<object[]> f4 = () => { if (b) return [o]; return [4]; };
                        Func<int?[]> f5 = () => { if (b) return [5]; return [null]; };
                        Func<int?[]> f6 = () => { if (b) return [null]; return [6]; };
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        // Overload resolution should choose array over interface.
        [Fact]
        public void OverloadResolution_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IEnumerable<int> F1(IEnumerable<int> arg) => arg;
                    static int[] F1(int[] arg) => arg;
                    static int[] F2(int[] arg) => arg;
                    static IEnumerable<int> F2(IEnumerable<int> arg) => arg;
                    static void Main()
                    {
                        var x = F1([]);
                        var y = F2([1, 2]);
                        x.Report(includeType: true);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [], (System.Int32[]) [1, 2], ");
        }

        // Overload resolution should choose collection initializer type over interface.
        [Fact]
        public void OverloadResolution_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IEnumerable<int> F1(IEnumerable<int> arg) => arg;
                    static List<int> F1(List<int> arg) => arg;
                    static List<int> F2(List<int> arg) => arg;
                    static IEnumerable<int> F2(IEnumerable<int> arg) => arg;
                    static void Main()
                    {
                        var x = F1([]);
                        var y = F2([1, 2]);
                        x.Report(includeType: true);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Collections.Generic.List<System.Int32>) [], (System.Collections.Generic.List<System.Int32>) [1, 2], ");
        }

        [Fact]
        public void OverloadResolution_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int> F(List<int> arg) => arg;
                    static int[] F(int[] arg) => arg;
                    static void Main()
                    {
                        var x = F([]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(List<int>)' and 'Program.F(int[])'
                //         var x = F([]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Collections.Generic.List<int>)", "Program.F(int[])").WithLocation(8, 17));
        }

        [Fact]
        public void OverloadResolution_04()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int> F1(List<int> arg) => arg;
                    static int[] F1(int[] arg) => arg;
                    static int[] F2(int[] arg) => arg;
                    static List<int> F2(List<int> arg) => arg;
                    static void Main()
                    {
                        var x = F1([1]);
                        var y = F2([2]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(List<int>)' and 'Program.F1(int[])'
                //         var x = F1([1]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(System.Collections.Generic.List<int>)", "Program.F1(int[])").WithLocation(10, 17),
                // (11,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(int[])' and 'Program.F2(List<int>)'
                //         var y = F2([2]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(int[])", "Program.F2(System.Collections.Generic.List<int>)").WithLocation(11, 17));
        }

        [Fact]
        public void OverloadResolution_05()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int> F1(List<int> arg) => arg;
                    static List<long?> F1(List<long?> arg) => arg;
                    static List<long?> F2(List<long?> arg) => arg;
                    static List<int> F2(List<int> arg) => arg;
                    static void Main()
                    {
                        var x = F1([1]);
                        var y = F2([2]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(List<int>)' and 'Program.F1(List<long?>)'
                //         var x = F1([1]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(System.Collections.Generic.List<int>)", "Program.F1(System.Collections.Generic.List<long?>)").WithLocation(10, 17),
                // (11,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(List<long?>)' and 'Program.F2(List<int>)'
                //         var y = F2([2]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(System.Collections.Generic.List<long?>)", "Program.F2(System.Collections.Generic.List<int>)").WithLocation(11, 17));
        }

        [Fact]
        public void OverloadResolution_06()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int?> F1(List<int?> arg) => arg;
                    static List<long> F1(List<long> arg) => arg;
                    static List<long> F2(List<long> arg) => arg;
                    static List<int?> F2(List<int?> arg) => arg;
                    static void Main()
                    {
                        var x = F1([1]);
                        var y = F2([2]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(List<int?>)' and 'Program.F1(List<long>)'
                //         var x = F1([1]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(System.Collections.Generic.List<int?>)", "Program.F1(System.Collections.Generic.List<long>)").WithLocation(10, 17),
                // 0.cs(11,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(List<long>)' and 'Program.F2(List<int?>)'
                //         var y = F2([2]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(System.Collections.Generic.List<long>)", "Program.F2(System.Collections.Generic.List<int?>)").WithLocation(11, 17));
        }

        [Fact]
        public void OverloadResolution_07()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                struct S : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public void Add(int i) { }
                }
                class Program
                {
                    static S F1(S arg) => arg;
                    static List<int> F1(List<int> arg) => arg;
                    static List<int> F2(List<int> arg) => arg;
                    static S F2(S arg) => arg;
                    static void Main()
                    {
                        var x = F1([1]);
                        var y = F2([2]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (16,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(S)' and 'Program.F1(List<int>)'
                //         var x = F1([1]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(S)", "Program.F1(System.Collections.Generic.List<int>)").WithLocation(16, 17),
                // (17,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(List<int>)' and 'Program.F2(S)'
                //         var y = F2([2]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(System.Collections.Generic.List<int>)", "Program.F2(S)").WithLocation(17, 17));
        }

        [Fact]
        public void OverloadResolution_08()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IEnumerable<T> F1<T>(IEnumerable<T> arg) => arg;
                    static T[] F1<T>(T[] arg) => arg;
                    static T[] F2<T>(T[] arg) => arg;
                    static IEnumerable<T> F2<T>(IEnumerable<T> arg) => arg;
                    static void Main()
                    {
                        var x = F1([1]);
                        var y = F2([2]);
                        x.Report(includeType: true);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [1], (System.Int32[]) [2], ");
        }

        [Fact]
        public void OverloadResolution_09()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static int[] F1(int[] arg) => arg;
                    static string[] F1(string[] arg) => arg;
                    static List<int> F2(List<int> arg) => arg;
                    static List<string> F2(List<string> arg) => arg;
                    static string[] F3(string[] arg) => arg;
                    static List<int?> F3(List<int?> arg) => arg;
                    static void Main()
                    {
                        F1([]);
                        F2([]);
                        F3([null]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[])' and 'Program.F1(string[])'
                //         F1([]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(int[])", "Program.F1(string[])").WithLocation(12, 9),
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(List<int>)' and 'Program.F2(List<string>)'
                //         F2([]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(System.Collections.Generic.List<int>)", "Program.F2(System.Collections.Generic.List<string>)").WithLocation(13, 9),
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F3(string[])' and 'Program.F3(List<int?>)'
                //         F3([null]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F3").WithArguments("Program.F3(string[])", "Program.F3(System.Collections.Generic.List<int?>)").WithLocation(14, 9));
        }

        [Fact]
        public void OverloadResolution_ArgumentErrors()
        {
            string source = """
                using System.Linq;
                class Program
                {
                    static void Main()
                    {
                        [Unknown2].Zip([Unknown1]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,10): error CS0103: The name 'Unknown2' does not exist in the current context
                //         [Unknown2].Zip([Unknown1]);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown2").WithArguments("Unknown2").WithLocation(6, 10),
                // (6,25): error CS0103: The name 'Unknown1' does not exist in the current context
                //         [Unknown2].Zip([Unknown1]);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown1").WithArguments("Unknown1").WithLocation(6, 25));
        }

        [Fact]
        public void BestCommonType_01()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var x = new[] { new int[0], [1, 2, 3] };
                        x.Report(includeType: true);
                        var y = new[] { new[] { new int[0] }, [[1, 2, 3]] };
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[][]) [[], [1, 2, 3]], (System.Int32[][][]) [[[]], [[1, 2, 3]]], ");
        }

        [Fact]
        public void BestCommonType_02()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var x = new[] { new byte[0], [1, 2, 3] };
                        x.Report(includeType: true);
                        var y = new[] { new[] { new byte[0] }, [[1, 2, 3]] };
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Byte[][]) [[], [1, 2, 3]], (System.Byte[][][]) [[[]], [[1, 2, 3]]], ");
        }

        [Fact]
        public void BestCommonType_03()
        {
            string source = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = new[] { [""], new object[0] };
                        var y = new[] { [[""]], [new object[0]] };
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0826: No best type found for implicitly-typed array
                //         var y = new[] { [[""]], [new object[0]] };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, @"new[] { [[""""]], [new object[0]] }").WithLocation(6, 17));
        }

        [Fact]
        public void BestCommonType_04()
        {
            string source = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = args.Length > 0 ? new int[0] : [1, 2, 3];
                        x.Report(includeType: true);
                        var y = args.Length == 0 ? [[4, 5]] : new[] { new byte[0] };
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [1, 2, 3], (System.Byte[][]) [[4, 5]], ");
        }

        [Fact]
        public void BestCommonType_05()
        {
            string source = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        bool b = args.Length > 0;
                        var y = b ? [new int[0]] : [[1, 2, 3]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'collection expressions' and 'collection expressions'
                //         var y = b ? [new int[0]] : [[1, 2, 3]];
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? [new int[0]] : [[1, 2, 3]]").WithArguments("collection expressions", "collection expressions").WithLocation(6, 17));
        }

        [Fact]
        public void TypeInference_01()
        {
            string source = """
                static class Program
                {
                    static T F<T>(T a, T b)
                    {
                        return b;
                    }
                    static void Main()
                    {
                        var x = F(["str"]);
                        var y = F([[], [1, 2]]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS7036: There is no argument given that corresponds to the required parameter 'b' of 'Program.F<T>(T, T)'
                //         var x = F(["str"]);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F").WithArguments("b", "Program.F<T>(T, T)").WithLocation(9, 17),
                // (10,17): error CS7036: There is no argument given that corresponds to the required parameter 'b' of 'Program.F<T>(T, T)'
                //         var y = F([[], [1, 2]]);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F").WithArguments("b", "Program.F<T>(T, T)").WithLocation(10, 17));
        }

        [Fact]
        public void TypeInference_02()
        {
            string source = """
                static class Program
                {
                    static T F<T>(T a, T b)
                    {
                        return b;
                    }
                    static void Main()
                    {
                        _ = F([new int[0]], new[] { [1, 2, 3] });
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,29): error CS0826: No best type found for implicitly-typed array
                //         _ = F([new int[0]], new[] { [1, 2, 3] });
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { [1, 2, 3] }").WithLocation(9, 29));
        }

        [Fact]
        public void TypeInference_03()
        {
            string source = """
                class Program
                {
                    static T[] AsArray1<T>(T[] args) => args;
                    static T[] AsArray2<T>(params T[] args) => args;
                    static void Main()
                    {
                        var a = AsArray1([1, 2, 3]);
                        a.Report();
                        var b = AsArray2(["4", null]);
                        b.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2, 3], [4, null], ");
        }

        [Fact]
        public void TypeInference_04()
        {
            string source = """
                class Program
                {
                    static T[] AsArray<T>(T[] args)
                    {
                        return args;
                    }
                    static void Main()
                    {
                        AsArray([]);
                        AsArray([1, null]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'Program.AsArray<T>(T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         AsArray([]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "AsArray").WithArguments("Program.AsArray<T>(T[])").WithLocation(9, 9),
                // (10,21): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         AsArray([1, null]);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(10, 21));
        }

        [Fact]
        public void TypeInference_06()
        {
            string source = """
                class Program
                {
                    static T[] AsArray<T>(T[] args)
                    {
                        return args;
                    }
                    static void F(bool b, int x, int y)
                    {
                        var a = AsArray([.. b ? [x] : [y]]);
                        a.Report();
                    }
                    static void Main()
                    {
                        F(false, 1, 2);
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, s_collectionExtensions });
            comp.VerifyEmitDiagnostics(
                // 0.cs(9,29): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'collection expressions' and 'collection expressions'
                //         var a = AsArray([.. b ? [x] : [y]]);
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? [x] : [y]").WithArguments("collection expressions", "collection expressions").WithLocation(9, 29));
        }

        [Fact]
        public void TypeInference_07()
        {
            string source = """
                static class Program
                {
                    static T[] AsArray<T>(this T[] args)
                    {
                        return args;
                    }
                    static void Main()
                    {
                        var a = [1, 2, 3].AsArray();
                        a.Report();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, s_collectionExtensions });
            comp.VerifyEmitDiagnostics(
                // 0.cs(9,17): error CS9176: There is no target type for the collection expression.
                //         var a = [1, 2, 3].AsArray();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(9, 17));
        }

        [Fact]
        public void TypeInference_08()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                struct S<T> : IEnumerable<T>
                {
                    private List<T> _list;
                    public void Add(T t)
                    {
                        _list ??= new List<T>();
                        _list.Add(t);
                    }
                    public IEnumerator<T> GetEnumerator()
                    {
                        _list ??= new List<T>();
                        return _list.GetEnumerator();
                    }
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                static class Program
                {
                    static S<T> AsCollection<T>(this S<T> args)
                    {
                        return args;
                    }
                    static void Main()
                    {
                        var a = AsCollection([1, 2, 3]);
                        var b = [4].AsCollection();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (27,17): error CS9176: There is no target type for the collection expression.
                //         var b = [4].AsCollection();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[4]").WithLocation(27, 17));
        }

        [Fact]
        public void TypeInference_09()
        {
            string source = """
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                static class Program
                {
                    static S<T> AsCollection<T>(this S<T> args)
                    {
                        return args;
                    }
                    static void Main()
                    {
                        _ = AsCollection([1, 2, 3]);
                        _ = [4].AsCollection();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (15,13): error CS0411: The type arguments for method 'Program.AsCollection<T>(S<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         _ = AsCollection([1, 2, 3]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "AsCollection").WithArguments("Program.AsCollection<T>(S<T>)").WithLocation(15, 13),
                // (16,13): error CS9176: There is no target type for the collection expression.
                //         _ = [4].AsCollection();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[4]").WithLocation(16, 13));
        }

        [Fact]
        public void TypeInference_10()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static T[] F<T>(T[] arg) => arg;
                    static List<T> F<T>(List<T> arg) => arg;
                    static void Main()
                    {
                        _ = F([1, 2, 3]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,13): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F<T>(T[])' and 'Program.F<T>(List<T>)'
                //         _ = F([1, 2, 3]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F<T>(T[])", "Program.F<T>(System.Collections.Generic.List<T>)").WithLocation(8, 13));
        }

        [Fact]
        public void TypeInference_11()
        {
            string source = """
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static T[] F<T>(T[] arg) => arg;
                    static S<T> F<T>(S<T> arg) => arg;
                    static void Main()
                    {
                        var x = F([1, 2, 3]);
                        x.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [1, 2, 3], ");
        }

        [Fact]
        public void TypeInference_12()
        {
            string source = """
                class Program
                {
                    static T[] F<T>(T[] x, T[] y) => x;
                    static void Main()
                    {
                        var x = F(["1"], [(object)"2"]);
                        x.Report(includeType: true);
                        var y = F([(object)"3"], ["4"]);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Object[]) [1], (System.Object[]) [3], ");
        }

        [Fact]
        public void TypeInference_13A()
        {
            string source = """
                class Program
                {
                    static T[] F<T>(T[] x, T[] y) => x;
                    static void Main()
                    {
                        var x = F([1], [(long)2]);
                        x.Report(includeType: true);
                        var y = F([(long)3], [4]);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int64[]) [1], (System.Int64[]) [3], ");
        }

        [Fact]
        public void TypeInference_13B()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static HashSet<T> F<T>(HashSet<T> x, HashSet<T> y) => x;
                    static void Main()
                    {
                        var x = F([1], [(long)2]);
                        x.Report(includeType: true);
                        var y = F([(long)3], [4]);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Collections.Generic.HashSet<System.Int64>) [1], (System.Collections.Generic.HashSet<System.Int64>) [3], ");
        }

        [Fact]
        public void TypeInference_14()
        {
            string source = """
                class Program
                {
                    static T[] F<T>(T[][] x) => x[0];
                    static void Main()
                    {
                        var x = F([[1, 2, 3]]);
                        x.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [1, 2, 3], ");
        }

        [Fact]
        public void TypeInference_15()
        {
            string source = """
                class Program
                {
                    static T F0<T>(T[] x, T y) => y;
                    static T[] F1<T>(T[] x, T[] y) => y;
                    static T[] F2<T>(T[][] x, T[][] y) => y[0];
                    static void Main()
                    {
                        var x = F0(new byte[0], 1);
                        var y = F1(new byte[0], [1, 2]);
                        var z = F2(new[] { new byte[0] }, [[3, 4]]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS0411: The type arguments for method 'Program.F0<T>(T[], T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x = F0(new byte[0], 1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F0").WithArguments("Program.F0<T>(T[], T)").WithLocation(8, 17),
                // (9,17): error CS0411: The type arguments for method 'Program.F1<T>(T[], T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F1(new byte[0], [1, 2]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(T[], T[])").WithLocation(9, 17),
                // (10,17): error CS0411: The type arguments for method 'Program.F2<T>(T[][], T[][])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var z = F2(new[] { new byte[0] }, [[3, 4]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T[][], T[][])").WithLocation(10, 17));
        }

        [Fact]
        public void TypeInference_16()
        {
            string source = """
                class Program
                {
                    static T[] F1<T>(T[] x, T[] y) => y;
                    static T[] F2<T>(T[][] x, T[][] y) => y[0];
                    static void Main()
                    {
                        var x = F1([1], [(byte)2]);
                        x.Report(true);
                        var y = F2([[3]], [[(byte)4]]);
                        y.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [2], (System.Int32[]) [4], ");
        }

        [Fact]
        public void TypeInference_17()
        {
            string source = """
                class Program
                {
                    static T[] F1<T>(T[] x, T[] y) => y;
                    static T[] F2<T>(T[][] x, T[][] y) => y[0];
                    static void Main()
                    {
                        var x = F1([(long)1], [(int?)2]);
                        var y = F2([[(int?)3]], [[(long)4]]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,17): error CS0411: The type arguments for method 'Program.F1<T>(T[], T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x = F1([(long)1], [(int?)2]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(T[], T[])").WithLocation(7, 17),
                // (8,17): error CS0411: The type arguments for method 'Program.F2<T>(T[][], T[][])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F2([[(int?)3]], [[(long)4]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T[][], T[][])").WithLocation(8, 17));
        }

        [Fact]
        public void TypeInference_18()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<T[]> AsListOfArray<T>(List<T[]> arg) => arg;
                    static void Main()
                    {
                        var x = AsListOfArray([[4, 5], []]);
                        x.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Collections.Generic.List<System.Int32[]>) [[4, 5], []], ");
        }

        [Fact]
        public void TypeInference_19()
        {
            string source = """
                class Program
                {
                    static T[] F<T>(T[][] x) => x[1];
                    static void Main()
                    {
                        var y = F([new byte[0], [1, 2, 3]]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0411: The type arguments for method 'Program.F<T>(T[][])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F([new byte[0], [1, 2, 3]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T[][])").WithLocation(6, 17));
        }

        [Fact]
        public void TypeInference_20()
        {
            string source = """
                class Program
                {
                    static T[] F<T>(in T[] x, T[] y) => x;
                    static void Main()
                    {
                        var x = F([1], [2]);
                        x.Report(true);
                        var y = F([3], [(object)4]);
                        y.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [1], (System.Object[]) [3], ");
        }

        [Fact]
        public void TypeInference_21()
        {
            string source = """
                class Program
                {
                    static T[] F<T>(in T[] x, T[] y) => x;
                    static void Main()
                    {
                        var y = F(in [3], [(object)4]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         var y = F(in [3], [(object)4]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "[3]").WithLocation(6, 22));
        }

        [Fact]
        public void TypeInference_22()
        {
            string source = """
                class Program
                {
                    static T[] F1<T>(T[] x, T[] y) => y;
                    static T[] F2<T>(T[][] x, T[][] y) => y[0];
                    static void Main()
                    {
                        var x = F1([], [default, 2]);
                        x.Report(true);
                        var y = F2([[null]], [[default, (int?)4]]);
                        y.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [0, 2], (System.Nullable<System.Int32>[]) [null, 4], ");
        }

        [Fact]
        public void TypeInference_23()
        {
            string source = """
                class Program
                {
                    static T[] F1<T>(T[] x, T[] y) => y;
                    static T[] F2<T>(T[][] x, T[][] y) => y[0];
                    static void Main()
                    {
                        var x = F1([], [default]);
                        var y = F2([[null]], [[default]]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,17): error CS0411: The type arguments for method 'Program.F1<T>(T[], T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x = F1([], [default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(T[], T[])").WithLocation(7, 17),
                // (8,17): error CS0411: The type arguments for method 'Program.F2<T>(T[][], T[][])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F2([[null]], [[default]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T[][], T[][])").WithLocation(8, 17));
        }

        [Fact]
        public void TypeInference_24()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static ReadOnlySpan<T> F1<T>(Span<T> x, ReadOnlySpan<T> y) => y;
                    static List<T> F2<T>(Span<T[]> x, ReadOnlySpan<List<T>> y) => y[0];
                    static void Main()
                    {
                        var x = F1([], [default, 2]);
                        x.Report();
                        var y = F2([[null]], [[default, (int?)4]]);
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[0, 2], [null, 4], "));
        }

        [Fact]
        public void TypeInference_25()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static ReadOnlySpan<T> F1<T>(Span<T> x, ReadOnlySpan<T> y) => y;
                    static List<T> F2<T>(Span<T[]> x, ReadOnlySpan<List<T>> y) => y[0];
                    static void Main()
                    {
                        var x = F1([], [default]);
                        var y = F2([[null]], [[default]]);
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS0411: The type arguments for method 'Program.F1<T>(Span<T>, ReadOnlySpan<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x = F1([], [default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Span<T>, System.ReadOnlySpan<T>)").WithLocation(9, 17),
                // (10,17): error CS0411: The type arguments for method 'Program.F2<T>(Span<T[]>, ReadOnlySpan<List<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F2([[null]], [[default]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Span<T[]>, System.ReadOnlySpan<System.Collections.Generic.List<T>>)").WithLocation(10, 17));
        }

        [Fact]
        public void TypeInference_26()
        {
            string source = """
                class Program
                {
                    static void F<T>(T x) { }
                    static void Main()
                    {
                        F([]);
                        F([null, default, 0]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(6, 9),
                // (7,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([null, default, 0]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(7, 9));
        }

        [Fact]
        public void TypeInference_27()
        {
            string source = """
                class Program
                {
                    static void F<T>(T[,] x) { }
                    static void Main()
                    {
                        F([]);
                        F([null, default, 0]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'Program.F<T>(T[*,*])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T[*,*])").WithLocation(6, 9),
                // (7,11): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'int[*,*]'
                //         F([null, default, 0]);
                Diagnostic(ErrorCode.ERR_BadArgType, "[null, default, 0]").WithArguments("1", "collection expressions", "int[*,*]").WithLocation(7, 11));
        }

        [Fact]
        public void TypeInference_28()
        {
            string source = """
                class Program
                {
                    static void F<T>(string x, T[] y) { }
                    static void Main()
                    {
                        F([], ['B']);
                        F([default], ['B']);
                        F(['A'], ['B']);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,11): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F([], ['B']);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("string", "0").WithLocation(6, 11),
                // (7,11): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F([default], ['B']);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[default]").WithArguments("string", "0").WithLocation(7, 11),
                // (7,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F([default], ['B']);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "default").WithArguments("string", "Add").WithLocation(7, 12),
                // (8,11): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F(['A'], ['B']);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "['A']").WithArguments("string", "0").WithLocation(8, 11),
                // (8,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F(['A'], ['B']);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'A'").WithArguments("string", "Add").WithLocation(8, 12));
        }

        [Fact]
        public void TypeInference_29()
        {
            string source = """
                delegate void D();
                enum E { }
                class Program
                {
                    static void F1<T>(dynamic x, T[] y) { }
                    static void F2<T>(D x, T[] y) { }
                    static void F3<T>(E x, T[] y) { }
                    static void Main()
                    {
                        F1([1], [2]);
                        F2([3], [4]);
                        F3([5], [6]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,12): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'dynamic'
                //         F1([1], [2]);
                Diagnostic(ErrorCode.ERR_BadArgType, "[1]").WithArguments("1", "collection expressions", "dynamic").WithLocation(10, 12),
                // (11,12): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'D'
                //         F2([3], [4]);
                Diagnostic(ErrorCode.ERR_BadArgType, "[3]").WithArguments("1", "collection expressions", "D").WithLocation(11, 12),
                // (12,12): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'E'
                //         F3([5], [6]);
                Diagnostic(ErrorCode.ERR_BadArgType, "[5]").WithArguments("1", "collection expressions", "E").WithLocation(12, 12));
        }

        [Fact]
        public void TypeInference_30()
        {
            string source = """
                delegate void D();
                enum E { }
                class Program
                {
                    static void F1<T>(dynamic[] x, T[] y) { }
                    static void F2<T>(D[] x, T[] y) { }
                    static void F3<T>(E[] x, T[] y) { }
                    static void Main()
                    {
                        F1([1], [2]);
                        F2([null], [4]);
                        F3([default], [6]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void TypeInference_31()
        {
            string source = """
                class Program
                {
                    static void F<T>(T[] x) { }
                    static void Main()
                    {
                        F([null]);
                        F([Unknown]);
                        F([Main()]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'Program.F<T>(T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([null]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T[])").WithLocation(6, 9),
                // (7,12): error CS0103: The name 'Unknown' does not exist in the current context
                //         F([Unknown]);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown").WithArguments("Unknown").WithLocation(7, 12),
                // (8,9): error CS0411: The type arguments for method 'Program.F<T>(T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([Main()]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T[])").WithLocation(8, 9));
        }

        [Fact]
        public void TypeInference_32()
        {
            string source = """
                delegate void D();
                class Program
                {
                    static T[] F<T>(T[] x) => x;
                    static void Main()
                    {
                        var x = F([null, Main]);
                        x.Report(includeType: true);
                        var y = F([Main, (D)Main]);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Action[]) [null, System.Action], (D[]) [D, D], ");
        }

        [Fact]
        public void TypeInference_33()
        {
            string source = """
                delegate byte D();
                class Program
                {
                    static T[] F<T>(T[] x) => x;
                    static void Main()
                    {
                        var x = F([null, () => 1]);
                        x.Report(includeType: true);
                        var y = F([() => 2, (D)(() => 3)]);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Func<System.Int32>[]) [null, System.Func`1[System.Int32]], (D[]) [D, D], ");
        }

        [Fact]
        public void TypeInference_34()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static List<Func<T>> F1<T>(List<Func<T>> x) => x;
                    static string F2() => null;
                    static void Main()
                    {
                        var x = F1([F2]);
                        x.Report();
                        var y = F1([null, () => 1]);
                        y.Report();
                        var z = F1([F2, () => default]);
                        z.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensions },
                expectedOutput: "[System.Func`1[System.String]], [null, System.Func`1[System.Int32]], [System.Func`1[System.String], System.Func`1[System.String]], ");
        }

        [Fact]
        public void TypeInference_35()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static List<Action<T>> F1<T>(List<Action<T>> x) => x;
                    static void F2(string s) { }
                    static void Main()
                    {
                        var x = F1([F2, (string s) => { }]);
                        x.Report();
                        var y = F1([null, (int a) => { }]);
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensions },
                expectedOutput: "[System.Action`1[System.String], System.Action`1[System.String]], [null, System.Action`1[System.Int32]], ");
        }

        [Fact]
        public void TypeInference_36()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static List<Func<T>> F1<T>(List<Func<T>> x) => x;
                    static string F2() => null;
                    static void Main()
                    {
                        var x = F1([() => default]);
                        var y = F1([() => 2, F2]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS0411: The type arguments for method 'Program.F1<T>(List<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x = F1([() => default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Collections.Generic.List<System.Func<T>>)").WithLocation(9, 17),
                // (10,17): error CS0411: The type arguments for method 'Program.F1<T>(List<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F1([null, () => 1]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Collections.Generic.List<System.Func<T>>)").WithLocation(10, 17));
        }

        [Fact]
        public void TypeInference_37()
        {
            string source = """
                class Program
                {
                    static (T, U)[] F<T, U>((T, U)[] x) => x;
                    static void Main()
                    {
                        var x = F([(1, "2")]);
                        x.Report(includeType: true);
                        var y = F([default, (3, (byte)4)]);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensions },
                expectedOutput: "(System.ValueTuple<System.Int32, System.String>[]) [(1, 2)], (System.ValueTuple<System.Int32, System.Byte>[]) [(0, 0), (3, 4)], ");
        }

        [Fact]
        public void TypeInference_38()
        {
            string source = """
                class Program
                {
                    static T[] F1<T>(T[] x, T y) => x;
                    static T[] F2<T>(T[] x, ref T y) => x;
                    static T[] F3<T>(T[] x, in T y) => x;
                    static T[] F4<T>(T[] x, out T y) { y = default; return x; }
                    static void Main()
                    {
                        object y = null;
                        var x1 = F1([1], y);
                        var x2 = F2([2], ref y);
                        var x3A = F3([3], y);
                        var x3B = F3([3], in y);
                        var x4 = F4([4], out y);
                        x1.Report(true);
                        x2.Report(true);
                        x3A.Report(true);
                        x3B.Report(true);
                        x4.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Object[]) [1], (System.Object[]) [2], (System.Object[]) [3], (System.Object[]) [3], (System.Object[]) [4], ");
        }

        [Fact]
        public void TypeInference_39A()
        {
            string source = """
                class Program
                {
                    static T[] F1<T>(T[] x, T y) => x;
                    static T[] F3<T>(T[] x, in T y) => x;
                    static void Main()
                    {
                        byte y = 0;
                        var x1 = F1([1], y);
                        var x3A = F3([3], y);
                        x1.Report(true);
                        x3A.Report(true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "(System.Int32[]) [1], (System.Int32[]) [3], ");
        }

        [Fact]
        public void TypeInference_39B()
        {
            string source = """
                class Program
                {
                    static T[] F2<T>(T[] x, ref T y) => x;
                    static T[] F3<T>(T[] x, in T y) => x;
                    static T[] F4<T>(T[] x, out T y) { y = default; return x; }
                    static void Main()
                    {
                        byte y = 0;
                        var x2 = F2([2], ref y);
                        var x3B = F3([3], in y);
                        var x4 = F4([4], out y);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,18): error CS0411: The type arguments for method 'Program.F2<T>(T[], ref T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x2 = F2([2], ref y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T[], ref T)").WithLocation(9, 18),
                // (10,19): error CS0411: The type arguments for method 'Program.F3<T>(T[], in T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x3B = F3([3], in y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F3").WithArguments("Program.F3<T>(T[], in T)").WithLocation(10, 19),
                // (11,18): error CS0411: The type arguments for method 'Program.F4<T>(T[], out T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x4 = F4([4], out y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F4").WithArguments("Program.F4<T>(T[], out T)").WithLocation(11, 18));
        }

        [Fact]
        public void TypeInference_40()
        {
            string source = """
                using System;
                class Program
                {
                    static Func<T[]> F<T>(Func<T[]> arg) => arg;
                    static void Main(string[] args)
                    {
                        var x = F(() => [1, 2, 3]);
                        x.Report(includeType: true);
                        var y = F(() => { if (args.Length == 0) return []; return [1, 2, 3]; });
                        y.Report(includeType: true);
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, s_collectionExtensions });
            comp.VerifyEmitDiagnostics(
                // 0.cs(7,17): error CS0411: The type arguments for method 'Program.F<T>(Func<T[]>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var x = F(() => [1, 2, 3]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<T[]>)").WithLocation(7, 17),
                // 0.cs(9,17): error CS0411: The type arguments for method 'Program.F<T>(Func<T[]>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var y = F(() => { if (args.Length == 0) return []; return [1, 2, 3]; });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<T[]>)").WithLocation(9, 17));
        }

        [Fact]
        public void MemberAccess_01()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        [].GetHashCode();
                        []?.GetHashCode();
                        [][0].GetHashCode();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS9176: There is no target type for the collection expression.
                //         [].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(5, 9),
                // (6,9): error CS9176: There is no target type for the collection expression.
                //         []?.GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(6, 9),
                // (7,9): error CS9176: There is no target type for the collection expression.
                //         [][0].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 9));
        }

        [Fact]
        public void MemberAccess_02()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        [1].GetHashCode();
                        [2]?.GetHashCode();
                        [3][0].GetHashCode();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS9176: There is no target type for the collection expression.
                //         [1].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1]").WithLocation(5, 9),
                // (6,9): error CS9176: There is no target type for the collection expression.
                //         [2]?.GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[2]").WithLocation(6, 9),
                // (7,9): error CS9176: There is no target type for the collection expression.
                //         [3][0].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[3]").WithLocation(7, 9));
        }

        [Fact]
        public void MemberAccess_03()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        _ = [].GetHashCode();
                        _ = []?.GetHashCode();
                        _ = [][0].GetHashCode();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,13): error CS9176: There is no target type for the collection expression.
                //         _ = [].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(5, 13),
                // (6,13): error CS9176: There is no target type for the collection expression.
                //         _ = []?.GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(6, 13),
                // (7,13): error CS9176: There is no target type for the collection expression.
                //         _ = [][0].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 13));
        }

        [Fact]
        public void MemberAccess_04()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        _ = [1].GetHashCode();
                        _ = [2]?.GetHashCode();
                        _ = [3][0].GetHashCode();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,13): error CS9176: There is no target type for the collection expression.
                //         _ = [1].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1]").WithLocation(5, 13),
                // (6,13): error CS9176: There is no target type for the collection expression.
                //         _ = [2]?.GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[2]").WithLocation(6, 13),
                // (7,13): error CS9176: There is no target type for the collection expression.
                //         _ = [3][0].GetHashCode();
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[3]").WithLocation(7, 13));
        }

        [Fact]
        public void ListBase()
        {
            string sourceA = """
                using System.Collections;
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public class ListBase<T> : IEnumerable
                    {
                        public void Add(string s) { }
                    }
                    public class List<T> : ListBase<T>
                    {
                        public void Add(T t) { }
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        ListBase<int> x = [];
                        ListBase<int> y = [1, 2];
                        ListBase<string> z = ["a", "b"];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(7,28): error CS1950: The best overloaded Add method 'ListBase<int>.Add(string)' for the collection initializer has some invalid arguments
                //         ListBase<int> y = [1, 2];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "1").WithArguments("System.Collections.Generic.ListBase<int>.Add(string)").WithLocation(7, 28),
                // 1.cs(7,28): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         ListBase<int> y = [1, 2];
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(7, 28),
                // 1.cs(7,31): error CS1950: The best overloaded Add method 'ListBase<int>.Add(string)' for the collection initializer has some invalid arguments
                //         ListBase<int> y = [1, 2];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "2").WithArguments("System.Collections.Generic.ListBase<int>.Add(string)").WithLocation(7, 31),
                // 1.cs(7,31): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         ListBase<int> y = [1, 2];
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("1", "int", "string").WithLocation(7, 31));
        }

        [Fact]
        public void ListInterfaces_01()
        {
            string sourceA = """
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public interface IA { }
                    public interface IB<T> { }
                    public interface IC<T> { }
                    public interface ID<T1, T2> { }
                    public class List<T> : IEnumerable, IA, IB<T>, IC<object>, ID<T, object>
                    {
                        public void Add(T t) { }
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<int> l = [1];
                        IA a = [2];
                        IB<object> b = [3];
                        IC<object> c = [4];
                        ID<object, object> d = [5];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(7,16): error CS9174: Cannot initialize type 'IA' with a collection expression because the type is not constructible.
                //         IA a = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("System.Collections.Generic.IA").WithLocation(7, 16),
                // 1.cs(9,24): error CS9174: Cannot initialize type 'IC<object>' with a collection expression because the type is not constructible.
                //         IC<object> c = [4];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[4]").WithArguments("System.Collections.Generic.IC<object>").WithLocation(9, 24),
                // 1.cs(10,32): error CS9174: Cannot initialize type 'ID<object, object>' with a collection expression because the type is not constructible.
                //         ID<object, object> d = [5];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[5]").WithArguments("System.Collections.Generic.ID<object, object>").WithLocation(10, 32));
        }

        [Fact]
        public void ListInterfaces_02()
        {
            string sourceA = """
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public interface IEquatable<T>
                    {
                        bool Equals(T other);
                    }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public class List<T> : IEnumerable, IEquatable<List<T>>
                    {
                        public bool Equals(List<T> other) => false;
                        public void Add(T t) { }
                    }
                }
                """;
            string sourceB = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<int> l = [1];
                        IEquatable<int> e = [2];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(8,29): error CS9174: Cannot initialize type 'IEquatable<int>' with a collection expression because the type is not constructible.
                //         IEquatable<int> e = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("System.IEquatable<int>").WithLocation(8, 29));
        }

        [Fact]
        public void ListInterfaces_NoInterfaces()
        {
            string sourceA = """
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                }
                namespace System.Collections.Generic
                {
                    public interface IEnumerable<T> { }
                    public class List<T>
                    {
                        public void Add(T t) { }
                    }
                }
                """;
            string sourceB = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<int> l = [1];
                        IEnumerable<int> e = [2];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(7,23): error CS9174: Cannot initialize type 'List<int>' with a collection expression because the type is not constructible.
                //         List<int> l = [1];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1]").WithArguments("System.Collections.Generic.List<int>").WithLocation(7, 23),
                // 1.cs(8,30): error CS9174: Cannot initialize type 'IEnumerable<int>' with a collection expression because the type is not constructible.
                //         IEnumerable<int> e = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("System.Collections.Generic.IEnumerable<int>").WithLocation(8, 30));
        }

        [Fact]
        public void ListInterfaces_MissingList()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IEnumerable<int> a = [];
                        ICollection<int> b = [2];
                        IList<int> c = [];
                        IReadOnlyCollection<int> d = [3];
                        IReadOnlyList<int> e = [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_List_T);
            comp.VerifyEmitDiagnostics(
                // (6,30): error CS9174: Cannot initialize type 'IEnumerable<int>' with a collection expression because the type is not constructible.
                //         IEnumerable<int> a = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("System.Collections.Generic.IEnumerable<int>").WithLocation(6, 30),
                // (7,30): error CS9174: Cannot initialize type 'ICollection<int>' with a collection expression because the type is not constructible.
                //         ICollection<int> b = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("System.Collections.Generic.ICollection<int>").WithLocation(7, 30),
                // (8,24): error CS9174: Cannot initialize type 'IList<int>' with a collection expression because the type is not constructible.
                //         IList<int> c = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("System.Collections.Generic.IList<int>").WithLocation(8, 24),
                // (9,38): error CS9174: Cannot initialize type 'IReadOnlyCollection<int>' with a collection expression because the type is not constructible.
                //         IReadOnlyCollection<int> d = [3];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[3]").WithArguments("System.Collections.Generic.IReadOnlyCollection<int>").WithLocation(9, 38),
                // (10,32): error CS9174: Cannot initialize type 'IReadOnlyList<int>' with a collection expression because the type is not constructible.
                //         IReadOnlyList<int> e = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("System.Collections.Generic.IReadOnlyList<int>").WithLocation(10, 32));
        }

        [Fact]
        public void Array_01()
        {
            string source = """
                class Program
                {
                    static int[] Create1() => [];
                    static object[] Create2() => [1, 2];
                    static int[] Create3() => [3, 4, 5];
                    static long?[] Create4() => [null, 7];
                    static void Main()
                    {
                        Create1().Report();
                        Create2().Report();
                        Create3().Report();
                        Create4().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "int[] System.Array.Empty<int>()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", """
                {
                  // Code size       25 (0x19)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "object"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  box        "int"
                  IL_000e:  stelem.ref
                  IL_000f:  dup
                  IL_0010:  ldc.i4.1
                  IL_0011:  ldc.i4.2
                  IL_0012:  box        "int"
                  IL_0017:  stelem.ref
                  IL_0018:  ret
                }
                """);
            verifier.VerifyIL("Program.Create3", """
                {
                  // Code size       18 (0x12)
                  .maxstack  3
                  IL_0000:  ldc.i4.3
                  IL_0001:  newarr     "int"
                  IL_0006:  dup
                  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.CE99AE045C8B2A2A8A58FD1A2120956E74E90322EEF45F7DFE1CA73EEFE655D4"
                  IL_000c:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                  IL_0011:  ret
                }
                """);
            verifier.VerifyIL("Program.Create4", """
                {
                  // Code size       21 (0x15)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "long?"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.1
                  IL_0008:  ldc.i4.7
                  IL_0009:  conv.i8
                  IL_000a:  newobj     "long?..ctor(long)"
                  IL_000f:  stelem     "long?"
                  IL_0014:  ret
                }
                """);
        }

        [Fact]
        public void Array_02()
        {
            string source = """
                using System;
                class Program
                {
                    static int[][] Create1() => [];
                    static object[][] Create2() => [[]];
                    static object[][] Create3() => [[1], [2, 3]];
                    static void Main()
                    {
                        Report(Create1());
                        Report(Create2());
                        Report(Create3());
                    }
                    static void Report<T>(T[][] a)
                    {
                        Console.Write("Length={0}, ", a.Length);
                        foreach (var x in a)
                        {
                            Console.Write("Length={0}, ", x.Length);
                            foreach (var y in x)
                                Console.Write("{0}, ", y);
                        }
                        Console.WriteLine();
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                Length=0, 
                Length=1, Length=0, 
                Length=2, Length=1, 1, Length=2, 2, 3, 
                """);
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "int[][] System.Array.Empty<int[]>()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", """
                {
                  // Code size       15 (0xf)
                  .maxstack  4
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "object[]"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  call       "object[] System.Array.Empty<object>()"
                  IL_000d:  stelem.ref
                  IL_000e:  ret
                }
                """);
            verifier.VerifyIL("Program.Create3", """
                {
                  // Code size       52 (0x34)
                  .maxstack  7
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "object[]"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  newarr     "object"
                  IL_000e:  dup
                  IL_000f:  ldc.i4.0
                  IL_0010:  ldc.i4.1
                  IL_0011:  box        "int"
                  IL_0016:  stelem.ref
                  IL_0017:  stelem.ref
                  IL_0018:  dup
                  IL_0019:  ldc.i4.1
                  IL_001a:  ldc.i4.2
                  IL_001b:  newarr     "object"
                  IL_0020:  dup
                  IL_0021:  ldc.i4.0
                  IL_0022:  ldc.i4.2
                  IL_0023:  box        "int"
                  IL_0028:  stelem.ref
                  IL_0029:  dup
                  IL_002a:  ldc.i4.1
                  IL_002b:  ldc.i4.3
                  IL_002c:  box        "int"
                  IL_0031:  stelem.ref
                  IL_0032:  stelem.ref
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void Array_03()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object o;
                        o = (int[])[];
                        o.Report();
                        o = (long?[])[null, 2];
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [null, 2], ");
        }

        [Fact]
        public void Array_04()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[,] x = [];
                        int[,] y = [null, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,23): error CS9174: Cannot initialize type 'object[*,*]' with a collection expression because the type is not constructible.
                //         object[,] x = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("object[*,*]").WithLocation(5, 23),
                // (6,20): error CS9174: Cannot initialize type 'int[*,*]' with a collection expression because the type is not constructible.
                //         int[,] y = [null, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[null, 2]").WithArguments("int[*,*]").WithLocation(6, 20));
        }

        [Fact]
        public void Array_05()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        int[,] z = [[1, 2], [3, 4]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9174: Cannot initialize type 'int[*,*]' with a collection expression because the type is not constructible.
                //         int[,] z = [[1, 2], [3, 4]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[[1, 2], [3, 4]]").WithArguments("int[*,*]").WithLocation(5, 20));
        }

        [Theory]
        [CombinatorialData]
        public void Span_01(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static {{spanType}}<int> Create1() => [];
                    static {{spanType}}<object> Create2() => [1, 2];
                    static {{spanType}}<int> Create3() => [3, 4, 5];
                    static {{spanType}}<long?> Create4() => [null, 7];
                    static void Main()
                    {
                        Create1().Report();
                        Create2().Report();
                        Create3().Report();
                        Create4().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1, 2], [3, 4, 5], [null, 7], "));
            verifier.VerifyIL("Program.Create1", $$"""
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  call       "int[] System.Array.Empty<int>()"
                  IL_0005:  newobj     "System.{{spanType}}<int>..ctor(int[])"
                  IL_000a:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", $$"""
                {
                  // Code size       30 (0x1e)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "object"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  box        "int"
                  IL_000e:  stelem.ref
                  IL_000f:  dup
                  IL_0010:  ldc.i4.1
                  IL_0011:  ldc.i4.2
                  IL_0012:  box        "int"
                  IL_0017:  stelem.ref
                  IL_0018:  newobj     "System.{{spanType}}<object>..ctor(object[])"
                  IL_001d:  ret
                }
                """);
            if (useReadOnlySpan)
            {
                verifier.VerifyIL("Program.Create3", """
                    {
                      // Code size       11 (0xb)
                      .maxstack  1
                      IL_0000:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.CE99AE045C8B2A2A8A58FD1A2120956E74E90322EEF45F7DFE1CA73EEFE655D44"
                      IL_0005:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
                      IL_000a:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.Create3", """
                    {
                      // Code size       23 (0x17)
                      .maxstack  3
                      IL_0000:  ldc.i4.3
                      IL_0001:  newarr     "int"
                      IL_0006:  dup
                      IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.CE99AE045C8B2A2A8A58FD1A2120956E74E90322EEF45F7DFE1CA73EEFE655D4"
                      IL_000c:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                      IL_0011:  newobj     "System.Span<int>..ctor(int[])"
                      IL_0016:  ret
                    }
                    """);
            }
            verifier.VerifyIL("Program.Create4", $$"""
                {
                  // Code size       26 (0x1a)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "long?"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.1
                  IL_0008:  ldc.i4.7
                  IL_0009:  conv.i8
                  IL_000a:  newobj     "long?..ctor(long)"
                  IL_000f:  stelem     "long?"
                  IL_0014:  newobj     "System.{{spanType}}<long?>..ctor(long?[])"
                  IL_0019:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void Span_02(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static void Main()
                    {
                        {{spanType}}<string> x = [];
                        {{spanType}}<int> y = [1, 2, 3];
                        x.Report();
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [Theory]
        [CombinatorialData]
        public void Span_03(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static void Main()
                    {
                        var x = ({{spanType}}<string>)[];
                        var y = ({{spanType}}<int>)[1, 2, 3];
                        x.Report();
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [Theory]
        [CombinatorialData]
        public void Span_04(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static ref readonly {{spanType}}<int> F1()
                    {
                        return ref F2<int>([]);
                    }
                    static ref readonly {{spanType}}<T> F2<T>(in {{spanType}}<T> s)
                    {
                        return ref s;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (6,20): error CS8347: Cannot use a result of 'Program.F2<int>(in Span<int>)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         return ref F2<int>([]);
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2<int>([])").WithArguments($"Program.F2<int>(in System.{spanType}<int>)", "s").WithLocation(6, 20),
                // (6,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref F2<int>([]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "[]").WithLocation(6, 28));
        }

        [Theory]
        [CombinatorialData]
        public void Span_05(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static ref readonly {{spanType}}<int> F1()
                    {
                        return ref F2<int>([]);
                    }
                    static ref readonly {{spanType}}<T> F2<T>(scoped in {{spanType}}<T> s)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Span_MissingConstructor()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Span<string> x = [];
                        ReadOnlySpan<int> y = [1, 2, 3];
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //         Span<string> x = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Span`1", ".ctor").WithLocation(6, 26));

            comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);
            comp.VerifyEmitDiagnostics(
                // (7,31): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1..ctor'
                //         ReadOnlySpan<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[1, 2, 3]").WithArguments("System.ReadOnlySpan`1", ".ctor").WithLocation(7, 31));
        }

        [Fact]
        public void InterfaceType()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Runtime.InteropServices;

                [ComImport]
                [Guid("1FC6664D-C61E-4131-81CD-A3EE0DD6098F")]
                [CoClass(typeof(C))]
                interface I : IEnumerable
                {
                    void Add(int i);
                }

                class C : I
                {
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    void I.Add(int i) { }
                }

                class Program
                {
                    static void Main()
                    {
                        I i;
                        i = new() { };
                        i = new() { 1, 2 };
                        i = [];
                        i = [3, 4];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (26,13): error CS9174: Cannot initialize type 'I' with a collection expression because the type is not constructible.
                //         i = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("I").WithLocation(26, 13),
                // (27,13): error CS9174: Cannot initialize type 'I' with a collection expression because the type is not constructible.
                //         i = [3, 4];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[3, 4]").WithArguments("I").WithLocation(27, 13));
        }

        [Fact]
        public void EnumType_01()
        {
            string source = """
                enum E { }
                class Program
                {
                    static void Main()
                    {
                        E e;
                        e = [];
                        e = [1, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS9174: Cannot initialize type 'E' with a collection expression because the type is not constructible.
                //         e = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("E").WithLocation(7, 13),
                // (8,13): error CS9174: Cannot initialize type 'E' with a collection expression because the type is not constructible.
                //         e = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("E").WithLocation(8, 13));
        }

        [Fact]
        public void EnumType_02()
        {
            string sourceA = """
                using System.Collections;
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public struct Enum : IEnumerable { }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public class List<T> : IEnumerable { }
                }
                """;
            string sourceB = """
                enum E { }
                class Program
                {
                    static void Main()
                    {
                        E e;
                        e = [];
                        e = [1, 2];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            // ConversionsBase.GetConstructibleCollectionType() ignores whether the enum
            // implements IEnumerable, so the type is not considered constructible.
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(7,13): error CS9174: Cannot initialize type 'E' with a collection expression because the type is not constructible.
                //         e = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("E").WithLocation(7, 13),
                // 1.cs(8,13): error CS9174: Cannot initialize type 'E' with a collection expression because the type is not constructible.
                //         e = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("E").WithLocation(8, 13));
        }

        [Fact]
        public void DelegateType_01()
        {
            string source = """
                delegate void D();
                class Program
                {
                    static void Main()
                    {
                        D d;
                        d = [];
                        d = [1, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS9174: Cannot initialize type 'D' with a collection expression because the type is not constructible.
                //         d = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("D").WithLocation(7, 13),
                // (8,13): error CS9174: Cannot initialize type 'D' with a collection expression because the type is not constructible.
                //         d = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("D").WithLocation(8, 13));
        }

        [Fact]
        public void DelegateType_02()
        {
            string sourceA = """
                using System.Collections;
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public struct IntPtr { }
                    public abstract class Delegate : IEnumerable { }
                    public abstract class MulticastDelegate : Delegate { }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public class List<T> : IEnumerable { }
                }
                """;
            string sourceB = """
                delegate void D();
                class Program
                {
                    static void Main()
                    {
                        D d;
                        d = [];
                        d = [1, 2];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            // ConversionsBase.GetConstructibleCollectionType() ignores whether the delegate
            // implements IEnumerable, so the type is not considered constructible.
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(7,13): error CS9174: Cannot initialize type 'D' with a collection expression because the type is not constructible.
                //         d = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("D").WithLocation(7, 13),
                // 1.cs(8,13): error CS9174: Cannot initialize type 'D' with a collection expression because the type is not constructible.
                //         d = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("D").WithLocation(8, 13));
        }

        [Fact]
        public void PointerType_01()
        {
            string source = """
                class Program
                {
                    unsafe static void Main()
                    {
                        int* x = [];
                        int* y = [1, 2];
                        var z = (int*)[3];
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,18): error CS9174: Cannot initialize type 'int*' with a collection expression because the type is not constructible.
                //         int* x = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("int*").WithLocation(5, 18),
                // (6,18): error CS9174: Cannot initialize type 'int*' with a collection expression because the type is not constructible.
                //         int* y = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("int*").WithLocation(6, 18),
                // (7,17): error CS9174: Cannot initialize type 'int*' with a collection expression because the type is not constructible.
                //         var z = (int*)[3];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "(int*)[3]").WithArguments("int*").WithLocation(7, 17));
        }

        [Fact]
        public void PointerType_02()
        {
            string source = """
                class Program
                {
                    unsafe static void Main()
                    {
                        delegate*<void> x = [];
                        delegate*<void> y = [1, 2];
                        var z = (delegate*<void>)[3];
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,29): error CS9174: Cannot initialize type 'delegate*<void>' with a collection expression because the type is not constructible.
                //         delegate*<void> x = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("delegate*<void>").WithLocation(5, 29),
                // (6,29): error CS9174: Cannot initialize type 'delegate*<void>' with a collection expression because the type is not constructible.
                //         delegate*<void> y = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("delegate*<void>").WithLocation(6, 29),
                // (7,17): error CS9174: Cannot initialize type 'delegate*<void>' with a collection expression because the type is not constructible.
                //         var z = (delegate*<void>)[3];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "(delegate*<void>)[3]").WithArguments("delegate*<void>").WithLocation(7, 17));
        }

        [Fact]
        public void PointerType_03()
        {
            string source = """
                class Program
                {
                    unsafe static void Main()
                    {
                        void* p = null;
                        delegate*<void> d = null;
                        var x = [p];
                        var y = [d];
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,17): error CS9176: There is no target type for the collection expression.
                //         var x = [p];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[p]").WithLocation(7, 17),
                // (8,17): error CS9176: There is no target type for the collection expression.
                //         var y = [d];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[d]").WithLocation(8, 17));
        }

        [Fact]
        public void PointerType_04()
        {
            string source = """
                using System;
                class Program
                {
                    unsafe static void Main()
                    {
                        void*[] a = [null, (void*)2];
                        foreach (void* p in a)
                            Console.Write("{0}, ", (nint)p);
                    }
                }
                """;
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: "0, 2, ");
        }

        [Fact]
        public void PointerType_05()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable
                {
                    private List<nint> _list = new List<nint>();
                    unsafe public void Add(void* p) { _list.Add((nint)p); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    unsafe static void Main()
                    {
                        void* p = (void*)2;
                        C c = [null, p];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: "[0, 2], ");
        }

        [Fact]
        public void CollectionInitializerType_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int> Create1() => [];
                    static List<object> Create2() => [1, 2];
                    static List<int> Create3() => [3, 4, 5];
                    static List<long?> Create4() => [null, 7];
                    static void Main()
                    {
                        Create1().Report();
                        Create2().Report();
                        Create3().Report();
                        Create4().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  3
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.1
                  IL_0007:  box        "int"
                  IL_000c:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0011:  dup
                  IL_0012:  ldc.i4.2
                  IL_0013:  box        "int"
                  IL_0018:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_001d:  ret
                }
                """);
            verifier.VerifyIL("Program.Create3", """
                {
                  // Code size       27 (0x1b)
                  .maxstack  3
                  IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.3
                  IL_0007:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_000c:  dup
                  IL_000d:  ldc.i4.4
                  IL_000e:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_0013:  dup
                  IL_0014:  ldc.i4.5
                  IL_0015:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_001a:  ret
                }
                """);
            verifier.VerifyIL("Program.Create4", """
                {
                  // Code size       34 (0x22)
                  .maxstack  3
                  .locals init (long? V_0)
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  initobj    "long?"
                  IL_000e:  ldloc.0
                  IL_000f:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0014:  dup
                  IL_0015:  ldc.i4.7
                  IL_0016:  conv.i8
                  IL_0017:  newobj     "long?..ctor(long)"
                  IL_001c:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_02()
        {
            string source = """
                S s;
                s = [];
                s = [1, 2];
                s = [default];
                s = [Unknown];
                struct S { }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                // s = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(2, 5),
                // (3,5): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("S").WithLocation(3, 5),
                // (4,5): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                // s = [default];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[default]").WithArguments("S").WithLocation(4, 5),
                // (5,6): error CS0103: The name 'Unknown' does not exist in the current context
                // s = [Unknown];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown").WithArguments("Unknown").WithLocation(5, 6));
        }

        [Fact]
        public void CollectionInitializerType_03()
        {
            string source = """
                using System.Collections;
                S s;
                s = [];
                struct S : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            CompileAndVerify(source, expectedOutput: "");

            source = """
                using System.Collections;
                S s;
                s = [1, 2];
                struct S : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,6): error CS1061: 'S' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'S' could be found (are you missing a using directive or an assembly reference?)
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "1").WithArguments("S", "Add").WithLocation(3, 6),
                // (3,9): error CS1061: 'S' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'S' could be found (are you missing a using directive or an assembly reference?)
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "2").WithArguments("S", "Add").WithLocation(3, 9));
        }

        [Fact]
        public void CollectionInitializerType_04()
        {
            string source = """
                using System.Collections;
                C c;
                c = [];
                c = [1, 2];
                class C : IEnumerable
                {
                    C(object o) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    public void Add(int i) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                // c = [];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("C", "0").WithLocation(3, 5),
                // (4,5): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                // c = [1, 2];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[1, 2]").WithArguments("C", "0").WithLocation(4, 5));
        }

        [Fact]
        public void CollectionInitializerType_05()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class A : IEnumerable<int>
                {
                    A() { }
                    public void Add(int i) { }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    static A Create1() => [];
                }
                class B
                {
                    static A Create2() => [1, 2];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,27): error CS0122: 'A.A()' is inaccessible due to its protection level
                //     static A Create2() => [1, 2];
                Diagnostic(ErrorCode.ERR_BadAccess, "[1, 2]").WithArguments("A.A()").WithLocation(13, 27));
        }

        [Fact]
        public void CollectionInitializerType_06()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    private List<T> _list = new List<T>();
                    public void Add(T t) { _list.Add(t); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c;
                        object o;
                        c = [];
                        o = (C<object>)[];
                        c.Report();
                        o.Report();
                        c = [1, 2];
                        o = (C<object>)[3, 4];
                        c.Report();
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        [Fact]
        public void CollectionInitializerType_ConstructorOptionalParameters()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable<int>
                {
                    private List<int> _list = new List<int>();
                    internal C(int x = 1, int y = 2) { }
                    public void Add(int i) { _list.Add(i); }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C c;
                        object o;
                        c = [];
                        o = (C)([]);
                        c.Report();
                        o.Report();
                        c = [1, 2];
                        o = (C)([3, 4]);
                        c.Report();
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        [Fact]
        public void CollectionInitializerType_ConstructorParamsArray()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable<int>
                {
                    private List<int> _list = new List<int>();
                    internal C(params int[] args) { }
                    public void Add(int i) { _list.Add(i); }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C c;
                        object o;
                        c = [];
                        o = (C)([]);
                        c.Report();
                        o.Report();
                        c = [1, 2];
                        o = (C)([3, 4]);
                        c.Report();
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        [Fact]
        public void CollectionInitializerType_07()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                abstract class A : IEnumerable<int>
                {
                    public void Add(int i) { }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class B : A { }
                class Program
                {
                    static void Main()
                    {
                        A a = [];
                        B b = [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,15): error CS0144: Cannot create an instance of the abstract type or interface 'A'
                //         A a = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("A").WithLocation(14, 15));
        }

        [Fact]
        public void CollectionInitializerType_08()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                struct S0<T> : IEnumerable
                {
                    public void Add(T t) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S1<T> : IEnumerable<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S2<T> : IEnumerable<T>
                {
                    public S2() { }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static void M0()
                    {
                        object o = (S0<int>)[];
                        S0<int> s = [1, 2];
                    }
                    static void M1()
                    {
                        object o = (S1<int>)[];
                        S1<int> s = [1, 2];
                    }
                    static void M2()
                    {
                        S2<int> s = [];
                        object o = (S2<int>)[1, 2];
                    }
                }
                """;
            var verifier = CompileAndVerify(source);
            verifier.VerifyIL("Program.M0", """
                {
                  // Code size       35 (0x23)
                  .maxstack  2
                  .locals init (S0<int> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S0<int>"
                  IL_0008:  ldloc.0
                  IL_0009:  pop
                  IL_000a:  ldloca.s   V_0
                  IL_000c:  initobj    "S0<int>"
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  ldc.i4.1
                  IL_0015:  call       "void S0<int>.Add(int)"
                  IL_001a:  ldloca.s   V_0
                  IL_001c:  ldc.i4.2
                  IL_001d:  call       "void S0<int>.Add(int)"
                  IL_0022:  ret
                }
                """);
            verifier.VerifyIL("Program.M1", """
                {
                  // Code size       35 (0x23)
                  .maxstack  2
                  .locals init (S1<int> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S1<int>"
                  IL_0008:  ldloc.0
                  IL_0009:  pop
                  IL_000a:  ldloca.s   V_0
                  IL_000c:  initobj    "S1<int>"
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  ldc.i4.1
                  IL_0015:  call       "void S1<int>.Add(int)"
                  IL_001a:  ldloca.s   V_0
                  IL_001c:  ldc.i4.2
                  IL_001d:  call       "void S1<int>.Add(int)"
                  IL_0022:  ret
                }
                """);
            verifier.VerifyIL("Program.M2", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  2
                  .locals init (S2<int> V_0)
                  IL_0000:  newobj     "S2<int>..ctor()"
                  IL_0005:  pop
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  call       "S2<int>..ctor()"
                  IL_000d:  ldloca.s   V_0
                  IL_000f:  ldc.i4.1
                  IL_0010:  call       "void S2<int>.Add(int)"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  ldc.i4.2
                  IL_0018:  call       "void S2<int>.Add(int)"
                  IL_001d:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_09()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        UnknownType u;
                        u = [];
                        u = [null, B];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS0246: The type or namespace name 'UnknownType' could not be found (are you missing a using directive or an assembly reference?)
                //         UnknownType u;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownType").WithArguments("UnknownType").WithLocation(7, 9),
                // (9,20): error CS0103: The name 'B' does not exist in the current context
                //         u = [null, B];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "B").WithArguments("B").WithLocation(9, 20));
        }

        [Fact]
        public void CollectionInitializerType_10()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                struct S<T> : IEnumerable<string>
                {
                    public void Add(string i) { }
                    IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class Program
                {
                    static void Main()
                    {
                        S<UnknownType> s;
                        s = [];
                        s = [null, B];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,11): error CS0246: The type or namespace name 'UnknownType' could not be found (are you missing a using directive or an assembly reference?)
                //         S<UnknownType> s;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownType").WithArguments("UnknownType").WithLocation(13, 11),
                // (15,20): error CS0103: The name 'B' does not exist in the current context
                //         s = [null, B];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "B").WithArguments("B").WithLocation(15, 20));
        }

        [Fact]
        public void CollectionInitializerType_11()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<List<int>> l;
                        l = [[], [2, 3]];
                        l = [[], {2, 3}];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,18): error CS1003: Syntax error, ']' expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]").WithLocation(8, 18),
                // (8,18): error CS1002: ; expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(8, 18),
                // (8,20): error CS1002: ; expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(8, 20),
                // (8,20): error CS1513: } expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(8, 20),
                // (8,23): error CS1002: ; expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(8, 23),
                // (8,24): error CS1513: } expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_RbraceExpected, "]").WithLocation(8, 24));
        }

        [Fact]
        public void CollectionInitializerType_12()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable
                {
                    List<string> _list = new List<string>();
                    public void Add(int i) { _list.Add($"i={i}"); }
                    public void Add(object o) { _list.Add($"o={o}"); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C x = [];
                        C y = [1, (object)2];
                        x.Report();
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [i=1, o=2], ");
        }

        [Fact]
        public void CollectionInitializerType_13()
        {
            string source = """
                using System.Collections;
                interface IA { }
                interface IB { }
                class AB : IA, IB { }
                class C : IEnumerable
                {
                    public void Add(IA a) { }
                    public void Add(IB b) { }
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class Program
                {
                    static void Main()
                    {
                        C c = [(IA)null, (IB)null, new AB()];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (15,36): error CS0121: The call is ambiguous between the following methods or properties: 'C.Add(IA)' and 'C.Add(IB)'
                //         C c = [(IA)null, (IB)null, new AB()];
                Diagnostic(ErrorCode.ERR_AmbigCall, "new AB()").WithArguments("C.Add(IA)", "C.Add(IB)").WithLocation(15, 36));
        }

        [Fact]
        public void CollectionInitializerType_14()
        {
            string source = """
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T x, T y) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static void Main()
                    {
                        S<int> s;
                        s = [];
                        s = [1, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,14): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'S<int>.Add(int, int)'
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "1").WithArguments("y", "S<int>.Add(int, int)").WithLocation(13, 14),
                // (13,17): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'S<int>.Add(int, int)'
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "2").WithArguments("y", "S<int>.Add(int, int)").WithLocation(13, 17));
        }

        [Fact]
        public void CollectionInitializerType_15()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    List<T> _list = new List<T>();
                    public void Add(T t, int index = -1) { _list.Add(t); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c = [1, 2];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], ");
        }

        [Fact]
        public void CollectionInitializerType_16()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    List<T> _list = new List<T>();
                    public void Add(T t, params T[] args) { _list.Add(t); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c = [1, 2];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], ");
        }

        [Fact]
        public void CollectionInitializerType_17()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    List<T> _list = new List<T>();
                    public void Add(params T[] args) { _list.AddRange(args); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c = [[], [1, 2], 3];
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2, 3], ");
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       61 (0x3d)
                  .maxstack  5
                  .locals init (C<int> V_0)
                  IL_0000:  newobj     "C<int>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  call       "int[] System.Array.Empty<int>()"
                  IL_000c:  callvirt   "void C<int>.Add(params int[])"
                  IL_0011:  ldloc.0
                  IL_0012:  ldc.i4.2
                  IL_0013:  newarr     "int"
                  IL_0018:  dup
                  IL_0019:  ldc.i4.0
                  IL_001a:  ldc.i4.1
                  IL_001b:  stelem.i4
                  IL_001c:  dup
                  IL_001d:  ldc.i4.1
                  IL_001e:  ldc.i4.2
                  IL_001f:  stelem.i4
                  IL_0020:  callvirt   "void C<int>.Add(params int[])"
                  IL_0025:  ldloc.0
                  IL_0026:  ldc.i4.1
                  IL_0027:  newarr     "int"
                  IL_002c:  dup
                  IL_002d:  ldc.i4.0
                  IL_002e:  ldc.i4.3
                  IL_002f:  stelem.i4
                  IL_0030:  callvirt   "void C<int>.Add(params int[])"
                  IL_0035:  ldloc.0
                  IL_0036:  ldc.i4.0
                  IL_0037:  call       "void CollectionExtensions.Report(object, bool)"
                  IL_003c:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_18()
        {
            string source = """
                using System.Collections;
                class S<T, U> : IEnumerable
                {
                    internal void Add(T t) { }
                    private void Add(U u) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    static S<T, U> Create(T t, U u) => [t, u];
                }
                class Program
                {
                    static S<T, U> Create<T, U>(T x, U y) => [x, y];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (11,50): error CS1950: The best overloaded Add method 'S<T, U>.Add(T)' for the collection initializer has some invalid arguments
                //     static S<T, U> Create<T, U>(T x, U y) => [x, y];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "y").WithArguments("S<T, U>.Add(T)").WithLocation(11, 50),
                // (11,50): error CS1503: Argument 1: cannot convert from 'U' to 'T'
                //     static S<T, U> Create<T, U>(T x, U y) => [x, y];
                Diagnostic(ErrorCode.ERR_BadArgType, "y").WithArguments("1", "U", "T").WithLocation(11, 50));
        }

        [Fact]
        public void CollectionInitializerType_19()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        string s;
                        s = [];
                        s = ['a'];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         s = [];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("string", "0").WithLocation(6, 13),
                // (7,13): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         s = ['a'];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "['a']").WithArguments("string", "0").WithLocation(7, 13),
                // (7,14): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         s = ['a'];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(7, 14));
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public void TypeParameter_01(string type)
        {
            string source = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable
                {
                    void Add(T t);
                }
                {{type}} C<T> : I<T>
                {
                    private List<T> _list;
                    public void Add(T t)
                    {
                        GetList().Add(t);
                    }
                    IEnumerator IEnumerable.GetEnumerator()
                    {
                        return GetList().GetEnumerator();
                    }
                    private List<T> GetList() => _list ??= new List<T>();
                }
                class Program
                {
                    static void Main()
                    {
                        CreateEmpty<C<object>, object>().Report();
                        Create<C<long?>, long?>(null, 2).Report();
                    }
                    static T CreateEmpty<T, U>() where T : I<U>, new()
                    {
                        return [];
                    }
                    static T Create<T, U>(U a, U b) where T : I<U>, new()
                    {
                        return [a, b];
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [null, 2], ");
            verifier.VerifyIL("Program.CreateEmpty<T, U>", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "T System.Activator.CreateInstance<T>()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.Create<T, U>", """
                {
                  // Code size       36 (0x24)
                  .maxstack  2
                  .locals init (T V_0)
                  IL_0000:  call       "T System.Activator.CreateInstance<T>()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  ldarg.0
                  IL_0009:  constrained. "T"
                  IL_000f:  callvirt   "void I<U>.Add(U)"
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldarg.1
                  IL_0017:  constrained. "T"
                  IL_001d:  callvirt   "void I<U>.Add(U)"
                  IL_0022:  ldloc.0
                  IL_0023:  ret
                }
                """);
        }

        [Fact]
        public void TypeParameter_02()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct S<T> : I<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static T Create1<T, U>() where T : struct, I<U> => [];
                    static T? Create2<T, U>() where T : struct, I<U> => [];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (16,57): error CS9174: Cannot initialize type 'T?' with a collection expression because the type is not constructible.
                //     static T? Create2<T, U>() where T : struct, I<U> => [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("T?").WithLocation(16, 57));
        }

        [Fact]
        public void TypeParameter_03()
        {
            string source = """
                using System.Collections;
                class Program
                {
                    static T Create1<T, U>() where T : IEnumerable => []; // 1
                    static T Create2<T, U>() where T : class, IEnumerable => []; // 2
                    static T Create3<T, U>() where T : struct, IEnumerable => [];
                    static T Create4<T, U>() where T : IEnumerable, new() => [];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,55): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create1<T, U>() where T : IEnumerable => []; // 1
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[]").WithArguments("T").WithLocation(4, 55),
                // (5,62): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create2<T, U>() where T : class, IEnumerable => []; // 2
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[]").WithArguments("T").WithLocation(5, 62));
        }

        [Fact]
        public void TypeParameter_04()
        {
            string source = """
                using System.Collections;
                interface IAdd : IEnumerable
                {
                    void Add(int i);
                }
                class Program
                {
                    static T Create1<T>() where T : IAdd => [1]; // 1
                    static T Create2<T>() where T : class, IAdd => [2]; // 2
                    static T Create3<T>() where T : struct, IAdd => [3];
                    static T Create4<T>() where T : IAdd, new() => [4];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,45): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create1<T>() where T : IAdd => [1]; // 1
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[1]").WithArguments("T").WithLocation(8, 45),
                // (9,52): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create2<T>() where T : class, IAdd => [2]; // 2
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[2]").WithArguments("T").WithLocation(9, 52));
        }

        [Fact]
        public void CollectionInitializerType_MissingIEnumerable()
        {
            string source = """
                struct S
                {
                }
                class Program
                {
                    static void Main()
                    {
                        S s = [];
                        object o = (S)([1, 2]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(SpecialType.System_Collections_IEnumerable);
            comp.VerifyEmitDiagnostics(
                // (8,15): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         S s = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(8, 15),
                // (9,20): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         object o = (S)([1, 2]);
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "(S)([1, 2])").WithArguments("S").WithLocation(9, 20));
        }

        [Fact]
        public void CollectionInitializerType_UseSiteErrors()
        {
            string assemblyA = GetUniqueName();
            string sourceA = """
                public class A1 { }
                public class A2 { }
                """;
            var comp = CreateCompilation(sourceA, assemblyName: assemblyA);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections;
                using System.Collections.Generic;
                public class B1 : IEnumerable
                {
                    List<int> _list = new List<int>();
                    public B1(A1 a = null) { }
                    public void Add(int i) { _list.Add(i); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                public class B2 : IEnumerable
                {
                    List<int> _list = new List<int>();
                    public void Add(int x, A2 y = null) { _list.Add(x); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA });
            var refB = comp.EmitToImageReference();

            string sourceC = """
                class C
                {
                    static void Main()
                    {
                        B1 x;
                        x = [];
                        x.Report();
                        x = [1, 2];
                        x.Report();
                        B2 y;
                        y = [];
                        y.Report();
                        y = [3, 4];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { sourceC, s_collectionExtensions }, references: new[] { refA, refB }, expectedOutput: "[], [1, 2], [], [3, 4], ");

            comp = CreateCompilation(new[] { sourceC, s_collectionExtensions }, references: new[] { refB });
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS0012: The type 'A1' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         x = [];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "[]").WithArguments("A1", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 13),
                // (8,13): error CS0012: The type 'A1' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         x = [1, 2];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "[1, 2]").WithArguments("A1", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 13),
                // (13,14): error CS0012: The type 'A2' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         y = [3, 4];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "3").WithArguments("A2", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(13, 14),
                // (13,17): error CS0012: The type 'A2' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         y = [3, 4];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "4").WithArguments("A2", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(13, 17));
        }

        [Fact]
        public void ConditionalAdd()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Diagnostics;
                class C<T, U> : IEnumerable
                {
                    List<object> _list = new List<object>();
                    [Conditional("DEBUG")] internal void Add(T t) { _list.Add(t); }
                    internal void Add(U u) { _list.Add(u); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int, string> c = [1, "2", 3];
                        c.Report();
                    }
                }
                """;
            var parseOptions = TestOptions.RegularPreview;
            CompileAndVerify(new[] { source, s_collectionExtensions }, parseOptions: parseOptions.WithPreprocessorSymbols("DEBUG"), expectedOutput: "[1, 2, 3], ");
            CompileAndVerify(new[] { source, s_collectionExtensions }, parseOptions: parseOptions, expectedOutput: "[2], ");
        }

        [Fact]
        public void DictionaryElement_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, int> d;
                        d = [];
                        d = [new KeyValuePair<int, int>(1, 2)];
                        d = [3:4];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,14): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'Dictionary<int, int>.Add(int, int)'
                //         d = [new KeyValuePair<int, int>(1, 2)];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new KeyValuePair<int, int>(1, 2)").WithArguments("value", "System.Collections.Generic.Dictionary<int, int>.Add(int, int)").WithLocation(8, 14),
                // (9,15): error CS1003: Syntax error, ',' expected
                //         d = [3:4];
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(9, 15),
                // (9,16): error CS1003: Syntax error, ',' expected
                //         d = [3:4];
                Diagnostic(ErrorCode.ERR_SyntaxError, "4").WithArguments(",").WithLocation(9, 16));
        }

        [Theory]
        [CombinatorialData]
        public void SpreadElement_01(
            [CombinatorialValues("IEnumerable<int>", "int[]", "List<int>", "Span<int>", "ReadOnlySpan<int>")] string spreadType,
            [CombinatorialValues("IEnumerable<int>", "int[]", "List<int>", "Span<int>", "ReadOnlySpan<int>")] string collectionType)
        {
            string source = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F([1, 2, 3]).Report();
                    }
                    static {{collectionType}} F({{spreadType}} s) => [..s];
                }
                """;

            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1, 2, 3], "));

            // Verify some of the cases.
            string expectedIL = (spreadType, collectionType) switch
            {
                ("IEnumerable<int>", "IEnumerable<int>") =>
                    """
                    {
                      // Code size       51 (0x33)
                      .maxstack  2
                      .locals init (System.Collections.Generic.List<int> V_0,
                                    System.Collections.Generic.IEnumerator<int> V_1,
                                    int V_2)
                      IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldarg.0
                      IL_0007:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
                      IL_000c:  stloc.1
                      .try
                      {
                        IL_000d:  br.s       IL_001d
                        IL_000f:  ldloc.1
                        IL_0010:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
                        IL_0015:  stloc.2
                        IL_0016:  ldloc.0
                        IL_0017:  ldloc.2
                        IL_0018:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                        IL_001d:  ldloc.1
                        IL_001e:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                        IL_0023:  brtrue.s   IL_000f
                        IL_0025:  leave.s    IL_0031
                      }
                      finally
                      {
                        IL_0027:  ldloc.1
                        IL_0028:  brfalse.s  IL_0030
                        IL_002a:  ldloc.1
                        IL_002b:  callvirt   "void System.IDisposable.Dispose()"
                        IL_0030:  endfinally
                      }
                      IL_0031:  ldloc.0
                      IL_0032:  ret
                    }
                    """,
                ("IEnumerable<int>", "int[]") =>
                    """
                    {
                      // Code size       56 (0x38)
                      .maxstack  2
                      .locals init (System.Collections.Generic.List<int> V_0,
                                    System.Collections.Generic.IEnumerator<int> V_1,
                                    int V_2)
                      IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldarg.0
                      IL_0007:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
                      IL_000c:  stloc.1
                      .try
                      {
                        IL_000d:  br.s       IL_001d
                        IL_000f:  ldloc.1
                        IL_0010:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
                        IL_0015:  stloc.2
                        IL_0016:  ldloc.0
                        IL_0017:  ldloc.2
                        IL_0018:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                        IL_001d:  ldloc.1
                        IL_001e:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                        IL_0023:  brtrue.s   IL_000f
                        IL_0025:  leave.s    IL_0031
                      }
                      finally
                      {
                        IL_0027:  ldloc.1
                        IL_0028:  brfalse.s  IL_0030
                        IL_002a:  ldloc.1
                        IL_002b:  callvirt   "void System.IDisposable.Dispose()"
                        IL_0030:  endfinally
                      }
                      IL_0031:  ldloc.0
                      IL_0032:  callvirt   "int[] System.Collections.Generic.List<int>.ToArray()"
                      IL_0037:  ret
                    }
                    """,
                ("int[]", "int[]") =>
                    // https://github.com/dotnet/roslyn/issues/68785: Avoid intermediate List<T> if all spread elements have Length property.
                    """
                    {
                      // Code size       40 (0x28)
                      .maxstack  2
                      .locals init (System.Collections.Generic.List<int> V_0,
                                    int[] V_1,
                                    int V_2,
                                    int V_3)
                      IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldarg.0
                      IL_0007:  stloc.1
                      IL_0008:  ldc.i4.0
                      IL_0009:  stloc.2
                      IL_000a:  br.s       IL_001b
                      IL_000c:  ldloc.1
                      IL_000d:  ldloc.2
                      IL_000e:  ldelem.i4
                      IL_000f:  stloc.3
                      IL_0010:  ldloc.0
                      IL_0011:  ldloc.3
                      IL_0012:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                      IL_0017:  ldloc.2
                      IL_0018:  ldc.i4.1
                      IL_0019:  add
                      IL_001a:  stloc.2
                      IL_001b:  ldloc.2
                      IL_001c:  ldloc.1
                      IL_001d:  ldlen
                      IL_001e:  conv.i4
                      IL_001f:  blt.s      IL_000c
                      IL_0021:  ldloc.0
                      IL_0022:  callvirt   "int[] System.Collections.Generic.List<int>.ToArray()"
                      IL_0027:  ret
                    }
                    """,
                ("ReadOnlySpan<int>", "ReadOnlySpan<int>") =>
                    // https://github.com/dotnet/roslyn/issues/68785: Avoid intermediate List<T> if all spread elements have Length property.
                    """
                    {
                      // Code size       53 (0x35)
                      .maxstack  2
                      .locals init (System.Collections.Generic.List<int> V_0,
                                    System.ReadOnlySpan<int>.Enumerator V_1,
                                    int V_2)
                      IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldarga.s   V_0
                      IL_0008:  call       "System.ReadOnlySpan<int>.Enumerator System.ReadOnlySpan<int>.GetEnumerator()"
                      IL_000d:  stloc.1
                      IL_000e:  br.s       IL_0020
                      IL_0010:  ldloca.s   V_1
                      IL_0012:  call       "ref readonly int System.ReadOnlySpan<int>.Enumerator.Current.get"
                      IL_0017:  ldind.i4
                      IL_0018:  stloc.2
                      IL_0019:  ldloc.0
                      IL_001a:  ldloc.2
                      IL_001b:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                      IL_0020:  ldloca.s   V_1
                      IL_0022:  call       "bool System.ReadOnlySpan<int>.Enumerator.MoveNext()"
                      IL_0027:  brtrue.s   IL_0010
                      IL_0029:  ldloc.0
                      IL_002a:  callvirt   "int[] System.Collections.Generic.List<int>.ToArray()"
                      IL_002f:  newobj     "System.ReadOnlySpan<int>..ctor(int[])"
                      IL_0034:  ret
                    }
                    """,
                _ => null
            };
            if (expectedIL is { })
            {
                verifier.VerifyIL("Program.F", expectedIL);
            }
        }

        [Theory]
        [InlineData("int[]")]
        [InlineData("System.Collections.Generic.List<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.ReadOnlySpan<int>")]
        public void SpreadElement_02(string collectionType)
        {
            string source = $$"""
                class Program
                {
                    static void Main()
                    {
                        {{collectionType}} c;
                        c = [];
                        c = Append(c);
                        c.Report();
                    }
                    static {{collectionType}} Append({{collectionType}} x)
                    {
                        {{collectionType}} y = [1, 2];
                        return [..x, ..y];
                    }
                }
                """;

            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1, 2], "));

            if (collectionType == "System.ReadOnlySpan<int>")
            {
                verifier.VerifyIL("Program.Append",
                    """
                    {
                      // Code size       99 (0x63)
                      .maxstack  2
                      .locals init (System.ReadOnlySpan<int> V_0, //y
                                    System.Collections.Generic.List<int> V_1,
                                    System.ReadOnlySpan<int>.Enumerator V_2,
                                    int V_3)
                      IL_0000:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=8_Align=4 <PrivateImplementationDetails>.34FB5C825DE7CA4AEA6E712F19D439C1DA0C92C37B423936C5F618545CA4FA1F4"
                      IL_0005:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
                      IL_000a:  stloc.0
                      IL_000b:  newobj     "System.Collections.Generic.List<int>..ctor()"
                      IL_0010:  stloc.1
                      IL_0011:  ldarga.s   V_0
                      IL_0013:  call       "System.ReadOnlySpan<int>.Enumerator System.ReadOnlySpan<int>.GetEnumerator()"
                      IL_0018:  stloc.2
                      IL_0019:  br.s       IL_002b
                      IL_001b:  ldloca.s   V_2
                      IL_001d:  call       "ref readonly int System.ReadOnlySpan<int>.Enumerator.Current.get"
                      IL_0022:  ldind.i4
                      IL_0023:  stloc.3
                      IL_0024:  ldloc.1
                      IL_0025:  ldloc.3
                      IL_0026:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                      IL_002b:  ldloca.s   V_2
                      IL_002d:  call       "bool System.ReadOnlySpan<int>.Enumerator.MoveNext()"
                      IL_0032:  brtrue.s   IL_001b
                      IL_0034:  ldloca.s   V_0
                      IL_0036:  call       "System.ReadOnlySpan<int>.Enumerator System.ReadOnlySpan<int>.GetEnumerator()"
                      IL_003b:  stloc.2
                      IL_003c:  br.s       IL_004e
                      IL_003e:  ldloca.s   V_2
                      IL_0040:  call       "ref readonly int System.ReadOnlySpan<int>.Enumerator.Current.get"
                      IL_0045:  ldind.i4
                      IL_0046:  stloc.3
                      IL_0047:  ldloc.1
                      IL_0048:  ldloc.3
                      IL_0049:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                      IL_004e:  ldloca.s   V_2
                      IL_0050:  call       "bool System.ReadOnlySpan<int>.Enumerator.MoveNext()"
                      IL_0055:  brtrue.s   IL_003e
                      IL_0057:  ldloc.1
                      IL_0058:  callvirt   "int[] System.Collections.Generic.List<int>.ToArray()"
                      IL_005d:  newobj     "System.ReadOnlySpan<int>..ctor(int[])"
                      IL_0062:  ret
                    }
                    """);
            }
        }

        [Fact]
        public void SpreadElement_03()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                struct S<T> : IEnumerable<T>
                {
                    private List<T> _list;
                    public void Add(T t)
                    {
                        _list ??= new List<T>();
                        _list.Add(t);
                    }
                    public IEnumerator<T> GetEnumerator()
                    {
                        _list ??= new List<T>();
                        return _list.GetEnumerator();
                    }
                    IEnumerator IEnumerable.GetEnumerator()
                    {
                        return GetEnumerator();
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        S<int> s;
                        s = [];
                        s = Append(s);
                        s.Report();
                    }
                    static S<int> Append(S<int> x)
                    {
                        S<int> y = [1, 2];
                        return [..x, ..y];
                    }
                }
                """;

            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensions },
                options: TestOptions.ReleaseExe,
                expectedOutput: "[1, 2], ");

            verifier.VerifyIL("Program.Append",
                """
                {
                  // Code size      126 (0x7e)
                  .maxstack  2
                  .locals init (S<int> V_0, //y
                                S<int> V_1,
                                System.Collections.Generic.IEnumerator<int> V_2,
                                int V_3)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  initobj    "S<int>"
                  IL_0008:  ldloca.s   V_1
                  IL_000a:  ldc.i4.1
                  IL_000b:  call       "void S<int>.Add(int)"
                  IL_0010:  ldloca.s   V_1
                  IL_0012:  ldc.i4.2
                  IL_0013:  call       "void S<int>.Add(int)"
                  IL_0018:  ldloc.1
                  IL_0019:  stloc.0
                  IL_001a:  ldloca.s   V_1
                  IL_001c:  initobj    "S<int>"
                  IL_0022:  ldarga.s   V_0
                  IL_0024:  call       "System.Collections.Generic.IEnumerator<int> S<int>.GetEnumerator()"
                  IL_0029:  stloc.2
                  .try
                  {
                    IL_002a:  br.s       IL_003b
                    IL_002c:  ldloc.2
                    IL_002d:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
                    IL_0032:  stloc.3
                    IL_0033:  ldloca.s   V_1
                    IL_0035:  ldloc.3
                    IL_0036:  call       "void S<int>.Add(int)"
                    IL_003b:  ldloc.2
                    IL_003c:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_0041:  brtrue.s   IL_002c
                    IL_0043:  leave.s    IL_004f
                  }
                  finally
                  {
                    IL_0045:  ldloc.2
                    IL_0046:  brfalse.s  IL_004e
                    IL_0048:  ldloc.2
                    IL_0049:  callvirt   "void System.IDisposable.Dispose()"
                    IL_004e:  endfinally
                  }
                  IL_004f:  ldloca.s   V_0
                  IL_0051:  call       "System.Collections.Generic.IEnumerator<int> S<int>.GetEnumerator()"
                  IL_0056:  stloc.2
                  .try
                  {
                    IL_0057:  br.s       IL_0068
                    IL_0059:  ldloc.2
                    IL_005a:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
                    IL_005f:  stloc.3
                    IL_0060:  ldloca.s   V_1
                    IL_0062:  ldloc.3
                    IL_0063:  call       "void S<int>.Add(int)"
                    IL_0068:  ldloc.2
                    IL_0069:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_006e:  brtrue.s   IL_0059
                    IL_0070:  leave.s    IL_007c
                  }
                  finally
                  {
                    IL_0072:  ldloc.2
                    IL_0073:  brfalse.s  IL_007b
                    IL_0075:  ldloc.2
                    IL_0076:  callvirt   "void System.IDisposable.Dispose()"
                    IL_007b:  endfinally
                  }
                  IL_007c:  ldloc.1
                  IL_007d:  ret
                }
                """);
        }

        [Fact]
        public void SpreadElement_04()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var a = [1, 2, ..[]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,26): error CS9176: There is no target type for the collection expression.
                //         var a = [1, 2, ..[]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(5, 26));
        }

        [Fact]
        public void SpreadElement_05()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        int[] a = [1, 2];
                        a = [..a, ..[]];
                        a = [..[default]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,21): error CS9176: There is no target type for the collection expression.
                //         a = [..a, ..[]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(6, 21),
                // (7,16): error CS9176: There is no target type for the collection expression.
                //         a = [..[default]];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[default]").WithLocation(7, 16));
        }

        [Fact]
        public void SpreadElement_06()
        {
            string source = """
                class Program
                {
                    static string[] Append(string a, string b, bool c)
                    {
                        return [a, b, .. c ? [null] : []];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,26): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'collection expressions' and 'collection expressions'
                //         return [a, b, .. c ? [null] : []];
                Diagnostic(ErrorCode.ERR_InvalidQM, "c ? [null] : []").WithArguments("collection expressions", "collection expressions").WithLocation(5, 26));
        }

        [Fact]
        public void SpreadElement_07()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int[,] a = new[,] { { 1, 2 }, { 3, 4 } };
                        int[] b = F(a);
                        b.Report();
                    }
                    static int[] F(int[,] a) => [..a];
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2, 3, 4], ");
            verifier.VerifyIL("Program.F",
                """
                {
                  // Code size       95 (0x5f)
                  .maxstack  3
                  .locals init (System.Collections.Generic.List<int> V_0,
                                int[,] V_1,
                                int V_2,
                                int V_3,
                                int V_4,
                                int V_5,
                                int V_6)
                  IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarg.0
                  IL_0007:  stloc.1
                  IL_0008:  ldloc.1
                  IL_0009:  ldc.i4.0
                  IL_000a:  callvirt   "int System.Array.GetUpperBound(int)"
                  IL_000f:  stloc.2
                  IL_0010:  ldloc.1
                  IL_0011:  ldc.i4.1
                  IL_0012:  callvirt   "int System.Array.GetUpperBound(int)"
                  IL_0017:  stloc.3
                  IL_0018:  ldloc.1
                  IL_0019:  ldc.i4.0
                  IL_001a:  callvirt   "int System.Array.GetLowerBound(int)"
                  IL_001f:  stloc.s    V_4
                  IL_0021:  br.s       IL_0053
                  IL_0023:  ldloc.1
                  IL_0024:  ldc.i4.1
                  IL_0025:  callvirt   "int System.Array.GetLowerBound(int)"
                  IL_002a:  stloc.s    V_5
                  IL_002c:  br.s       IL_0048
                  IL_002e:  ldloc.1
                  IL_002f:  ldloc.s    V_4
                  IL_0031:  ldloc.s    V_5
                  IL_0033:  call       "int[*,*].Get"
                  IL_0038:  stloc.s    V_6
                  IL_003a:  ldloc.0
                  IL_003b:  ldloc.s    V_6
                  IL_003d:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_0042:  ldloc.s    V_5
                  IL_0044:  ldc.i4.1
                  IL_0045:  add
                  IL_0046:  stloc.s    V_5
                  IL_0048:  ldloc.s    V_5
                  IL_004a:  ldloc.3
                  IL_004b:  ble.s      IL_002e
                  IL_004d:  ldloc.s    V_4
                  IL_004f:  ldc.i4.1
                  IL_0050:  add
                  IL_0051:  stloc.s    V_4
                  IL_0053:  ldloc.s    V_4
                  IL_0055:  ldloc.2
                  IL_0056:  ble.s      IL_0023
                  IL_0058:  ldloc.0
                  IL_0059:  callvirt   "int[] System.Collections.Generic.List<int>.ToArray()"
                  IL_005e:  ret
                }
                """);
        }

        [Fact]
        public void SpreadElement_08()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        int[] a = [1, 2, 3];
                        object[] b = F1(a);
                        b.Report();
                        long?[] c = F2(a);
                        c.Report();
                        object[] d = F3<int, object>(a);
                        d.Report();
                    }
                    static object[] F1(int[] a) => [..a];
                    static long?[] F2(int[] a) => [..a];
                    static U[] F3<T, U>(T[] a) where T : U => [..a];
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2, 3], [1, 2, 3], [1, 2, 3], ");
            verifier.VerifyIL("Program.F1",
                """
                {
                  // Code size       45 (0x2d)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<object> V_0,
                                int[] V_1,
                                int V_2,
                                int V_3)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarg.0
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  IL_000a:  br.s       IL_0020
                  IL_000c:  ldloc.1
                  IL_000d:  ldloc.2
                  IL_000e:  ldelem.i4
                  IL_000f:  stloc.3
                  IL_0010:  ldloc.0
                  IL_0011:  ldloc.3
                  IL_0012:  box        "int"
                  IL_0017:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_001c:  ldloc.2
                  IL_001d:  ldc.i4.1
                  IL_001e:  add
                  IL_001f:  stloc.2
                  IL_0020:  ldloc.2
                  IL_0021:  ldloc.1
                  IL_0022:  ldlen
                  IL_0023:  conv.i4
                  IL_0024:  blt.s      IL_000c
                  IL_0026:  ldloc.0
                  IL_0027:  callvirt   "object[] System.Collections.Generic.List<object>.ToArray()"
                  IL_002c:  ret
                }
                """);
            verifier.VerifyIL("Program.F2",
                """
                {
                  // Code size       46 (0x2e)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<long?> V_0,
                                int[] V_1,
                                int V_2,
                                int V_3)
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarg.0
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  IL_000a:  br.s       IL_0021
                  IL_000c:  ldloc.1
                  IL_000d:  ldloc.2
                  IL_000e:  ldelem.i4
                  IL_000f:  stloc.3
                  IL_0010:  ldloc.0
                  IL_0011:  ldloc.3
                  IL_0012:  conv.i8
                  IL_0013:  newobj     "long?..ctor(long)"
                  IL_0018:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_001d:  ldloc.2
                  IL_001e:  ldc.i4.1
                  IL_001f:  add
                  IL_0020:  stloc.2
                  IL_0021:  ldloc.2
                  IL_0022:  ldloc.1
                  IL_0023:  ldlen
                  IL_0024:  conv.i4
                  IL_0025:  blt.s      IL_000c
                  IL_0027:  ldloc.0
                  IL_0028:  callvirt   "long?[] System.Collections.Generic.List<long?>.ToArray()"
                  IL_002d:  ret
                }
                """);
            verifier.VerifyIL("Program.F3<T, U>",
                """
                {
                  // Code size       54 (0x36)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<U> V_0,
                                T[] V_1,
                                int V_2,
                                T V_3)
                  IL_0000:  newobj     "System.Collections.Generic.List<U>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarg.0
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  IL_000a:  br.s       IL_0029
                  IL_000c:  ldloc.1
                  IL_000d:  ldloc.2
                  IL_000e:  ldelem     "T"
                  IL_0013:  stloc.3
                  IL_0014:  ldloc.0
                  IL_0015:  ldloc.3
                  IL_0016:  box        "T"
                  IL_001b:  unbox.any  "U"
                  IL_0020:  callvirt   "void System.Collections.Generic.List<U>.Add(U)"
                  IL_0025:  ldloc.2
                  IL_0026:  ldc.i4.1
                  IL_0027:  add
                  IL_0028:  stloc.2
                  IL_0029:  ldloc.2
                  IL_002a:  ldloc.1
                  IL_002b:  ldlen
                  IL_002c:  conv.i4
                  IL_002d:  blt.s      IL_000c
                  IL_002f:  ldloc.0
                  IL_0030:  callvirt   "U[] System.Collections.Generic.List<U>.ToArray()"
                  IL_0035:  ret
                }
                """);
        }

        [Theory]
        [InlineData("List")]
        [InlineData("Span")]
        [InlineData("ReadOnlySpan")]
        public void SpreadElement_09(string collectionType)
        {
            string source = $$"""
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        {{collectionType}}<int> a = [1, 2, 3];
                        {{collectionType}}<object> b;
                        b = F1(a);
                        b.Report();
                        b = F2<int, object>(a);
                        b.Report();
                    }
                    static {{collectionType}}<object> F1({{collectionType}}<int> a) => [..a];
                    static {{collectionType}}<U> F2<T, U>({{collectionType}}<T> a) where T : U => [..a];
                }
                """;
            CompileAndVerify(
                new[] { source, s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1, 2, 3], [1, 2, 3], "));
        }

        [Fact]
        public void SpreadElement_10()
        {
            string source = """
                using System.Collections;
                class Program
                {
                    static void Main()
                    {
                        IEnumerable a = new[] { 1, 2, 3 };
                        object[] b = [..a, 4];
                        b.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2, 3, 4], ");
        }

        [Fact]
        public void SpreadElement_11()
        {
            string source = """
                using System.Collections;
                class Program
                {
                    static void Main()
                    {
                        F([1, 2, 3]);
                    }
                    static int[] F(IEnumerable s) => [..s];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,11): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'System.Collections.IEnumerable'
                //         F([1, 2, 3]);
                Diagnostic(ErrorCode.ERR_BadArgType, "[1, 2, 3]").WithArguments("1", "collection expressions", "System.Collections.IEnumerable").WithLocation(6, 11),
                // (8,39): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
                //     static int[] F(IEnumerable s) => [..s];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "..s").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(8, 39),
                // (8,39): error CS1503: Argument 1: cannot convert from 'object' to 'int'
                //     static int[] F(IEnumerable s) => [..s];
                Diagnostic(ErrorCode.ERR_BadArgType, "..s").WithArguments("1", "object", "int").WithLocation(8, 39));
        }

        [Theory]
        [InlineData("object[]")]
        [InlineData("List<object>")]
        [InlineData("int[]")]
        [InlineData("List<int>")]
        public void SpreadElement_Dynamic_01(string resultType)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static {{resultType}} F(List<dynamic> e)
                    {
                        return [..e];
                    }
                    static void Main()
                    {
                        var a = F([1, 2, 3]);
                        a.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, references: new[] { CSharpRef }, options: TestOptions.ReleaseExe, expectedOutput: "[1, 2, 3], ");
            if (resultType == "List<object>")
            {
                verifier.VerifyIL("Program.F",
                    """
                    {
                      // Code size      141 (0x8d)
                      .maxstack  9
                      .locals init (System.Collections.Generic.List<object> V_0,
                                    System.Collections.Generic.List<dynamic>.Enumerator V_1,
                                    object V_2)
                      IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldarg.0
                      IL_0007:  callvirt   "System.Collections.Generic.List<dynamic>.Enumerator System.Collections.Generic.List<dynamic>.GetEnumerator()"
                      IL_000c:  stloc.1
                      .try
                      {
                        IL_000d:  br.s       IL_0072
                        IL_000f:  ldloca.s   V_1
                        IL_0011:  call       "dynamic System.Collections.Generic.List<dynamic>.Enumerator.Current.get"
                        IL_0016:  stloc.2
                        IL_0017:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>> Program.<>o__0.<>p__0"
                        IL_001c:  brtrue.s   IL_005c
                        IL_001e:  ldc.i4     0x100
                        IL_0023:  ldstr      "Add"
                        IL_0028:  ldnull
                        IL_0029:  ldtoken    "Program"
                        IL_002e:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                        IL_0033:  ldc.i4.2
                        IL_0034:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
                        IL_0039:  dup
                        IL_003a:  ldc.i4.0
                        IL_003b:  ldc.i4.1
                        IL_003c:  ldnull
                        IL_003d:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                        IL_0042:  stelem.ref
                        IL_0043:  dup
                        IL_0044:  ldc.i4.1
                        IL_0045:  ldc.i4.0
                        IL_0046:  ldnull
                        IL_0047:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                        IL_004c:  stelem.ref
                        IL_004d:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
                        IL_0052:  call       "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                        IL_0057:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>> Program.<>o__0.<>p__0"
                        IL_005c:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>> Program.<>o__0.<>p__0"
                        IL_0061:  ldfld      "System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>>.Target"
                        IL_0066:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>> Program.<>o__0.<>p__0"
                        IL_006b:  ldloc.0
                        IL_006c:  ldloc.2
                        IL_006d:  callvirt   "void System.Action<System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Collections.Generic.List<object>, dynamic)"
                        IL_0072:  ldloca.s   V_1
                        IL_0074:  call       "bool System.Collections.Generic.List<dynamic>.Enumerator.MoveNext()"
                        IL_0079:  brtrue.s   IL_000f
                        IL_007b:  leave.s    IL_008b
                      }
                      finally
                      {
                        IL_007d:  ldloca.s   V_1
                        IL_007f:  constrained. "System.Collections.Generic.List<dynamic>.Enumerator"
                        IL_0085:  callvirt   "void System.IDisposable.Dispose()"
                        IL_008a:  endfinally
                      }
                      IL_008b:  ldloc.0
                      IL_008c:  ret
                    }
                    """);
            }
        }

        [Fact]
        public void SpreadElement_MissingList()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int[] a = [1, 2];
                        IEnumerable<int> e = a;
                        int[] b;
                        b = [..a];
                        b = [..e];
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_List_T);
            // https://github.com/dotnet/roslyn/issues/68785: Should not report missing List<T> for [..a].
            comp.VerifyEmitDiagnostics(
                // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1.ToArray'
                //         b = [..a];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[..a]").WithArguments("System.Collections.Generic.List`1", "ToArray").WithLocation(9, 13),
                // (9,13): error CS0518: Predefined type 'System.Collections.Generic.List`1' is not defined or imported
                //         b = [..a];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[..a]").WithArguments("System.Collections.Generic.List`1").WithLocation(9, 13),
                // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1.ToArray'
                //         b = [..e];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[..e]").WithArguments("System.Collections.Generic.List`1", "ToArray").WithLocation(10, 13),
                // (10,13): error CS0518: Predefined type 'System.Collections.Generic.List`1' is not defined or imported
                //         b = [..e];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[..e]").WithArguments("System.Collections.Generic.List`1").WithLocation(10, 13));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_List_T__ToArray);
            // https://github.com/dotnet/roslyn/issues/68785: Should not report missing List<T>.ToArray() for [..a].
            comp.VerifyEmitDiagnostics(
                // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1.ToArray'
                //         b = [..a];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[..a]").WithArguments("System.Collections.Generic.List`1", "ToArray").WithLocation(9, 13),
                // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1.ToArray'
                //         b = [..e];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[..e]").WithArguments("System.Collections.Generic.List`1", "ToArray").WithLocation(10, 13));
        }

        [CombinatorialData]
        [Theory]
        public void ArrayEmpty_01([CombinatorialValues(TargetFramework.Mscorlib45Extended, TargetFramework.Net80)] TargetFramework targetFramework)
        {
            if (!ExecutionConditionUtil.IsCoreClr && targetFramework == TargetFramework.Net80) return;

            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        EmptyArray<object>().Report();
                        EmptyIEnumerable<object>().Report();
                        EmptyICollection<object>().Report();
                        EmptyIList<object>().Report();
                        EmptyIReadOnlyCollection<object>().Report();
                        EmptyIReadOnlyList<object>().Report();
                    }
                    static T[] EmptyArray<T>() => [];
                    static IEnumerable<T> EmptyIEnumerable<T>() => [];
                    static ICollection<T> EmptyICollection<T>() => [];
                    static IList<T> EmptyIList<T>() => [];
                    static IReadOnlyCollection<T> EmptyIReadOnlyCollection<T>() => [];
                    static IReadOnlyList<T> EmptyIReadOnlyList<T>() => [];
                }
                """;
            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensions },
                targetFramework: targetFramework,
                expectedOutput: "[], [], [], [], [], [], ");

            string expectedIL = (targetFramework == TargetFramework.Mscorlib45Extended) ?
                """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "T"
                  IL_0006:  ret
                }
                """ :
                """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "T[] System.Array.Empty<T>()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.EmptyArray<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyIEnumerable<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyIReadOnlyCollection<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyIReadOnlyList<T>", expectedIL);

            expectedIL =
                """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.EmptyICollection<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyIList<T>", expectedIL);
        }

        [CombinatorialData]
        [Theory]
        public void ArrayEmpty_02([CombinatorialValues(TargetFramework.Mscorlib45Extended, TargetFramework.Net80)] TargetFramework targetFramework)
        {
            if (!ExecutionConditionUtil.IsCoreClr && targetFramework == TargetFramework.Net80) return;

            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        EmptyArray().Report();
                        EmptyIEnumerable().Report();
                        EmptyICollection().Report();
                        EmptyIList().Report();
                        EmptyIReadOnlyCollection().Report();
                        EmptyIReadOnlyList().Report();
                    }
                    static string[] EmptyArray() => [];
                    static IEnumerable<string> EmptyIEnumerable() => [];
                    static ICollection<string> EmptyICollection() => [];
                    static IList<string> EmptyIList() => [];
                    static IReadOnlyCollection<string> EmptyIReadOnlyCollection() => [];
                    static IReadOnlyList<string> EmptyIReadOnlyList() => [];
                }
                """;
            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensions },
                targetFramework: targetFramework,
                expectedOutput: "[], [], [], [], [], [], ");

            string expectedIL = (targetFramework == TargetFramework.Mscorlib45Extended) ?
                """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "string"
                  IL_0006:  ret
                }
                """ :
                """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "string[] System.Array.Empty<string>()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.EmptyArray", expectedIL);
            verifier.VerifyIL("Program.EmptyIEnumerable", expectedIL);
            verifier.VerifyIL("Program.EmptyIReadOnlyCollection", expectedIL);
            verifier.VerifyIL("Program.EmptyIReadOnlyList", expectedIL);

            expectedIL =
                """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<string>..ctor()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.EmptyICollection", expectedIL);
            verifier.VerifyIL("Program.EmptyIList", expectedIL);
        }

        [Fact]
        public void ArrayEmpty_PointerElementType()
        {
            string source = """
                unsafe class Program
                {
                    static void Main()
                    {
                        EmptyArray().Report();
                        EmptyNestedArray().Report();
                    }
                    static void*[] EmptyArray() => [];
                    static void*[][] EmptyNestedArray() => [];
                }
                """;
            var verifier = CompileAndVerify(
                new[] { source, s_collectionExtensions },
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.FailsPEVerify,
                expectedOutput: "[], [], ");
            verifier.VerifyIL("Program.EmptyArray",
                """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "void*"
                  IL_0006:  ret
                }
                """);
            verifier.VerifyIL("Program.EmptyNestedArray",
                """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "void*[][] System.Array.Empty<void*[]>()"
                  IL_0005:  ret
                }
                """);
        }

        [Fact]
        public void ArrayEmpty_MissingMethod()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int[] x = [];
                        IEnumerable<int> y = [];
                        x.Report();
                        y.Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[], [], ");
            verifier.VerifyIL("Program.Main",
                """
                {
                  // Code size       25 (0x19)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //y
                  IL_0000:  call       "int[] System.Array.Empty<int>()"
                  IL_0005:  call       "int[] System.Array.Empty<int>()"
                  IL_000a:  stloc.0
                  IL_000b:  ldc.i4.0
                  IL_000c:  call       "void CollectionExtensions.Report(object, bool)"
                  IL_0011:  ldloc.0
                  IL_0012:  ldc.i4.0
                  IL_0013:  call       "void CollectionExtensions.Report(object, bool)"
                  IL_0018:  ret
                }
                """);

            comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.MakeMemberMissing(WellKnownMember.System_Array__Empty);
            verifier = CompileAndVerify(comp, expectedOutput: "[], [], ");
            verifier.VerifyIL("Program.Main",
                """
                {
                  // Code size       27 (0x1b)
                  .maxstack  3
                  .locals init (int[] V_0) //x
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "int"
                  IL_0006:  stloc.0
                  IL_0007:  ldc.i4.0
                  IL_0008:  newarr     "int"
                  IL_000d:  ldloc.0
                  IL_000e:  ldc.i4.0
                  IL_000f:  call       "void CollectionExtensions.Report(object, bool)"
                  IL_0014:  ldc.i4.0
                  IL_0015:  call       "void CollectionExtensions.Report(object, bool)"
                  IL_001a:  ret
                }
                """);
        }

        [Fact]
        public void Nullable_01()
        {
            string source = """
                #nullable enable
                class Program
                {
                    static void Main()
                    {
                        object?[] x = [1];
                        x[0].ToString(); // 1
                        object[] y = [null]; // 2
                        y[0].ToString();
                        y = [2, null]; // 3
                        y[1].ToString();
                        object[]? z = [];
                        z.ToString();
                        z = [3];
                        z.ToString();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // https://github.com/dotnet/roslyn/issues/68786: // 2 should be reported as a warning (compare with array initializer: new object[] { null }).
            comp.VerifyEmitDiagnostics(
                // (7,9): warning CS8602: Dereference of a possibly null reference.
                //         x[0].ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x[0]").WithLocation(7, 9));
        }

        [Fact]
        public void Nullable_02()
        {
            string source = """
                #nullable enable
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<object?> x = [1];
                        x[0].ToString(); // 1
                        List<object> y = [null]; // 2
                        y[0].ToString();
                        y = [2, null]; // 3
                        y[1].ToString();
                        List<object>? z = [];
                        z.ToString();
                        z = [3];
                        z.ToString();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,9): warning CS8602: Dereference of a possibly null reference.
                //         x[0].ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x[0]").WithLocation(8, 9),
                // (9,27): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         List<object> y = [null]; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 27),
                // (11,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         y = [2, null]; // 3
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 17));
        }

        [Fact]
        public void Nullable_03()
        {
            string source = """
                #nullable enable
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    public T this[int index] => default!;
                    IEnumerator IEnumerable.GetEnumerator() => default!;
                }
                class Program
                {
                    static void Main()
                    {
                        S<object?> x = [1];
                        x[0].ToString(); // 1
                        S<object> y = [null]; // 2
                        y[0].ToString();
                        y = [2, null]; // 3
                        y[1].ToString();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         x[0].ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x[0]").WithLocation(14, 9),
                // (15,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         S<object> y = [null]; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(15, 24),
                // (17,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         y = [2, null]; // 3
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(17, 17));
        }

        [Fact]
        public void Nullable_04()
        {
            string source = """
                #nullable enable
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    public T this[int index] => default!;
                    IEnumerator IEnumerable.GetEnumerator() => default!;
                }
                class Program
                {
                    static void Main()
                    {
                        S<object>? x = [];
                        x = [];
                        S<object>? y = [1];
                        y = [2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,24): error CS9174: Cannot initialize type 'S<object>?' with a collection expression because the type is not constructible.
                //         S<object>? x = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S<object>?").WithLocation(13, 24),
                // (14,13): error CS9174: Cannot initialize type 'S<object>?' with a collection expression because the type is not constructible.
                //         x = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S<object>?").WithLocation(14, 13),
                // (15,24): error CS9174: Cannot initialize type 'S<object>?' with a collection expression because the type is not constructible.
                //         S<object>? y = [1];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1]").WithArguments("S<object>?").WithLocation(15, 24),
                // (16,13): error CS9174: Cannot initialize type 'S<object>?' with a collection expression because the type is not constructible.
                //         y = [2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[2]").WithArguments("S<object>?").WithLocation(16, 13));
        }

        [Fact]
        public void OrderOfEvaluation()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    private List<T> _list = new List<T>();
                    public void Add(T t)
                    {
                        Console.WriteLine("Add {0}", t);
                        _list.Add(t);
                    }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> x = [Get(1), Get(2)];
                        C<C<int>> y = [[Get(3)], [Get(4), Get(5)]];
                    }
                    static int Get(int value)
                    {
                        Console.WriteLine("Get {0}", value);
                        return value;
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: """
                Get 1
                Add 1
                Get 2
                Add 2
                Get 3
                Add 3
                Add C`1[System.Int32]
                Get 4
                Add 4
                Get 5
                Add 5
                Add C`1[System.Int32]
                """);
        }

        // Ensure collection expression conversions are not standard implicit conversions
        // and, as a result, are ignored when determining user-defined conversions.
        [Fact]
        public void UserDefinedConversions_01()
        {
            string source = """
                struct S
                {
                    public static implicit operator S(int[] a) => default;
                }
                class Program
                {
                    static void Main()
                    {
                        S s = [];
                        s = [1, 2];
                        s = (S)([3, 4]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,15): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         S s = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(9, 15),
                // (10,13): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("S").WithLocation(10, 13),
                // (11,13): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         s = (S)([3, 4]);
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "(S)([3, 4])").WithArguments("S").WithLocation(11, 13));
        }

        [Fact]
        public void UserDefinedConversions_02()
        {
            string source = """
                struct S
                {
                    public static explicit operator S(int[] a) => default;
                }
                class Program
                {
                    static void Main()
                    {
                        S s = [];
                        s = [1, 2];
                        s = (S)([3, 4]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,15): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         S s = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(9, 15),
                // (10,13): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2]").WithArguments("S").WithLocation(10, 13),
                // (11,13): error CS9174: Cannot initialize type 'S' with a collection expression because the type is not constructible.
                //         s = (S)([3, 4]);
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "(S)([3, 4])").WithArguments("S").WithLocation(11, 13));
        }

        [Fact]
        public void PrimaryConstructorParameters_01()
        {
            string source = """
                struct S(int x, int y, int z)
                {
                    int[] F = [x, y];
                    int[] M() => [y];
                    static void Main()
                    {
                        var s = new S(1, 2, 3);
                        s.F.Report();
                        s.M().Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // 0.cs(1,28): warning CS9113: Parameter 'z' is unread.
                // struct S(int x, int y, int z)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "z").WithArguments("z").WithLocation(1, 28));

            var verifier = CompileAndVerify(comp, expectedOutput: "[1, 2], [2], ");
            verifier.VerifyIL("S..ctor(int, int, int)",
                """
                {
                  // Code size       33 (0x21)
                  .maxstack  5
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.2
                  IL_0002:  stfld      "int S.<y>P"
                  IL_0007:  ldarg.0
                  IL_0008:  ldc.i4.2
                  IL_0009:  newarr     "int"
                  IL_000e:  dup
                  IL_000f:  ldc.i4.0
                  IL_0010:  ldarg.1
                  IL_0011:  stelem.i4
                  IL_0012:  dup
                  IL_0013:  ldc.i4.1
                  IL_0014:  ldarg.0
                  IL_0015:  ldfld      "int S.<y>P"
                  IL_001a:  stelem.i4
                  IL_001b:  stfld      "int[] S.F"
                  IL_0020:  ret
                }
                """);
        }

        [Fact]
        public void PrimaryConstructorParameters_02()
        {
            string source = """
                using System;
                class C(int x, int y, int z)
                {
                    Func<int[]> F = () => [x, y];
                    Func<int[]> M() => () => [y];
                    static void Main()
                    {
                        var c = new C(1, 2, 3);
                        c.F().Report();
                        c.M()().Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // 0.cs(2,27): warning CS9113: Parameter 'z' is unread.
                // class C(int x, int y, int z)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "z").WithArguments("z").WithLocation(2, 27));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: "[1, 2], [2], ");
            verifier.VerifyIL("C..ctor(int, int, int)",
                """
                {
                  // Code size       52 (0x34)
                  .maxstack  3
                  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.2
                  IL_0002:  stfld      "int C.<y>P"
                  IL_0007:  newobj     "C.<>c__DisplayClass0_0..ctor()"
                  IL_000c:  stloc.0
                  IL_000d:  ldloc.0
                  IL_000e:  ldarg.1
                  IL_000f:  stfld      "int C.<>c__DisplayClass0_0.x"
                  IL_0014:  ldloc.0
                  IL_0015:  ldarg.0
                  IL_0016:  stfld      "C C.<>c__DisplayClass0_0.<>4__this"
                  IL_001b:  ldarg.0
                  IL_001c:  ldloc.0
                  IL_001d:  ldftn      "int[] C.<>c__DisplayClass0_0.<.ctor>b__0()"
                  IL_0023:  newobj     "System.Func<int[]>..ctor(object, System.IntPtr)"
                  IL_0028:  stfld      "System.Func<int[]> C.F"
                  IL_002d:  ldarg.0
                  IL_002e:  call       "object..ctor()"
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void PrimaryConstructorParameters_03()
        {
            string source = """
                using System.Collections.Generic;
                class A(int[] x, List<int> y)
                {
                    public int[] X = x;
                    public List<int> Y = y;
                }
                class B(int x, int y, int z) : A([y, z], [z])
                {
                }
                class Program
                {
                    static void Main()
                    {
                        var b = new B(1, 2, 3);
                        b.X.Report();
                        b.Y.Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // 0.cs(7,13): warning CS9113: Parameter 'x' is unread.
                // class B(int x, int y, int z) : A([y, z], [z])
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(7, 13));

            var verifier = CompileAndVerify(comp, expectedOutput: "[2, 3], [3], ");
            verifier.VerifyIL("B..ctor(int, int, int)",
                """
                {
                  // Code size       33 (0x21)
                  .maxstack  5
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.2
                  IL_0002:  newarr     "int"
                  IL_0007:  dup
                  IL_0008:  ldc.i4.0
                  IL_0009:  ldarg.2
                  IL_000a:  stelem.i4
                  IL_000b:  dup
                  IL_000c:  ldc.i4.1
                  IL_000d:  ldarg.3
                  IL_000e:  stelem.i4
                  IL_000f:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0014:  dup
                  IL_0015:  ldarg.3
                  IL_0016:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_001b:  call       "A..ctor(int[], System.Collections.Generic.List<int>)"
                  IL_0020:  ret
                }
                """);
        }

        [Fact]
        public void SemanticModel()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                struct S1 : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S2
                {
                }
                class Program
                {
                    static void Main()
                    {
                        int[] v1 = [];
                        List<object> v2 = [];
                        Span<int> v3 = [];
                        ReadOnlySpan<object> v4 = [];
                        S1 v5 = [];
                        S2 v6 = [];
                        var v7 = (int[])[];
                        var v8 = (List<object>)[];
                        var v9 = (Span<int>)[];
                        var v10 = (ReadOnlySpan<object>)[];
                        var v11 = (S1)([]);
                        var v12 = (S2)([]);
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (20,17): error CS9174: Cannot initialize type 'S2' with a collection expression because the type is not constructible.
                //         S2 v6 = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("S2").WithLocation(20, 17),
                // (26,19): error CS9174: Cannot initialize type 'S2' with a collection expression because the type is not constructible.
                //         var v12 = (S2)([]);
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "(S2)([])").WithArguments("S2").WithLocation(26, 19));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
            Assert.Equal(12, collections.Length);
            VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: "System.Int32[]", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: "System.Collections.Generic.List<System.Object>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[2], expectedType: null, expectedConvertedType: "System.Span<System.Int32>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[3], expectedType: null, expectedConvertedType: "System.ReadOnlySpan<System.Object>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[4], expectedType: null, expectedConvertedType: "S1", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[5], expectedType: null, expectedConvertedType: "S2", ConversionKind.NoConversion);
            VerifyTypes(model, collections[6], expectedType: null, expectedConvertedType: "System.Int32[]", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[7], expectedType: null, expectedConvertedType: "System.Collections.Generic.List<System.Object>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[8], expectedType: null, expectedConvertedType: "System.Span<System.Int32>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[9], expectedType: null, expectedConvertedType: "System.ReadOnlySpan<System.Object>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[10], expectedType: null, expectedConvertedType: "S1", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[11], expectedType: null, expectedConvertedType: "S2", ConversionKind.NoConversion);
        }

        private static void VerifyTypes(SemanticModel model, ExpressionSyntax expr, string expectedType, string expectedConvertedType, ConversionKind expectedConversionKind)
        {
            var typeInfo = model.GetTypeInfo(expr);
            var conversion = model.GetConversion(expr);
            Assert.Equal(expectedType, typeInfo.Type?.ToTestDisplayString());
            Assert.Equal(expectedConvertedType, typeInfo.ConvertedType?.ToTestDisplayString());
            Assert.Equal(expectedConversionKind, conversion.Kind);
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = F0();
                        x.Report();
                        MyCollection<int> y = F1();
                        y.Report();
                        MyCollection<object> z = F2(3, 4);
                        z.Report();
                    }
                    static MyCollection<string> F0()
                    {
                        return [];
                    }
                    static MyCollection<int> F1()
                    {
                        return [0, 1, 2];
                    }
                    static MyCollection<object> F2(int x, object y)
                    {
                        return [x, y, null];
                    }
                }
                """;

            var verifier = CompileAndVerify(
                new[] { sourceB1, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [0, 1, 2], [3, 4, null], "));
            verifier.VerifyIL("Program.F0",
                """
                {
                  // Code size       16 (0x10)
                  .maxstack  1
                  IL_0000:  call       "string[] System.Array.Empty<string>()"
                  IL_0005:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
                  IL_000a:  call       "MyCollection<string> MyCollectionBuilder.Create<string>(System.ReadOnlySpan<string>)"
                  IL_000f:  ret
                }
                """);
            verifier.VerifyIL("Program.F1",
                """
                {
                  // Code size       52 (0x34)
                  .maxstack  2
                  .locals init (<>y__InlineArray3<int> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "<>y__InlineArray3<int>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "InlineArrayElementRef<<>y__InlineArray3<int>, int>(ref <>y__InlineArray3<int>, int)"
                  IL_0010:  ldc.i4.0
                  IL_0011:  stind.i4
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  ldc.i4.1
                  IL_0015:  call       "InlineArrayElementRef<<>y__InlineArray3<int>, int>(ref <>y__InlineArray3<int>, int)"
                  IL_001a:  ldc.i4.1
                  IL_001b:  stind.i4
                  IL_001c:  ldloca.s   V_0
                  IL_001e:  ldc.i4.2
                  IL_001f:  call       "InlineArrayElementRef<<>y__InlineArray3<int>, int>(ref <>y__InlineArray3<int>, int)"
                  IL_0024:  ldc.i4.2
                  IL_0025:  stind.i4
                  IL_0026:  ldloca.s   V_0
                  IL_0028:  ldc.i4.3
                  IL_0029:  call       "InlineArrayAsReadOnlySpan<<>y__InlineArray3<int>, int>(in <>y__InlineArray3<int>, int)"
                  IL_002e:  call       "MyCollection<int> MyCollectionBuilder.Create<int>(System.ReadOnlySpan<int>)"
                  IL_0033:  ret
                }
                """);
            verifier.VerifyIL("Program.F2",
                """
                {
                  // Code size       57 (0x39)
                  .maxstack  2
                  .locals init (<>y__InlineArray3<object> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "<>y__InlineArray3<object>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "InlineArrayElementRef<<>y__InlineArray3<object>, object>(ref <>y__InlineArray3<object>, int)"
                  IL_0010:  ldarg.0
                  IL_0011:  box        "int"
                  IL_0016:  stind.ref
                  IL_0017:  ldloca.s   V_0
                  IL_0019:  ldc.i4.1
                  IL_001a:  call       "InlineArrayElementRef<<>y__InlineArray3<object>, object>(ref <>y__InlineArray3<object>, int)"
                  IL_001f:  ldarg.1
                  IL_0020:  stind.ref
                  IL_0021:  ldloca.s   V_0
                  IL_0023:  ldc.i4.2
                  IL_0024:  call       "InlineArrayElementRef<<>y__InlineArray3<object>, object>(ref <>y__InlineArray3<object>, int)"
                  IL_0029:  ldnull
                  IL_002a:  stind.ref
                  IL_002b:  ldloca.s   V_0
                  IL_002d:  ldc.i4.3
                  IL_002e:  call       "InlineArrayAsReadOnlySpan<<>y__InlineArray3<object>, object>(in <>y__InlineArray3<object>, int)"
                  IL_0033:  call       "MyCollection<object> MyCollectionBuilder.Create<object>(System.ReadOnlySpan<object>)"
                  IL_0038:  ret
                }
                """);

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> c = F2([1, 2]);
                        c.Report();
                    }
                    static MyCollection<object> F2(MyCollection<object> c)
                    {
                        return [..c, 3];
                    }
                }
                """;

            verifier = CompileAndVerify(
                new[] { sourceB2, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[1, 2, 3], "));
            verifier.VerifyIL("Program.F2",
                """
                {
                  // Code size       79 (0x4f)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<object> V_0,
                                System.Collections.Generic.IEnumerator<object> V_1,
                                object V_2)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarga.s   V_0
                  IL_0008:  call       "System.Collections.Generic.IEnumerator<object> MyCollection<object>.GetEnumerator()"
                  IL_000d:  stloc.1
                  .try
                  {
                    IL_000e:  br.s       IL_001e
                    IL_0010:  ldloc.1
                    IL_0011:  callvirt   "object System.Collections.Generic.IEnumerator<object>.Current.get"
                    IL_0016:  stloc.2
                    IL_0017:  ldloc.0
                    IL_0018:  ldloc.2
                    IL_0019:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                    IL_001e:  ldloc.1
                    IL_001f:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_0024:  brtrue.s   IL_0010
                    IL_0026:  leave.s    IL_0032
                  }
                  finally
                  {
                    IL_0028:  ldloc.1
                    IL_0029:  brfalse.s  IL_0031
                    IL_002b:  ldloc.1
                    IL_002c:  callvirt   "void System.IDisposable.Dispose()"
                    IL_0031:  endfinally
                  }
                  IL_0032:  ldloc.0
                  IL_0033:  ldc.i4.3
                  IL_0034:  box        "int"
                  IL_0039:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_003e:  ldloc.0
                  IL_003f:  callvirt   "object[] System.Collections.Generic.List<object>.ToArray()"
                  IL_0044:  newobj     "System.ReadOnlySpan<object>..ctor(object[])"
                  IL_0049:  call       "MyCollection<object> MyCollectionBuilder.Create<object>(System.ReadOnlySpan<object>)"
                  IL_004e:  ret
                }
                """);
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_02A(
            [CombinatorialValues(TargetFramework.Net70, TargetFramework.Net80)] TargetFramework targetFramework,
            bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: targetFramework);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        var x = F();
                        x.Report();
                    }
                    static MyCollection<int?> F()
                    {
                        return [1, 2, null];
                    }
                }
                """;
            comp = CreateCompilation(new[] { sourceB, s_collectionExtensions }, references: new[] { refA }, targetFramework: targetFramework, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics();

            var verifier = CompileAndVerify(
                comp,
                symbolValidator: module =>
                {
                    var type = module.GlobalNamespace.GetTypeMembers("<>y__InlineArray3").SingleOrDefault();
                    if (targetFramework == TargetFramework.Net80)
                    {
                        Assert.NotNull(type);
                    }
                    else
                    {
                        Assert.Null(type);
                    }
                },
                verify: targetFramework == TargetFramework.Net80 ? Verification.Fails : Verification.FailsPEVerify,
                expectedOutput: IncludeExpectedOutput("[1, 2, null], "));
            if (targetFramework == TargetFramework.Net80)
            {
                verifier.VerifyIL("Program.F",
                    """
                    {
                      // Code size       74 (0x4a)
                      .maxstack  2
                      .locals init (<>y__InlineArray3<int?> V_0)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  initobj    "<>y__InlineArray3<int?>"
                      IL_0008:  ldloca.s   V_0
                      IL_000a:  ldc.i4.0
                      IL_000b:  call       "InlineArrayElementRef<<>y__InlineArray3<int?>, int?>(ref <>y__InlineArray3<int?>, int)"
                      IL_0010:  ldc.i4.1
                      IL_0011:  newobj     "int?..ctor(int)"
                      IL_0016:  stobj      "int?"
                      IL_001b:  ldloca.s   V_0
                      IL_001d:  ldc.i4.1
                      IL_001e:  call       "InlineArrayElementRef<<>y__InlineArray3<int?>, int?>(ref <>y__InlineArray3<int?>, int)"
                      IL_0023:  ldc.i4.2
                      IL_0024:  newobj     "int?..ctor(int)"
                      IL_0029:  stobj      "int?"
                      IL_002e:  ldloca.s   V_0
                      IL_0030:  ldc.i4.2
                      IL_0031:  call       "InlineArrayElementRef<<>y__InlineArray3<int?>, int?>(ref <>y__InlineArray3<int?>, int)"
                      IL_0036:  initobj    "int?"
                      IL_003c:  ldloca.s   V_0
                      IL_003e:  ldc.i4.3
                      IL_003f:  call       "InlineArrayAsReadOnlySpan<<>y__InlineArray3<int?>, int?>(in <>y__InlineArray3<int?>, int)"
                      IL_0044:  call       "MyCollection<int?> MyCollectionBuilder.Create<int?>(System.ReadOnlySpan<int?>)"
                      IL_0049:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.F",
                    """
                    {
                      // Code size       43 (0x2b)
                      .maxstack  4
                      IL_0000:  ldc.i4.3
                      IL_0001:  newarr     "int?"
                      IL_0006:  dup
                      IL_0007:  ldc.i4.0
                      IL_0008:  ldc.i4.1
                      IL_0009:  newobj     "int?..ctor(int)"
                      IL_000e:  stelem     "int?"
                      IL_0013:  dup
                      IL_0014:  ldc.i4.1
                      IL_0015:  ldc.i4.2
                      IL_0016:  newobj     "int?..ctor(int)"
                      IL_001b:  stelem     "int?"
                      IL_0020:  newobj     "System.ReadOnlySpan<int?>..ctor(int?[])"
                      IL_0025:  call       "MyCollection<int?> MyCollectionBuilder.Create<int?>(System.ReadOnlySpan<int?>)"
                      IL_002a:  ret
                    }
                    """);
            }
        }

        // As above, but with TargetFramework.NetFramework.
        [ConditionalFact(typeof(DesktopOnly))]
        public void CollectionBuilder_02B()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        var list = new List<T>();
                        foreach (var i in items) list.Add(i);
                        return new MyCollection<T>(list);
                    }
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        var x = F();
                        x.Report();
                    }
                    static MyCollection<int?> F()
                    {
                        return [1, 2, null];
                    }
                }
                """;
            var comp = CreateCompilationWithSpanAndMemoryExtensions(
                new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition, s_collectionExtensions },
                targetFramework: TargetFramework.NetFramework,
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics();

            var verifier = CompileAndVerify(
                comp,
                symbolValidator: module =>
                {
                    var type = module.GlobalNamespace.GetTypeMembers("<>y__InlineArray3").SingleOrDefault();
                    Assert.Null(type);
                },
                expectedOutput: "[1, 2, null], ");
            verifier.VerifyIL("Program.F",
                """
                {
                  // Code size       43 (0x2b)
                  .maxstack  4
                  IL_0000:  ldc.i4.3
                  IL_0001:  newarr     "int?"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  newobj     "int?..ctor(int)"
                  IL_000e:  stelem     "int?"
                  IL_0013:  dup
                  IL_0014:  ldc.i4.1
                  IL_0015:  ldc.i4.2
                  IL_0016:  newobj     "int?..ctor(int)"
                  IL_001b:  stelem     "int?"
                  IL_0020:  newobj     "System.ReadOnlySpan<int?>..ctor(int?[])"
                  IL_0025:  call       "MyCollection<int?> MyCollectionBuilder.Create<int?>(System.ReadOnlySpan<int?>)"
                  IL_002a:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder_InlineArrayTypes()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                class A
                {
                    static void M()
                    {
                        MyCollection<object> x;
                        x = [];
                        x = [null, null];
                        x = [1, 2, 3];
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            CompileAndVerify(
                comp,
                symbolValidator: module =>
                {
                    AssertEx.Equal(new[] { "<>y__InlineArray2", "<>y__InlineArray3" }, getInlineArrayTypeNames(module));
                },
                verify: Verification.Skipped);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                class B
                {
                    static void M<T>(MyCollection<T> c)
                    {
                    }
                    static void M1()
                    {
                        M([1]);
                    }
                    static void M2()
                    {
                        M([4, 5, 6]);
                        M(["a"]);
                        M(["b"]);
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            CompileAndVerify(
                comp,
                symbolValidator: module =>
                {
                    AssertEx.Equal(new[] { "<>y__InlineArray1", "<>y__InlineArray3" }, getInlineArrayTypeNames(module));
                },
                verify: Verification.Skipped);

            const int n = 1025;
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append(i);
            }
            string sourceC = $$"""
                using System;
                using System.Linq;
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c = [{{builder.ToString()}}];
                        Console.WriteLine(c.Count());
                    }
                }
                """;
            comp = CreateCompilation(sourceC, references: new[] { refA }, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                symbolValidator: module =>
                {
                    AssertEx.Equal(new[] { $"<>y__InlineArray{n}" }, getInlineArrayTypeNames(module));
                },
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput($"{n}"));

            static ImmutableArray<string> getInlineArrayTypeNames(ModuleSymbol module)
            {
                return module.GlobalNamespace.GetTypeMembers().WhereAsArray(t => t.Name.StartsWith("<>y__InlineArray")).SelectAsArray(t => t.Name);
            }
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_RefStructCollection(bool useCompilationReference, bool useScoped)
        {
            string qualifier = useScoped ? "scoped " : "";
            string sourceA = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public ref struct MyCollection<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                }
                public static class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>({{qualifier}}ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System;
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> c = F();
                        foreach (var i in c)
                            Console.Write("{0}, ", i);
                    }
                    static MyCollection<object> F()
                    {
                        return [1, 2, 3];
                    }
                }
                """;

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80, verify: Verification.Fails, expectedOutput: IncludeExpectedOutput("1, 2, 3, "));
            if (useScoped)
            {
                verifier.VerifyIL("Program.F",
                    $$"""
                    {
                      // Code size       67 (0x43)
                      .maxstack  2
                      .locals init (<>y__InlineArray3<object> V_0)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  initobj    "<>y__InlineArray3<object>"
                      IL_0008:  ldloca.s   V_0
                      IL_000a:  ldc.i4.0
                      IL_000b:  call       "InlineArrayElementRef<<>y__InlineArray3<object>, object>(ref <>y__InlineArray3<object>, int)"
                      IL_0010:  ldc.i4.1
                      IL_0011:  box        "int"
                      IL_0016:  stind.ref
                      IL_0017:  ldloca.s   V_0
                      IL_0019:  ldc.i4.1
                      IL_001a:  call       "InlineArrayElementRef<<>y__InlineArray3<object>, object>(ref <>y__InlineArray3<object>, int)"
                      IL_001f:  ldc.i4.2
                      IL_0020:  box        "int"
                      IL_0025:  stind.ref
                      IL_0026:  ldloca.s   V_0
                      IL_0028:  ldc.i4.2
                      IL_0029:  call       "InlineArrayElementRef<<>y__InlineArray3<object>, object>(ref <>y__InlineArray3<object>, int)"
                      IL_002e:  ldc.i4.3
                      IL_002f:  box        "int"
                      IL_0034:  stind.ref
                      IL_0035:  ldloca.s   V_0
                      IL_0037:  ldc.i4.3
                      IL_0038:  call       "InlineArrayAsReadOnlySpan<<>y__InlineArray3<object>, object>(in <>y__InlineArray3<object>, int)"
                      IL_003d:  call       "MyCollection<object> MyCollectionBuilder.Create<object>(scoped System.ReadOnlySpan<object>)"
                      IL_0042:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.F",
                    $$"""
                        {
                      // Code size       44 (0x2c)
                      .maxstack  4
                      IL_0000:  ldc.i4.3
                      IL_0001:  newarr     "object"
                      IL_0006:  dup
                      IL_0007:  ldc.i4.0
                      IL_0008:  ldc.i4.1
                      IL_0009:  box        "int"
                      IL_000e:  stelem.ref
                      IL_000f:  dup
                      IL_0010:  ldc.i4.1
                      IL_0011:  ldc.i4.2
                      IL_0012:  box        "int"
                      IL_0017:  stelem.ref
                      IL_0018:  dup
                      IL_0019:  ldc.i4.2
                      IL_001a:  ldc.i4.3
                      IL_001b:  box        "int"
                      IL_0020:  stelem.ref
                      IL_0021:  newobj     "System.ReadOnlySpan<object>..ctor(object[])"
                      IL_0026:  call       "MyCollection<object> MyCollectionBuilder.Create<object>(System.ReadOnlySpan<object>)"
                      IL_002b:  ret
                    }
                    """);
            }
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_NonGenericCollection(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public sealed class MyCollection : IEnumerable<int>
                {
                    private readonly List<int> _list;
                    public MyCollection(List<int> list) { _list = list; }
                    public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection Create(ReadOnlySpan<int> items) =>
                        new MyCollection(new List<int>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        x.Report();
                        MyCollection y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InterfaceCollection_ReturnInterface(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public interface IMyCollection<T> : IEnumerable<T>
                {
                }
                public sealed class MyCollectionBuilder
                {
                    public static IMyCollection<T> Create<T>(ReadOnlySpan<T> items) =>
                        new MyCollection<T>(new List<T>(items.ToArray()));
                    public sealed class MyCollection<T> : IMyCollection<T>
                    {
                        private readonly List<T> _list;
                        public MyCollection(List<T> list) { _list = list; }
                        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        IMyCollection<string> x = [];
                        x.Report(includeType: true);
                        IMyCollection<int> y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(MyCollectionBuilder.MyCollection<System.String>) [], (MyCollectionBuilder.MyCollection<System.Int32>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InterfaceCollection_ReturnImplementation(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public interface IMyCollection<T> : IEnumerable<T>
                {
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) =>
                        new MyCollection<T>(new List<T>(items.ToArray()));
                    public sealed class MyCollection<T> : IMyCollection<T>
                    {
                        private readonly List<T> _list;
                        public MyCollection(List<T> list) { _list = list; }
                        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        IMyCollection<string> x = [];
                        IMyCollection<int> y = [1, 2, 3];
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (5,35): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'IMyCollection<T>'.
                //         IMyCollection<string> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "IMyCollection<T>").WithLocation(5, 35),
                // (6,32): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'IMyCollection<T>'.
                //         IMyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "T", "IMyCollection<T>").WithLocation(6, 32));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_NestedCollectionAndBuilder(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public class Container
                {
                    [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                    public sealed class MyCollection<T> : IEnumerable<T>
                    {
                        private readonly List<T> _list;
                        public MyCollection(List<T> list) { _list = list; }
                        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                    public sealed class MyCollectionBuilder
                    {
                        public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                            => new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Container.MyCollection<string> x = [];
                        x.Report(includeType: true);
                        Container.MyCollection<int> y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(Container.MyCollection<System.String>) [], (Container.MyCollection<System.Int32>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_NoElementType(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T>
                {
                    public MyCollection(T[] array) { }
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<string> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,34): error CS9188: 'MyCollection<object>' has a CollectionBuilderAttribute but no element type.
                //         MyCollection<object> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<object>").WithLocation(6, 34),
                // (7,31): error CS9188: 'MyCollection<int>' has a CollectionBuilderAttribute but no element type.
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[1, 2, 3]").WithArguments("MyCollection<int>").WithLocation(7, 31));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ElementTypeFromPattern_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T>
                {
                    private readonly T[] _array;
                    public MyCollection(T[] array) { _array = array; }
                    public MyEnumerator<T> GetEnumerator()
                        => new MyEnumerator<T>(_array);
                }
                public struct MyEnumerator<T>
                {
                    private readonly T[] _array;
                    private int _index;
                    public MyEnumerator(T[] array)
                    {
                        _array = array;
                        _index = -1;
                    }
                    public bool MoveNext()
                    {
                        if (_index < _array.Length) _index++;
                        return _index < _array.Length;
                    }
                    public T Current => _array[_index];
                }
                public struct MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                        => new MyCollection<T>(items.ToArray());
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        GetElements(x).Report();
                        MyCollection<int> y = [1, 2, 3];
                        GetElements(y).Report();
                    }
                    static IEnumerable<T> GetElements<T>(MyCollection<T> c)
                    {
                        foreach (var e in c) yield return e;
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ElementTypeFromPattern_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection
                {
                    private readonly object[] _array;
                    public MyCollection(object[] array) { _array = array; }
                    public MyEnumerator GetEnumerator()
                        => new MyEnumerator(_array);
                }
                public struct MyEnumerator
                {
                    private readonly object[] _array;
                    private int _index;
                    public MyEnumerator(object[] array)
                    {
                        _array = array;
                        _index = -1;
                    }
                    public bool MoveNext()
                    {
                        if (_index < _array.Length) _index++;
                        return _index < _array.Length;
                    }
                    public object Current => _array[_index];
                }
                public struct MyCollectionBuilder
                {
                    public static MyCollection Create(ReadOnlySpan<object> items)
                        => new MyCollection(items.ToArray());
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System.Collections;
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        GetElements(x).Report();
                        MyCollection y = [1, 2, 3];
                        GetElements(y).Report();
                    }
                    static IEnumerable GetElements(MyCollection c)
                    {
                        foreach (var e in c) yield return e;
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ObjectElementType_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection : IEnumerable
                {
                    private readonly object[] _array;
                    public MyCollection(object[] array) { _array = array; }
                    IEnumerator IEnumerable.GetEnumerator() => _array.GetEnumerator();
                }
                public struct MyCollectionBuilder
                {
                    public static MyCollection Create(ReadOnlySpan<object> items)
                        => new MyCollection(items.ToArray());
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        x.Report();
                        MyCollection y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ObjectElementType_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable
                {
                    public MyCollection(T[] array) { }
                    public IEnumerator GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<string> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<object>' and return type 'MyCollection<T>'.
                //         MyCollection<object> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "object", "MyCollection<T>").WithLocation(6, 34),
                // (7,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<object>' and return type 'MyCollection<T>'.
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "object", "MyCollection<T>").WithLocation(7, 31));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ConstructedElementType(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public sealed class E<T>
                {
                    private readonly T _t;
                    public E(T t) { _t = t; }
                    public override string ToString() => $"E({_t})";
                }
                [CollectionBuilder(typeof(Builder), "Create")]
                public sealed class C<T> : IEnumerable<E<T>>
                {
                    private readonly List<E<T>> _list;
                    public C(List<E<T>> list) { _list = list; }
                    public IEnumerator<E<T>> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class Builder
                {
                    public static C<T> Create<T>(ReadOnlySpan<E<T>> items)
                        => new C<T>(new List<E<T>>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        C<string> x = [null];
                        x.Report(includeType: true);
                        C<int> y = [new E<int>(1), default];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(C<System.String>) [null], (C<System.Int32>) [E(1), null], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_Dictionary(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyDictionaryBuilder), "Create")]
                public class MyImmutableDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private readonly Dictionary<K, V> _d;
                    public MyImmutableDictionary(ReadOnlySpan<KeyValuePair<K, V>> items)
                    {
                        _d = new();
                        foreach (var (k, v) in items) _d.Add(k, v);
                    }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyDictionaryBuilder
                {
                    public static MyImmutableDictionary<K, V> Create<K, V>(ReadOnlySpan<KeyValuePair<K, V>> items)
                        => new MyImmutableDictionary<K, V>(items);
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        MyImmutableDictionary<string, object> x = [];
                        x.Report();
                        MyImmutableDictionary<string, int> y = [KeyValuePair.Create("one", 1), KeyValuePair.Create("two", 2)];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [[one, 1], [two, 2]], "));
        }

        [Fact]
        public void CollectionBuilder_MissingBuilderType()
        {
            string sourceA = """
                public class MyCollectionBuilder
                {
                }
                """;
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net80);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                """;
            comp = CreateCompilation(new[] { sourceB, CollectionBuilderAttributeDefinition }, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            var refB = comp.EmitToImageReference();

            string sourceC = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceC, references: new[] { refB }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_MissingBuilderMethod(bool useCompilationReference)
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_NullBuilderType()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(null, "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(4,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(null, "Create")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(4, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_NullBuilderType_FromMetadata()
        {
            // [CollectionBuilder(null, "Create")]
            string sourceA = """
                .class public sealed System.Runtime.CompilerServices.CollectionBuilderAttribute extends [mscorlib]System.Attribute
                {
                  .method public hidebysig specialname rtspecialname instance void .ctor(class [mscorlib]System.Type builderType, string methodName) cil managed { ret }
                }
                .class public sealed MyCollection`1<T>
                {
                  .custom instance void System.Runtime.CompilerServices.CollectionBuilderAttribute::.ctor(class [mscorlib]System.Type, string) = { type(nullref) string('Create') }
                  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
                  .method public instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!T> GetEnumerator() { ldnull ret }
                }
                """;
            var refA = CompileIL(sourceA);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_InvalidBuilderType_Interface()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public interface MyCollectionBuilder
                {
                    MyCollection<T> Create<T>(ReadOnlySpan<T> items);
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(5, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_InvalidBuilderType_Interface_FromMetadata()
        {
            // [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
            // class MyCollection<T> { ... }
            // interface MyCollectionBuilder { ... }
            string sourceA = """
                .class public sealed System.ReadOnlySpan`1<T> extends [mscorlib]System.ValueType
                {
                }
                .class public sealed System.Runtime.CompilerServices.CollectionBuilderAttribute extends [mscorlib]System.Attribute
                {
                  .method public hidebysig specialname rtspecialname instance void .ctor(class [mscorlib]System.Type builderType, string methodName) cil managed { ret }
                }
                .class public sealed MyCollection`1<T>
                {
                  .custom instance void System.Runtime.CompilerServices.CollectionBuilderAttribute::.ctor(class [mscorlib]System.Type, string) = { type(MyCollectionBuilder) string('Create') }
                  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
                  .method public instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!T> GetEnumerator() { ldnull ret }
                }
                .class interface public abstract MyCollectionBuilder
                {
                  .method public abstract virtual instance class MyCollection`1<!!T> Create<T>(valuetype System.ReadOnlySpan`1<!!T> items) { }
                }
                """;
            var refA = CompileIL(sourceA);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InvalidBuilderType_03(
            [CombinatorialValues("public delegate void MyCollectionBuilder();", "public enum MyCollectionBuilder { }")] string builderTypeDefinition)
        {
            string sourceA = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "ToString")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                {{builderTypeDefinition}}
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(typeof(MyCollectionBuilder), "ToString")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(5, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible 'ToString' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("ToString", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible 'ToString' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("ToString", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InvalidBuilderType_04(
            [CombinatorialValues("int[]", "int*", "(object, object)")] string builderTypeName)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof({{builderTypeName}}), "ToString")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(4,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(typeof(int*), "ToString")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(4, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible 'ToString' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("ToString", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible 'ToString' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("ToString", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_InvalidBuilderType_TypeParameter()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                struct Container<T>
                {
                    [CollectionBuilder(typeof(T), "ToString")]
                    public struct MyCollection : IEnumerable<int>
                    {
                        IEnumerator<int> IEnumerable<int>.GetEnumerator() => default;
                        IEnumerator IEnumerable.GetEnumerator() => default;
                    }
                }
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        Container<int>.MyCollection x = [];
                        Container<string>.MyCollection y = [null];
                        Container<object>.MyCollection z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(6,24): error CS0416: 'T': an attribute argument cannot use type parameters
                //     [CollectionBuilder(typeof(T), "ToString")]
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(T)").WithArguments("T").WithLocation(6, 24),
                // 0.cs(19,45): error CS1061: 'Container<string>.MyCollection' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'Container<string>.MyCollection' could be found (are you missing a using directive or an assembly reference?)
                //         Container<string>.MyCollection y = [null];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "null").WithArguments("Container<string>.MyCollection", "Add").WithLocation(19, 45));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_NullOrEmptyMethodName([CombinatorialValues("null", "\"\"")] string methodName)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), {{methodName}})]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(4,2): error CS9186: The CollectionBuilderAttribute method name is invalid.
                // [CollectionBuilder(typeof(MyCollectionBuilder), "")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidMethodName, "CollectionBuilder").WithLocation(4, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible '' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible '' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_NullOrEmptyMethodName_FromMetadata([CombinatorialValues("nullref", "''")] string methodName)
        {
            // [CollectionBuilder(typeof(MyCollectionBuilder), "")]
            string sourceA = $$"""
                .class public sealed System.ReadOnlySpan`1<T> extends [mscorlib]System.ValueType
                {
                }
                .class public sealed System.Runtime.CompilerServices.CollectionBuilderAttribute extends [mscorlib]System.Attribute
                {
                  .method public hidebysig specialname rtspecialname instance void .ctor(class [mscorlib]System.Type builderType, string methodName) cil managed { ret }
                }
                .class public sealed MyCollection`1<T>
                {
                  .custom instance void System.Runtime.CompilerServices.CollectionBuilderAttribute::.ctor(class [mscorlib]System.Type, string) = { type(MyCollectionBuilder) string({{methodName}}) }
                  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
                  .method public instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!T> GetEnumerator() { ldnull ret }
                }
                .class public sealed MyCollectionBuilder
                {
                  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
                  .method public static class MyCollection`1<!!T> Create<T>(valuetype System.ReadOnlySpan`1<!!T> items) { ldnull ret }
                }
                """;
            var refA = CompileIL(sourceA);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible '' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible '' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InstanceMethod(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    public MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_OtherMember_01(
            [CombinatorialValues(
                "public MyCollection Create = null;",
                "public MyCollection Create => null;",
                "public class Create { }")]
            string createMember,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                using System;
                using System.Collections;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public class MyCollection : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                        {{createMember}}
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        MyCollection y = [null];
                        MyCollection z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<object>' and return type 'MyCollection'.
                //         MyCollection x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "object", "MyCollection").WithLocation(6, 26),
                // (7,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<object>' and return type 'MyCollection'.
                //         MyCollection y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "object", "MyCollection").WithLocation(7, 26));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_TypeDifferences_Dynamic_01(bool useCompilationReference)
        {
            CollectionBuilder_TypeDifferences("object", "dynamic", "1, 2, 3", "[1, 2, 3]", useCompilationReference);
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_TypeDifferences_Dynamic_02(bool useCompilationReference)
        {
            string sourceA = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection
                {
                    private readonly List<dynamic> _list;
                    public MyCollection(List<dynamic> list) { _list = list; }
                    public IEnumerator<dynamic> GetEnumerator() => _list.GetEnumerator();
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection Create(ReadOnlySpan<object> items)
                        => new MyCollection(new List<dynamic>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System.Collections.Generic;
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        GetElements(x).Report();
                        MyCollection y = [1, 2, 3];
                        GetElements(y).Report();
                    }
                    static IEnumerable<object> GetElements(MyCollection c)
                    {
                        foreach (var e in c) yield return e;
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput($"[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_TypeDifferences_TupleElementNames(bool useCompilationReference)
        {
            CollectionBuilder_TypeDifferences("(int, int)", "(int A, int B)", "(1, 2), default", "[(1, 2), (0, 0)]", useCompilationReference);
            CollectionBuilder_TypeDifferences("(int A, int B)", "(int, int)", "(1, 2), default", "[(1, 2), (0, 0)]", useCompilationReference);
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_TypeDifferences_Nullability(bool useCompilationReference)
        {
            CollectionBuilder_TypeDifferences("object", "object?", "1, 2, 3", "[1, 2, 3]", useCompilationReference);
            CollectionBuilder_TypeDifferences("object?", "object", "1, null, 3", "[1, null, 3]", useCompilationReference);
        }

        private void CollectionBuilder_TypeDifferences(string collectionElementType, string builderElementType, string values, string expectedOutput, bool useCompilationReference)
        {
            string sourceA = $$"""
                #nullable enable
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection : IEnumerable<{{collectionElementType}}>
                {
                    private readonly List<{{collectionElementType}}> _list;
                    public MyCollection(List<{{collectionElementType}}> list) { _list = list; }
                    public IEnumerator<{{collectionElementType}}> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection Create(ReadOnlySpan<{{builderElementType}}> items)
                        => new MyCollection(new List<{{collectionElementType}}>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                #nullable enable
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        x.Report();
                        MyCollection y = [{{values}}];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput($"[], {expectedOutput}, "));
        }

        // If there are multiple attributes, the first is used.
        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_MultipleAttributes(bool useCompilationReference)
        {
            string sourceAttribute = """
                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
                    public sealed class CollectionBuilderAttribute : Attribute
                    {
                        public CollectionBuilderAttribute(Type builderType, string methodName) { }
                    }
                }
                """;
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder1), "Create1")]
                [CollectionBuilder(typeof(MyCollectionBuilder2), "Create2")]
                public sealed class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public struct MyCollectionBuilder1
                {
                    public static MyCollection<T> Create1<T>(ReadOnlySpan<T> items)
                        => new MyCollection<T>(new List<T>(items.ToArray()));
                }
                public struct MyCollectionBuilder2
                {
                    public static MyCollection<T> Create2<T>(ReadOnlySpan<T> items)
                        => throw null;
                }
                """;
            var comp = CreateCompilation(new[] { sourceAttribute, sourceA }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static MyCollection<int> F() => [1, 2, 3];
                    static void Main()
                    {
                        F().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[1, 2, 3], "));
            comp = (CSharpCompilation)verifier.Compilation;

            var collectionType = (NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F").ReturnType;
            Assert.Equal("MyCollection<System.Int32>", collectionType.ToTestDisplayString());
            TypeSymbol builderType;
            string methodName;
            Assert.True(collectionType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.Equal("MyCollectionBuilder1", builderType.ToTestDisplayString());
            Assert.Equal("Create1", methodName);
        }

        [Fact]
        public void CollectionBuilder_GenericBuilderType_01()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder<>), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public sealed class MyCollectionBuilder<T>
                {
                    public static MyCollection<T> Create(ReadOnlySpan<T> items) => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(typeof(MyCollectionBuilder<>), "Create")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(5, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_GenericBuilderType_02()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder<int>), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public sealed class MyCollectionBuilder<T>
                {
                    public static MyCollection<U> Create<U>(ReadOnlySpan<U> items) => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(typeof(MyCollectionBuilder<int>), "Create")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(5, 2),
                // 1.cs(6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // 1.cs(7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_GenericBuilderType_03()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public class Container<T>
                {
                    [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                    public struct MyCollection : IEnumerable<T>
                    {
                        private readonly List<T> _list;
                        public MyCollection(List<T> list) { _list = list; }
                        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                    public sealed class MyCollectionBuilder
                    {
                        public static MyCollection Create(ReadOnlySpan<T> items)
                            => new MyCollection(new List<T>(items.ToArray()));
                    }
                }
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        Container<string>.MyCollection x = [];
                        Container<int>.MyCollection y = [default];
                        Container<object>.MyCollection z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(7,24): error CS0416: 'Container<T>.MyCollectionBuilder': an attribute argument cannot use type parameters
                //     [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(MyCollectionBuilder)").WithArguments("Container<T>.MyCollectionBuilder").WithLocation(7, 24),
                // 0.cs(27,42): error CS1061: 'Container<int>.MyCollection' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'Container<int>.MyCollection' could be found (are you missing a using directive or an assembly reference?)
                //         Container<int>.MyCollection y = [default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "default").WithArguments("Container<int>.MyCollection", "Add").WithLocation(27, 42));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_GenericCollectionContainerType_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public class Container<T>
                {
                    [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                    public struct MyCollection : IEnumerable<T>
                    {
                        private readonly List<T> _list;
                        public MyCollection(List<T> list) { _list = list; }
                        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                }
                public sealed class MyCollectionBuilder
                {
                    public static Container<T>.MyCollection Create<T>(ReadOnlySpan<T> items)
                        => new Container<T>.MyCollection(new List<T>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Container<string>.MyCollection x = [];
                        x.Report(includeType: true);
                        Container<int>.MyCollection y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(Container<T>.MyCollection<System.String>) [], (Container<T>.MyCollection<System.Int32>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_GenericCollectionContainerType_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public class Container<T>
                {
                    [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                    public struct MyCollection : IEnumerable<int>
                    {
                        private readonly List<int> _list;
                        public MyCollection(List<int> list) { _list = list; }
                        public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                }
                public sealed class MyCollectionBuilder
                {
                    public static Container<T>.MyCollection Create<T>(ReadOnlySpan<int> items)
                        => new Container<T>.MyCollection(new List<int>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Container<int>.MyCollection x = [];
                        x.Report(includeType: true);
                        Container<string>.MyCollection y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(Container<T>.MyCollection<System.Int32>) [], (Container<T>.MyCollection<System.String>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_GenericCollectionContainerType_03(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public class Container<T>
                {
                    [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                    public struct MyCollection<U> : IEnumerable<U>
                    {
                        private readonly List<U> _list;
                        public MyCollection(List<U> list) { _list = list; }
                        public IEnumerator<U> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                }
                public sealed class MyCollectionBuilder
                {
                    public static Container<T>.MyCollection<U> Create<T, U>(ReadOnlySpan<U> items)
                        => new Container<T>.MyCollection<U>(new List<U>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Container<int>.MyCollection<string> x = [];
                        x.Report(includeType: true);
                        Container<string>.MyCollection<int> y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(Container<T>.MyCollection<System.Int32, System.String>) [], (Container<T>.MyCollection<System.String, System.Int32>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_GenericType_ElementTypeFirstOfTwo(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T, U> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection<T, U> Create<T, U>(ReadOnlySpan<T> items)
                        => new MyCollection<T, U>(new List<T>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string, int> x = [];
                        x.Report(includeType: true);
                        MyCollection<int, string> y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(MyCollection<System.String, System.Int32>) [], (MyCollection<System.Int32, System.String>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_GenericType_ElementTypeSecondOfTwo(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T, U> : IEnumerable<U>
                {
                    private readonly List<U> _list;
                    public MyCollection(List<U> list) { _list = list; }
                    public IEnumerator<U> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection<T, U> Create<T, U>(ReadOnlySpan<U> items)
                        => new MyCollection<T, U>(new List<U>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int, string> x = [];
                        x.Report(includeType: true);
                        MyCollection<string, int> y = [1, 2, 3];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("(MyCollection<System.Int32, System.String>) [], (MyCollection<System.String, System.Int32>) [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InaccessibleBuilderType_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                internal class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_NestedBuilderType(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public class MyCollection : IEnumerable<int>
                {
                    private readonly List<int> _list;
                    public MyCollection(List<int> list) { _list = list; }
                    public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public struct MyCollectionBuilder
                    {
                        public static MyCollection Create(ReadOnlySpan<int> items)
                            => new MyCollection(new List<int>(items.ToArray()));
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        x.Report();
                        MyCollection y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InaccessibleBuilderType_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public class MyCollection : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    protected class MyCollectionBuilder
                    {
                        public static MyCollection Create(ReadOnlySpan<int> items) => default;
                    }
                    static readonly MyCollection _instance = [1, 2, 3];
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        MyCollection y = [1, 2, 3];
                        MyCollection z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
                //         MyCollection x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "int", "MyCollection").WithLocation(6, 26),
                // (7,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
                //         MyCollection y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "int", "MyCollection").WithLocation(7, 26));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InaccessibleMethod(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    static readonly MyCollection<int> _instance = [1, 2, 3];
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    internal static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<string> y = [null];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 31),
                // (7,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> y = [null];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[null]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 34));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_Overloads_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>()
                    {
                        throw null;
                    }
                    public static MyCollection<T> Create<T>(Span<T> items)
                    {
                        throw null;
                    }
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, int index = 0)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        x.Report();
                        MyCollection<int> y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_Overloads_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                    public static MyCollection<int> Create(ReadOnlySpan<int> items)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        x.Report();
                        MyCollection<int> y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_UnexpectedSignature_01(
            [CombinatorialValues(
                "public static MyCollection<int> Create(ReadOnlySpan<int> items) => default;", // constructed parameter and return types
                "public static MyCollection<T> Create<T>(ReadOnlySpan<int> items) => default;", // constructed parameter type
                "public static MyCollection<int> Create<T>(ReadOnlySpan<T> items) => default;", // constructed return type
                "public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, int index = 0) => default;", // optional parameter
                "public static MyCollection<T> Create<T>() => default;", // no parameters
                "public static void Create<T>(ReadOnlySpan<T> items) { }", // no return type
                "public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, int index = 0) => default;", // optional parameter
                "public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, params object[] args) => default;", // params
                "public static MyCollection<T> Create<T, U>(ReadOnlySpan<T> items) => default;", // extra type parameter
                "public static MyCollection<T> Create<T>(Span<T> items) => default;", // Span<T>
                "public static MyCollection<T> Create<T>(T[] items) => default;", // T[]
                "public static MyCollection<T> Create<T>(in ReadOnlySpan<T> items) => default;", // in parameter
                "public static MyCollection<T> Create<T>(ref ReadOnlySpan<T> items) => default;", // ref parameter
                "public static MyCollection<T> Create<T>(out ReadOnlySpan<T> items) { items = default; return default; }")] // out parameter
            string methodDeclaration,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    {{methodDeclaration}}
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<string> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 34),
                // (7,31): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 31));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_UnexpectedSignature_MoreTypeParameters(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection : IEnumerable<object>
                {
                    IEnumerator<object> IEnumerable<object>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        MyCollection y = [1, 2, 3];
                        MyCollection z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<object>' and return type 'MyCollection'.
                //         MyCollection x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "object", "MyCollection").WithLocation(6, 26),
                // (7,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<object>' and return type 'MyCollection'.
                //         MyCollection y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "object", "MyCollection").WithLocation(7, 26));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_UnexpectedSignature_FewerTypeParameters(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T, U> : IEnumerable<T>
                {
                    public IEnumerator<T> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollection<T, int> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string, int> x = [];
                        MyCollection<int, string> y = [1, 2, 3];
                        MyCollection<int, object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,39): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T, U>'.
                //         MyCollection<string, int> x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T, U>").WithLocation(6, 39),
                // (7,39): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T, U>'.
                //         MyCollection<int, string> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "T", "MyCollection<T, U>").WithLocation(7, 39));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_InheritedAttributeOnBaseCollection(bool useCompilationReference)
        {
            string sourceAttribute = """
                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
                    public sealed class CollectionBuilderAttribute : Attribute
                    {
                        public CollectionBuilderAttribute(Type builderType, string methodName) { }
                    }
                }
                """;
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public abstract class MyCollectionBase : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                public sealed class MyCollection : MyCollectionBase
                {
                }
                public sealed class MyCollectionBuilder
                {
                    public static MyCollectionBase Create(ReadOnlySpan<int> items) => new MyCollection();
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceAttribute }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        MyCollection y = [2];
                        MyCollection z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,27): error CS1061: 'MyCollection' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyCollection' could be found (are you missing a using directive or an assembly reference?)
                //         MyCollection y = [2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "2").WithArguments("MyCollection", "Add").WithLocation(6, 27));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_CreateMethodOnBase(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public sealed class MyCollection : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                public abstract class MyCollectionBuilderBase
                {
                    public static MyCollection Create(ReadOnlySpan<int> items) => new MyCollection();
                }
                public sealed class MyCollectionBuilder : MyCollectionBuilderBase
                {
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection x = [];
                        MyCollection y = [1, 2, 3];
                        MyCollection z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (5,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
                //         MyCollection x = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "int", "MyCollection").WithLocation(5, 26),
                // (6,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
                //         MyCollection y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1, 2, 3]").WithArguments("Create", "int", "MyCollection").WithLocation(6, 26));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ObsoleteBuilderType_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                [Obsolete]
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,34): warning CS0612: 'MyCollectionBuilder' is obsolete
                //         MyCollection<string> x = [];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[]").WithArguments("MyCollectionBuilder").WithLocation(6, 34),
                // (7,31): warning CS0612: 'MyCollectionBuilder' is obsolete
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[1, 2, 3]").WithArguments("MyCollectionBuilder").WithLocation(7, 31));
        }

        [Fact]
        public void CollectionBuilder_ObsoleteBuilderType_02()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                [Obsolete("message 2", error: true)]
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,27): error CS0619: 'MyCollectionBuilder' is obsolete: 'message 2'
                // [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "MyCollectionBuilder").WithArguments("MyCollectionBuilder", "message 2").WithLocation(5, 27),
                // 1.cs(6,34): error CS0619: 'MyCollectionBuilder' is obsolete: 'message 2'
                //         MyCollection<string> x = [];
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[]").WithArguments("MyCollectionBuilder", "message 2").WithLocation(6, 34),
                // 1.cs(7,31): error CS0619: 'MyCollectionBuilder' is obsolete: 'message 2'
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[1, 2, 3]").WithArguments("MyCollectionBuilder", "message 2").WithLocation(7, 31));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ObsoleteBuilderMethod_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    [Obsolete]
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,34): warning CS0612: 'MyCollectionBuilder.Create<T>(ReadOnlySpan<T>)' is obsolete
                //         MyCollection<string> x = [];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[]").WithArguments("MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>)").WithLocation(6, 34),
                // (7,31): warning CS0612: 'MyCollectionBuilder.Create<T>(ReadOnlySpan<T>)' is obsolete
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[1, 2, 3]").WithArguments("MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>)").WithLocation(7, 31));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_ObsoleteBuilderMethod_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    [Obsolete("message 4", error: true)]
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,34): error CS0619: 'MyCollectionBuilder.Create<T>(ReadOnlySpan<T>)' is obsolete: 'message 4'
                //         MyCollection<string> x = [];
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[]").WithArguments("MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>)", "message 4").WithLocation(6, 34),
                // (7,31): error CS0619: 'MyCollectionBuilder.Create<T>(ReadOnlySpan<T>)' is obsolete: 'message 4'
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[1, 2, 3]").WithArguments("MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>)", "message 4").WithLocation(7, 31));
        }

        [Fact]
        public void CollectionBuilder_UnmanagedCallersOnly()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    [UnmanagedCallersOnly]
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<object> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 1.cs(6,34): error CS8901: 'MyCollectionBuilder.Create<string>(ReadOnlySpan<string>)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         MyCollection<string> x = [];
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "[]").WithArguments("MyCollectionBuilder.Create<string>(System.ReadOnlySpan<string>)").WithLocation(6, 34),
                // 1.cs(7,31): error CS8901: 'MyCollectionBuilder.Create<int>(ReadOnlySpan<int>)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "[1, 2, 3]").WithArguments("MyCollectionBuilder.Create<int>(System.ReadOnlySpan<int>)").WithLocation(7, 31),
                // 0.cs(14,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(14, 6),
                // 0.cs(15,45): error CS8894: Cannot use 'ReadOnlySpan<T>' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "ReadOnlySpan<T> items").WithArguments("System.ReadOnlySpan<T>", "parameter").WithLocation(15, 45));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_Constraints_CollectionAndBuilder(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T> where T : new()
                {
                    private List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) where T : struct
                        => new MyCollection<T>(new List<T>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        x.Report();
                        MyCollection<int> y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB1, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));

            string sourceB2 = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int?> x = [4, null];
                        MyCollection<int?> y = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB2, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,32): error CS0453: The type 'int?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyCollectionBuilder.Create<T>(ReadOnlySpan<T>)'
                //         MyCollection<int?> x = [4, null];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[4, null]").WithArguments("MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "int?").WithLocation(6, 32));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_Constraints_BuilderOnly(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) where T : struct
                        => new MyCollection<T>(new List<T>(items.ToArray()));
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        x.Report();
                        MyCollection<int> y = [1, 2, 3];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB1, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));

            string sourceB2 = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int?> x = [4, null];
                        MyCollection<int?> y = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB2, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,32): error CS0453: The type 'int?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyCollectionBuilder.Create<T>(ReadOnlySpan<T>)'
                //         MyCollection<int?> x = [4, null];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[4, null]").WithArguments("MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "int?").WithLocation(6, 32));
        }

        [Fact]
        public void CollectionBuilder_Constraints_CollectionOnly()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T> where T : class
                {
                    private List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                        => default;
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<string> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 1.cs(7,22): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyCollection<T>'
                //         MyCollection<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("MyCollection<T>", "T", "int").WithLocation(7, 22),
                // 0.cs(15,35): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyCollection<T>'
                //     public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "Create").WithArguments("MyCollection<T>", "T", "T").WithLocation(15, 35));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_Substituted_01(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    public IEnumerator<T> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        F([]);
                        F([1, 2, 3]);
                    }
                    static void F(MyCollection<int> c)
                    {
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();

            var collectionType = (NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F").Parameters[0].Type;
            Assert.Equal("MyCollection<System.Int32>", collectionType.ToTestDisplayString());
            TypeSymbol builderType;
            string methodName;
            Assert.True(collectionType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.Equal("MyCollectionBuilder", builderType.ToTestDisplayString());
            Assert.Equal("Create", methodName);

            var originalType = collectionType.OriginalDefinition;
            Assert.Equal("MyCollection<T>", originalType.ToTestDisplayString());
            Assert.True(originalType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.Equal("MyCollectionBuilder", builderType.ToTestDisplayString());
            Assert.Equal("Create", methodName);
        }

        [Fact]
        public void CollectionBuilder_Substituted_02()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(Container<string>.MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    public IEnumerator<T> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class Container<T>
                {
                    public class MyCollectionBuilder
                    {
                        public static MyCollection<U> Create<U>(ReadOnlySpan<U> items) => default;
                    }
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        F([]);
                        F(new());
                    }
                    static void F(MyCollection<int> c)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(5,2): error CS9185: The CollectionBuilderAttribute builder type must be a non-generic class or struct.
                // [CollectionBuilder(typeof(Container<string>.MyCollectionBuilder), "Create")]
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, "CollectionBuilder").WithLocation(5, 2),
                // 1.cs(6,11): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         F([]);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 11));

            var collectionType = (NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F").Parameters[0].Type;
            Assert.Equal("MyCollection<System.Int32>", collectionType.ToTestDisplayString());
            TypeSymbol builderType;
            string methodName;
            Assert.True(collectionType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.Equal("Container<System.String>.MyCollectionBuilder", builderType.ToTestDisplayString());
            Assert.Equal("Create", methodName);

            var originalType = collectionType.OriginalDefinition;
            Assert.Equal("MyCollection<T>", originalType.ToTestDisplayString());
            Assert.True(originalType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.Equal("Container<System.String>.MyCollectionBuilder", builderType.ToTestDisplayString());
            Assert.Equal("Create", methodName);
        }

        [Fact]
        public void CollectionBuilder_Substituted_03()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                public class Container<T>
                {
                    [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                    public struct MyCollection : IEnumerable<T>
                    {
                        public IEnumerator<T> GetEnumerator() => default;
                        IEnumerator IEnumerable.GetEnumerator() => default;
                    }
                    public class MyCollectionBuilder
                    {
                        public static MyCollection Create(ReadOnlySpan<T> items) => default;
                    }
                }
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        F([]);
                        F(new());
                    }
                    static void F(Container<string>.MyCollection c)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(7,24): error CS0416: 'Container<T>.MyCollectionBuilder': an attribute argument cannot use type parameters
                //     [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(MyCollectionBuilder)").WithArguments("Container<T>.MyCollectionBuilder").WithLocation(7, 24));

            var collectionType = (NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F").Parameters[0].Type;
            Assert.Equal("Container<System.String>.MyCollection", collectionType.ToTestDisplayString());
            Assert.False(collectionType.HasCollectionBuilderAttribute(out _, out _));

            var originalType = collectionType.OriginalDefinition;
            Assert.Equal("Container<T>.MyCollection", originalType.ToTestDisplayString());
            Assert.False(originalType.HasCollectionBuilderAttribute(out _, out _));
        }

        [Fact]
        public void CollectionBuilder_Retargeting()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    public IEnumerator<T> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    public static void Create(int[] items) { }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Mscorlib40);
            comp.VerifyEmitDiagnostics();
            var refA = comp.ToMetadataReference();

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        F([]);
                        F(new());
                    }
                    static void F(MyCollection<int> c)
                    {
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Mscorlib45);
            comp.VerifyEmitDiagnostics(
                // (6,11): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         F([]);
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 11));

            var collectionType = (NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F").Parameters[0].Type;
            Assert.Equal("MyCollection<System.Int32>", collectionType.ToTestDisplayString());
            TypeSymbol builderType;
            string methodName;
            Assert.True(collectionType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.Equal("MyCollectionBuilder", builderType.ToTestDisplayString());
            Assert.Equal("Create", methodName);

            var retargetingType = (RetargetingNamedTypeSymbol)collectionType.OriginalDefinition;
            Assert.Equal("MyCollection<T>", retargetingType.ToTestDisplayString());
            Assert.True(retargetingType.HasCollectionBuilderAttribute(out builderType, out methodName));
            Assert.IsType<RetargetingNamedTypeSymbol>(builderType);
            Assert.Equal("MyCollectionBuilder", builderType.ToTestDisplayString());
            Assert.Equal("Create", methodName);
        }

        [Fact]
        public void CollectionBuilder_ExtensionMethodGetEnumerator_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                class MyCollection<T>
                {
                }
                class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                namespace N
                {
                    static class Extensions
                    {
                        public static IEnumerator<T> GetEnumerator<T>(this MyCollection<T> c) => default;
                        static MyCollection<T> F<T>() => [];
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c = [];
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(24,31): error CS9188: 'MyCollection<int>' has a CollectionBuilderAttribute but no element type.
                //         MyCollection<int> c = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<int>").WithLocation(24, 31));
        }

        [Fact]
        public void CollectionBuilder_ExtensionMethodGetEnumerator_02()
        {
            string sourceA = """
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public class MyCollection<T>
                {
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                static class Extensions
                {
                    public static IEnumerator<T> GetEnumerator<T>(this MyCollection<T> c) => default;
                    static MyCollection<T> F<T>() => [];
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();
            var refA = comp.EmitToImageReference();

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c = [];
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,31): error CS9188: 'MyCollection<int>' has a CollectionBuilderAttribute but no element type.
                //         MyCollection<int> c = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<int>").WithLocation(6, 31));
        }

        [Fact]
        public void CollectionBuilder_InaccessibleGetEnumerator()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                class MyCollection<T>
                {
                    internal IEnumerator<T> GetEnumerator() => default;
                    public static MyCollection<T> F() => [];
                }
                class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c = [];
                    }
                }
                """;
            var comp = CreateCompilation(new[] { source, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(8,42): error CS9188: 'MyCollection<T>' has a CollectionBuilderAttribute but no element type.
                //     public static MyCollection<T> F() => [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<T>").WithLocation(8, 42),
                // 0.cs(18,31): error CS9188: 'MyCollection<int>' has a CollectionBuilderAttribute but no element type.
                //         MyCollection<int> c = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<int>").WithLocation(18, 31));
        }

        [InlineData("", "", false)]
        [InlineData("", "", true)]
        [InlineData("scoped", "", false)]
        [InlineData("scoped", "", true)]
        [InlineData("scoped", "scoped", false)]
        [InlineData("scoped", "scoped", true)]
        [Theory]
        public void CollectionBuilder_Scoped(string constructorParameterModifier, string builderParameterModifier, bool useCompilationReference)
        {
            string sourceA = $$"""
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public ref struct MyCollection<T>
                {
                    private readonly List<T> _list;
                    public MyCollection({{constructorParameterModifier}} ReadOnlySpan<T> items)
                    {
                        _list = new List<T>(items.ToArray());
                    }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>({{builderParameterModifier}} ReadOnlySpan<T> items) => new(items);
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        GetItems(x).Report();
                        MyCollection<int> y = [1, 2, 3];
                        GetItems(y).Report();
                    }
                    static List<T> GetItems<T>(MyCollection<T> c)
                    {
                        var list = new List<T>();
                        foreach (var i in c) list.Add(i);
                        return list;
                    }
                }
                """;
            CompileAndVerify(
                new[] { sourceB, s_collectionExtensions },
                references: new[] { refA },
                targetFramework: TargetFramework.Net80,
                verify: builderParameterModifier == "scoped" ? Verification.Fails : Verification.FailsPEVerify,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], "));
        }

        [Fact]
        public void CollectionBuilder_ScopedBuilderParameterOnly()
        {
            string sourceA = $$"""
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public ref struct MyCollection<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(ReadOnlySpan<T> items)
                    {
                        _list = new List<T>(items.ToArray());
                    }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(scoped ReadOnlySpan<T> items) => new(items);
                }
                """;
            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<int> y = [1, 2, 3];
                        MyCollection<string> z = new();
                    }
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(16,78): error CS8347: Cannot use a result of 'MyCollection<T>.MyCollection(ReadOnlySpan<T>)' in this context because it may expose variables referenced by parameter 'items' outside of their declaration scope
                //     public static MyCollection<T> Create<T>(scoped ReadOnlySpan<T> items) => new(items);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new(items)").WithArguments("MyCollection<T>.MyCollection(System.ReadOnlySpan<T>)", "items").WithLocation(16, 78),
                // 0.cs(16,82): error CS8352: Cannot use variable 'scoped ReadOnlySpan<T> items' in this context because it may expose referenced variables outside of their declaration scope
                //     public static MyCollection<T> Create<T>(scoped ReadOnlySpan<T> items) => new(items);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "items").WithArguments("scoped System.ReadOnlySpan<T> items").WithLocation(16, 82));
        }

        [CombinatorialData]
        [Theory]
        public void CollectionBuilder_MissingInt32(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T>
                {
                    public IEnumerator<T> GetEnumerator() => default;
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                }
                """;
            var comp = CreateCompilation(new[] { sourceA, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<string> x = [];
                        MyCollection<string> y = ["2"];
                        MyCollection<object> z = new();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
            comp.MakeTypeMissing(SpecialType.System_Int32);
            comp.VerifyEmitDiagnostics(
                // (7,34): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         MyCollection<string> y = ["2"];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[""2""]").WithArguments("System.Int32").WithLocation(7, 34),
                // (7,34): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         MyCollection<string> y = ["2"];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[""2""]").WithArguments("System.Int32").WithLocation(7, 34),
                // (7,34): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         MyCollection<string> y = ["2"];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[""2""]").WithArguments("System.Int32").WithLocation(7, 34));
        }

        [Fact]
        public void CollectionBuilder_Async()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class Program
                {
                    static async Task Main()
                    {
                        (await CreateCollection()).Report();
                    }
                    static async Task<MyCollection<int>> CreateCollection()
                    {
                        return [await F(1), 2, await F(3)];
                    }
                    static async Task<int> F(int i)
                    {
                        await Task.Yield();
                        return i;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                new[] { sourceA, sourceB, CollectionBuilderAttributeDefinition, s_collectionExtensions },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Fails,
                expectedOutput: IncludeExpectedOutput("[1, 2, 3], "));
            verifier.VerifyIL("Program.<CreateCollection>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
                """
                {
                  // Code size      324 (0x144)
                  .maxstack  3
                  .locals init (int V_0,
                                MyCollection<int> V_1,
                                int V_2,
                                int V_3,
                                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                                System.Exception V_5)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int Program.<CreateCollection>d__1.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  brfalse.s  IL_0057
                    IL_000a:  ldloc.0
                    IL_000b:  ldc.i4.1
                    IL_000c:  beq        IL_00cf
                    IL_0011:  ldarg.0
                    IL_0012:  ldflda     "<>y__InlineArray3<int> Program.<CreateCollection>d__1.<>7__wrap1"
                    IL_0017:  initobj    "<>y__InlineArray3<int>"
                    IL_001d:  ldc.i4.1
                    IL_001e:  call       "System.Threading.Tasks.Task<int> Program.F(int)"
                    IL_0023:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                    IL_0028:  stloc.s    V_4
                    IL_002a:  ldloca.s   V_4
                    IL_002c:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                    IL_0031:  brtrue.s   IL_0074
                    IL_0033:  ldarg.0
                    IL_0034:  ldc.i4.0
                    IL_0035:  dup
                    IL_0036:  stloc.0
                    IL_0037:  stfld      "int Program.<CreateCollection>d__1.<>1__state"
                    IL_003c:  ldarg.0
                    IL_003d:  ldloc.s    V_4
                    IL_003f:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<CreateCollection>d__1.<>u__1"
                    IL_0044:  ldarg.0
                    IL_0045:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>> Program.<CreateCollection>d__1.<>t__builder"
                    IL_004a:  ldloca.s   V_4
                    IL_004c:  ldarg.0
                    IL_004d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<CreateCollection>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<CreateCollection>d__1)"
                    IL_0052:  leave      IL_0143
                    IL_0057:  ldarg.0
                    IL_0058:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<CreateCollection>d__1.<>u__1"
                    IL_005d:  stloc.s    V_4
                    IL_005f:  ldarg.0
                    IL_0060:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<CreateCollection>d__1.<>u__1"
                    IL_0065:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                    IL_006b:  ldarg.0
                    IL_006c:  ldc.i4.m1
                    IL_006d:  dup
                    IL_006e:  stloc.0
                    IL_006f:  stfld      "int Program.<CreateCollection>d__1.<>1__state"
                    IL_0074:  ldloca.s   V_4
                    IL_0076:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                    IL_007b:  stloc.2
                    IL_007c:  ldarg.0
                    IL_007d:  ldflda     "<>y__InlineArray3<int> Program.<CreateCollection>d__1.<>7__wrap1"
                    IL_0082:  ldc.i4.0
                    IL_0083:  call       "InlineArrayElementRef<<>y__InlineArray3<int>, int>(ref <>y__InlineArray3<int>, int)"
                    IL_0088:  ldloc.2
                    IL_0089:  stind.i4
                    IL_008a:  ldarg.0
                    IL_008b:  ldflda     "<>y__InlineArray3<int> Program.<CreateCollection>d__1.<>7__wrap1"
                    IL_0090:  ldc.i4.1
                    IL_0091:  call       "InlineArrayElementRef<<>y__InlineArray3<int>, int>(ref <>y__InlineArray3<int>, int)"
                    IL_0096:  ldc.i4.2
                    IL_0097:  stind.i4
                    IL_0098:  ldc.i4.3
                    IL_0099:  call       "System.Threading.Tasks.Task<int> Program.F(int)"
                    IL_009e:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                    IL_00a3:  stloc.s    V_4
                    IL_00a5:  ldloca.s   V_4
                    IL_00a7:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                    IL_00ac:  brtrue.s   IL_00ec
                    IL_00ae:  ldarg.0
                    IL_00af:  ldc.i4.1
                    IL_00b0:  dup
                    IL_00b1:  stloc.0
                    IL_00b2:  stfld      "int Program.<CreateCollection>d__1.<>1__state"
                    IL_00b7:  ldarg.0
                    IL_00b8:  ldloc.s    V_4
                    IL_00ba:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<CreateCollection>d__1.<>u__1"
                    IL_00bf:  ldarg.0
                    IL_00c0:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>> Program.<CreateCollection>d__1.<>t__builder"
                    IL_00c5:  ldloca.s   V_4
                    IL_00c7:  ldarg.0
                    IL_00c8:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<CreateCollection>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<CreateCollection>d__1)"
                    IL_00cd:  leave.s    IL_0143
                    IL_00cf:  ldarg.0
                    IL_00d0:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<CreateCollection>d__1.<>u__1"
                    IL_00d5:  stloc.s    V_4
                    IL_00d7:  ldarg.0
                    IL_00d8:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<CreateCollection>d__1.<>u__1"
                    IL_00dd:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                    IL_00e3:  ldarg.0
                    IL_00e4:  ldc.i4.m1
                    IL_00e5:  dup
                    IL_00e6:  stloc.0
                    IL_00e7:  stfld      "int Program.<CreateCollection>d__1.<>1__state"
                    IL_00ec:  ldloca.s   V_4
                    IL_00ee:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                    IL_00f3:  stloc.3
                    IL_00f4:  ldarg.0
                    IL_00f5:  ldflda     "<>y__InlineArray3<int> Program.<CreateCollection>d__1.<>7__wrap1"
                    IL_00fa:  ldc.i4.2
                    IL_00fb:  call       "InlineArrayElementRef<<>y__InlineArray3<int>, int>(ref <>y__InlineArray3<int>, int)"
                    IL_0100:  ldloc.3
                    IL_0101:  stind.i4
                    IL_0102:  ldarg.0
                    IL_0103:  ldflda     "<>y__InlineArray3<int> Program.<CreateCollection>d__1.<>7__wrap1"
                    IL_0108:  ldc.i4.3
                    IL_0109:  call       "InlineArrayAsReadOnlySpan<<>y__InlineArray3<int>, int>(in <>y__InlineArray3<int>, int)"
                    IL_010e:  call       "MyCollection<int> MyCollectionBuilder.Create<int>(System.ReadOnlySpan<int>)"
                    IL_0113:  stloc.1
                    IL_0114:  leave.s    IL_012f
                  }
                  catch System.Exception
                  {
                    IL_0116:  stloc.s    V_5
                    IL_0118:  ldarg.0
                    IL_0119:  ldc.i4.s   -2
                    IL_011b:  stfld      "int Program.<CreateCollection>d__1.<>1__state"
                    IL_0120:  ldarg.0
                    IL_0121:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>> Program.<CreateCollection>d__1.<>t__builder"
                    IL_0126:  ldloc.s    V_5
                    IL_0128:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>>.SetException(System.Exception)"
                    IL_012d:  leave.s    IL_0143
                  }
                  IL_012f:  ldarg.0
                  IL_0130:  ldc.i4.s   -2
                  IL_0132:  stfld      "int Program.<CreateCollection>d__1.<>1__state"
                  IL_0137:  ldarg.0
                  IL_0138:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>> Program.<CreateCollection>d__1.<>t__builder"
                  IL_013d:  ldloc.1
                  IL_013e:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<MyCollection<int>>.SetResult(MyCollection<int>)"
                  IL_0143:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder_AttributeCycle()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;

                [CollectionBuilder(typeof(MyCollectionBuilder), MyCollectionBuilder.GetName([1, 2, 3]))]
                class MyCollection<T> : IEnumerable<T>
                {
                    public void Add(T t) { }
                    public IEnumerator<T> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }

                static class MyCollectionBuilder
                {
                    public static string GetName<T>(MyCollection<T> c) => null;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => null;
                }
                """;
            var comp = CreateCompilation(new[] { source, CollectionBuilderAttributeDefinition }, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 0.cs(6,49): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [CollectionBuilder(typeof(MyCollectionBuilder), MyCollectionBuilder.GetName([1, 2, 3]))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "MyCollectionBuilder.GetName([1, 2, 3])").WithLocation(6, 49));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void RestrictedTypes()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        var x = [default(TypedReference)];
                        var y = [default(ArgIterator)];
                        var z = [default(RuntimeArgumentHandle)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS9176: There is no target type for the collection expression.
                //         var x = [default(TypedReference)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[default(TypedReference)]").WithLocation(6, 17),
                // (7,17): error CS9176: There is no target type for the collection expression.
                //         var y = [default(ArgIterator)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[default(ArgIterator)]").WithLocation(7, 17),
                // (8,17): error CS9176: There is no target type for the collection expression.
                //         var z = [default(RuntimeArgumentHandle)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[default(RuntimeArgumentHandle)]").WithLocation(8, 17));
        }

        [Fact]
        public void RefStruct_01()
        {
            string source = """
                ref struct R
                {
                    public R(ref int i) { }
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 0;
                        var x = [default(R)];
                        var y = [new R(ref i)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS9176: There is no target type for the collection expression.
                //         var x = [default(R)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[default(R)]").WithLocation(10, 17),
                // (11,17): error CS9176: There is no target type for the collection expression.
                //         var y = [new R(ref i)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[new R(ref i)]").WithLocation(11, 17));
        }

        [Fact]
        public void RefStruct_02()
        {
            string source = """
                using System.Collections.Generic;
                ref struct R
                {
                    public int _i;
                    public R(ref int i) { _i = i; }
                    public static implicit operator int(R r) => r._i;
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 1;
                        int[] a = [default(R), new R(ref i)];
                        a.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[0, 1], ");
        }

        [Fact]
        public void RefStruct_03()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable
                {
                    private List<int> _list = new List<int>();
                    public void Add(R r) { _list.Add(r._i); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                ref struct R
                {
                    public int _i;
                    public R(ref int i) { _i = i; }
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 1;
                        C c = [default(R), new R(ref i)];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[0, 1], ");
        }

        [Fact]
        public void ExpressionTrees()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                interface I<T> : IEnumerable
                {
                    void Add(T t);
                }
                class Program
                {
                    static Expression<Func<int[]>> Create1()
                    {
                        return () => [];
                    }
                    static Expression<Func<List<object>>> Create2()
                    {
                        return () => [1, 2];
                    }
                    static Expression<Func<T>> Create3<T, U>(U a, U b) where T : I<U>, new()
                    {
                        return () => [a, b];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,22): error CS9175: An expression tree may not contain a collection expression.
                //         return () => [];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "[]").WithLocation(13, 22),
                // (17,22): error CS9175: An expression tree may not contain a collection expression.
                //         return () => [1, 2];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "[1, 2]").WithLocation(17, 22),
                // (21,22): error CS9175: An expression tree may not contain a collection expression.
                //         return () => [a, b];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "[a, b]").WithLocation(21, 22));
        }

        [Fact]
        public void IOperation_Array()
        {
            string source = """
                class Program
                {
                    static T[] Create<T>(T a, T b)
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: T[]) (Syntax: '[a, b]')
                  Children(2):
                      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                      IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T[], IsImplicit) (Syntax: '[a, b]')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (CollectionExpression)
                          Operand:
                            IOperation:  (OperationKind.None, Type: T[]) (Syntax: '[a, b]')
                              Children(2):
                                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void IOperation_Span()
        {
            string source = """
                using System;
                class Program
                {
                    static Span<T> Create<T>(T a, T b)
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: System.Span<T>) (Syntax: '[a, b]')
                  Children(2):
                      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                      IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<T>, IsImplicit) (Syntax: '[a, b]')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (CollectionExpression)
                          Operand:
                            IOperation:  (OperationKind.None, Type: System.Span<T>) (Syntax: '[a, b]')
                              Children(2):
                                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void IOperation_CollectionInitializer()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct S<T> : I<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static S<T> Create<T>(T a, T b)
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: S<T>) (Syntax: '[a, b]')
                  Children(2):
                      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T) (Syntax: 'a')
                      IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T) (Syntax: 'b')
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (CollectionExpression)
                          Operand:
                            IOperation:  (OperationKind.None, Type: S<T>) (Syntax: '[a, b]')
                              Children(2):
                                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T) (Syntax: 'a')
                                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T) (Syntax: 'b')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void IOperation_TypeParameter()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct S<T> : I<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static T Create<T, U>(U a, U b) where T : I<U>, new()
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: T) (Syntax: '[a, b]')
                  Children(2):
                      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: U) (Syntax: 'a')
                      IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: U) (Syntax: 'b')
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsImplicit) (Syntax: '[a, b]')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (CollectionExpression)
                          Operand:
                            IOperation:  (OperationKind.None, Type: T) (Syntax: '[a, b]')
                              Children(2):
                                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: U) (Syntax: 'a')
                                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: U) (Syntax: 'b')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void IOperation_Nested()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<List<int>> x = /*<bind>*/[[Get(1)]]/*</bind>*/;
                    }
                    static int Get(int value) => value;
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '[[Get(1)]]')
                  Children(1):
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[Get(1)]')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          IOperation:  (OperationKind.None, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '[Get(1)]')
                            Children(1):
                                IInvocationOperation (System.Int32 Program.Get(System.Int32 value)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get(1)')
                                  Instance Receiver:
                                    null
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '1')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Main");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> x]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'x = /*<bind>*/[[Get(1)]]')
                              Left:
                                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'x = /*<bind>*/[[Get(1)]]')
                              Right:
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: '[[Get(1)]]')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (CollectionExpression)
                                  Operand:
                                    IOperation:  (OperationKind.None, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '[[Get(1)]]')
                                      Children(1):
                                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[Get(1)]')
                                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              (CollectionExpression)
                                            Operand:
                                              IOperation:  (OperationKind.None, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '[Get(1)]')
                                                Children(1):
                                                    IInvocationOperation (System.Int32 Program.Get(System.Int32 value)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get(1)')
                                                      Instance Receiver:
                                                        null
                                                      Arguments(1):
                                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '1')
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Next (Regular) Block[B2]
                            Leaving: {R1}
                }
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void IOperation_SpreadElement_01()
        {
            string source = """
                class Program
                {
                    static int[] Append(int[] a)
                    {
                        return /*<bind>*/[..a]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: System.Int32[]) (Syntax: '[..a]')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: '..a')
                        Children(1):
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Append");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], IsImplicit) (Syntax: '[..a]')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (CollectionExpression)
                          Operand:
                            IOperation:  (OperationKind.None, Type: System.Int32[]) (Syntax: '[..a]')
                              Children(1):
                                  IOperation:  (OperationKind.None, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: '..a')
                                    Children(1):
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void IOperation_SpreadElement_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int> Append(int[] a)
                    {
                        return /*<bind>*/[..a]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                IOperation:  (OperationKind.None, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '[..a]')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: '..a')
                        Children(1):
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                """);

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Append");
            VerifyFlowGraph(comp, method,
                """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[..a]')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (CollectionExpression)
                          Operand:
                            IOperation:  (OperationKind.None, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '[..a]')
                              Children(1):
                                  IOperation:  (OperationKind.None, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: '..a')
                                    Children(1):
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """);
        }

        [Fact]
        public void Async_01()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class Program
                {
                    static async Task Main()
                    {
                        (await CreateArray()).Report();
                        (await CreateList()).Report();
                    }
                    static async Task<int[]> CreateArray()
                    {
                        return [await F(1), await F(2)];
                    }
                    static async Task<List<int>> CreateList()
                    {
                        return [await F(3), await F(4)];
                    }
                    static async Task<int> F(int i)
                    {
                        await Task.Yield();
                        return i;
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], [3, 4], ");
        }

        [Fact]
        public void Async_02()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class Program
                {
                    static async Task Main()
                    {
                        (await F2(F1())).Report();
                    }
                    static async Task<int[]> F1()
                    {
                        return [await F(1), await F(2)];
                    }
                    static async Task<int[]> F2(Task<int[]> e)
                    {
                        return [3, .. await e, 4];
                    }
                    static async Task<T> F<T>(T t)
                    {
                        await Task.Yield();
                        return t;
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[3, 1, 2, 4], ");
        }

        [Fact]
        public void PostfixIncrementDecrement()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main()
                    {
                        []++;
                        []--;
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         []++;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "[]").WithLocation(7, 9),
                // (8,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         []--;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "[]").WithLocation(8, 9));
        }

        [Fact]
        public void PostfixPointerAccess()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main()
                    {
                        var v = []->Count;
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,17): error CS9503: There is no target type for the collection expression.
                //         var v = []->Count;
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 17));
        }

        [Fact]
        public void LeftHandAssignment()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main()
                    {
                        [] = null;
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         [] = null;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[]").WithLocation(7, 9));
        }

        [Fact]
        public void BinaryOperator()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(List<int> list)
                    {
                        [] + list;
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         [] + list;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("string", "0").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [] + list;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "[] + list").WithLocation(7, 9));
        }

        [Fact]
        public void RangeOperator()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(List<int> list)
                    {
                        []..;
                    }
                }
                """;
            CreateCompilationWithIndexAndRangeAndSpan(source).VerifyEmitDiagnostics(
                // (7,9): error CS9500: Cannot initialize type 'Index' with a collection expression because the type is not constructible.
                //         []..;
                Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("System.Index").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         []..;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "[]..").WithLocation(7, 9));
        }

        [Fact]
        public void TopLevelSwitchExpression()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(List<int> list)
                    {
                        [] switch { null => 0 };
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS9503: There is no target type for the collection expression.
                //         [] switch
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [] switch
                Diagnostic(ErrorCode.ERR_IllegalStatement, "[] switch { null => 0 }").WithLocation(7, 9));
        }

        [Fact]
        public void TopLevelWithExpression()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(List<int> list)
                    {
                        [] with { Count = 1, };
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS9503: There is no target type for the collection expression.
                //         [] with { Count = 1, };
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [] with { Count = 1, };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "[] with { Count = 1, }").WithLocation(7, 9));
        }

        [Fact]
        public void TopLevelIsExpressions()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(List<int> list)
                    {
                        [] is object;
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS9503: There is no target type for the collection expression.
                //         [] is object;
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [] is object;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "[] is object").WithLocation(7, 9));
        }

        [Fact]
        public void TopLevelAsExpressions()
        {
            string source = """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(List<int> list)
                    {
                        [] as List<int>;
                    }
                }
                """;
            CreateCompilation(source).VerifyEmitDiagnostics(
                // (7,9): error CS9503: There is no target type for the collection expression.
                //         [] as List<int>;
                Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[]").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [] as List<int>;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "[] as List<int>").WithLocation(7, 9));
        }
    }
}
