using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace RoslynEx.UnitTests
{
    public class SourceTransformersTests : CommandLineTestBase
    {
        private static Assembly LoadCompiledAssembly(string path)
        {
            var resolver = new PathAssemblyResolver(new string[] { typeof(object).Assembly.Location });
            var mlc = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
            return mlc.LoadFromAssemblyPath(path);
        }

        [Fact]
        public void TransformerWorks()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var args = new[] { "/t:library", src.Path };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: (new ISourceTransformer[] { new TestTransformer() }).ToImmutableArray());

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

        class TestTransformer : ISourceTransformer
        {
            public Compilation Execute(TransformerContext context)
            {
                context.ReportDiagnostic(Diagnostic.Create("TEST001", "Test", "Test warning", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1));

                return context.Compilation.ReplaceSyntaxTree(context.Compilation.SyntaxTrees.Single(), SyntaxFactory.ParseSyntaxTree("class Generated {}"));
            }
        }

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
            public Compilation Execute(TransformerContext context)
            {
                context.GlobalOptions.TryGetValue("config_transformer_class_name", out var className);

                return context.Compilation.ReplaceSyntaxTree(context.Compilation.SyntaxTrees.Single(), SyntaxFactory.ParseSyntaxTree($"class {className} {{}}"));
            }
        }

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
    }
}
