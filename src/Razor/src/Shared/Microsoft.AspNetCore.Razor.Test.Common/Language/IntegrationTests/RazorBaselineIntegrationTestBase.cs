// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public abstract class RazorBaselineIntegrationTestBase : RazorIntegrationTestBase
{
    // UTF-8 with BOM
    private static readonly Encoding _baselineEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    protected RazorBaselineIntegrationTestBase(TestProject.Layer layer)
    {
        TestProjectRoot = TestProject.GetProjectDirectory(GetType(), layer);
    }

    protected string TestProjectRoot { get; }

    // For consistent line endings because the character counts are going to be recorded in files.
    internal override string LineEnding => "\r\n";

    internal override bool NormalizeSourceLineEndings => true;

    internal override string PathSeparator => "\\";

    // Force consistent paths since they are going to be recorded in files.
    internal override string WorkingDirectory => ArbitraryWindowsPath;

    protected abstract string GetDirectoryPath(string testName);

    protected void AssertDocumentNodeMatchesBaseline(RazorCodeDocument codeDocument, [CallerMemberName]string testName = "")
    {
        var document = codeDocument.GetRequiredDocumentNode();
        var baselineFilePath = GetBaselineFilePath(codeDocument, ".ir.txt", testName);

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(baselineFullPath));
            WriteBaseline(IntermediateNodeSerializer.Serialize(document), baselineFullPath);

            return;
        }

        var irFile = TestFile.Create(baselineFilePath, GetType().Assembly);
        if (!irFile.Exists())
        {
            throw new XunitException($"The resource {baselineFilePath} was not found.");
        }

        // Normalize newlines by splitting into an array.
        var irText = irFile.ReadAllText();
        var baseline = irText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        AssertEx.AssertEqualToleratingWhitespaceDifferences(irText, IntermediateNodeSerializer.Serialize(document));
        IntermediateNodeVerifier.Verify(document, baseline);
    }

    protected void AssertCSharpDocumentMatchesBaseline(RazorCodeDocument codeDocument, bool verifyLinePragmas = true, [CallerMemberName] string testName = "")
    {
        var document = codeDocument.GetRequiredCSharpDocument();

        // Normalize newlines to match those in the baseline.
        var actualCode = document.Text.ToString().Replace("\r", "").Replace("\n", "\r\n");

        var baselineFilePath = GetBaselineFilePath(codeDocument, ".codegen.cs", testName);
        var baselineDiagnosticsFilePath = GetBaselineFilePath(codeDocument, ".diagnostics.txt", testName);
        var baselineMappingsFilePath = GetBaselineFilePath(codeDocument, ".mappings.txt", testName);

        var serializedMappings = SourceMappingsSerializer.Serialize(document, codeDocument.Source);

        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(baselineFullPath));
            WriteBaseline(actualCode, baselineFullPath);

            var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFilePath);
            var lines = document.Diagnostics.Select(RazorDiagnosticSerializer.Serialize).ToArray();
            if (lines.Any())
            {
                WriteBaseline(lines, baselineDiagnosticsFullPath);
            }
            else if (File.Exists(baselineDiagnosticsFullPath))
            {
                File.Delete(baselineDiagnosticsFullPath);
            }

            var baselineMappingsFullPath = Path.Combine(TestProjectRoot, baselineMappingsFilePath);
            var text = SourceMappingsSerializer.Serialize(document, codeDocument.Source);
            if (!string.IsNullOrEmpty(text))
            {
                WriteBaseline(text, baselineMappingsFullPath);
            }
            else if (File.Exists(baselineMappingsFullPath))
            {
                File.Delete(baselineMappingsFullPath);
            }

            return;
        }

        var codegenFile = TestFile.Create(baselineFilePath, GetType().Assembly);
        if (!codegenFile.Exists())
        {
            throw new XunitException($"The resource {baselineFilePath} was not found.");
        }

        var baseline = codegenFile.ReadAllText();
        Assert.Equal(baseline, actualCode);

        var baselineDiagnostics = string.Empty;
        var diagnosticsFile = TestFile.Create(baselineDiagnosticsFilePath, GetType().Assembly);
        if (diagnosticsFile.Exists())
        {
            baselineDiagnostics = diagnosticsFile.ReadAllText();
        }

        var actualDiagnostics = string.Concat(document.Diagnostics.Select(d => RazorDiagnosticSerializer.Serialize(d) + "\r\n"));
        Assert.Equal(baselineDiagnostics, actualDiagnostics);

        var baselineMappings = string.Empty;
        var mappingsFile = TestFile.Create(baselineMappingsFilePath, GetType().Assembly);
        if (mappingsFile.Exists())
        {
            baselineMappings = mappingsFile.ReadAllText();
        }

        var actualMappings = SourceMappingsSerializer.Serialize(document, codeDocument.Source);
        actualMappings = actualMappings.Replace("\r", "").Replace("\n", "\r\n");
        Assert.Equal(baselineMappings, actualMappings);

        if (verifyLinePragmas)
        {
            AssertLinePragmas(codeDocument);
        }
    }

    protected void AssertLinePragmas(RazorCodeDocument codeDocument)
    {
        var csharpDocument = codeDocument.GetCSharpDocument();
        Assert.NotNull(csharpDocument);
        var linePragmas = csharpDocument.LinePragmas;
        if (DesignTime)
        {
            var sourceMappings = csharpDocument.SourceMappingsSortedByOriginal;
            foreach (var sourceMapping in sourceMappings)
            {
                var content = codeDocument.Source.Text.GetSubText(new TextSpan(sourceMapping.OriginalSpan.AbsoluteIndex, sourceMapping.OriginalSpan.Length)).ToString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var foundMatchingPragma = false;
                foreach (var linePragma in linePragmas)
                {
                    if (sourceMapping.OriginalSpan.LineIndex >= linePragma.StartLineIndex &&
                        sourceMapping.OriginalSpan.LineIndex <= linePragma.EndLineIndex)
                    {
                        // Found a match.
                        foundMatchingPragma = true;
                        break;
                    }
                }

                Assert.True(foundMatchingPragma, $"No line pragma found for code at line {sourceMapping.OriginalSpan.LineIndex + 1}.");
            }
        }
        else
        {
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

            // check that the pragmas in the main document are enhanced
            Assert.All(linePragmas.Where(p => p.FilePath == codeDocument.Source.FilePath), p => Assert.True(p.IsEnhanced));
        }
    }

    protected void AssertSequencePointsMatchBaseline(CompileToAssemblyResult result, RazorCodeDocument codeDocument, [CallerMemberName] string testName = "")
    {
        using var peReader = new PEReader(result.ExecutableStream);
        var metadataReader = peReader.GetMetadataReader();

        var debugDirectory = peReader.ReadDebugDirectory().First(d => d.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
        var debugReader = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(debugDirectory).GetMetadataReader();

        var builder = new StringBuilder();
        foreach (var methodHandle in debugReader.MethodDebugInformation)
        {
            var methodDebugInfo = debugReader.GetMethodDebugInformation(methodHandle);
            var sequencePoints = methodDebugInfo.GetSequencePoints();
            if (!sequencePoints.Any())
                continue;

            var methodDefinition = metadataReader.GetMethodDefinition(methodHandle.ToDefinitionHandle());
            builder.AppendLine($"{metadataReader.GetString(methodDefinition.Name)}: ");

            foreach (var sequencePoint in sequencePoints)
            {
                if (!sequencePoint.IsHidden)
                {
                    var documentName = debugReader.GetString(debugReader.GetDocument(sequencePoint.Document).Name);
                    builder.AppendLine($"\tIL_{sequencePoint.Offset:x4}: ({sequencePoint.StartLine},{sequencePoint.StartColumn})-({sequencePoint.EndLine},{sequencePoint.EndColumn}) \"{documentName}\"");
                }
            }
        }

        var actualSequencePoints = builder.ToString().ReplaceLineEndings();

        var baselineFilePath = GetBaselineFilePath(codeDocument, ".sp.txt", testName);
        if (GenerateBaselines.ShouldGenerate)
        {
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(baselineFullPath));
            WriteBaseline(actualSequencePoints, baselineFullPath);
        }
        else
        {
            var baselineSequencePoints = string.Empty;
            var spFile = TestFile.Create(baselineFilePath, GetType().Assembly);
            if (spFile.Exists())
            {
                baselineSequencePoints = spFile.ReadAllText().ReplaceLineEndings();
            }

            AssertEx.Equal(baselineSequencePoints, actualSequencePoints);
        }
    }

    private string GetBaselineFilePath(RazorCodeDocument codeDocument, string extension, string testName)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        if (extension == null)
        {
            throw new ArgumentNullException(nameof(extension));
        }

        var lastSlash = codeDocument.Source.FilePath.LastIndexOfAny(['/', '\\']);
        var fileName = lastSlash == -1 ? null : codeDocument.Source.FilePath[(lastSlash + 1)..];
        if (string.IsNullOrEmpty(fileName))
        {
            var message = "Integration tests require a filename";
            throw new InvalidOperationException(message);
        }

        return Path.Combine(GetDirectoryPath(testName), Path.ChangeExtension(fileName, extension));
    }

    private static void WriteBaseline(string text, string filePath)
    {
        File.WriteAllText(filePath, text, _baselineEncoding);
    }

    private static void WriteBaseline(string[] lines, string filePath)
    {
        using (var writer = new StreamWriter(File.Open(filePath, FileMode.Create), _baselineEncoding))
        {
            // Force windows-style line endings so that we're consistent. This isn't
            // required for correctness, but will prevent churn when developing on OSX.
            writer.NewLine = "\r\n";

            for (var i = 0; i < lines.Length; i++)
            {
                writer.WriteLine(lines[i]);
            }
        }
    }
}
