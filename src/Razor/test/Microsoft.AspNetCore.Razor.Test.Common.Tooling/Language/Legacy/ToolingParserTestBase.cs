// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

// Sets the FileName static variable.
// Finds the test method name using reflection, and uses
// that to find the expected input/output test files in the file system.
[InitializeTestFile]

// These tests must be run serially due to the test specific ParserTestBase.FileName static var.
[Collection("ParserTestSerialRuns")]
public abstract class ToolingParserTestBase : ToolingTestBase, IParserTest
{
    protected ToolingParserTestBase(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        TestProjectRoot = TestProject.GetProjectDirectory(GetType(), layer: TestProject.Layer.Tooling);
    }

    /// <summary>
    /// Set to true to autocorrect the locations of spans to appear in document order with no gaps.
    /// Use this when spans were not created in document order.
    /// </summary>
    protected bool FixupSpans { get; set; }

    protected string TestProjectRoot { get; }

    protected int BaselineTestCount { get; set; }

    protected virtual bool EnableSpanEditHandlers => false;

    internal virtual void AssertSyntaxTreeNodeMatchesBaseline(RazorSyntaxTree syntaxTree)
    {
        var root = syntaxTree.Root;
        var diagnostics = syntaxTree.Diagnostics;
        var filePath = syntaxTree.Source.FilePath;
        if (ParserTestBase.FileName is null)
        {
            var message = $"{nameof(AssertSyntaxTreeNodeMatchesBaseline)} should only be called from a parser test ({nameof(ParserTestBase.FileName)} is null).";
            throw new InvalidOperationException(message);
        }

        if (ParserTestBase.IsTheory)
        {
            var message = $"{nameof(AssertSyntaxTreeNodeMatchesBaseline)} should not be called from a [Theory] test.";
            throw new InvalidOperationException(message);
        }

        var fileName = BaselineTestCount > 0 ? ParserTestBase.FileName + $"_{BaselineTestCount}" : ParserTestBase.FileName;
        var baselineFileName = Path.ChangeExtension(fileName, ".stree.txt");
        var baselineDiagnosticsFileName = Path.ChangeExtension(fileName, ".diag.txt");
        var baselineClassifiedSpansFileName = Path.ChangeExtension(fileName, ".cspans.txt");
        var baselineTagHelperSpansFileName = Path.ChangeExtension(fileName, ".tspans.txt");
        BaselineTestCount++;

        if (GenerateBaselines.ShouldGenerate)
        {
            // Write syntax tree baseline
            var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
            File.WriteAllText(baselineFullPath, TestSyntaxSerializer.Serialize(root, allowSpanEditHandlers: EnableSpanEditHandlers));

            // Write diagnostics baseline
            var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFileName);
            var lines = diagnostics.Select(SerializeDiagnostic).ToArray();
            if (lines.Any())
            {
                File.WriteAllLines(baselineDiagnosticsFullPath, lines);
            }
            else if (File.Exists(baselineDiagnosticsFullPath))
            {
                File.Delete(baselineDiagnosticsFullPath);
            }

            // Write classified spans baseline
            var classifiedSpansBaselineFullPath = Path.Combine(TestProjectRoot, baselineClassifiedSpansFileName);
            File.WriteAllText(classifiedSpansBaselineFullPath, ClassifiedSpanSerializer.Serialize(syntaxTree, validateSpanEditHandlers: EnableSpanEditHandlers));

            // Write tag helper spans baseline
            var tagHelperSpansBaselineFullPath = Path.Combine(TestProjectRoot, baselineTagHelperSpansFileName);
            var serializedTagHelperSpans = TagHelperSpanSerializer.Serialize(syntaxTree);
            if (!string.IsNullOrEmpty(serializedTagHelperSpans))
            {
                File.WriteAllText(tagHelperSpansBaselineFullPath, serializedTagHelperSpans);
            }
            else if (File.Exists(tagHelperSpansBaselineFullPath))
            {
                File.Delete(tagHelperSpansBaselineFullPath);
            }

            return;
        }

        // Verify syntax tree
        var stFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
        if (!stFile.Exists())
        {
            throw new XunitException($"The resource {baselineFileName} was not found.");
        }

        var syntaxNodeBaseline = stFile.ReadAllText();
        var actualSyntaxNodes = TestSyntaxSerializer.Serialize(root, allowSpanEditHandlers: EnableSpanEditHandlers);
        AssertEx.AssertEqualToleratingWhitespaceDifferences(syntaxNodeBaseline, actualSyntaxNodes);

        // Verify diagnostics
        var baselineDiagnostics = string.Empty;
        var diagnosticsFile = TestFile.Create(baselineDiagnosticsFileName, GetType().GetTypeInfo().Assembly);
        if (diagnosticsFile.Exists())
        {
            baselineDiagnostics = diagnosticsFile.ReadAllText();
        }

        var actualDiagnostics = string.Concat(diagnostics.Select(d => SerializeDiagnostic(d) + "\r\n"));
        Assert.Equal(baselineDiagnostics, actualDiagnostics);

        // Verify classified spans
        var classifiedSpanFile = TestFile.Create(baselineClassifiedSpansFileName, GetType().GetTypeInfo().Assembly);
        if (!classifiedSpanFile.Exists())
        {
            throw new XunitException($"The resource {baselineClassifiedSpansFileName} was not found.");
        }
        else
        {
            var classifiedSpanBaseline = classifiedSpanFile.ReadAllText();
            var actualClassifiedSpans = ClassifiedSpanSerializer.Serialize(syntaxTree, validateSpanEditHandlers: EnableSpanEditHandlers);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(classifiedSpanBaseline, actualClassifiedSpans);
        }

        // Verify tag helper spans
        var tagHelperSpanFile = TestFile.Create(baselineTagHelperSpansFileName, GetType().GetTypeInfo().Assembly);
        if (tagHelperSpanFile.Exists())
        {
            var tagHelperSpanBaseline = tagHelperSpanFile.ReadAllText();
            var actualTagHelperSpans = TagHelperSpanSerializer.Serialize(syntaxTree);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(tagHelperSpanBaseline, actualTagHelperSpans);
        }
    }

    protected static string SerializeDiagnostic(RazorDiagnostic diagnostic)
    {
        var content = RazorDiagnosticSerializer.Serialize(diagnostic);
        var normalized = NormalizeNewLines(content);

        return normalized;
    }

    private static string NormalizeNewLines(string content)
    {
        return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
    }

    internal virtual void BaselineTest(RazorSyntaxTree syntaxTree, bool verifySyntaxTree = true, bool ensureFullFidelity = true)
    {
        if (verifySyntaxTree)
        {
            SyntaxTreeVerifier.Verify(syntaxTree, ensureFullFidelity);
        }

        AssertSyntaxTreeNodeMatchesBaseline(syntaxTree);
    }
}
