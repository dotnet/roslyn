// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Text;
using Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
    public class PropertySetAnalysisTests : DiagnosticAnalyzerTestBase
    {
        /// <summary>
        /// Just a container for parameters necessary for PropertySetAnalysis for unit tests below.
        /// </summary>
        private class PropertySetAnalysisParameters
        {
            public PropertySetAnalysisParameters(string typeToTrack, ConstructorMapper constructorMapper, PropertyMapperCollection propertyMapperCollection, HazardousUsageEvaluatorCollection hazardousUsageEvaluatorCollection)
            {
                TypeToTrack = typeToTrack ?? throw new ArgumentNullException(nameof(typeToTrack));
                ConstructorMapper = constructorMapper ?? throw new ArgumentNullException(nameof(constructorMapper));
                PropertyMapperCollection = propertyMapperCollection ?? throw new ArgumentNullException(nameof(propertyMapperCollection));
                HazardousUsageEvaluatorCollection = hazardousUsageEvaluatorCollection ?? throw new ArgumentNullException(nameof(hazardousUsageEvaluatorCollection));
            }

            public string TypeToTrack { get; }
            public ConstructorMapper ConstructorMapper { get; }
            public PropertyMapperCollection PropertyMapperCollection { get; }
            public HazardousUsageEvaluatorCollection HazardousUsageEvaluatorCollection { get; }
        }

        /// <summary>
        /// Verification helper.
        /// </summary>
        /// <param name="source">C# source code.</param>
        /// <param name="propertySetAnalysisParameters">PropertySetAnalysis parameters.</param>
        /// <param name="expectedResults">Expected hazardous usages.</param>
        private void VerifyCSharp(
            string source,
            PropertySetAnalysisParameters propertySetAnalysisParameters,
            params (int Line, int Column, string Method, HazardousUsageEvaluationResult Result)[] expectedResults)
        {
            if (expectedResults == null)
            {
                expectedResults = Array.Empty<(int Line, int Column, string MethodName, HazardousUsageEvaluationResult Result)>();
            }

            Project project = CreateProject(new string[] { source, TestTypeToTrackSource });
            Compilation compilation = project.GetCompilationAsync().Result;
            CompilationUtils.ValidateNoCompileErrors(compilation.GetDiagnostics());
            (IOperation operation, SemanticModel model, SyntaxNode syntaxNode) = GetOperationAndSyntaxForTest<BlockSyntax>((CSharpCompilation)compilation);
            Assert.True(
                operation != null,
                $"Could not find code block to analyze.  Does your test code have {StartString} and {EndString} around the braces of block to analyze?");
            ISymbol symbol = syntaxNode.Parent.GetDeclaredOrReferencedSymbol(model);

            ImmutableDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> actual =
                PropertySetAnalysis.GetOrComputeHazardousUsages(
                    operation.GetEnclosingControlFlowGraph(),
                    compilation,
                    symbol,
                    propertySetAnalysisParameters.TypeToTrack,
                    propertySetAnalysisParameters.ConstructorMapper,
                    propertySetAnalysisParameters.PropertyMapperCollection,
                    propertySetAnalysisParameters.HazardousUsageEvaluatorCollection,
                    InterproceduralAnalysisConfiguration.Create(
                        new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                        ImmutableArray<DiagnosticDescriptor>.Empty,
                        InterproceduralAnalysisKind.None,
                        new CancellationTokenSource().Token,
                        defaultMaxInterproceduralMethodCallChain: 1));
            try
            {
                Assert.Equal(expectedResults.Length, actual.Count);
                foreach ((int Line, int Column, string Method, HazardousUsageEvaluationResult Result) expected in expectedResults)
                {
                    HazardousUsageEvaluationResult? actualResult = null;
                    foreach (KeyValuePair<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> kvp in actual)
                    {
                        FileLinePositionSpan span = kvp.Key.Location.GetLineSpan();
                        if (span.Path != "Test0.cs")
                        {
                            continue;
                        }

                        if (span.StartLinePosition.Line + 1 == expected.Line
                            && span.StartLinePosition.Character + 1 == expected.Column
                            && kvp.Key.Method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) == expected.Method)
                        {
                            actualResult = kvp.Value;
                            break;
                        }
                    }

                    Assert.True(
                        actualResult.HasValue,
                        $"Could not find expected result Line {expected.Line} Column {expected.Column} Method {expected.Method} Result {expected.Result}");
                    Assert.True(
                        actualResult == expected.Result,
                        $"Expected {expected.Result}, Actual {actualResult}, for Line {expected.Line} Column {expected.Column} Method {expected.Method}");
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

                foreach (KeyValuePair<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> kvp in actual)
                {
                    LinePosition linePosition = kvp.Key.Location.GetLineSpan().StartLinePosition;
                    int lineNumber = linePosition.Line + 1;
                    int columnNumber = linePosition.Character + 1;
                    TestOutput.WriteLine(
                        $"Line {lineNumber}, Column {columnNumber}, Method {kvp.Key.Method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}: {kvp.Value}");
                }
                TestOutput.WriteLine("============================");

                throw;
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
}";

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrack.AString property is not null when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfStringIsNonNull =
            new PropertySetAnalysisParameters(
                "TestTypeToTrack",
                new ConstructorMapper(     // Only one constructor, which leaves its AString property as null (not hazardous).
                    ImmutableArray.Create<PropertySetAbstractValueKind>(
                        PropertySetAbstractValueKind.Unflagged)),
                new PropertyMapperCollection(
                    new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                        "AString",
                        (NullAbstractValue nullAbstractValue) =>
                        {
                            switch (nullAbstractValue)
                            {
                                case NullAbstractValue.Null:
                                    return PropertySetAbstractValueKind.Unflagged;
                                case NullAbstractValue.NotNull:
                                    return PropertySetAbstractValueKind.Flagged;
                                default:
                                    return PropertySetAbstractValueKind.MaybeFlagged;
                            }
                        })),
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                        "Method",
                        (IMethodSymbol methodSymbol, PropertySetAbstractValue abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.
                            switch (abstractValue[0])
                            {
                                case PropertySetAbstractValueKind.Flagged:
                                    return HazardousUsageEvaluationResult.Flagged;
                                case PropertySetAbstractValueKind.MaybeFlagged:
                                    return HazardousUsageEvaluationResult.MaybeFlagged;
                                default:
                                    return HazardousUsageEvaluationResult.Unflagged;
                            }
                        })));

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNull_Flagged()
        {
            VerifyCSharp(@"
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
        }

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNull_StringEmpty_MaybeFlagged()
        {
            VerifyCSharp(@"
using System;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrack t = new TestTypeToTrack();
        t.AString = String.Empty;   // Ideally String.Empty would be NullAbstractValue.NonNull.
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrack_HazardousIfStringIsNonNull,
                (10, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.MaybeFlagged));
        }

        [Fact]
        public void TestTypeToTrack_HazardousIfStringIsNull_Unflagged()
        {
            VerifyCSharp(@"
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
        }

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrackWithConstructor.AString property is not null when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull =
            new PropertySetAnalysisParameters(
                "TestTypeToTrackWithConstructor",
                new ConstructorMapper(
                    (IMethodSymbol method, IReadOnlyList<NullAbstractValue> argumentNullAbstractValues) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.
                        PropertySetAbstractValueKind kind = PropertySetAbstractValueKind.Unknown;
                        if (method.Parameters.Length >= 2)
                        {
                            // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                            switch (argumentNullAbstractValues[2])
                            {
                                case NullAbstractValue.Null:
                                    kind = PropertySetAbstractValueKind.Unflagged;
                                    break;
                                case NullAbstractValue.NotNull:
                                    kind = PropertySetAbstractValueKind.Flagged;
                                    break;
                                default:
                                    kind = PropertySetAbstractValueKind.MaybeFlagged;
                                    break;
                            }
                        }

                        return PropertySetAbstractValue.GetInstance(kind);
                    }),
            new PropertyMapperCollection(
                new PropertyMapper(    // Definitely null => unflagged, definitely non-null => flagged, otherwise => maybe.
                    "String",
                    (NullAbstractValue nullAbstractValue) =>
                    {
                        switch (nullAbstractValue)
                        {
                            case NullAbstractValue.Null:
                                return PropertySetAbstractValueKind.Unflagged;
                            case NullAbstractValue.NotNull:
                                return PropertySetAbstractValueKind.Flagged;
                            default:
                                return PropertySetAbstractValueKind.MaybeFlagged;
                        }
                    })),
            new HazardousUsageEvaluatorCollection(
                new HazardousUsageEvaluator(
                    "Method",
                    (IMethodSymbol methodSymbol, PropertySetAbstractValue abstractValue) =>
                    {
                        // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                        // With only one property being tracked, this is straightforward.
                        switch (abstractValue[0])
                        {
                            case PropertySetAbstractValueKind.Flagged:
                                return HazardousUsageEvaluationResult.Flagged;
                            case PropertySetAbstractValueKind.MaybeFlagged:
                                return HazardousUsageEvaluationResult.MaybeFlagged;
                            default:
                                return HazardousUsageEvaluationResult.Unflagged;
                        }
                    })));

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_Flagged()
        {
            VerifyCSharp(@"
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
        }

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_StringEmpty_MaybeFlagged()
        {
            VerifyCSharp(@"
using System;

class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, String.Empty);   // Ideally String.Empty would be NullAbstractValue.NonNull.
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull,
                (9, 9, "void TestTypeToTrack.Method()", HazardousUsageEvaluationResult.MaybeFlagged));
        }

        [Fact]
        public void TestTypeToTrackWithConstructor_HazardousIfStringIsNull_Unflagged()
        {
            VerifyCSharp(@"
class TestClass
{
    void TestMethod()
    /*<bind>*/{
        TestTypeToTrackWithConstructor t = new TestTypeToTrackWithConstructor(default(TestEnum), null, null);
        t.Method();
    }/*</bind>*/
}",
                TestTypeToTrackWithConstructor_HazardousIfStringIsNonNull);
        }

        /// <summary>
        /// Parameters for PropertySetAnalysis to flag hazardous usage when the TestTypeToTrack.AnEnum property is Value1 when calling its Method() method.
        /// </summary>
        private readonly PropertySetAnalysisParameters TestTypeToTrack_HazardousIfEnumIsValue0 =
            new PropertySetAnalysisParameters(
                "TestTypeToTrack",
                new ConstructorMapper(     // Only one constructor, which leaves its AnEnum property as Value1 (hazardous).
                    ImmutableArray.Create<PropertySetAbstractValueKind>(
                        PropertySetAbstractValueKind.Flagged)),
                new PropertyMapperCollection(
                    new PropertyMapper(
                        "AnEnum",
                        (ValueContentAbstractValue valueContentAbstractValue) =>
                        {
                            switch (valueContentAbstractValue.NonLiteralState)
                            {
                                case ValueContainsNonLiteralState.No:
                                    // We know all values, so we can say Flagged or Unflagged.
                                    return valueContentAbstractValue.LiteralValues.Contains(0)
                                        ? PropertySetAbstractValueKind.Flagged
                                        : PropertySetAbstractValueKind.Unflagged;
                                case ValueContainsNonLiteralState.Maybe:
                                    // We don't know all values, so we can say Flagged, or who knows.
                                    return valueContentAbstractValue.LiteralValues.Contains(0)
                                        ? PropertySetAbstractValueKind.Flagged
                                        : PropertySetAbstractValueKind.Unknown;
                                default:
                                    return PropertySetAbstractValueKind.Unknown;
                            }
                        })),
                new HazardousUsageEvaluatorCollection(
                    new HazardousUsageEvaluator(    // When TypeToTrack.Method() is invoked, need to evaluate its state.
                        "Method",
                        (IMethodSymbol methodSymbol, PropertySetAbstractValue abstractValue) =>
                        {
                            // When doing this for reals, need to examine the method to make sure we're looking at the right method and arguments.

                            // With only one property being tracked, this is straightforward.
                            switch (abstractValue[0])
                            {
                                case PropertySetAbstractValueKind.Flagged:
                                    return HazardousUsageEvaluationResult.Flagged;
                                case PropertySetAbstractValueKind.MaybeFlagged:
                                    return HazardousUsageEvaluationResult.MaybeFlagged;
                                default:
                                    return HazardousUsageEvaluationResult.Unflagged;
                            }
                        })));

        [Fact]
        public void TestTypeToTrack_HazardousIfEnumIsValue0_Flagged()
        {
            VerifyCSharp(@"
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
        }

        [Fact]
        public void TestTypeToTrack_HazardousIfEnumIsValue0_Unflagged()
        {
            VerifyCSharp(@"
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
        }

        #region Infrastructure
        private ITestOutputHelper TestOutput { get; }

        public PropertySetAnalysisTests(ITestOutputHelper output)
        {
            this.TestOutput = output;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return null;
        }
        #endregion Infrastructure
    }
}
