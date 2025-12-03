// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

internal abstract class AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer<TSyntaxNode> :
    AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    where TSyntaxNode : SyntaxNode
{
    // NOTE: This is a special helper diagnostic ID which is reported when the remove unnecesssary diagnostic ID (IDE0005) is
    // ecalated to a warning or an error, but 'GenerateDocumentationFile' is false, which leads to IDE0005 not being reported
    // on command line builds. See https://github.com/dotnet/roslyn/issues/41640 for more details.
    internal const string EnableGenerateDocumentationFileId = "EnableGenerateDocumentationFile";

    // The NotConfigurable custom tag ensures that user can't turn this diagnostic into a warning / error via
    // ruleset editor or solution explorer. Setting messageFormat to empty string ensures that we won't display
    // this diagnostic in the preview pane header.
    private static readonly DiagnosticDescriptor s_fixableIdDescriptor = CreateDescriptorWithId(
        RemoveUnnecessaryImportsConstants.DiagnosticFixableId, EnforceOnBuild.Never, hasAnyCodeStyleOption: false, "", "", isConfigurable: false);

#pragma warning disable RS0030 // Do not used banned APIs - Special diagnostic with 'Warning' default severity.
    private static readonly DiagnosticDescriptor s_enableGenerateDocumentationFileIdDescriptor = new(
        EnableGenerateDocumentationFileId,
        title: AnalyzersResources.Set_MSBuild_Property_GenerateDocumentationFile_to_true,
        messageFormat: AnalyzersResources.Set_MSBuild_Property_GenerateDocumentationFile_to_true_in_project_file_to_enable_IDE0005_Remove_unnecessary_usings_imports_on_build,
        category: DiagnosticCategory.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/dotnet/roslyn/issues/41640",
        description: AnalyzersResources.Add_the_following_PropertyGroup_to_your_MSBuild_project_file_to_enable_IDE0005_Remove_unnecessary_usings_imports_on_build,
        customTags: [.. DiagnosticCustomTags.Microsoft, EnforceOnBuild.Never.ToCustomTag()]);
#pragma warning restore RS0030 // Do not used banned APIs

    private readonly DiagnosticDescriptor _classificationIdDescriptor;
    private readonly DiagnosticDescriptor _generatedCodeClassificationIdDescriptor;

    protected AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer(LocalizableString titleAndMessage)
        : base(GetDescriptors(titleAndMessage, out var classificationIdDescriptor, out var generatedCodeClassificationIdDescriptor))
    {
        _classificationIdDescriptor = classificationIdDescriptor;
        _generatedCodeClassificationIdDescriptor = generatedCodeClassificationIdDescriptor;
    }

    private static ImmutableArray<DiagnosticDescriptor> GetDescriptors(LocalizableString titleAndMessage, out DiagnosticDescriptor classificationIdDescriptor, out DiagnosticDescriptor generatedCodeClassificationIdDescriptor)
    {
        classificationIdDescriptor = CreateDescriptorWithId(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId, EnforceOnBuildValues.RemoveUnnecessaryImports, hasAnyCodeStyleOption: false, titleAndMessage, isUnnecessary: true);
        generatedCodeClassificationIdDescriptor = CreateDescriptorWithId(IDEDiagnosticIds.RemoveUnnecessaryImportsGeneratedCodeDiagnosticId, EnforceOnBuild.Never, hasAnyCodeStyleOption: false, titleAndMessage, isUnnecessary: true, isConfigurable: false);
        return
        [
            s_fixableIdDescriptor,
            s_enableGenerateDocumentationFileIdDescriptor,
            classificationIdDescriptor,
            generatedCodeClassificationIdDescriptor,
        ];
    }

    protected abstract ISyntaxFacts SyntaxFacts { get; }
    protected abstract ImmutableArray<SyntaxNode> MergeImports(ImmutableArray<TSyntaxNode> unnecessaryImports);
    protected abstract bool IsRegularCommentOrDocComment(SyntaxTrivia trivia);
    protected abstract IUnnecessaryImportsProvider<TSyntaxNode> UnnecessaryImportsProvider { get; }

    protected abstract SyntaxToken? TryGetLastToken(SyntaxNode node);

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var tree = context.SemanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        var unnecessaryImports = UnnecessaryImportsProvider.GetUnnecessaryImports(context.SemanticModel, context.FilterSpan, cancellationToken);
        if (unnecessaryImports.Any())
        {
            // The IUnnecessaryImportsService will return individual import pieces that
            // need to be removed.  For example, it will return individual import-clauses
            // from VB.  However, we want to mark the entire import statement if we are
            // going to remove all the clause.  Defer to our subclass to stitch this up
            // for us appropriately.
            var mergedImports = MergeImports(unnecessaryImports);

            var descriptor = GeneratedCodeUtilities.IsGeneratedCode(tree, IsRegularCommentOrDocComment, cancellationToken)
                ? _generatedCodeClassificationIdDescriptor
                : _classificationIdDescriptor;
            var contiguousSpans = GetContiguousSpans(mergedImports);
            var diagnostics =
                CreateClassificationDiagnostics(contiguousSpans, tree, descriptor, cancellationToken).Concat(
                CreateFixableDiagnostics(mergedImports, tree, cancellationToken));

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // Due to https://github.com/dotnet/roslyn/issues/41640, enabling this analyzer (IDE0005) on build requires users
        // to enable generation of XML documentation comments. We detect if generation of XML documentation comments
        // is disabled for this tree and IDE0005 diagnostics are being reported with effective severity "Warning" or "Error".
        // If so, we report a special diagnostic that recommends the users to set "GenerateDocumentationFile" to "true"
        // in their project file to enable IDE0005 on build.

        var compilation = context.Compilation;
        if (!IsAnalysisLevelGreaterThanOrEquals(8, context.Options))
            return;

        var tree = compilation.SyntaxTrees.FirstOrDefault(tree => !GeneratedCodeUtilities.IsGeneratedCode(tree, IsRegularCommentOrDocComment, context.CancellationToken));
        if (tree is null || tree.Options.DocumentationMode != DocumentationMode.None)
            return;

        if (ShouldSkipAnalysis(tree, context.Options, compilation.Options, notification: null, context.CancellationToken))
            return;

        var effectiveSeverity = _classificationIdDescriptor.GetEffectiveSeverity(compilation.Options, tree, context.Options);
        if (effectiveSeverity is ReportDiagnostic.Warn or ReportDiagnostic.Error)
        {
            context.ReportDiagnostic(Diagnostic.Create(s_enableGenerateDocumentationFileIdDescriptor, Location.None));
        }
    }

    private IEnumerable<TextSpan> GetContiguousSpans(ImmutableArray<SyntaxNode> nodes)
    {
        var syntaxFacts = this.SyntaxFacts;
        (SyntaxNode node, TextSpan textSpan)? previous = null;

        // Sort the nodes in source location order.
        foreach (var node in nodes.OrderBy(n => n.SpanStart))
        {
            TextSpan textSpan;
            var nodeEnd = GetEnd(node);
            if (previous == null)
            {
                textSpan = TextSpan.FromBounds(node.Span.Start, nodeEnd);
            }
            else
            {
                var lastToken = TryGetLastToken(previous.Value.node) ?? previous.Value.node.GetLastToken();
                if (lastToken.GetNextToken(includeDirectives: true) == node.GetFirstToken())
                {
                    // Expand the span
                    textSpan = TextSpan.FromBounds(previous.Value.textSpan.Start, nodeEnd);
                }
                else
                {
                    // Return the last span, and start a new one
                    yield return previous.Value.textSpan;
                    textSpan = TextSpan.FromBounds(node.Span.Start, nodeEnd);
                }
            }

            previous = (node, textSpan);
        }

        if (previous.HasValue)
            yield return previous.Value.textSpan;

        yield break;

        int GetEnd(SyntaxNode node)
        {
            var end = node.Span.End;
            foreach (var trivia in node.GetTrailingTrivia())
            {
                if (syntaxFacts.IsRegularComment(trivia))
                    end = trivia.Span.End;
            }

            return end;
        }
    }

    // Create one diagnostic for each unnecessary span that will be classified as Unnecessary
    private static IEnumerable<Diagnostic> CreateClassificationDiagnostics(
        IEnumerable<TextSpan> contiguousSpans, SyntaxTree tree,
        DiagnosticDescriptor descriptor, CancellationToken cancellationToken)
    {
        foreach (var span in contiguousSpans)
        {
            if (tree.OverlapsHiddenPosition(span, cancellationToken))
            {
                continue;
            }

            yield return Diagnostic.Create(descriptor, tree.GetLocation(span));
        }
    }

    protected abstract IEnumerable<TextSpan> GetFixableDiagnosticSpans(
        IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken);

    private IEnumerable<Diagnostic> CreateFixableDiagnostics(
        IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var spans = GetFixableDiagnosticSpans(nodes, tree, cancellationToken);

        foreach (var span in spans)
            yield return Diagnostic.Create(s_fixableIdDescriptor, tree.GetLocation(span));
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
}
