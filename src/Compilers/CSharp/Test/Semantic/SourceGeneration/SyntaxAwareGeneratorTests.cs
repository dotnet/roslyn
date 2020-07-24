// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

#nullable enable
namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class SyntaxAwareGeneratorTests
         : CSharpTestBase
    {

        [Fact]
        public void Syntax_Receiver_Is_Present_When_Registered()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver()),
                onExecute: (e) => receiver = e.SyntaxReceiver
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver.RunFullGeneration(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxReceiver>(receiver);
        }

        [Fact]
        public void Syntax_Receiver_Is_Null_WhenNot_Registered()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => { },
                onExecute: (e) => receiver = e.SyntaxReceiver
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver.RunFullGeneration(compilation, out var outputCompilation, out _);

            Assert.Null(receiver);
        }

        [Fact]
        public void Syntax_Receiver_Can_Be_Registered_Only_Once()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var testGenerator = new CallbackGenerator(
                onInit: Initialize,
                onExecute: (e) => { }
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver.RunFullGeneration(compilation, out _, out _);

            void Initialize(InitializationContext initContext)
            {
                initContext.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
                Assert.Throws<InvalidOperationException>(() =>
                {
                    initContext.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
                });
            }
        }

        [Fact]
        public void Syntax_Receiver_Visits_Syntax_In_Compilation()
        {
            var source = @"
class C 
{
    int Property { get; set; }

    void Function()
    {
        var x = 5;
        x += 4;
    }
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver()),
                onExecute: (e) => receiver = e.SyntaxReceiver
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver.RunFullGeneration(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxReceiver>(receiver);

            TestSyntaxReceiver testReceiver = (TestSyntaxReceiver)receiver!;
            Assert.Equal(21, testReceiver.VisitedNodes.Count);
            Assert.IsType<CompilationUnitSyntax>(testReceiver.VisitedNodes[0]);
        }

        [Fact]
        public void Syntax_Receiver_Is_Not_Reused_Between_Invocations()
        {
            var source = @"
class C 
{
    int Property { get; set; }

    void Function()
    {
        var x = 5;
        x += 4;
    }
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxReceiver? receiver = null;
            int invocations = 0;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(++invocations)),
                onExecute: (e) => receiver = e.SyntaxReceiver
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver = driver.RunFullGeneration(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxReceiver>(receiver);

            TestSyntaxReceiver testReceiver = (TestSyntaxReceiver)receiver!;
            Assert.Equal(1, testReceiver.Tag);
            Assert.Equal(21, testReceiver.VisitedNodes.Count);
            Assert.IsType<CompilationUnitSyntax>(testReceiver.VisitedNodes[0]);

            var previousReceiver = receiver;
            driver = driver.RunFullGeneration(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.NotEqual(receiver, previousReceiver);

            testReceiver = (TestSyntaxReceiver)receiver!;
            Assert.Equal(2, testReceiver.Tag);
            Assert.Equal(21, testReceiver.VisitedNodes.Count);
            Assert.IsType<CompilationUnitSyntax>(testReceiver.VisitedNodes[0]);
        }

        [Fact]
        public void Syntax_Receiver_Exception_During_Creation()
        {
            var source = @"
class C 
{
    int Property { get; set; }

    void Function()
    {
        var x = 5;
        x += 4;
    }
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            Exception ex = new Exception("Test Exception");

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => throw ex),
                onExecute: (e) => { }
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver = driver.RunFullGeneration(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Empty(results.SyntaxTrees);
            Assert.Single(results.Diagnostics);
            Assert.Single(results.Results);
            Assert.Single(results.Results[0].Diagnostics);

            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", ex.Message);

            outputDiagnostics.Verify(
                Diagnostic(ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Syntax_Receiver_Exception_During_Visit()
        {
            var source = @"
class C 
{
    int Property { get; set; }

    void Function()
    {
        var x = 5;
        x += 4;
    }
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            Exception ex = new Exception("Test Exception");

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 0, callback: (a) => { if (a is AssignmentExpressionSyntax) throw ex; })),
                onExecute: (e) => { e.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)); }
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver = driver.RunFullGeneration(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Empty(results.SyntaxTrees);
            Assert.Single(results.Diagnostics);
            Assert.Single(results.Results);
            Assert.Single(results.Results[0].Diagnostics);

            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", ex.Message);

            outputDiagnostics.Verify(
                Diagnostic(ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Syntax_Receiver_Exception_During_Visit_Doesnt_Stop_Other_Receivers()
        {
            var source = @"
class C 
{
    int Property { get; set; }

    void Function()
    {
        var x = 5;
        x += 4;
    }
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            Exception ex = new Exception("Test Exception");

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 0, callback: (a) => { if (a is AssignmentExpressionSyntax) throw ex; })),
                onExecute: (e) => { }
                );

            ISyntaxReceiver? receiver = null;
            var testGenerator2 = new CallbackGenerator2(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 1)),
                onExecute: (e) => { receiver = e.SyntaxReceiver; e.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)); }
                );

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(testGenerator, testGenerator2), CompilerAnalyzerConfigOptionsProvider.Empty, ImmutableArray<AdditionalText>.Empty);
            driver = driver.RunFullGeneration(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Single(results.SyntaxTrees);
            Assert.Single(results.Diagnostics);
            Assert.Equal(2, results.Results.Length);

            Assert.Single(results.Results[0].Diagnostics);
            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", ex.Message);

            Assert.Empty(results.Results[1].Diagnostics);

            var testReceiver = (TestSyntaxReceiver)receiver!;
            Assert.Equal(1, testReceiver.Tag);
            Assert.Equal(21, testReceiver.VisitedNodes.Count);

            outputDiagnostics.Verify(
                Diagnostic(ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator").WithLocation(1, 1)
                );
        }

        class TestSyntaxReceiver : ISyntaxReceiver
        {
            private readonly Action<SyntaxNode>? _callback;

            public List<SyntaxNode> VisitedNodes { get; } = new List<SyntaxNode>();

            public int Tag { get; }

            public TestSyntaxReceiver(int tag = 0, Action<SyntaxNode>? callback = null)
            {
                Tag = tag;
                _callback = callback;
            }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                VisitedNodes.Add(syntaxNode);
                if (_callback is object)
                {
                    _callback(syntaxNode);
                }
            }
        }
    }
}
