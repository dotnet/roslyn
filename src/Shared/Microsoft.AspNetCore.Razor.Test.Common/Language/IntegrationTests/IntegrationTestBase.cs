// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public abstract class IntegrationTestBase
{
    // UTF-8 with BOM
    private static readonly Encoding _baselineEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static readonly CSharpCompilation DefaultBaseCompilation;

    static IntegrationTestBase()
    {
        DefaultBaseCompilation = CSharpCompilation.Create(
            "TestAssembly",
            Array.Empty<SyntaxTree>(),
            ReferenceUtil.AspNetLatestAll,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    protected IntegrationTestBase(TestProject.Layer layer, string? projectDirectoryHint = null)
    {
        TestProjectRoot = projectDirectoryHint == null ? TestProject.GetProjectDirectory(GetType(), layer) : TestProject.GetProjectDirectory(projectDirectoryHint, layer);
    }

    /// <summary>
    /// Gets the <see cref="CSharpCompilation"/> that will be used as the 'app' compilation.
    /// </summary>
    protected virtual CSharpCompilation BaseCompilation { get; set; } = DefaultBaseCompilation;

    /// <summary>
    /// Gets the parse options applied when using <see cref="AddCSharpSyntaxTree(string, string)"/>.
    /// </summary>
    protected virtual CSharpParseOptions CSharpParseOptions { get; } = new CSharpParseOptions(LanguageVersion.Preview);

    /// <summary>
    /// Gets the compilation options applied when compiling assemblies.
    /// </summary>
    protected virtual CSharpCompilationOptions CSharpCompilationOptions { get; } = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

    /// <summary>
    /// Gets a list of CSharp syntax trees used that are considered part of the 'app'.
    /// </summary>
    protected virtual List<CSharpSyntaxTree> CSharpSyntaxTrees { get; } = new List<CSharpSyntaxTree>();

    /// <summary>
    /// Gets the <see cref="RazorConfiguration"/> that will be used for code generation.
    /// </summary>
    protected virtual RazorConfiguration Configuration { get; } = RazorConfiguration.Default;

    protected virtual bool DesignTime { get; } = false;

    protected bool SkipVerifyingCSharpDiagnostics { get; set; }

    protected bool NullableEnable { get; set; }

    protected Dictionary<SourceLocation, string>? ExpectedMissingSourceMappings { get; set; }

    /// <summary>
    /// Gets the
    /// </summary>
    internal VirtualRazorProjectFileSystem FileSystem { get; } = new VirtualRazorProjectFileSystem();

    /// <summary>
    /// Used to force a specific style of line-endings for testing. This matters for the baseline tests that exercise line mappings.
    /// Even though we normalize newlines for testing, the difference between platforms affects the data through the *count* of
    /// characters written.
    /// </summary>
    protected virtual string LineEnding { get; } = "\r\n";

    protected string TestProjectRoot { get; }

    public virtual string GetTestFileName([CallerMemberName] string? testName = null)
    {
        return $"TestFiles/IntegrationTests/{this.GetType().Name}/{testName}";
    }

    public string FileExtension { get; set; } = ".cshtml";

    protected virtual void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
    }

    protected CSharpSyntaxTree AddCSharpSyntaxTree(string text, string? filePath = null)
    {
        var syntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text, CSharpParseOptions, path: filePath ?? string.Empty);
        CSharpSyntaxTrees.Add(syntaxTree);
        return syntaxTree;
    }

    protected RazorProjectItem AddProjectItemFromText(string text, string filePath = "_ViewImports.cshtml", [CallerMemberName] string testName = "")
    {
        var projectItem = CreateProjectItemFromText(text, filePath, GetTestFileName(testName));
        FileSystem.Add(projectItem);
        return projectItem;
    }

    private RazorProjectItem CreateProjectItemFromText(string text, string filePath, string testFileName, string? cssScope = null)
    {
        // Consider the file path to be relative to the 'FileName' of the test.
        var workingDirectory = Path.GetDirectoryName(testFileName);
        Assert.NotNull(workingDirectory);

        // Since these paths are used in baselines, we normalize them to windows style. We
        // use "" as the base path by convention to avoid baking in an actual file system
        // path.
        var basePath = "";
        var physicalPath = Path.Combine(workingDirectory, filePath).Replace('/', '\\');
        var relativePhysicalPath = physicalPath;

        // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
        filePath = physicalPath.Replace('\\', '/');
        if (!filePath.StartsWith("/", StringComparison.Ordinal))
        {
            filePath = '/' + filePath;
        }

        text = NormalizeNewLines(text);

        var projectItem = new TestRazorProjectItem(
            basePath: basePath,
            filePath: filePath,
            physicalPath: physicalPath,
            relativePhysicalPath: relativePhysicalPath,
            cssScope: cssScope)
        {
            Content = text,
        };

        return projectItem;
    }

    protected RazorProjectItem CreateProjectItemFromFile(
        string? filePath = null,
        RazorFileKind? fileKind = null,
        [CallerMemberName] string? testName = "")
    {
        var fileName = GetTestFileName(testName);

        var suffixIndex = fileName.LastIndexOf("_", StringComparison.Ordinal);
        var normalizedFileName = suffixIndex == -1 ? fileName : fileName[..suffixIndex];
        var sourceFileName = Path.ChangeExtension(normalizedFileName, FileExtension);
        var testFile = TestFile.Create(sourceFileName, GetType().GetTypeInfo().Assembly);
        if (!testFile.Exists())
        {
            throw new XunitException($"The resource {sourceFileName} was not found.");
        }

        var fileContent = testFile.ReadAllText();

        var workingDirectory = Path.GetDirectoryName(fileName);
        var fullPath = sourceFileName;

        // Normalize to forward-slash - these strings end up in the baselines.
        fullPath = fullPath.Replace('\\', '/');
        sourceFileName = sourceFileName.Replace('\\', '/');

        // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
        filePath ??= sourceFileName;
        if (!filePath.StartsWith("/", StringComparison.Ordinal))
        {
            filePath = '/' + filePath;
        }

        var projectItem = new TestRazorProjectItem(
            basePath: workingDirectory,
            filePath: filePath,
            physicalPath: fullPath,
            relativePhysicalPath: sourceFileName,
            fileKind: fileKind)
        {
            Content = fileContent,
        };

        return projectItem;
    }

    protected CompiledCSharpCode CompileToCSharp(string text, string path = "test.cshtml", bool? designTime = null, string? cssScope = null, [CallerMemberName] string testName = "")
    {
        var projectItem = CreateProjectItemFromText(text, path, GetTestFileName(testName), cssScope);
        return CompileToCSharp(projectItem, designTime);
    }

    protected CompiledCSharpCode CompileToCSharp(RazorProjectItem projectItem, bool? designTime = null)
    {
        var compilation = CreateCompilation();
        var references = compilation.References.Concat(new[] { compilation.ToMetadataReference(), }).ToArray();

        var projectEngine = CreateProjectEngine(Configuration, references, ConfigureProjectEngine);
        var codeDocument = (designTime ?? DesignTime) ? projectEngine.ProcessDesignTime(projectItem) : projectEngine.Process(projectItem);

        return new CompiledCSharpCode(CSharpCompilation.Create(compilation.AssemblyName + ".Views", references: references, options: compilation.Options), codeDocument);
    }

    protected CompiledAssembly CompileToAssembly(string text, string path = "test.cshtml", bool? designTime = null, bool throwOnFailure = true)
    {
        var compiled = CompileToCSharp(text, path, designTime);
        return CompileToAssembly(compiled, throwOnFailure);
    }

    protected CompiledAssembly CompileToAssembly(RazorProjectItem projectItem, bool? designTime = null, bool throwOnFailure = true)
    {
        var compiled = CompileToCSharp(projectItem, designTime);
        return CompileToAssembly(compiled, throwOnFailure: throwOnFailure);
    }

    protected CompiledAssembly CompileToAssembly(CompiledCSharpCode code, bool throwOnFailure = true, bool ignoreRazorDiagnostics = false)
    {
        var csharpDocument = code.CodeDocument.GetRequiredCSharpDocument();
        if (!ignoreRazorDiagnostics && csharpDocument.Diagnostics.Any())
        {
            var diagnosticsLog = string.Join(Environment.NewLine, csharpDocument.Diagnostics.Select(d => d.ToString()).ToArray());
            throw new InvalidOperationException($"Aborting compilation to assembly because RazorCompiler returned nonempty diagnostics: {diagnosticsLog}");
        }

        var syntaxTrees = new[]
        {
            (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(csharpDocument.Text, CSharpParseOptions, path: code.CodeDocument.Source.FilePath ?? string.Empty),
        };

        var compilation = code.BaseCompilation.AddSyntaxTrees(syntaxTrees);

        var diagnostics = compilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();

        if (diagnostics.Length > 0 && throwOnFailure)
        {
            throw new CompilationFailedException(compilation, diagnostics);
        }
        else if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return new CompiledAssembly(compilation, code.CodeDocument, assembly: null);
        }

        using (var peStream = new MemoryStream())
        {
            var emit = compilation.Emit(peStream);
            diagnostics = emit
                .Diagnostics
                .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                .ToArray();
            if (diagnostics.Length > 0 && throwOnFailure)
            {
                throw new CompilationFailedException(compilation, diagnostics);
            }
            else if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return new CompiledAssembly(compilation, code.CodeDocument, assembly: null)
                {
                    EmitDiagnostics = diagnostics.ToImmutableArray(),
                };
            }

            return new CompiledAssembly(compilation, code.CodeDocument, Assembly.Load(peStream.ToArray()));
        }
    }

    private CSharpCompilation CreateCompilation()
    {
        var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTrees);
        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToArray();
        if (diagnostics.Length > 0)
        {
            throw new CompilationFailedException(compilation, diagnostics);
        }

        return compilation;
    }

    protected RazorProjectEngine CreateProjectEngine(Action<RazorProjectEngineBuilder>? configure = null)
    {
        var compilation = CreateCompilation();
        var references = compilation.References.Concat(new[] { compilation.ToMetadataReference(), }).ToArray();
        return CreateProjectEngine(Configuration, references, configure);
    }

    private RazorProjectEngine CreateProjectEngine(RazorConfiguration configuration, MetadataReference[] references, Action<RazorProjectEngineBuilder>? configure)
    {
        return RazorProjectEngine.Create(configuration, FileSystem, b =>
        {
            b.ConfigureCodeGenerationOptions(builder =>
            {
                builder.NewLine = LineEnding;
                builder.SuppressUniqueIds = "__UniqueIdSuppressedForTesting__";
            });

            b.RegisterExtensions();

            configure?.Invoke(b);

            // Allow the test to do custom things with tag helpers, but do the default thing most of the time.
            if (!b.Features.OfType<ITagHelperFeature>().Any())
            {
                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultMetadataReferenceFeature()
                {
                    References = references,
                });
            }

            b.SetCSharpLanguageVersion(CSharpParseOptions.LanguageVersion);

            b.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
                builder.CSharpParseOptions = CSharpParseOptions;
            });

            // Decorate each import feature so we can normalize line endings.
            foreach (var importFeature in b.Features.OfType<IImportProjectFeature>().ToArray())
            {
                b.Features.Replace(importFeature, new NormalizedDefaultImportFeature(importFeature, LineEnding));
            }
        });
    }

    protected void AssertDocumentNodeMatchesBaseline(DocumentIntermediateNode document, [CallerMemberName] string testName = "")
    {
        var baselineFileName = Path.ChangeExtension(GetTestFileName(testName), ".ir.txt");

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
            File.WriteAllText(baselineFullPath, IntermediateNodeSerializer.Serialize(document), _baselineEncoding);
            return;
        }

        var irFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
        if (!irFile.Exists())
        {
            throw new XunitException($"The resource {baselineFileName} was not found.");
        }

        var baseline = irFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        IntermediateNodeVerifier.Verify(document, baseline);
    }

    internal void AssertHtmlDocumentMatchesBaseline(RazorHtmlDocument htmlDocument, [CallerMemberName] string testName = "")
    {
        var baselineFileName = Path.ChangeExtension(GetTestFileName(testName), ".codegen.html");

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
            File.WriteAllText(baselineFullPath, htmlDocument.Text.ToString(), _baselineEncoding);
            return;
        }

        var htmlFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
        if (!htmlFile.Exists())
        {
            throw new XunitException($"The resource {baselineFileName} was not found.");
        }

        var baseline = htmlFile.ReadAllText();

        // Normalize newlines to match those in the baseline.
        var actual = htmlDocument.Text.ToString().Replace("\r", "").Replace("\n", "\r\n");
        Assert.Equal(baseline, actual);
    }

    protected void AssertCSharpDocumentMatchesBaseline(RazorCSharpDocument csharpDocument, [CallerMemberName] string testName = "")
    {
        var fileName = GetTestFileName(testName);
        var baselineFileName = Path.ChangeExtension(fileName, ".codegen.cs");
        var baselineDiagnosticsFileName = Path.ChangeExtension(fileName, ".diagnostics.txt");

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
            File.WriteAllText(baselineFullPath, csharpDocument.Text.ToString(), _baselineEncoding);

            var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFileName);
            var lines = csharpDocument.Diagnostics.Select(RazorDiagnosticSerializer.Serialize).ToArray();
            if (lines.Any())
            {
                File.WriteAllLines(baselineDiagnosticsFullPath, lines, _baselineEncoding);
            }
            else if (File.Exists(baselineDiagnosticsFullPath))
            {
                File.Delete(baselineDiagnosticsFullPath);
            }

            return;
        }

        var codegenFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
        if (!codegenFile.Exists())
        {
            throw new XunitException($"The resource {baselineFileName} was not found.");
        }

        var baseline = codegenFile.ReadAllText();

        // Normalize newlines to match those in the baseline.
        var actual = csharpDocument.Text.ToString().Replace("\r", "").Replace("\n", "\r\n");
        Assert.Equal(baseline, actual);

        var baselineDiagnostics = string.Empty;
        var diagnosticsFile = TestFile.Create(baselineDiagnosticsFileName, GetType().GetTypeInfo().Assembly);
        if (diagnosticsFile.Exists())
        {
            baselineDiagnostics = diagnosticsFile.ReadAllText();
        }

        var actualDiagnostics = string.Concat(csharpDocument.Diagnostics.Select(d => NormalizeNewLines(RazorDiagnosticSerializer.Serialize(d)) + "\r\n"));
        Assert.Equal(baselineDiagnostics, actualDiagnostics);
    }

    protected void AssertSourceMappingsMatchBaseline(RazorCodeDocument codeDocument, [CallerMemberName] string testName = "")
    {
        var csharpDocument = codeDocument.GetCSharpDocument();
        Assert.NotNull(csharpDocument);

        var baselineFileName = Path.ChangeExtension(GetTestFileName(testName), ".mappings.txt");
        var serializedMappings = SourceMappingsSerializer.Serialize(csharpDocument, codeDocument.Source);

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
            File.WriteAllText(baselineFullPath, serializedMappings, _baselineEncoding);
            return;
        }

        var testFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
        if (!testFile.Exists())
        {
            throw new XunitException($"The resource {baselineFileName} was not found.");
        }

        var baseline = testFile.ReadAllText();

        // Normalize newlines to match those in the baseline.
        var actualBaseline = serializedMappings.Replace("\r", "").Replace("\n", "\r\n");

        Assert.Equal(baseline, actualBaseline);

        var syntaxTree = codeDocument.GetTagHelperRewrittenSyntaxTree() ?? codeDocument.GetRequiredSyntaxTree();
        var visitor = new CodeSpanVisitor();
        visitor.Visit(syntaxTree.Root);

        var sourceContent = codeDocument.Source.Text.ToString();

        var spans = visitor.CodeSpans;
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            var sourceSpan = span.GetSourceSpan(codeDocument.Source);
            var expectedSpan = sourceContent.Substring(sourceSpan.AbsoluteIndex, sourceSpan.Length);

            // See #2593
            if (string.IsNullOrWhiteSpace(expectedSpan))
            {
                // For now we don't verify whitespace inside of a directive. We know that directives cheat
                // with how they bound whitespace/C#/markup to make completion work.
                if (span.FirstAncestorOrSelf<BaseRazorDirectiveSyntax>() != null)
                {
                    continue;
                }
            }

            // See #2594
            if (string.Equals("@", expectedSpan))
            {
                // For now we don't verify an escaped transition. In some cases one of the @ tokens in @@foo
                // will be mapped as C# but will not be present in the output buffer because it's not actually C#.
                continue;
            }

            // See https://github.com/dotnet/razor/issues/10062
            if (expectedSpan.Contains("<TModel>") ||
                span.FirstAncestorOrSelf<RazorDirectiveSyntax>()?.IsDirective(ModelDirective.Directive) is true)
            {
                // Inject directives in MVC replace the TModel with a user defined model type, so we aren't able to find
                // the matching text in the generated document
                continue;
            }

            var found = false;
            foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
            {
                if (mapping.OriginalSpan == sourceSpan)
                {
                    var actualSpan = csharpDocument.Text.ToString(mapping.GeneratedSpan.AsTextSpan());

                    if (!string.Equals(expectedSpan, actualSpan, StringComparison.Ordinal))
                    {
                        throw new XunitException(
                            $"Found the span {sourceSpan} in the output mappings but it contains " +
                            $"'{EscapeWhitespace(actualSpan)}' instead of '{EscapeWhitespace(expectedSpan)}'.");
                    }

                    found = true;
                    break;
                }
                else if (mapping.OriginalSpan.CompareByStartThenLength(sourceSpan) > 0)
                {
                    // This span (and all following) are after the area we're interested in
                    break;
                }
            }

            if (ExpectedMissingSourceMappings?.TryGetValue(SourceLocation.FromSpan(sourceSpan), out var expectedMissingSpan) == true)
            {
                if (found)
                {
                    throw new XunitException($"Remove {sourceSpan} from {nameof(ExpectedMissingSourceMappings)}.");
                }
                else if (expectedSpan != expectedMissingSpan)
                {
                    throw new XunitException($"Missing span {sourceSpan} has different content '{EscapeWhitespace(expectedSpan)}' " +
                        $"than expected '{EscapeWhitespace(expectedMissingSpan)}'.");
                }

                ExpectedMissingSourceMappings.Remove(SourceLocation.FromSpan(sourceSpan));
            }
            else if (!found)
            {
                throw new XunitException(
                    $"Could not find the span {sourceSpan} - containing '{EscapeWhitespace(expectedSpan)}' " +
                    $"in the output.");
            }
        }

        if (ExpectedMissingSourceMappings?.Count > 0)
        {
            throw new XunitException($"Found unused {nameof(ExpectedMissingSourceMappings)} ({ExpectedMissingSourceMappings.Count}), " +
                $"for example {ExpectedMissingSourceMappings.First()}.");
        }
    }

    protected void AssertLinePragmas(RazorCodeDocument codeDocument)
    {
        var csharpDocument = codeDocument.GetCSharpDocument();
        Assert.NotNull(csharpDocument);
        var linePragmas = csharpDocument.LinePragmas;

        var syntaxTree = codeDocument.GetTagHelperRewrittenSyntaxTree() ?? codeDocument.GetRequiredSyntaxTree();
        var sourceContent = syntaxTree.Source.Text.ToString();
        var classifiedSpans = syntaxTree.GetClassifiedSpans();
        foreach (var classifiedSpan in classifiedSpans)
        {
            var content = sourceContent.Substring(classifiedSpan.Span.AbsoluteIndex, classifiedSpan.Span.Length);
            if (!string.IsNullOrWhiteSpace(content) &&
                classifiedSpan.BlockKind != BlockKindInternal.Directive &&
                classifiedSpan.SpanKind == SpanKindInternal.Code)
            {
                var foundMatchingPragma = false;
                foreach (var linePragma in linePragmas)
                {
                    if (classifiedSpan.Span.LineIndex >= linePragma.StartLineIndex &&
                        classifiedSpan.Span.LineIndex <= linePragma.EndLineIndex)
                    {
                        // Found a match.
                        foundMatchingPragma = true;
                        break;
                    }
                }

                Assert.True(foundMatchingPragma, $"No line pragma found for code '{content}' at line {classifiedSpan.Span.LineIndex + 1}.");
            }
        }
    }

    protected void AssertCSharpDiagnosticsMatchBaseline(RazorCodeDocument codeDocument, [CallerMemberName] string testName = "")
    {
        if (SkipVerifyingCSharpDiagnostics)
        {
            return;
        }

        var fileName = GetTestFileName(testName);
        var baselineFileName = Path.ChangeExtension(fileName, ".cs-diagnostics.txt");

        var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTrees);

        if (NullableEnable)
        {
            compilation = compilation.WithOptions(compilation.Options
                .WithNullableContextOptions(NullableContextOptions.Enable));
        }

        var compiled = CompileToAssembly(
            new CompiledCSharpCode(compilation, codeDocument),
            ignoreRazorDiagnostics: true,
            throwOnFailure: false);
        var cSharpAllDiagnostics = !compiled.EmitDiagnostics.IsDefault
            ? compiled.EmitDiagnostics
            : compiled.Compilation.GetDiagnostics();
        var cSharpDiagnostics = cSharpAllDiagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden);
        var actualDiagnosticsText = getActualDiagnosticsText(cSharpDiagnostics);

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
            if (!string.IsNullOrWhiteSpace(actualDiagnosticsText))
            {
                File.WriteAllText(baselineFullPath, actualDiagnosticsText, _baselineEncoding);
            }
            else if (File.Exists(baselineFullPath))
            {
                File.Delete(baselineFullPath);
            }

            return;
        }

        var baselineDiagnostics = string.Empty;
        var diagnosticsFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
        if (diagnosticsFile.Exists())
        {
            baselineDiagnostics = diagnosticsFile.ReadAllText();
        }

        AssertEx.EqualOrDiff(baselineDiagnostics, NormalizeNewLines(actualDiagnosticsText));

        static string getActualDiagnosticsText(IEnumerable<Diagnostic> diagnostics)
        {
            var assertText = DiagnosticDescription.GetAssertText(
            expected: [],
            actual: diagnostics,
            unmatchedExpected: [],
            unmatchedActual: diagnostics);
            var startAnchor = "Actual:" + Environment.NewLine;
            var endAnchor = "Diff:" + Environment.NewLine;
            var start = assertText.IndexOf(startAnchor, StringComparison.Ordinal) + startAnchor.Length;
            var end = assertText.IndexOf(endAnchor, start, StringComparison.Ordinal);
            var result = assertText[start..end];
            return removeIndentation(result);
        }

        static string removeIndentation(string text)
        {
            var spaces = new string(' ', 16);
            return text.Trim().Replace(Environment.NewLine + spaces, Environment.NewLine);
        }
    }

    private class CodeSpanVisitor : SyntaxRewriter
    {
        public List<Syntax.SyntaxNode> CodeSpans { get; } = new List<Syntax.SyntaxNode>();

        public override Syntax.SyntaxNode VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            if (node.ChunkGenerator != null && node.ChunkGenerator != SpanChunkGenerator.Null)
            {
                CodeSpans.Add(node);
            }

            return base.VisitCSharpStatementLiteral(node);
        }

        public override Syntax.SyntaxNode VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
        {
            if (node.ChunkGenerator != null && node.ChunkGenerator != SpanChunkGenerator.Null)
            {
                CodeSpans.Add(node);
            }

            return base.VisitCSharpExpressionLiteral(node);
        }

        public override Syntax.SyntaxNode VisitCSharpStatement(CSharpStatementSyntax node)
        {
            if (node.FirstAncestorOrSelf<MarkupTagHelperAttributeValueSyntax>() != null)
            {
                // We don't support code blocks inside tag helper attribute values.
                // If it exists, we don't want to track its code spans for source mappings.
                return node;
            }

            return base.VisitCSharpStatement(node);
        }

        public override Syntax.SyntaxNode VisitRazorUsingDirective(RazorUsingDirectiveSyntax node)
        {
            if (node.FirstAncestorOrSelf<MarkupTagHelperAttributeValueSyntax>() != null)
            {
                // We don't support Razor directives inside tag helper attribute values.
                // If it exists, we don't want to track its code spans for source mappings.
                return node;
            }

            return base.VisitRazorUsingDirective(node);
        }

        public override Syntax.SyntaxNode VisitRazorDirective(RazorDirectiveSyntax node)
        {
            if (node.FirstAncestorOrSelf<MarkupTagHelperAttributeValueSyntax>() != null)
            {
                // We don't support Razor directives inside tag helper attribute values.
                // If it exists, we don't want to track its code spans for source mappings.
                return node;
            }

            return base.VisitRazorDirective(node);
        }
    }

    private static string EscapeWhitespace(string content)
    {
        return content
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private string NormalizeNewLines(string content)
    {
        return NormalizeNewLines(content, LineEnding);
    }

    private static string NormalizeNewLines(string content, string lineEnding)
    {
        return Regex.Replace(content, "(?<!\r)\n", lineEnding, RegexOptions.None, TimeSpan.FromSeconds(10));
    }

    // 'Default' imports won't have normalized line-endings, which is unfriendly for testing.
    private sealed class NormalizedDefaultImportFeature(IImportProjectFeature innerFeature, string lineEnding) : RazorProjectEngineFeatureBase, IImportProjectFeature
    {
        private readonly IImportProjectFeature _innerFeature = innerFeature;
        private readonly string _lineEnding = lineEnding;

        protected override void OnInitialized()
        {
            _innerFeature.Initialize(ProjectEngine);
        }

        public void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports)
        {
            using var innerImports = new PooledArrayBuilder<RazorProjectItem>();
            _innerFeature.CollectImports(projectItem, ref innerImports.AsRef());

            if (innerImports.Count == 0)
            {
                return;
            }

            foreach (var import in innerImports)
            {
                if (import.Exists)
                {
                    string text;

                    using (var stream = import.Read())
                    using (var reader = new StreamReader(stream))
                    {
                        text = reader.ReadToEnd().Trim();
                    }

                    // It's important that we normalize the newlines in the default imports. The default imports will
                    // be created with Environment.NewLine, but we need to normalize to `\r\n` so that the indices
                    // are the same on other platforms.
                    var normalizedText = NormalizeNewLines(text, _lineEnding);
                    var normalizedImport = new TestRazorProjectItem(import.FilePath, import.PhysicalPath, import.RelativePhysicalPath, import.BasePath)
                    {
                        Content = normalizedText
                    };

                    imports.Add(normalizedImport);
                }
            }
        }
    }
}
