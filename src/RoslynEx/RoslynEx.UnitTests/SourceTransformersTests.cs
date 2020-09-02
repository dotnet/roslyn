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
        [Fact]
        public void TransformerWorks()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var transformer = new TestTransformer();

            var args = new[] { "/t:library", src.Path };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: new ISourceTransformer[] { transformer }.ToImmutableArray());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);
            Assert.Contains("warning TEST001: Test warning", output);

            var resolver = new PathAssemblyResolver(new string[] { typeof(object).Assembly.Location });
            var mlc = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
            var assembly = mlc.LoadFromAssemblyPath(Path.Combine(dir.Path, "temp.dll"));

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
    }
}
