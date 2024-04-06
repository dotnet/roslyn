// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.FileHeaders;

internal abstract class AbstractFileHeaderDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    private static readonly LocalizableString s_invalidHeaderTitle = new LocalizableResourceString(nameof(AnalyzersResources.The_file_header_does_not_match_the_required_text), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableString s_invalidHeaderMessage = new LocalizableResourceString(nameof(AnalyzersResources.A_source_file_contains_a_header_that_does_not_match_the_required_text), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly DiagnosticDescriptor s_invalidHeaderDescriptor = CreateDescriptorForFileHeader(s_invalidHeaderTitle, s_invalidHeaderMessage);

    private static readonly LocalizableString s_missingHeaderTitle = new LocalizableResourceString(nameof(AnalyzersResources.The_file_header_is_missing_or_not_located_at_the_top_of_the_file), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableString s_missingHeaderMessage = new LocalizableResourceString(nameof(AnalyzersResources.A_source_file_is_missing_a_required_header), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly DiagnosticDescriptor s_missingHeaderDescriptor = CreateDescriptorForFileHeader(s_missingHeaderTitle, s_missingHeaderMessage);

    private static DiagnosticDescriptor CreateDescriptorForFileHeader(LocalizableString title, LocalizableString message)
        => CreateDescriptorWithId(IDEDiagnosticIds.FileHeaderMismatch, EnforceOnBuildValues.FileHeaderMismatch, hasAnyCodeStyleOption: true, title, message);

    protected AbstractFileHeaderDiagnosticAnalyzer()
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                .Add(s_invalidHeaderDescriptor, CodeStyleOptions2.FileHeaderTemplate)
                .Add(s_missingHeaderDescriptor, CodeStyleOptions2.FileHeaderTemplate))
    {
    }

    protected abstract AbstractFileHeaderHelper FileHeaderHelper { get; }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
            context.RegisterSyntaxTreeAction(treeContext => HandleSyntaxTree(treeContext, context.Compilation.Options)));

    private void HandleSyntaxTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
    {
        if (ShouldSkipAnalysis(context, compilationOptions, notification: null))
            return;

        var tree = context.Tree;
        var root = tree.GetRoot(context.CancellationToken);

        // don't process empty files
        if (root.FullSpan.IsEmpty)
        {
            return;
        }

        var fileHeaderTemplate = context.GetAnalyzerOptions().FileHeaderTemplate;
        if (string.IsNullOrEmpty(fileHeaderTemplate))
        {
            return;
        }

        var fileHeader = FileHeaderHelper.ParseFileHeader(root);

        if (!context.ShouldAnalyzeSpan(fileHeader.GetLocation(tree).SourceSpan))
        {
            return;
        }

        if (fileHeader.IsMissing)
        {
            context.ReportDiagnostic(Diagnostic.Create(s_missingHeaderDescriptor, fileHeader.GetLocation(tree)));
            return;
        }

        var expectedFileHeader = fileHeaderTemplate.Replace("{fileName}", Path.GetFileName(tree.FilePath));
        if (!CompareCopyrightText(expectedFileHeader, fileHeader.CopyrightText))
        {
            context.ReportDiagnostic(Diagnostic.Create(s_invalidHeaderDescriptor, fileHeader.GetLocation(tree)));
            return;
        }
    }

    private static bool CompareCopyrightText(string expectedFileHeader, string copyrightText)
    {
        // make sure that both \n and \r\n are accepted from the settings.
        var reformattedCopyrightTextParts = expectedFileHeader.Replace("\r\n", "\n").Split('\n');
        var fileHeaderCopyrightTextParts = copyrightText.Replace("\r\n", "\n").Split('\n');

        if (reformattedCopyrightTextParts.Length != fileHeaderCopyrightTextParts.Length)
        {
            return false;
        }

        // compare line by line, ignoring leading and trailing whitespace on each line.
        for (var i = 0; i < reformattedCopyrightTextParts.Length; i++)
        {
            if (string.CompareOrdinal(reformattedCopyrightTextParts[i].Trim(), fileHeaderCopyrightTextParts[i].Trim()) != 0)
            {
                return false;
            }
        }

        return true;
    }
}
