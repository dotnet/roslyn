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
using Roslyn.Test.Utilities;
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

            ISyntaxReceiver? syntaxReceiver = null;
            ISyntaxContextReceiver? contextReceiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver()),
                onExecute: (e) => { syntaxReceiver = e.SyntaxReceiver; contextReceiver = e.SyntaxContextReceiver; }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.Null(contextReceiver);
            Assert.NotNull(syntaxReceiver);
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

            ISyntaxReceiver? syntaxReceiver = null;
            ISyntaxContextReceiver? contextReceiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver()),
                onExecute: (e) => { syntaxReceiver = e.SyntaxReceiver; contextReceiver = e.SyntaxContextReceiver; }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.Null(syntaxReceiver);
            Assert.NotNull(contextReceiver);
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
        public void SyntaxContext_Receiver_Visits_Syntax_In_Compilation()
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

            ISyntaxContextReceiver? receiver = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver()),
                onExecute: (e) => receiver = e.SyntaxContextReceiver
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxContextReceiver>(receiver);

            TestSyntaxContextReceiver testReceiver = (TestSyntaxContextReceiver)receiver!;
            Assert.Equal(21, testReceiver.VisitedNodes.Count);
            Assert.IsType<CompilationUnitSyntax>(testReceiver.VisitedNodes[0].Node);
            Assert.NotNull(testReceiver.VisitedNodes[0].SemanticModel);
            Assert.Equal(testReceiver.VisitedNodes[0].SemanticModel.SyntaxTree, testReceiver.VisitedNodes[0].Node.SyntaxTree);
        }

        [Fact]
        public void Syntax_Receiver_Is_Not_Reused_Between_Non_Cached_Invocations()
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

            // update the compilation. In v1 we always re-created the receiver, but in v2 we only re-create
            // it if the compilation has changed.
            compilation = compilation.WithAssemblyName("modified");

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

        [Fact]
        public void Syntax_Receiver_Return_Null_During_Creation()
        {
            var source = @"
class C 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxReceiver? syntaxRx = null;
            ISyntaxContextReceiver? syntaxContextRx = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications((SyntaxReceiverCreator)(() => null!)),
                onExecute: (e) => { syntaxRx = e.SyntaxReceiver; syntaxContextRx = e.SyntaxContextReceiver; }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            outputDiagnostics.Verify();
            var results = driver.GetRunResult();
            Assert.Empty(results.GeneratedTrees);
            Assert.Null(syntaxContextRx);
            Assert.Null(syntaxRx);
        }

        [Fact]
        public void Syntax_Receiver_Is_Not_Created_If_Exception_During_PostInitialize()
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
                onInit: (i) =>
                {
                    i.RegisterForSyntaxNotifications(() => receiver = new TestSyntaxReceiver());
                    i.RegisterForPostInitialization((pic) => throw exception);
                },
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

        [Fact]
        public void Syntax_Receiver_Visits_Syntax_Added_In_PostInit()
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

            var source2 = @"
class D
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
                onInit: (i) =>
                {
                    i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver());
                    i.RegisterForPostInitialization((pic) => pic.AddSource("postInit", source2));
                },
                onExecute: (e) => receiver = e.SyntaxReceiver
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxReceiver>(receiver);

            TestSyntaxReceiver testReceiver = (TestSyntaxReceiver)receiver!;

            var classDeclarations = testReceiver.VisitedNodes.OfType<ClassDeclarationSyntax>().Select(c => c.Identifier.Text);
            Assert.Equal(new[] { "C", "D" }, classDeclarations);
        }

        [Fact]
        public void Syntax_Receiver_Visits_Syntax_Added_In_PostInit_From_Other_Generator()
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

            var source2 = @"
class D
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

            var testGenerator2 = new CallbackGenerator2(
                onInit: (i) => i.RegisterForPostInitialization((pic) => pic.AddSource("postInit", source2)),
                onExecute: (e) => { }
            );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator, testGenerator2 }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.NotNull(receiver);
            Assert.IsType<TestSyntaxReceiver>(receiver);

            TestSyntaxReceiver testReceiver = (TestSyntaxReceiver)receiver!;
            var classDeclarations = testReceiver.VisitedNodes.OfType<ClassDeclarationSyntax>().Select(c => c.Identifier.Text);
            Assert.Equal(new[] { "C", "D" }, classDeclarations);
        }

        [Fact]
        public void Syntax_Receiver_Can_Access_Types_Added_In_PostInit()
        {
            var source = @"
class C : D
{
}
";

            var postInitSource = @"
class D 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            Assert.Single(compilation.SyntaxTrees);

            compilation.VerifyDiagnostics(
                // (2,11): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                // class C : D
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(2, 11)
                );

            var testGenerator = new CallbackGenerator(
                onInit: (i) =>
                {
                    i.RegisterForSyntaxNotifications(() => new TestSyntaxContextReceiver(callback: (ctx) =>
                    {
                        if (ctx.Node is ClassDeclarationSyntax cds
                            && cds.Identifier.Value?.ToString() == "C")
                        {
                            // ensure we can query the semantic model for D
                            var dType = ctx.SemanticModel.Compilation.GetTypeByMetadataName("D");
                            Assert.NotNull(dType);
                            Assert.False(dType.IsErrorType());

                            // and the code referencing it now works
                            var typeInfo = ctx.SemanticModel.GetTypeInfo(cds.BaseList!.Types[0].Type);
                            Assert.Same(dType, typeInfo.Type);
                        }
                    }));
                    i.RegisterForPostInitialization((pic) => pic.AddSource("postInit", postInitSource));
                },
                onExecute: (e) => { }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        }

        [Fact]
        public void SyntaxContext_Receiver_Return_Null_During_Creation()
        {
            var source = @"
class C 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            ISyntaxReceiver? syntaxRx = null;
            ISyntaxContextReceiver? syntaxContextRx = null;

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications((SyntaxContextReceiverCreator)(() => null!)),
                onExecute: (e) => { syntaxRx = e.SyntaxReceiver; syntaxContextRx = e.SyntaxContextReceiver; }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            outputDiagnostics.Verify();
            var results = driver.GetRunResult();
            Assert.Empty(results.GeneratedTrees);
            Assert.Null(syntaxContextRx);
            Assert.Null(syntaxRx);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();


            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
        }

        [Fact]
        public void IncrementalGenerator_With_Multiple_Filters()
        {
            var source1 = @"
#pragma warning disable CS0414
class classC 
{
    string fieldA = null; 
}
";
            var source2 = @"
#pragma warning disable CS0414
class classD 
{
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();


            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                });

                var source2 = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is ClassDeclarationSyntax fds, (c, _) => ((ClassDeclarationSyntax)c.Node).Identifier.ValueText);
                context.RegisterSourceOutput(source2, (spc, className) =>
                {
                    spc.AddSource(className, "");
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("classC.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("classD.cs", results.GeneratedTrees[2].FilePath);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Does_Not_Run_When_Not_Changed()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            List<string> syntaxFilterVisited = new();
            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) =>
                {
                    if (c is FieldDeclarationSyntax fds)
                    {
                        syntaxFilterVisited.Add(fds.Declaration.Variables[0].Identifier.ValueText);
                        return true;
                    }
                    return false;
                }, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                });
            });

            // Don't enable incremental tracking here as incremental tracking disables the "unchanged compilation implies unchanged syntax trees" optimization.
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC" }, syntaxFilterVisited);

            syntaxFilterVisited.Clear();
            // run again on the *same* compilation
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Empty(syntaxFilterVisited);

            // now change the compilation, but don't change the syntax trees
            compilation = compilation.WithAssemblyName("newCompilation");
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC" }, syntaxFilterVisited);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Added_Tree()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}";

            var source2 = @"
#pragma warning disable CS0414
class D
{
    string fieldD = null; 
    string fieldE = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText).WithTrackingName("Fields");
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));

            // add the second tree and re-run
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source2, parseOptions));
            driver = driver.RunGenerators(compilation);

            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(5, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.EndsWith("fieldD.cs", results.GeneratedTrees[3].FilePath);
            Assert.EndsWith("fieldE.cs", results.GeneratedTrees[4].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldD", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldE", IncrementalStepRunReason.New), output)));
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Removed_Tree()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}";

            var source2 = @"
#pragma warning disable CS0414
class D
{
    string fieldD = null; 
    string fieldE = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText).WithTrackingName("Fields");
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(5, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.EndsWith("fieldD.cs", results.GeneratedTrees[3].FilePath);
            Assert.EndsWith("fieldE.cs", results.GeneratedTrees[4].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldD", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldE", IncrementalStepRunReason.New), output)));

            // remove the second tree and re-run
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last());
            driver = driver.RunGenerators(compilation);

            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldD", IncrementalStepRunReason.Removed), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldE", IncrementalStepRunReason.Removed), output)));
        }

        [Fact]
        [WorkItem(58647, "https://github.com/dotnet/roslyn/issues/58647")]
        public void IncrementalGenerator_With_Syntax_Filter_Removed_Tree_Add_New_Generator()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}";

            var source2 = @"
#pragma warning disable CS0414
class D
{
    string fieldD = null; 
    string fieldE = null;
}
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                });
            });

            var testGenerator2 = new PipelineCallbackGenerator2(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            // remove the second tree, and add the new generator
            compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last());
            driver = driver.AddGenerators(ImmutableArray.Create(testGenerator2.AsSourceGenerator()));
            driver = driver.RunGenerators(compilation);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Runs_Only_For_Changed_Trees()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
}";
            var source2 = @"
#pragma warning disable CS0414
class D 
{
    string fieldB = null; 
}";
            var source3 = @"
#pragma warning disable CS0414
class E 
{
    string fieldC = null; 
}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            List<string> fieldsCalledFor = new List<string>();
            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText).WithTrackingName("Fields");
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                    fieldsCalledFor.Add(fieldName);
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));

            // edit one of the syntax trees
            var firstTree = compilation.SyntaxTrees.First();
            var newTree = CSharpSyntaxTree.ParseText(@"
#pragma warning disable CS0414
class F 
{
    string fieldD = null; 
}", parseOptions);
            compilation = compilation.ReplaceSyntaxTree(firstTree, newTree);

            fieldsCalledFor.Clear();
            // now re-run the drivers 
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);

            // we produced the expected modified sources, but only called for the one different tree
            Assert.EndsWith("fieldD.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldD", IncrementalStepRunReason.Modified), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Unchanged), output)));
            Assert.Equal("fieldD", Assert.Single(fieldsCalledFor));
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Doesnt_Run_When_Compilation_Is_Unchanged()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
}";
            var source2 = @"
#pragma warning disable CS0414
class D 
{
    string fieldB = null; 
}";
            var source3 = @"
#pragma warning disable CS0414
class E 
{
    string fieldC = null; 
}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            List<string> fieldsCalledFor = new List<string>();
            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText).WithTrackingName("Fields");
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                    fieldsCalledFor.Add(fieldName);
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: false));
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC" }, fieldsCalledFor);

            // now re-run the drivers with the same compilation
            fieldsCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);

            // we produced the expected modified sources, but didn't call for any of the trees
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Empty(fieldsCalledFor);

            // now re-run the drivers with the same compilation again to ensure we cached the trees correctly
            fieldsCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);

            // we produced the expected modified sources, but didn't call for any of the trees
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Empty(fieldsCalledFor);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_And_Changed_Tree_Order()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
}";
            var source2 = @"
#pragma warning disable CS0414
class D 
{
    string fieldB = null; 
}";
            var source3 = @"
#pragma warning disable CS0414
class E 
{
    string fieldC = null; 
}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            List<string> syntaxFieldsCalledFor = new List<string>();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) =>
                {
                    if (c is FieldDeclarationSyntax fds)
                    {
                        syntaxFieldsCalledFor.Add(fds.Declaration.Variables[0].Identifier.ValueText);
                        return true;
                    }
                    return false;
                },
                (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText).WithTrackingName("Syntax")
                .Select((s, ct) => s).WithTrackingName("Fields");

                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    spc.AddSource(fieldName, "");
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new IncrementalGeneratorWrapper(testGenerator) },
                parseOptions: parseOptions,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));
            Assert.Equal("fieldA", syntaxFieldsCalledFor[0]);
            Assert.Equal("fieldB", syntaxFieldsCalledFor[1]);
            Assert.Equal("fieldC", syntaxFieldsCalledFor[2]);

            //swap the order of the first and last trees
            var firstTree = compilation.SyntaxTrees.First();
            var lastTree = compilation.SyntaxTrees.Last();
            var dummyTree = CSharpSyntaxTree.ParseText("", parseOptions);

            compilation = compilation.ReplaceSyntaxTree(firstTree, dummyTree)
                                     .ReplaceSyntaxTree(lastTree, firstTree)
                                     .ReplaceSyntaxTree(dummyTree, lastTree);

            // now re-run the drivers and confirm we didn't actually run the node selector for any nodes.
            // Verify that we still ran the transform.
            syntaxFieldsCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Syntax"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Unchanged), output)));
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.Cached), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Cached), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Cached), output)));
            Assert.Empty(syntaxFieldsCalledFor);


            // swap a tree for a tree with the same contents, but a new reference
            var newLastTree = CSharpSyntaxTree.ParseText(lastTree.ToString(), parseOptions);

            compilation = compilation.ReplaceSyntaxTree(firstTree, dummyTree)
                                     .ReplaceSyntaxTree(lastTree, firstTree)
                                     .ReplaceSyntaxTree(dummyTree, newLastTree);

            // now re-run the drivers and confirm we only ran the selector for the 'new' syntax tree
            // but then cached the result after the transform when we got the same value out
            syntaxFieldsCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Empty(results.Diagnostics);
            Assert.Equal(3, results.GeneratedTrees.Length);
            Assert.EndsWith("fieldA.cs", results.GeneratedTrees[0].FilePath);
            Assert.EndsWith("fieldB.cs", results.GeneratedTrees[1].FilePath);
            Assert.EndsWith("fieldC.cs", results.GeneratedTrees[2].FilePath);
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.Cached), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Cached), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Cached), output)));
            Assert.Equal("fieldC", Assert.Single(syntaxFieldsCalledFor));
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Can_Have_Comparer()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";

            var source2 = @"
#pragma warning disable CS0414
class C 
{
    string fieldD = null; 
    string fieldE = null;
    string fieldF = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) => ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                source = source.WithComparer(new LambdaComparer<string>((a, b) => true)).WithTrackingName("Fields");
                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new IncrementalGeneratorWrapper(testGenerator) },
                parseOptions: parseOptions,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var results = driver.GetRunResult();
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));

            // make a change to the syntax tree
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(source2, parseOptions));

            // when we run it again, we get cached steps with the original values because the comparer has suppressed the modification.
            // the original value is preserved to ensure that separate runs of the generator have the same output when the user-provided comparer returns true.
            driver = driver.RunGenerators(compilation);
            results = driver.GetRunResult();
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.Unchanged), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.Unchanged), output)));
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_And_Comparer_Doesnt_Do_Duplicate_Work()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            List<string> syntaxCalledFor = new List<string>();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) =>
                {
                    syntaxCalledFor.Add(((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                    return ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText;
                });
                source = source.WithComparer(new LambdaComparer<string>((a, b) => false));
                source = source.WithComparer(new LambdaComparer<string>((a, b) => false));
                source = source.WithComparer(new LambdaComparer<string>((a, b) => false));
                source = source.WithComparer(new LambdaComparer<string>((a, b) => false));
                context.RegisterSourceOutput(source, (spc, fieldName) => { });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            // verify we only call the syntax transform once, even though we created multiple nodes via the withComparer
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC" }, syntaxCalledFor);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_And_Comparer_Can_Feed_Two_Outputs()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";

            var source2 = @"
#pragma warning disable CS0414
class C 
{
    string fieldD = null; 
    string fieldE = null;
    string fieldF = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            List<string> syntaxCalledFor = new List<string>();
            List<string> noCompareCalledFor = new List<string>();
            List<string> compareCalledFor = new List<string>();


            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) =>
                {
                    syntaxCalledFor.Add(((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText);
                    return ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText;
                });

                context.RegisterSourceOutput(source, (spc, fieldName) =>
                {
                    noCompareCalledFor.Add(fieldName);
                });

                var comparerSource = source.WithComparer(new LambdaComparer<string>((a, b) => true));
                context.RegisterSourceOutput(comparerSource, (spc, fieldName) =>
                {
                    compareCalledFor.Add(fieldName);
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            // verify we ran the syntax transform twice, one for each output, even though we duplicated them
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC", "fieldA", "fieldB", "fieldC" }, syntaxCalledFor);
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC" }, noCompareCalledFor);
            Assert.Equal(new[] { "fieldA", "fieldB", "fieldC" }, compareCalledFor);

            // make a change to the syntax tree
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(source2, parseOptions));

            // now, when we re-run, both transforms will run, but the comparer will suppress the modified output
            syntaxCalledFor.Clear();
            noCompareCalledFor.Clear();
            compareCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            Assert.Equal(new[] { "fieldD", "fieldE", "fieldF", "fieldD", "fieldE", "fieldF" }, syntaxCalledFor);
            Assert.Equal(new[] { "fieldD", "fieldE", "fieldF" }, noCompareCalledFor);
            Assert.Empty(compareCalledFor);
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Can_Feed_Two_Outputs()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) =>
                {
                    return ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText;
                }).WithTrackingName("Fields");

                context.RegisterSourceOutput(source.Select((s, ct) => $"Output1_{s}").WithTrackingName("Output"), (spc, fieldName) =>
                {
                });

                context.RegisterSourceOutput(source.Select((s, ct) => $"Output2_{s}").WithTrackingName("Output"), (spc, fieldName) =>
                {
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);

            // verify we ran the syntax transform once, but fed both outputs
            var results = driver.GetRunResult();
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));
            Assert.Collection(results.Results[0].TrackedSteps["Output"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("Output1_fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("Output1_fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("Output1_fieldC", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("Output2_fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("Output2_fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("Output2_fieldC", IncrementalStepRunReason.New), output)));
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_Isnt_Duplicated_By_Combines()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) =>
                {
                    return ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText;
                }).WithTrackingName("Fields");

                var source2 = source.Combine(context.AdditionalTextsProvider.Collect())
                                    .Combine(context.AnalyzerConfigOptionsProvider)
                                    .Combine(context.ParseOptionsProvider);

                context.RegisterSourceOutput(source2.Select((value, ct) => value.Left.Left.Left).WithTrackingName("Output"), (spc, output) =>
                {
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var results = driver.GetRunResult();

            // verify we only ran the syntax transform once, even though we called through a join
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));
            Assert.Collection(results.Results[0].TrackedSteps["Output"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));
        }

        [Fact]
        public void IncrementalGenerator_With_Syntax_Filter_And_Comparer_Survive_Combines()
        {
            var source1 = @"
#pragma warning disable CS0414
class C 
{
    string fieldA = null; 
    string fieldB = null;
    string fieldC = null;
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var testGenerator = new PipelineCallbackGenerator(context =>
            {
                var source = context.SyntaxProvider.CreateSyntaxProvider((c, _) => c is FieldDeclarationSyntax fds, (c, _) =>
                {
                    return ((FieldDeclarationSyntax)c.Node).Declaration.Variables[0].Identifier.ValueText;
                }).WithTrackingName("Fields");

                var comparerSource = source.WithComparer(new LambdaComparer<string>((a, b) => false));

                // now join the two sources together
                var joinedSource = source.Combine(comparerSource.Collect());
                context.RegisterSourceOutput(joinedSource.Select((value, ct) => value.Left).WithTrackingName("Output"), (spc, fieldName) =>
                {
                });

            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var results = driver.GetRunResult();

            // verify we ran the syntax transform twice, one for each input node, but only called into the output once
            Assert.Collection(results.Results[0].TrackedSteps["Fields"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));
            Assert.Collection(results.Results[0].TrackedSteps["Output"],
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldA", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldB", IncrementalStepRunReason.New), output)),
                step => Assert.Collection(step.Outputs,
                    output => Assert.Equal(("fieldC", IncrementalStepRunReason.New), output)));
        }

        [Fact]
        public void IncrementalGenerator_Throws_In_Syntax_Filter()
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
            var parseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview);
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new Exception("Test Exception");
            var testGenerator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider((s, _) => { if (s is AssignmentExpressionSyntax) throw exception; return true; }, (c, _) => c.Node), (spc, s) => { });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Empty(results.GeneratedTrees);
            Assert.Single(results.Diagnostics);

            Assert.Single(results.Results[0].Diagnostics);
            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", results.Results[0].Exception?.Message);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("PipelineCallbackGenerator", "Exception", "Test Exception").WithLocation(1, 1)
                );
        }

        [Fact]
        public void IncrementalGenerator_Throws_In_Syntax_Transform()
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
            var parseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview);
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new Exception("Test Exception");
            var testGenerator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider<object>((s, _) => s is AssignmentExpressionSyntax, (c, _) => throw exception), (spc, s) => { });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator) }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Empty(results.GeneratedTrees);
            Assert.Single(results.Diagnostics);

            Assert.Single(results.Results[0].Diagnostics);
            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", results.Results[0].Exception?.Message);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("PipelineCallbackGenerator", "Exception", "Test Exception").WithLocation(1, 1)
                );
        }

        [Fact]
        public void IncrementalGenerator_Throws_In_Syntax_Transform_Doesnt_Stop_Other_Generators()
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
            var parseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview);
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new Exception("Test Exception");
            var testGenerator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider<object>((s, _) => s is AssignmentExpressionSyntax, (c, _) => throw exception), (spc, s) => { });
            });

            var testGenerator2 = new PipelineCallbackGenerator2(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, s) => spc.AddSource("test", ""));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new IncrementalGeneratorWrapper(testGenerator), new IncrementalGeneratorWrapper(testGenerator2) }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var results = driver.GetRunResult();

            Assert.Single(results.GeneratedTrees);
            Assert.Single(results.Diagnostics);

            Assert.Single(results.Results[0].Diagnostics);
            Assert.NotNull(results.Results[0].Exception);
            Assert.Equal("Test Exception", results.Results[0].Exception?.Message);

            Assert.Single(results.Results[1].GeneratedSources);
            Assert.Null(results.Results[1].Exception);

            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("PipelineCallbackGenerator", "Exception", "Test Exception").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Incremental_Generators_Can_Be_Cancelled_During_Syntax()
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
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            CancellationTokenSource cts = new CancellationTokenSource();
            int filterCalled = 0;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                var step1 = ctx.SyntaxProvider.CreateSyntaxProvider((c, ct) => { filterCalled++; if (c is AssignmentExpressionSyntax) cts.Cancel(); return true; }, (a, _) => a);
                ctx.RegisterSourceOutput(step1, (spc, c) => spc.AddSource("step1", ""));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            Assert.Throws<OperationCanceledException>(() => driver = driver.RunGenerators(compilation, cancellationToken: cts.Token));
            Assert.Equal(19, filterCalled);
        }

        [Fact]
        public void Incremental_Generators_Can_Be_Cancelled_During_Syntax_And_Stop_Other_SyntaxVisits()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            CancellationTokenSource cts = new CancellationTokenSource();
            bool generatorCancelled = false;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                var step1 = ctx.SyntaxProvider.CreateSyntaxProvider((c, ct) => { generatorCancelled = true; cts.Cancel(); return true; }, (a, _) => a);
                ctx.RegisterSourceOutput(step1, (spc, c) => spc.AddSource("step1", ""));

                var step2 = ctx.SyntaxProvider.CreateSyntaxProvider((c, ct) => { return true; }, (a, _) => a);
                ctx.RegisterSourceOutput(step2, (spc, c) => spc.AddSource("step2", ""));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            Assert.Throws<OperationCanceledException>(() => driver = driver.RunGenerators(compilation, cancellationToken: cts.Token));
            Assert.True(generatorCancelled);
        }

        [Fact]
        public void Syntax_Receiver_Cancellation_During_Visit()
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

            var testGenerator = new CallbackGenerator(
                onInit: (i) => i.RegisterForSyntaxNotifications(() => new TestSyntaxReceiver(tag: 0, callback: (a) => { if (a is AssignmentExpressionSyntax) { throw new OperationCanceledException("Simulated cancellation from external source"); } })),
                onExecute: (e) => { e.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)); }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation, CancellationToken.None);
            var results = driver.GetRunResult();

            Assert.Single(results.Results);
            Assert.IsType<OperationCanceledException>(results.Results[0].Exception);
            Assert.Equal("Simulated cancellation from external source", results.Results[0].Exception!.Message);
        }

        [Fact]
        public void Syntax_Provider_Doesnt_Attribute_Incorrect_Timing()
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var sleepTimeInMs = 50;
            var testGenerator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider<object>((s, _) => s is AssignmentExpressionSyntax, (c, _) => { Thread.Sleep(sleepTimeInMs); return true; }), (spc, s) => { });
            }).AsSourceGenerator();

            var testGenerator2 = new PipelineCallbackGenerator2(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider<object>((s, _) => s is AssignmentExpressionSyntax, (c, _) => { Thread.Sleep(sleepTimeInMs); return true; }), (spc, s) => { });
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider<object>((s, _) => s is AssignmentExpressionSyntax, (c, _) => { Thread.Sleep(sleepTimeInMs); return true; }), (spc, s) => { });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator, testGenerator2 });
            driver = driver.RunGenerators(compilation);

            var timing = driver.GetTimingInfo();

            Assert.NotEqual(TimeSpan.Zero, timing.ElapsedTime);
            Assert.Equal(2, timing.GeneratorTimes.Length);

            // check generator one took at least 'sleepTimeInMs'
            var timing1 = timing.GeneratorTimes[0];
            Assert.Equal(testGenerator, timing1.Generator);
            Assert.NotEqual(TimeSpan.Zero, timing1.ElapsedTime);
            Assert.True(timing.ElapsedTime >= timing1.ElapsedTime);
            Assert.True(timing1.ElapsedTime.TotalMilliseconds >= sleepTimeInMs);

            // check generator two took at least 'sleepTimeInMs' * 2
            var timing2 = timing.GeneratorTimes[1];
            Assert.Equal(testGenerator2, timing2.Generator);
            Assert.NotEqual(TimeSpan.Zero, timing2.ElapsedTime);
            Assert.True(timing.ElapsedTime >= timing2.ElapsedTime);
            Assert.True(timing2.ElapsedTime.TotalMilliseconds >= sleepTimeInMs * 2);

            // now check that generator two took longer than generator one (and one didn't get attributed the time)
            Assert.True(timing2.ElapsedTime > timing1.ElapsedTime);
        }

        private class TestReceiverBase<T>
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

        private class TestSyntaxReceiver : TestReceiverBase<SyntaxNode>, ISyntaxReceiver
        {
            public TestSyntaxReceiver(int tag = 0, Action<SyntaxNode>? callback = null)
                : base(tag, callback)
            {
            }
        }

        private class TestSyntaxContextReceiver : TestReceiverBase<GeneratorSyntaxContext>, ISyntaxContextReceiver
        {
            public TestSyntaxContextReceiver(int tag = 0, Action<GeneratorSyntaxContext>? callback = null)
                : base(tag, callback)
            {
            }
        }
    }
}
