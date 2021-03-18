// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class PipelineGeneratorTests
         : CSharpTestBase
    {
        [Fact]
        public void PipelineCallback_Is_Invoked_When_Registered()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            bool pipelineCalledBack = false;
            PipelineGenerator generator = new PipelineGenerator((ctx) =>
            {
                pipelineCalledBack = true;
            });


            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            Assert.True(pipelineCalledBack);
        }

        //class TestValueSource : IValueSource<Uri> { }


        [Fact]
        public void PipelineCallback_Can_Build_Pipeline()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            PipelineGenerator generator = new PipelineGenerator((ctx) =>
            {
                var compilationSource = ctx.Sources.CompilationSource;

                var strings = ctx.Sources.Strings;

                var t1 = compilationSource.Transform(c => c.Options);
                var t2 = t1.GenerateSource((ctx, co) => { });


                var t3 = strings.Transform((s) => (IEnumerable<string>)(new List<string>() { s, "inserted" }));
                var t4 = t3.Transform((s) => s + "_transformed");
                var t5 = t4.GenerateSource((ctx, s) => { });

                ctx.AddProducer(t2);
                ctx.AddProducer(t5);
            });


            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
        }


        [Fact]
        public void Manually_Call_StateTable()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);



            GraphStateTable.Builder gst = new GraphStateTable.Builder(GraphStateTable.Empty);
            ValueSources sources = ValueSources.Create(compilation);

            var compilationSource = sources.CompilationSource;
            var strings = sources.Strings;

            var t1 = compilationSource.Transform(c => c.Options);
            var t2 = t1.GenerateSource((ctx, co) => { });


            var t3 = strings.Transform((s) => (IEnumerable<string>)(new List<string>() { s, "inserted" }));
            var t4 = t3.Transform((s) => s + "_transformed");
            var t5 = t4.GenerateSource((ctx, s) => { });


            // get the initial version for t5
            var newTable = t5.node.GetStateTable(gst);

            var previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            // get a version which should all be cached
            var cachedTable = t5.node.GetStateTable(gst);

            previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            // add a new value to the string table
            ((MultiItemValueProvider<string>)sources.Strings.node).AddValue("123");
            var added1 = t5.node.GetStateTable(gst);

            previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            // add a new value to the string table
            ((MultiItemValueProvider<string>)sources.Strings.node).AddValue("456");
            ((MultiItemValueProvider<string>)sources.Strings.node).AddValue("789");
            var added2 = t5.node.GetStateTable(gst);

            previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            //// remove some things
            ((MultiItemValueProvider<string>)sources.Strings.node).RemoveValue(6);
            ((MultiItemValueProvider<string>)sources.Strings.node).RemoveValue(2);
            var removed = t5.node.GetStateTable(gst);

            previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            // we need to compact the sources too.
            // 

            // remove some things
            ((MultiItemValueProvider<string>)sources.Strings.node).RemoveValue(0);
            var removed2 = t5.node.GetStateTable(gst);

            previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            var removed3 = t5.node.GetStateTable(gst);

            previous = gst.ToImmutable();
            gst = new GraphStateTable.Builder(previous);

            // ok. lets *edit* some values :/
            ((MultiItemValueProvider<string>)sources.Strings.node).UpdateValue(7, "replaced_at_7");
            var updated = t5.node.GetStateTable(gst);


        }
    }
}
