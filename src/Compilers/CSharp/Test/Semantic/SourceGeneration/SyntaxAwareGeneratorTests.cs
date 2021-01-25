// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

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

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

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

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Null(receiver);
        }

        [Fact]
        public void SyntaxContext_Receiver_Is_Present_When_Registered()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxContextReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver()),
                onExecute: (e) => receiver = e.SyntaxContextReceiver
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxContextReceiver>(receiver);
        }

        [Fact]
        public void SyntaxContext_Receiver_Is_Null_WhenNot_Registered()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxContextReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => { },
                onExecute: (e) => receiver = e.SyntaxContextReceiver
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Null(receiver);
        }

        [Fact]
        public void SyntaxContext_Receiver_Is_Null_When_Syntax_Receiver_Registered()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxContextReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver()),
                onExecute: (e) => receiver = e.SyntaxContextReceiver
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.Null(receiver);
        }

        [Fact]
        public void Syntax_Receiver_Is_Null_When_SyntaxContext_Receiver_Registered()
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
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver()),
                onExecute: (e) => receiver = e.SyntaxReceiver
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.Null(receiver);
        }

        [Fact]
        public void Syntax_Receiver_Can_Be_Registered_Only_Once()
        {
            // ISyntaxReceiver + ISyntaxReceiver
            GeneratorInitializationContext init = new GeneratorInitializationContext(CancellationToken.None);
            init.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
            Assert.Throws<InvalidOperationException>(() =>
            {
                init.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
            });

            // ISyntaxContextReceiver + ISyntaxContextReceiver
            init = new GeneratorInitializationContext(CancellationToken.None);
            init.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver());
            Assert.Throws<InvalidOperationException>(() =>
            {
                init.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver());
            });

            // ISyntaxContextReceiver + ISyntaxReceiver
            init = new GeneratorInitializationContext(CancellationToken.None);
            init.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver());
            Assert.Throws<InvalidOperationException>(() =>
            {
                init.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
            });


            // ISyntaxReceiver + ISyntaxContextReceiver
            init = new GeneratorInitializationContext(CancellationToken.None);
            init.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
            Assert.Throws<InvalidOperationException>(() =>
            {
                init.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver());
            });
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

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

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

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxReceiver>(receiver);

            TestSyntaxReceiver testReceiver = (TestSyntaxReceiver)receiver!;
            Assert.Equal(1, testReceiver.Tag);
            Assert.Equal(21, testReceiver.VisitedNodes.Count);
            Assert.IsType<CompilationUnitSyntax>(testReceiver.VisitedNodes[0]);

            var previousReceiver = receiver;
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

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

            var exception = new Exception("Test Exception");
            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications((SyntaxReceiverCreator)(() => throw exception)),
                onExecute: (e) => { Assert.True(false); }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Empty(results.GeneratedTrees);
            Assert.Single(results.Diagnostics);
            Assert.Single(results.Results);
            Assert.Single(results.Results[0].Diagnostics);

            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", results.Results[0].Exception?.Message);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "Exception", "Test Exception").WithLocation(1, 1)
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

            var exception = new Exception("Test Exception");
            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 0, callback: (a) => { if (a is AssignmentExpressionSyntax) throw exception; })),
                onExecute: (e) => { e.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)); }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Empty(results.GeneratedTrees);
            Assert.Single(results.Diagnostics);
            Assert.Single(results.Results);
            Assert.Single(results.Results[0].Diagnostics);

            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", results.Results[0].Exception?.Message);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "Exception", "Test Exception").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Syntax_Receiver_Exception_During_Visit_Stops_Visits_On_Other_Trees()
        {
            var source1 = @"
class C 
{
    int Property { get; set; }
}
";
            var source2 = @"
class D
{
    public void Method() { }
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Equal(2, compilation.SyntaxTrees.Count());

            TestSyntaxReceiver receiver1 = new TestSyntaxReceiver(tag: 0, callback: (a) => { if (a is PropertyDeclarationSyntax) throw new Exception("Test Exception"); });
            var testGenerator1 = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => receiver1),
                onExecute: (e) => { }
                );

            TestSyntaxReceiver receiver2 = new TestSyntaxReceiver(tag: 1);
            var testGenerator2 = new CallbackGenerator2(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => receiver2),
                onExecute: (e) => { }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator1, testGenerator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.DoesNotContain(receiver1.VisitedNodes, n => n is MethodDeclarationSyntax);
            Assert.Contains(receiver2.VisitedNodes, n => n is MethodDeclarationSyntax);

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

            var exception = new Exception("Test Exception");
            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 0, callback: (a) => { if (a is AssignmentExpressionSyntax) throw exception; })),
                onExecute: (e) => { }
                );

            ISyntaxReceiver? receiver = null;
            var testGenerator2 = new CallbackGenerator2(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 1)),
                onExecute: (e) => { receiver = e.SyntaxReceiver; e.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)); }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator, testGenerator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Single(results.GeneratedTrees);
            Assert.Single(results.Diagnostics);
            Assert.Equal(2, results.Results.Length);

            Assert.Single(results.Results[0].Diagnostics);
            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", results.Results[0].Exception?.Message);

            Assert.Empty(results.Results[1].Diagnostics);

            var testReceiver = (TestSyntaxReceiver)receiver!;
            Assert.Equal(1, testReceiver.Tag);
            Assert.Equal(21, testReceiver.VisitedNodes.Count);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "Exception", "Test Exception").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Syntax_Receiver_Is_Not_Created_If_Exception_During_Initialize()
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

            TestSyntaxReceiver? receiver = null;
            var exception = new Exception("test exception");
            var testGenerator = new CallbackGenerator(
                onInit: (i) => { i.RegisterForSyntaxNotifications(() => receiver = new TestSyntaxReceiver()); throw exception; },
                onExecute: (e) => { Assert.True(false); }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Null(receiver);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringInitialization).WithArguments("CallbackGenerator", "Exception", "test exception").WithLocation(1, 1)
                );
        }

        class TestReceiverBase<T>
        {
            private readonly Action<T>? _callback;

            public List<T> VisitedNodes { get; } = new List<T>();

            public int Tag { get; }

            public TestReceiverBase(int tag = 0, Action<T>? callback = null)
            {
                Tag = tag;
                _callback = callback;
            }

            public void OnVisitSyntaxNode(T syntaxNode)
            {
                VisitedNodes.Add(syntaxNode);
                if (_callback is object)
                {
                    _callback(syntaxNode);
                }
            }
        }

        class TestSyntaxReceiver : TestReceiverBase<SyntaxNode>, ISyntaxReceiver
        {
            public TestSyntaxReceiver(int tag = 0, Action<SyntaxNode>? callback = null)
                : base(tag, callback)
            {
            }
        }

        class TestSyntaxContextReceiver : TestReceiverBase<GeneratorSyntaxContext>, ISyntaxContextReceiver
        {
            public TestSyntaxContextReceiver(int tag = 0, Action<GeneratorSyntaxContext>? callback = null)
                : base(tag, callback)
            {
            }
        }
    }
}
