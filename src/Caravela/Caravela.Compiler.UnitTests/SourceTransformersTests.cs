﻿using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Caravela.Compiler.UnitTests
{
    public class SourceTransformersTests : CommandLineTestBase
    {
        private static Assembly LoadCompiledAssembly(string path)
        {
            var resolver = new PathAssemblyResolver(new string[] { typeof(object).Assembly.Location });
            var mlc = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
            return mlc.LoadFromAssemblyPath(path);
        }

        /// <summary>
        /// Tests that transformers are even called.
        /// </summary>
        [Fact]
        public void TransformerIsCalled()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var args = new[] { "/t:library", src.Path };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: (new ISourceTransformer[] { new TrivialTransformer() }).ToImmutableArray());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);
            Assert.Contains("warning TEST001: Test warning", output);

            var assembly = LoadCompiledAssembly(Path.Combine(dir.Path, "temp.dll"));

            Assert.NotNull(assembly.GetType("Generated"));
            Assert.Null(assembly.GetType("C"));

            CleanupAllGeneratedFiles(src.Path);
        }
      

        class TrivialTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create("TEST001", "Test", "Test warning", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1));

                context.Compilation = context.Compilation.ReplaceSyntaxTree(context.Compilation.SyntaxTrees.Single(), SyntaxFactory.ParseSyntaxTree("class Generated {}"));
            }
        }
        
          
        /// <summary>
        /// Tests that source code can reference declarations that will be introduced by transformers.
        /// </summary>
        [Fact]
        public void ReferenceToIntroductionCompiles()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { public Generated g; }");

            var args = new[] { "/t:library", src.Path };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: (new ISourceTransformer[] { new IntroductionTransformer("class Generated {}") }).ToImmutableArray());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            var assembly = LoadCompiledAssembly(Path.Combine(dir.Path, "temp.dll"));

            Assert.NotNull(assembly.GetType("Generated"));
            Assert.NotNull(assembly.GetType("C"));

            CleanupAllGeneratedFiles(src.Path);
        }
        
        /// <summary>
        /// Tests that warnings that may appear in source code without transformed code are absent with absent code.
        /// </summary>
        [Fact]
        public void SourceCodeWarningResolvedByTransformedCode()
        {
            const string sourceCode = "partial class C { int f = 1; }";
            const string introducedCode = "partial class C { public int P => f; }";
            
            var dir = Temp.CreateDirectory();
            
            var src = dir.CreateFile("temp.cs").WriteAllText(sourceCode);

            var args = new[] { "/t:library", src.Path };

            
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: (new ISourceTransformer[] { new IntroductionTransformer(introducedCode) }).ToImmutableArray());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();
            
            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("warning", output);

            var assembly = LoadCompiledAssembly(Path.Combine(dir.Path, "temp.dll"));

            Assert.NotNull(assembly.GetType("C"));

            CleanupAllGeneratedFiles(src.Path);
        }
        
        class IntroductionTransformer : ISourceTransformer
        {
            private readonly string _introducedText;

            public IntroductionTransformer(string introducedText)
            {
                this._introducedText = introducedText;
            }
            public void Execute(TransformerContext context)
            {
                context.Compilation = context.Compilation.AddSyntaxTrees( SyntaxFactory.ParseSyntaxTree(this._introducedText));
            }
        }


        /// <summary>
        /// Tests that .editorconfig is properly passed to transformers.
        /// </summary>
        [Fact]
        public void Config()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");
            var editorconfig = dir.CreateFile(".editorconfig").WriteAllText(@"
is_global = true
config_transformer_class_name = ConfigTestClass
");

            var args = new[] { "/t:library", $"/analyzerconfig:{editorconfig.Path}", src.Path };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: (new ISourceTransformer[] { new ConfigTransformer() }).ToImmutableArray());

            var exitCode = csc.Run(TextWriter.Null);

            Assert.Equal(0, exitCode);

            var assembly = LoadCompiledAssembly(Path.Combine(dir.Path, "temp.dll"));

            Assert.NotNull(assembly.GetType("ConfigTestClass"));

            CleanupAllGeneratedFiles(src.Path);
        }

        class ConfigTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                context.GlobalOptions.TryGetValue("config_transformer_class_name", out var className);

                context.Compilation = context.Compilation.ReplaceSyntaxTree(context.Compilation.SyntaxTrees.Single(), SyntaxFactory.ParseSyntaxTree($"class {className} {{}}"));
            }
        }

        /// <summary>
        /// Tests ordering of transformers from custom attributes.
        /// </summary>
        [Fact]
        public void TransformerOrderFromAssembly()
        {
            var dir = Temp.CreateDirectory();

            var orderDll = dir.CreateOrOpenFile("order.dll");

            using (var orderStream = orderDll.Open())
            {
                var result = CreateCompilation(
                    File.ReadAllText("TransformerOrderTransformers.cs"),
                    references: new[] { MetadataReference.CreateFromFile(typeof(TransformerOrderAttribute).Assembly.Location) })
                    .Emit(orderStream);
                result.Diagnostics.Verify();
                Assert.True(result.Success);
            }

            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/t:library", $"/analyzer:{orderDll.Path}", src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("warning", output);

            // verify TransformerOrderTransformer2 executed before TransformerOrderTransformer1
            Assert.Matches(@"(?s)
error RE0001: Transformer 'TransformerOrderTransformer2' failed: System.Exception: .*
error RE0001: Transformer 'TransformerOrderTransformer1' failed: System.Exception: ", output);

            CleanupAllGeneratedFiles(src.Path);
        }

        /// <summary>
        /// Tests that the transformed source files are properly written to disk.
        /// </summary>
        [Fact]
        public void WriteTransformedSources()
        {
            var dir = Temp.CreateDirectory();
            var src1 = dir.CreateFile("C.cs").WriteAllText("class C { }");
            var src2 = dir.CreateDirectory("dir").CreateFile("D.cs").WriteAllText("class D { }");
            var transformedDir = dir.CreateDirectory("transformed");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText($@"
is_global = true
build_property.CaravelaCompilerTransformedFilesOutputPath = {transformedDir.Path}");

            var args = new[] { "/t:library", $"/analyzerconfig:{analyzerConfig.Path}", src1.Path, src2.Path, "/out:lib.dll" };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: (new ISourceTransformer[] { new DoSomethingTransformer() }).ToImmutableArray());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);

            Assert.Equal(0, exitCode);

            Assert.Equal("/* comment */class C { }", File.ReadAllText(Path.Combine(transformedDir.Path, "C.cs")));
            Assert.Equal("/* comment */class D { }", File.ReadAllText(Path.Combine(transformedDir.Path, "dir/D.cs")));

            var generatedFile = Directory.EnumerateFiles(transformedDir.Path).Single(p => Guid.TryParse(Path.GetFileNameWithoutExtension(p), out _));
            Assert.Equal("class G {}", File.ReadAllText(generatedFile));

            // Clean up temp files
            CleanupAllGeneratedFiles(src1.Path);
            CleanupAllGeneratedFiles(src2.Path);
            Directory.Delete(dir.Path, true);
        }

        class DoSomethingTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                var compilation = context.Compilation;

                foreach (var tree in compilation.SyntaxTrees)
                {
                    compilation = compilation.ReplaceSyntaxTree(tree, tree.WithInsertAt(0, "/* comment */"));
                }

                context.Compilation = compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree("class G {}"));
            }
        }
    }
}
