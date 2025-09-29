// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.PropertySetAnalysis)]
    public class PropertySetAnalysisTests
    {
        /// <summary>
        /// Just a container for parameters necessary for PropertySetAnalysis for unit tests below.
        /// </summary>
        private sealed class PropertySetAnalysisParameters
        {
            public PropertySetAnalysisParameters(string typeToTrack, ConstructorMapper constructorMapper, PropertyMapperCollection propertyMapperCollection, HazardousUsageEvaluatorCollection hazardousUsageEvaluatorCollection)
            {
                TypesToTrack = new string[] { typeToTrack }.ToImmutableHashSet() ?? throw new ArgumentNullException(nameof(typeToTrack));
                ConstructorMapper = constructorMapper ?? throw new ArgumentNullException(nameof(constructorMapper));
                PropertyMapperCollection = propertyMapperCollection ?? throw new ArgumentNullException(nameof(propertyMapperCollection));
                HazardousUsageEvaluatorCollection = hazardousUsageEvaluatorCollection ?? throw new ArgumentNullException(nameof(hazardousUsageEvaluatorCollection));
            }

            public ImmutableHashSet<string> TypesToTrack { get; }
            public ConstructorMapper ConstructorMapper { get; }
            public PropertyMapperCollection PropertyMapperCollection { get; }
            public HazardousUsageEvaluatorCollection HazardousUsageEvaluatorCollection { get; }
        }

        /// <summary>
        /// Verification helper.
        /// </summary>
        /// <param name="source">C# source code, with /*&lt;bind&gt;*/ and /*&lt;/bind&gt;*/ around the method block to be analyzed.</param>
        /// <param name="propertySetAnalysisParameters">PropertySetAnalysis parameters.</param>
        /// <param name="expectedResults">Expected hazardous usages (MethodName = null => return statement).</param>
        private void VerifyCSharp(
            string source,
            PropertySetAnalysisParameters propertySetAnalysisParameters,
            params (int Line, int Column, string? Method, HazardousUsageEvaluationResult Result)[] expectedResults)
        {
            expectedResults ??= Array.Empty<(int Line, int Column, string? MethodName, HazardousUsageEvaluationResult Result)>();

            Project project = CreateProject([source, TestTypeToTrackSource]);
            Compilation? compilation = project.GetCompilationAsync().Result;
            Assert.NotNull(compilation);
            CompilationUtils.ValidateNoCompileErrors(compilation.GetDiagnostics());
            (IOperation? operation, SemanticModel? model, SyntaxNode? syntaxNode) = GetOperationAndSyntaxForTest<BlockSyntax>((CSharpCompilation)compilation);
            Assert.True(
                operation != null,
                $"Could not find code block to analyze.  Does your test code have {StartString} and {EndString} around the braces of block to analyze?");
            Assert.NotNull(model);
            Assert.NotNull(syntaxNode?.Parent);
            ISymbol? symbol = model.GetDeclaredSymbol(syntaxNode.Parent) ?? model.GetSymbolInfo(syntaxNode.Parent).Symbol;
            Assert.NotNull(symbol);
            var success = operation.TryGetEnclosingControlFlowGraph(out var cfg);
            Debug.Assert(success);
            Debug.Assert(cfg != null);

            DiagnosticDescriptor dummy = new DiagnosticDescriptor("fakeId", null!, null!, "fakeagory", DiagnosticSeverity.Info, true);
            PropertySetAnalysisResult? result =
                PropertySetAnalysis.GetOrComputeResult(
                    cfg,
                    compilation,
                    symbol,
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    propertySetAnalysisParameters.TypesToTrack,
                    propertySetAnalysisParameters.ConstructorMapper,
                    propertySetAnalysisParameters.PropertyMapperCollection,
                    propertySetAnalysisParameters.HazardousUsageEvaluatorCollection,
                    InterproceduralAnalysisConfiguration.Create(
                        new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                        dummy,
                        cfg,
                        compilation,
                        InterproceduralAnalysisKind.ContextSensitive));
            Assert.NotNull(result);
            ImmutableDictionary<(Location Location, IMethodSymbol? Method), HazardousUsageEvaluationResult> actual =
                result.HazardousUsages;
            try
            {
                Assert.Equal(expectedResults.Length, actual.Count);
                foreach ((int Line, int Column, string? Method, HazardousUsageEvaluationResult Result) in expectedResults)
                {
                    HazardousUsageEvaluationResult? actualResult = null;
                    foreach (KeyValuePair<(Location Location, IMethodSymbol? Method), HazardousUsageEvaluationResult> kvp in actual)
                    {
                        FileLinePositionSpan span = kvp.Key.Location.GetLineSpan();
                        if (span.Path != CSharpDefaultFilePath)
                        {
                            // Only looking in the first file, so that expectedResults doesn't have to specify a filename.
                            continue;
                        }

                        if (span.StartLinePosition.Line + 1 == Line
                            && span.StartLinePosition.Character + 1 == Column
                            && kvp.Key.Method?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) == Method)
                        {
                            actualResult = kvp.Value;
                            break;
                        }
                    }

                    Assert.True(
                        actualResult.HasValue,
                        $"Could not find expected result Line {Line} Column {Column} {MethodOrReturnString(Method)} Result {Result}");
                    Assert.True(
                        actualResult == Result,
                        $"Expected {Result}, Actual {actualResult}, for Line {Line} Column {Column} {MethodOrReturnString(Method)}");
                }
            }
            catch (XunitException)
            {
                TestOutput.WriteLine("PropertySetAnalysis actual results:");
                TestOutput.WriteLine("============================");
                if (actual == null)
                {
                    throw;
                }

                foreach (KeyValuePair<(Location Location, IMethodSymbol? Method), HazardousUsageEvaluationResult> kvp in actual)
                {
                    LinePosition linePosition = kvp.Key.Location.GetLineSpan().StartLinePosition;
                    int lineNumber = linePosition.Line + 1;
                    int columnNumber = linePosition.Character + 1;
                    TestOutput.WriteLine(
                        $"Line {lineNumber}, Column {columnNumber}, {MethodSymbolOrReturnString(kvp.Key.Method)}: {kvp.Value}");
                }

                TestOutput.WriteLine("============================");

                throw;
            }

            static string MethodSymbolOrReturnString(IMethodSymbol? methodSymbol)
            {
                return methodSymbol != null ? $"Method {methodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}" : "Return/Initialization";
            }

            static string MethodOrReturnString(string? method)
            {
                return method != null ? $"Method {method}" : "Return/Initialization";
            }
        }

        private readonly string TestTypeToTrackSource = @"
public class TestTypeToTrack
{
    public TestEnum AnEnum { get; set; }
    public object AnObject { get; set; }
    public string AString { get; set; }

    public void Method()
    {
    }
}

public class TestTypeToTrackWithConstructor : TestTypeToTrack
{
    private TestTypeToTrackWithConstructor()
    {
    }

    public TestTypeToTrackWithConstructor(TestEnum enu, object obj, string str)
    {
        this.AnEnum = enu;
        this.AnObject = obj;
        this.AString = str;
    }
}

public enum TestEnum
{
    Value0,
    Value1,
    Value2,
}

public class OtherClass
{
    public void OtherMethod(TestTypeToTrack t)
    {
    }

    public void OtherMethod(string s, TestTypeToTrack t)
    {
    }

    public static void StaticMethod(TestTypeToTrack staticMethodParameter)
    {
    }
}";

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrack.AString property is not null
        /// when calling its Method() method, OtherClass.OtherMethod() method, or OtherClass.StaticMethod() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfStringIsNonNull =
            new(
                "TestTypeToTrack",
                new ConstructorMapper(     // Only one constructor, which leaves its AString property as null (not hazardous).
                    ImmutableArray.Create<PropertySetAbstractValueKind>(
                        PropertySetAbstractValueKind.Unflagged)),
                new PropertyMapperCollection(
                    new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                        "AString",
                        pointsToAbstractValue =>
                        {
                            return pointsToAbstractValue.NullState switch
                            {
                                NullAbstractValue.Null => PropertySetAbstractValueKind.Unflagged,
                                NullAbstractValue.NotNull => PropertySetAbstractValueKind.Flagged,
                                NullAbstractValue.MaybeNull => PropertySetAbstractValueKind.MaybeFlagged,
                                _ => PropertySetAbstractValueKind.Unknown,
                            };
                        })),
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                        "Method",
                        (methodSymbol, abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.
                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        }),
                    new HazardousUsageEvaluator(    // When OtherClass.OtherMethod() is invoked, evaluate its "TypeToTrack t" argument.
                        "OtherClass",
                        "OtherMethod",
                        "t",
                        (methodSymbol, abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.

                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        }),
                    new HazardousUsageEvaluator(    // When OtherClass.StaticMethod() is invoked, evaluate its "TypeToTrack staticMethodParameter" argument.
                        "OtherClass",
                        "StaticMethod",
                        "staticMethodParameter",
                        (methodSymbol, abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.

                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        })));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNull_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull,
                (8, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNull_StringEmpty_Flagged()
            => VerifyCSharp(@"
using System;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = String.Empty;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull,
                (10, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNull_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = null;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull);

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNull_OtherMethod_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        OtherClass o = new OtherClass();
        o.OtherMethod(""this string parameter is ignored"", t);
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull,
                (9, 9, "void OtherClass.OtherMethod(string s, TestTypeToTrack t)", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNull_StaticMethod_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        OtherClass.StaticMethod(t);
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull,
                (8, 9, "void OtherClass.StaticMethod(TestTypeToTrack staticMethodParameter)", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNull_OtherClassBothMethods_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        OtherClass o = new OtherClass();
        o.OtherMethod(""this string parameter is ignored"", t);
        OtherClass.StaticMethod(t);
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull,
                (9, 9, "void OtherClass.OtherMethod(string s, TestTypeToTrack t)", HazardousUsageEvaluationResult.Flagged),
                (10, 9, "void OtherClass.StaticMethod(TestTypeToTrack staticMethodParameter)", HazardousUsageEvaluationResult.Flagged));

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrackWithConstructor.AString property
        /// is not null when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull =
            new(
                "TestTypeToTrackWithConstructor",
                new ConstructorMapper(
                    (method, argumentPointsToAbstractValues) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.
                        PropertySetAbstractValueKind kind = PropertySetAbstractValueKind.Unknown;
                        if (method.Parameters.Length >= 2)
                        {
                            // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                            kind = argumentPointsToAbstractValues[2].NullState switch
                            {
                                NullAbstractValue.Null => PropertySetAbstractValueKind.Unflagged,
                                NullAbstractValue.NotNull => PropertySetAbstractValueKind.Flagged,
                                NullAbstractValue.MaybeNull => PropertySetAbstractValueKind.MaybeFlagged,
                                _ => PropertySetAbstractValueKind.Unknown,
                            };
                        }

                        return PropertySetAbstractValue.GetInstance(kind);
                    }),
            new PropertyMapperCollection(
                new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                    "AString",
                    pointsToAbstractValue =>
                    {
                        return pointsToAbstractValue.NullState switch
                        {
                            NullAbstractValue.Null => PropertySetAbstractValueKind.Unflagged,
                            NullAbstractValue.NotNull => PropertySetAbstractValueKind.Flagged,
                            NullAbstractValue.MaybeNull => PropertySetAbstractValueKind.MaybeFlagged,
                            _ => PropertySetAbstractValueKind.Unknown,
                        };
                    })),
            new HazardousUsageEvaluatorCollection(
                new HazardousUsageEvaluator(
                    "Method",
                    (methodSymbol, abstractValue) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        // With only one property being tracked, this is straightforward.
                        return abstractValue[0] switch
                        {
                            PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                            PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                            _ => HazardousUsageEvaluationResult.Unflagged,
                        };
                    })));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, ""A non-null string"");
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull,
                (7, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_StringEmpty_Flagged()
            => VerifyCSharp(@"
using System;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, String.Empty);
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull,
                (9, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, null);
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull);

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_PropertyAssigned_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, null);
        t.AString = """";
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull,
                (8, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrack.AnEnum property is Value0 when
        /// calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfEnumIsValue0 =
            new(
                "TestTypeToTrack",
                new ConstructorMapper(     // Only one constructor, which leaves its AnEnum property as Value0 (hazardous).
                    ImmutableArray.Create<PropertySetAbstractValueKind>(
                        PropertySetAbstractValueKind.Flagged)),
                new PropertyMapperCollection(
                    new PropertyMapper(
                        "AnEnum",
                        valueContentAbstractValue =>
                        {
                            return PropertySetCallbacks.EvaluateLiteralValues(valueContentAbstractValue, v => v is not null && v.Equals(0));
                        })),
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                        "Method",
                        (methodSymbol, abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.
                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        })));

        [Fact]
        public void TestTypeToTrack_HazardousIfEnumIsValue0_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AnEnum = TestEnum.Value0;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfEnumIsValue0,
                (8, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfEnumIsValue0_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AnEnum = TestEnum.Value2;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfEnumIsValue0);

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrackWithConstructor.AnEnum property
        /// is Value0 when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrackWithConstructor_HazardousIfEnumIsValue0 =
            new(
                "TestTypeToTrackWithConstructor",
                new ConstructorMapper(
                    (method, argumentValueContentAbstractValues, argumentPointsToAbstractValues) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        PropertySetAbstractValueKind kind = PropertySetCallbacks.EvaluateLiteralValues(
                            argumentValueContentAbstractValues[0],
                            v => v is not null && v.Equals(0));
                        return PropertySetAbstractValue.GetInstance(kind);
                    }),
                new PropertyMapperCollection(
                    new PropertyMapper(
                        "AnEnum",
                        valueContentAbstractValue =>
                        {
                            return PropertySetCallbacks.EvaluateLiteralValues(valueContentAbstractValue, v => v is not null && v.Equals(0));
                        })),
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                        "Method",
                        (methodSymbol, abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.
                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        })));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfEnumIsValue0_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(TestEnum.Value2, null, null);
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfEnumIsValue0);

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfEnumIsValue0_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(TestEnum.Value0, null, null);
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfEnumIsValue0,
                (7, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when both TestTypeToTrack.AString starts with 'T' and
        /// TestTypeToTrack.AnEnum is Value2 when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfStringStartsWithTAndValue2 =
            new(
                "TestTypeToTrack",
                new ConstructorMapper(
                    ImmutableArray.Create<PropertySetAbstractValueKind>(   // Order is the same as the PropertyMappers below.
                        PropertySetAbstractValueKind.Unflagged,      // AString
                        PropertySetAbstractValueKind.Unflagged)),    // AnEnum
            new PropertyMapperCollection(
                new PropertyMapper(
                    "AString",
                    valueContentAbstractValue =>
                    {
                        return PropertySetCallbacks.EvaluateLiteralValues(
                            valueContentAbstractValue,
                            v => (v as string)?.StartsWith("T", StringComparison.Ordinal) == true);
                    }),
                new PropertyMapper(
                    "AnEnum",
                    valueContentAbstractValue =>
                    {
                        return PropertySetCallbacks.EvaluateLiteralValues(valueContentAbstractValue, v => v is not null && v.Equals(2));
                    })),
            new HazardousUsageEvaluatorCollection(
                new HazardousUsageEvaluator(
                    "Method",
                    (methodSymbol, abstractValue) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        // PropertyAbstractValueKinds are in the same order as the PropertyMappers that were used to initialize the PropertyMapperCollection.
                        PropertySetAbstractValueKind aStringKind = abstractValue[0];
                        PropertySetAbstractValueKind anEnumKind = abstractValue[1];
                        if (aStringKind == PropertySetAbstractValueKind.Flagged && anEnumKind == PropertySetAbstractValueKind.Flagged)
                        {
                            return HazardousUsageEvaluationResult.Flagged;
                        }
                        else if ((aStringKind == PropertySetAbstractValueKind.Flagged || aStringKind == PropertySetAbstractValueKind.MaybeFlagged)
                            && (anEnumKind == PropertySetAbstractValueKind.Flagged || anEnumKind == PropertySetAbstractValueKind.MaybeFlagged))
                        {
                            return HazardousUsageEvaluationResult.MaybeFlagged;
                        }
                        else
                        {
                            return HazardousUsageEvaluationResult.Unflagged;
                        }
                    })));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringStartsWithTAndValue2_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""The beginning of knowledge is the discovery of something we do not understand."";
        t.AnEnum = TestEnum.Value2;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringStartsWithTAndValue2,
                (9, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringStartsWithTAndValue2_BothMaybe_MaybeFlagged()
            => VerifyCSharp(@"
using System;
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        Random r = new Random();
        t.AString = ""T"";
        t.AnEnum = TestEnum.Value2;
        if (r.Next(6) == 4)
        {
            t.AString = ""A different string."";
        }

        if (r.Next(6) == 4)
        {
            t.AnEnum = TestEnum.Value1;
        }

        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringStartsWithTAndValue2,
                (21, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.MaybeFlagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringStartsWithTAndValue2_FirstMaybe_MaybeFlagged()
            => VerifyCSharp(@"
using System;
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        Random r = new Random();
        t.AString = ""T"";
        t.AnEnum = TestEnum.Value2;
        if (r.Next(6) == 4)
        {
            t.AString = ""A different string."";
        }

        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringStartsWithTAndValue2,
                (16, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.MaybeFlagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringStartsWithTAndValue2_SecondMaybe_MaybeFlagged()
            => VerifyCSharp(@"
using System;
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        Random r = new Random();
        t.AString = ""T"";
        t.AnEnum = TestEnum.Value2;
        if (r.Next(6) == 4)
        {
            t.AnEnum = TestEnum.Value1;
        }

        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringStartsWithTAndValue2,
                (16, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.MaybeFlagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringStartsWithTAndValue2_FirstFlagged_Unflagged()
            => VerifyCSharp(@"
using System;
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        Random r = new Random();
        t.AString = ""T"";
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringStartsWithTAndValue2);

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when both TestTypeToTrack.AnObject is a BitArray.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray =
            new(
                "TestTypeToTrackWithConstructor",
                new ConstructorMapper(
                    (constructorMethodSymbol, argumentPointsToAbstractValues) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        // Better to compare LocationTypeOpt to INamedTypeSymbol, but for this demonstration, just using MetadataName.
                        PropertySetAbstractValueKind kind;
                        if (argumentPointsToAbstractValues[1].Locations.Any(l =>
                                l.LocationType != null
                                && l.LocationType.MetadataName == "BitArray"))
                        {
                            kind = PropertySetAbstractValueKind.Flagged;
                        }
                        else
                        {
                            kind = PropertySetAbstractValueKind.Unflagged;
                        }

                        return PropertySetAbstractValue.GetInstance(kind);
                    }),
            new PropertyMapperCollection(
                new PropertyMapper(
                    "AnObject",
                    pointsToAbstractValue =>
                    {
                        // Better to compare LocationTypeOpt to INamedTypeSymbol, but for this demonstration, just using MetadataName.
                        PropertySetAbstractValueKind kind;
                        if (pointsToAbstractValue.Locations.Any(l =>
                                l.LocationType != null
                                && l.LocationType.MetadataName == "BitArray"))
                        {
                            kind = PropertySetAbstractValueKind.Flagged;
                        }
                        else
                        {
                            kind = PropertySetAbstractValueKind.Unflagged;
                        }

                        return kind;
                    })),
            new HazardousUsageEvaluatorCollection(
                new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                    "Method",
                    (methodSymbol, abstractValue) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        // With only one property being tracked, this is straightforward.
                        return abstractValue[0] switch
                        {
                            PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                            PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                            _ => HazardousUsageEvaluationResult.Unflagged,
                        };
                    })));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray_Constructor_Flagged()
            => VerifyCSharp(@"
using System;
using System.Collections;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), new BitArray(4), ""string"");
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray,
                (10, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray_Constructor_TwoPaths_Flagged()
            => VerifyCSharp(@"
using System;
using System.Collections;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t;
        if (new Random().Next(6) == 4)
            t = new TestTypeToTrackWithConstructor(default(TestEnum), new BitArray(6), ""string"");
        else
            t = new TestTypeToTrackWithConstructor(default(TestEnum), ""object string"", ""string"");
        t.Method();   // PropertySetAnalysis is aggressive--at least one previous code path being Flagged means it's Flagged at this point.
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray,
                (14, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray_Constructor_NotFlagged()
            => VerifyCSharp(@"
using System;
using System.Collections;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, ""string"");
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfObjectIsBitArray);

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when both TestTypeToTrackWithConstructor.AString starts
        /// with 'A'.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrackWithConstructor_HazardousIfAStringStartsWithA =
            new(
                "TestTypeToTrackWithConstructor",
                new ConstructorMapper(
                    (constructorMethodSymbol,
                        argumentValueContentAbstractValues,
                        argumentPointsToAbstractValues) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        PropertySetAbstractValueKind kind = PropertySetCallbacks.EvaluateLiteralValues(
                            argumentValueContentAbstractValues[2],
                            v => (v as string)?.StartsWith("A", StringComparison.Ordinal) == true);
                        return PropertySetAbstractValue.GetInstance(kind);
                    }),
            new PropertyMapperCollection(
                new PropertyMapper(
                    "AString",
                    valueContentAbstractValue =>
                    {
                        return PropertySetCallbacks.EvaluateLiteralValues(
                            valueContentAbstractValue,
                            v => (v as string)?.StartsWith("A", StringComparison.Ordinal) == true);
                    })),
            new HazardousUsageEvaluatorCollection(
                new HazardousUsageEvaluator(    // When TypeToTrackWithConstructor.Method() is invoked, need to evaluate its state.
                    "Method",
                    (methodSymbol, abstractValue) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        // With only one property being tracked, this is straightforward.
                        return abstractValue[0] switch
                        {
                            PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                            PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                            _ => HazardousUsageEvaluationResult.Unflagged,
                        };
                    })));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfAStringStartsWithA_Flagged()
            => VerifyCSharp(@"
using System;
using System.Collections;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, ""A string"");
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfAStringStartsWithA,
                (10, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfAStringStartsWithA_Interprocedural_Flagged()
            => VerifyCSharp(@"
using System;
using System.Collections;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = GetTestType();
        t.Method();
    }/*</bind>*/

    TestTypeToTrackWithConstructor GetTestType()
    {
        return new TestTypeToTrackWithConstructor(default(TestEnum), null, ""A string"");
    }
}",
                TestTypeToTrackWithConstructor_HazardousIfAStringStartsWithA,
                (10, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrack.AString property is not null
        /// when returning a TestTypeToTrack.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfStringIsNonNullOnReturn =
            new(
                "TestTypeToTrack",
                new ConstructorMapper(     // Only one constructor, which leaves its AString property as null (not hazardous).
                    ImmutableArray.Create<PropertySetAbstractValueKind>(
                        PropertySetAbstractValueKind.Unflagged)),
                new PropertyMapperCollection(
                    new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                        "AString",
                        pointsToAbstractValue =>
                        {
                            return pointsToAbstractValue.NullState switch
                            {
                                NullAbstractValue.Null => PropertySetAbstractValueKind.Unflagged,
                                NullAbstractValue.NotNull => PropertySetAbstractValueKind.Flagged,
                                NullAbstractValue.MaybeNull => PropertySetAbstractValueKind.MaybeFlagged,
                                _ => PropertySetAbstractValueKind.Unknown,
                            };
                        })),
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(
                        HazardousUsageEvaluatorKind.Return,
                        abstractValue =>
                        {
                            // With only one property being tracked, this is straightforward.
                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        })));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNullOnReturn_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    TestTypeToTrack TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        return t;
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNullOnReturn,
                (8, 16, null, HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNullOnReturn_StringEmpty_Flagged()
            => VerifyCSharp(@"
using System;
class TestClass
{
    TestTypeToTrack TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = String.Empty;
        return t;
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNullOnReturn,
                (9, 16, null, HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNullOnReturns_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    TestTypeToTrack TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = null;
        return t;
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNullOnReturn);

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNonNullOnReturns_ReturnObject_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    object TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        return new object();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNullOnReturn);

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrack.AString and
        /// TestTypeToTrack.AnObject are aliases, and the aliased value is not null, when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfStringObjectIsNonNull =
            new(
                "TestTypeToTrack",
                new ConstructorMapper(     // Only one constructor, which leaves its AString property as null (not hazardous).
                    ImmutableArray.Create<PropertySetAbstractValueKind>(
                        PropertySetAbstractValueKind.Unflagged)),
                new PropertyMapperCollection(
                    new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                        "AString",
                        pointsToAbstractValue =>
                        {
                            return pointsToAbstractValue.NullState switch
                            {
                                NullAbstractValue.Null => PropertySetAbstractValueKind.Unflagged,
                                NullAbstractValue.NotNull => PropertySetAbstractValueKind.Flagged,
                                NullAbstractValue.MaybeNull => PropertySetAbstractValueKind.MaybeFlagged,
                                _ => PropertySetAbstractValueKind.Unknown,
                            };
                        },
                        propertyIndex: 0),    // Both AString and AnObject point to index 0.
                    new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                        "AnObject",
                        pointsToAbstractValue =>
                        {
                            return pointsToAbstractValue.NullState switch
                            {
                                NullAbstractValue.Null => PropertySetAbstractValueKind.Unflagged,
                                NullAbstractValue.NotNull => PropertySetAbstractValueKind.Flagged,
                                NullAbstractValue.MaybeNull => PropertySetAbstractValueKind.MaybeFlagged,
                                _ => PropertySetAbstractValueKind.Unknown,
                            };
                        },
                        propertyIndex: 0)),    // Both AString and AnObject point to index 0.
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                        "Method",
                        (methodSymbol, abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only underlying value (from the two "aliased" properties) being tracked, this is straightforward.
                            return abstractValue[0] switch
                            {
                                PropertySetAbstractValueKind.Flagged => HazardousUsageEvaluationResult.Flagged,
                                PropertySetAbstractValueKind.MaybeFlagged => HazardousUsageEvaluationResult.MaybeFlagged,
                                _ => HazardousUsageEvaluationResult.Unflagged,
                            };
                        })));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringObjectIsNonNull_AStringNonNull_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringObjectIsNonNull,
                (8, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringObjectIsNonNull_AnObjectNonNull_Flagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AnObject = new System.Random();
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringObjectIsNonNull,
                (8, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringObjectIsNonNull_StringEmpty_Flagged()
            => VerifyCSharp(@"
using System;
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = String.Empty;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringObjectIsNonNull,
                (9, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.Flagged));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringObjectIsNonNull_StringNonNull_ObjectNull_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = ""A non-null string"";
        t.AnObject = null;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringObjectIsNonNull);

        [Fact]
        public void TestTypeToTrack_HazardousIfStringObjectIsNonNull_AnObjectNonNull_StringNull_Unflagged()
            => VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AnObject = new System.Random();
        t.AString = null;
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringObjectIsNonNull);

        private ITestOutputHelper TestOutput { get; }

        public PropertySetAnalysisTests(ITestOutputHelper output)
        {
            this.TestOutput = output;
        }

        protected static readonly CompilationOptions s_CSharpDefaultOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal const string DefaultFilePathPrefix = "Test";
        internal const string CSharpDefaultFileExt = "cs";
        protected static readonly string CSharpDefaultFilePath = DefaultFilePathPrefix + 0 + "." + CSharpDefaultFileExt;

        private const string TestProjectName = "TestProject";

        protected static Project CreateProject(string[] sources)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = CSharpDefaultFileExt;
            CompilationOptions options = s_CSharpDefaultOptions;

            ProjectId projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var defaultReferences = ReferenceAssemblies.NetFramework.Net48.Default;
            defaultReferences = defaultReferences.AddPackages(ImmutableArray.Create(new PackageIdentity("System.DirectoryServices", "6.0.1")));
            var references = Task.Run(() => defaultReferences.ResolveAsync(LanguageNames.CSharp, CancellationToken.None)).GetAwaiter().GetResult();

#pragma warning disable CA2000 // Dispose objects before losing scope - Current solution/project takes the dispose ownership of the created AdhocWorkspace
            Project project = new AdhocWorkspace().CurrentSolution
#pragma warning restore CA2000 // Dispose objects before losing scope
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .AddMetadataReferences(projectId, references)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.CodeAnalysisReference)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.WorkspacesReference)
#if !NETCOREAPP
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemWebReference)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemRuntimeSerialization)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemXaml)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.PresentationFramework)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemWebExtensions)
#endif
                .WithProjectCompilationOptions(projectId, options)
                .WithProjectParseOptions(projectId, new CSharpParseOptions())
                .GetProject(projectId)!;

            Assert.NotNull(project.ParseOptions);

            // Enable Flow-Analysis feature on the project
            var parseOptions = project.ParseOptions.WithFeatures(
                project.ParseOptions.Features.Concat(
                    new[] { new KeyValuePair<string, string>("flow-analysis", "true") }));
            project = project.WithParseOptions(parseOptions);

            MetadataReference symbolsReference = AdditionalMetadataReferences.CSharpSymbolsReference;
            project = project.AddMetadataReference(symbolsReference);

            project = project.AddMetadataReference(AdditionalMetadataReferences.SystemCollectionsImmutableReference);
            project = project.AddMetadataReference(AdditionalMetadataReferences.SystemXmlDataReference);

            int count = 0;
            foreach (var source in sources)
            {
                string newFileName = fileNamePrefix + count++ + "." + fileExt;
                DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                project = project.AddDocument(newFileName, SourceText.From(source)).Project;
            }

            return project;
        }

        protected static (IOperation? operation, SemanticModel? model, SyntaxNode? node) GetOperationAndSyntaxForTest<TSyntaxNode>(CSharpCompilation compilation)
    where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode? syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            if (syntaxNode == null)
            {
                return (null, null, null);
            }

            var operation = model.GetOperation(syntaxNode);
            if (operation != null)
            {
                Assert.Same(model, operation.SemanticModel);
            }

            return (operation, model, syntaxNode);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxTree syntaxTree)
        {
            return GetSyntaxNodeList(syntaxTree.GetRoot(), null);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxNode node, List<SyntaxNode>? synList)
        {
            synList ??= [];

            synList.Add(node);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var childNode = child.AsNode();
                    Assert.NotNull(childNode);
                    synList = GetSyntaxNodeList(childNode, synList);
                }
            }

            return synList;
        }

        protected const string StartString = "/*<bind>*/";
        protected const string EndString = "/*</bind>*/";

        protected static TNode? GetSyntaxNodeOfTypeForBinding<TNode>(List<SyntaxNode> synList) where TNode : SyntaxNode
        {
            foreach (var node in synList.OfType<TNode>())
            {
                string exprFullText = node.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(EndString, StringComparison.Ordinal))
                    {
                        if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }

                if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(StartString, StringComparison.Ordinal))
                    {
                        if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }
            }

            return null;
        }
    }
}
