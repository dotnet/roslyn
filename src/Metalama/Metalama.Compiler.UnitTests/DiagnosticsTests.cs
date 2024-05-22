// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Compiler.UnitTests
{
    public partial class DiagnosticsTests : CommandLineTestBase
    {
        private readonly ITestOutputHelper _logger;

        public DiagnosticsTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tests that warnings that stem from generated code are not reported to the user.
        /// </summary>
        [Fact]
        public void TransformedCodeDoesNotGenerateWarning()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  int _f;  }");

            var args = new[] { "/t:library", "/preferreduilang:en-us", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { int _f; }") };
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            // Check that warnings are only reported when located in source code.
            Assert.Contains("warning CS0169: The field 'C._f' is never used", output);
            Assert.DoesNotContain("warning CS0169: The field 'D._f' is never used", output);
        }

        [Fact]
        public void NoAnalyzerDiagnosticOnGeneratedCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");
            var editorConfig = CreateEditorConfig(dir, typeof(ReportDiagnosticForEachClassAnalyzer));

            var args = new[] { "/t:library", src.Path, $"/analyzerconfig:{editorConfig.Path}", };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") };
            var analyzers = new DiagnosticAnalyzer[]
            {
                new ReportDiagnosticForEachClassAnalyzer("MY001", DiagnosticSeverity.Warning, outWriter),
                new ReportDiagnosticForEachClassAnalyzer("MY002", DiagnosticSeverity.Error, outWriter)
            };
            var csc = CreateCSharpCompiler(null, dir.Path, args, analyzers: analyzers, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(1, exitCode);

            // Check that the analyzer ran.
            Assert.Contains("Analyzer initialized.", output);
            Assert.Contains("Analyzing syntax tree.", output);

            // Check that the analyzer did not see the transformed code and that reported warnings come through.
            Assert.Contains("Analyzing 'C'.", output);
            Assert.Contains("Analyzing 'D'.", output);
            Assert.Contains("warning MY001: Found a class 'C'.", output);
            Assert.DoesNotContain("warning MY001: Found a class 'D'.", output);

            // Errors should also be wrapped because they don't have the CS prefix.
            Assert.Contains("error MY002: Found a class 'C'.", output);
            Assert.DoesNotContain("error MY002: Found a class 'D'.", output);
        }

        [Fact]
        public void DiagnosticsInGeneratedCodeAreNotEscalated()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");
            var editorConfig = CreateEditorConfig(dir, typeof(ReportDiagnosticForEachClassAnalyzer));

            var args = new[] { "/t:library", src.Path, "/warnaserror+", $"/analyzerconfig:{editorConfig.Path}", };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") };
            var analyzers = new DiagnosticAnalyzer[]
            {
                new ReportDiagnosticForEachClassAnalyzer("MY001", DiagnosticSeverity.Warning, outWriter),
            };
            var csc = CreateCSharpCompiler(null, dir.Path, args, analyzers: analyzers, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(1, exitCode);

            // Check that the analyzer ran.
            Assert.Contains("Analyzer initialized.", output);
            Assert.Contains("Analyzing syntax tree.", output);

            // Check that the analyzer did not see the transformed code and that reported warnings come through.
            Assert.Contains("Analyzing 'C'.", output);
            Assert.Contains("Analyzing 'D'.", output);
            Assert.Contains("MY001: Found a class 'C'.", output);
            Assert.DoesNotContain("MY001: Found a class 'D'.", output);
        }

        [Fact]
        public void ErrorsInGeneratedCodeAreWrapped()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var args = new[] { "/t:library", src.Path };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D : Xyz { }") };

            var csc = CreateCSharpCompiler(null, dir.Path, args,  transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(1, exitCode);

            // Check that the message has been wrapped and that the origin has been found.
            Assert.Contains("LAMA0611", output);
            Assert.Contains("test-transformation-annotation", output);
        }

        [Fact]
        public void TransformersCanSuppressWarnings()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  int _f;  }");

            var args = new[] { "/t:library", src.Path };

            var analyzers =
                new DiagnosticAnalyzer[]
                {
                    new ReportDiagnosticForEachClassAnalyzer("MY001", DiagnosticSeverity.Warning)
                };
            var transformers =
                new ISourceTransformer[]
                {
                    new AppendTransformer("class D { int _f; }"), new SuppressTransformer("MY001"),
                    new SuppressTransformer("CS0169")
                };
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers, analyzers: analyzers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            // Check that the analyzer did not see the transformed code.
            Assert.DoesNotContain("warning MY001: Found a class 'C'.", output);
            Assert.DoesNotContain("warning MY001: Found a class 'D'.", output);
            Assert.DoesNotContain("warning CS0169: The field 'C._f' is never used", output);
            Assert.DoesNotContain("warning CS0169: The field 'D._f' is never used", output);
        }

        [Fact]
        public void SourceCodeCanReferenceGeneratedCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  D _d;  }");

            var args = new[] { "/t:library", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") };
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void SourceOnlyAnalyzersSeeSourceCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  }");


            var args = new[] { "/t:library", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") };
            var analyzers = new DiagnosticAnalyzer[] { new ReportWarningIfTwoCompilationUnitMembersAnalyzer() };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers, analyzers: analyzers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Contains("warning MY002", output);
            Assert.DoesNotContain("warning MY001", output);

            Assert.Equal(0, exitCode);
        }

        private static TempFile CreateEditorConfig(TempDirectory dir, Type transformedCodeAnalyzer)
        {
            return dir.CreateFile(".editorconfig").WriteAllText($@"
is_global = true
build_property.MetalamaTransformedCodeAnalyzers = {transformedCodeAnalyzer.FullName}");

        }


        [Fact]
        public void TransformedCodeAnalyzersSeeTransformedCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  }");
            var editorConfig = CreateEditorConfig(dir, typeof(ReportWarningIfTwoCompilationUnitMembersAnalyzer));

            var args = new[] { "/t:library", $"/analyzerconfig:{editorConfig.Path}", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") };
            var analyzers = new DiagnosticAnalyzer[] { new ReportWarningIfTwoCompilationUnitMembersAnalyzer() };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers, analyzers: analyzers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Contains("warning MY002", output);
            Assert.Contains("warning MY001", output);

            Assert.Equal(0, exitCode);
        }
        
        [Fact]
        public void TransformedCodeAnalyzersSeeTransformedCode_NamespaceRule()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  }");

            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText($@"
is_global = true
build_property.MetalamaTransformedCodeAnalyzers = {typeof(ReportWarningIfTwoCompilationUnitMembersAnalyzer).Namespace}");

            var args = new[] { "/t:library", $"/analyzerconfig:{analyzerConfig.Path}", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") };
            var analyzers =
                new DiagnosticAnalyzer[] { new ReportWarningIfTwoCompilationUnitMembersAnalyzer() };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers, analyzers: analyzers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Contains("warning MY002", output);
            Assert.Contains("warning MY001", output);

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DiagnosticOnMovedNodeIsFound()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  void M() { M(); }  }");

            var args = new[] { "/t:library", src.Path };

            var transformers = new ISourceTransformer[] { new ChangeTreeParentAndReportTransformer() };
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            Assert.Contains("MY001", output);
        }
        
        [Fact]
        public void AnalyzersSeeEditorOptions()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("public delegate void MyEventHandler();");
            var editorConfig = dir.CreateFile(".editorConfig").WriteAllText(@"[*.cs]
            dotnet_diagnostic.CA1711.severity = warning
            dotnet_code_quality.ca1711.allowed_suffixes = Flag|Flags|Collection|New|EventHandler
            ");

            var args = new[] { "/t:library", src.Path, $"/analyzerConfig:{editorConfig.Path}" };

            var transformers = new ISourceTransformer[] { new AppendTransformer("/* */") };
            var analyzers = new DiagnosticAnalyzer[]
            {
                new ConsumeEditorConfigAnalyzer("dotnet_code_quality.ca1711.allowed_suffixes")
            };
            var csc = CreateCSharpCompiler(null, dir.Path, args, analyzers: analyzers, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);
            
            Assert.DoesNotContain(ConsumeEditorConfigAnalyzer.DiagnosticId, output);
        }

        
        [Fact]
        public void DiagnosticsCanBeSuppressedWithPragma()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
#pragma warning disable MY001          
class C { }
#pragma warning restore MY001                      
class D { }
            ");

            var args = new[] { "/t:library", src.Path };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[]
            {
                new ReportDiagnosticOnEachClassTransformer("MY001", DiagnosticSeverity.Warning, outWriter)
            };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();
            this._logger.WriteLine(output);

            Assert.Equal(0, exitCode);

            Assert.DoesNotContain("warning MY001: Found a class 'C'.", output);
            Assert.Contains("warning MY001: Found a class 'D'.", output);
        }

        [Fact]
        public void DiagnosticsCanBeSuppressedWithEditorConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"class C { }");
            var editorConfig = dir.CreateFile(".editorConfig").WriteAllText(@"[*.cs]
            dotnet_diagnostic.MY001.severity = none
            ");
            var args = new[] { "/t:library", src.Path, $"/analyzerconfig:{editorConfig.Path}", };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[] { new ReportDiagnosticOnEachClassTransformer("MY001", DiagnosticSeverity.Warning, outWriter) };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();
            this._logger.WriteLine(output);

            Assert.Equal(0, exitCode);

            Assert.DoesNotContain("warning MY001: Found a class 'C'.", output);
        }

        [Fact]
        public void DiagnosticsCanBeSuppressedWithOption()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"class C { }");

            var args = new[] { "/t:library", src.Path, $"/nowarn:MY001" };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[]
            {
                new ReportDiagnosticOnEachClassTransformer("MY001", DiagnosticSeverity.Warning, outWriter)
            };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();
            this._logger.WriteLine(output);

            Assert.Equal(0, exitCode);

            Assert.DoesNotContain("warning MY001: Found a class 'C'.", output);
        }


        [Fact]
        public void DiagnosticsCanBeEscalatedWithEditorConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"class C { }");
            var editorConfig = dir.CreateFile(".editorConfig").WriteAllText(@"[*.cs]
            dotnet_diagnostic.MY001.severity = error
            ");
            var args = new[] { "/t:library", src.Path, $"/analyzerconfig:{editorConfig.Path}", };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[]
            {
                new ReportDiagnosticOnEachClassTransformer("MY001", DiagnosticSeverity.Warning, outWriter)
            };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();
            this._logger.WriteLine(output);

            Assert.Equal(1, exitCode);

            Assert.Contains("error MY001: Found a class 'C'.", output);
        }

        [Fact]
        public void DiagnosticsCanBeEscalatedWithOption()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"class C { }");

            var args = new[] { "/t:library", src.Path, $"/warnaserror:MY001" };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[]
            {
                new ReportDiagnosticOnEachClassTransformer("MY001", DiagnosticSeverity.Warning, outWriter)
            };

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();
            this._logger.WriteLine(output);

            Assert.Equal(1, exitCode);

            Assert.Contains("error MY001: Found a class 'C'.", output);
        }

    }
}
