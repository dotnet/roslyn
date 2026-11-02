// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

/// <summary>
/// Finds and removes the <c>@using</c> directives that a Razor component does not need, for dotnet format. A directive
/// is unnecessary when the editor reports it as unnecessary (RZ0005), or when it repeats a directive of an imports file.
/// </summary>
public static class RazorUnnecessaryDirectives
{
    private const string UnnecessaryUsingDirectiveId = "CS8019";

    /// <summary>
    /// Gets the spans in the Razor document text of the <c>@using</c> directives that the document does not need.
    /// </summary>
    /// <remarks>
    /// Only components (<c>.razor</c> files) are analyzed. Imports files report nothing, because a directive in an
    /// imports file applies to many components.
    /// </remarks>
    public static async Task<ImmutableArray<TextSpan>> GetUnnecessaryUsingDirectiveSpansAsync(
        Project project,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(documentId);

        var document = project.GetAdditionalDocument(documentId)
            ?? throw new ArgumentException("The document must be an additional document in the supplied project.", nameof(documentId));

        if (document.FilePath is not { } filePath ||
            !Path.GetExtension(filePath).Equals(".razor", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var generatorRunResult = await RazorProjectGeneratorRunResult.TryCreateAsync(project, cancellationToken).ConfigureAwait(false);
        if (generatorRunResult is null)
        {
            return [];
        }

        var codeDocument = generatorRunResult.GetRequiredCodeDocument(filePath);
        if (codeDocument.IsImportsFile() || codeDocument.FileKind.IsLegacy() || codeDocument.Source.FilePath is not { } sourceFilePath)
        {
            return [];
        }

        var implDocument = await generatorRunResult.TryGetSourceGeneratedDocumentAsync(filePath, declarationDocument: false, cancellationToken).ConfigureAwait(false);
        if (implDocument is null)
        {
            return [];
        }

        // A fallback component's declaration document is a bodiless type shell with no usings, so it cannot confirm
        // that a using is unnecessary. The editor ignores it for the same reason.
        var declDocument = codeDocument.GetCSharpDocument(declarationDocument: true) is { IsStubDocument: false }
            ? await generatorRunResult.TryGetSourceGeneratedDocumentAsync(filePath, declarationDocument: true, cancellationToken).ConfigureAwait(false)
            : null;

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return [];
        }

        var implTree = await GetRequiredSyntaxTreeAsync(implDocument, cancellationToken).ConfigureAwait(false);
        var declTree = declDocument is null
            ? null
            : await GetRequiredSyntaxTreeAsync(declDocument, cancellationToken).ConfigureAwait(false);

        // The compiler does not report the unnecessary usings of the declaration tree after it has bound the
        // implementation tree of the same component. Another caller can already have bound the implementation tree of
        // the project's compilation, so use a compilation that has not bound anything, and read the declaration tree first.
        compilation = compilation.Clone();
        (compilation, implTree) = await EnableUnusedUsingDiagnosticsAsync(compilation, implTree, cancellationToken).ConfigureAwait(false);
        if (declTree is not null)
        {
            (compilation, declTree) = await EnableUnusedUsingDiagnosticsAsync(compilation, declTree, cancellationToken).ConfigureAwait(false);
        }

        var sourceText = codeDocument.Source.Text;
        var declUnusedPositions = declTree is null
            ? null
            : GetUnusedUsingPositions(compilation, declTree, sourceFilePath, sourceText, cancellationToken);
        var unusedPositions = GetUnusedUsingPositions(compilation, implTree, sourceFilePath, sourceText, cancellationToken);

        // The compiler puts the usings of a component in both generated documents, so a using is unnecessary only when
        // both documents report it.
        if (declUnusedPositions is not null)
        {
            unusedPositions.IntersectWith(declUnusedPositions);
        }

        var root = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root;
        var spans = new SortedSet<TextSpan>();

        // A directive that repeats a directive of an imports file is unnecessary even when code uses the namespace,
        // because the imports file keeps the namespace in scope. Razor emits only one copy of a repeated using, so the
        // compiler cannot report the repeat.
        var importedUsings = GetImportedUsings(codeDocument);
        if (importedUsings.Count > 0)
        {
            foreach (var directive in root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>())
            {
                if (TryGetUsingKey(directive, out var key) && importedUsings.Contains(key))
                {
                    spans.Add(directive.Span);
                }
            }
        }

        foreach (var position in unusedPositions)
        {
            // The compiler cannot see that a using was needed to find a component, because the generated code refers
            // to components by their fully qualified names. Razor records that use, so keep those directives.
            if (root.FindToken(position).Parent?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>() is { } directive &&
                !codeDocument.IsDirectiveUsed(directive))
            {
                spans.Add(directive.Span);
            }
        }

        return [.. spans];
    }

    /// <summary>
    /// Gets the Razor document text with the <c>@using</c> directives that the document does not need removed, or
    /// <see langword="null"/> when the document has no such directives.
    /// </summary>
    /// <remarks>
    /// A directive that is the only content on its line is removed together with its line break.
    /// </remarks>
    public static async Task<SourceText?> RemoveUnnecessaryUsingDirectivesAsync(
        Project project,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
        var spans = await GetUnnecessaryUsingDirectiveSpansAsync(project, documentId, cancellationToken).ConfigureAwait(false);
        if (spans.IsEmpty)
        {
            return null;
        }

        var document = project.GetAdditionalDocument(documentId)
            ?? throw new ArgumentException("The document must be an additional document in the supplied project.", nameof(documentId));
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var changes = new List<TextChange>(spans.Length);
        foreach (var removalSpan in spans.Select(text.GetDirectiveRemovalSpan).OrderBy(static span => span.Start))
        {
            if (changes.Count > 0 && removalSpan.Start < changes[^1].Span.End)
            {
                var previous = changes[^1].Span;
                changes[^1] = new TextChange(TextSpan.FromBounds(previous.Start, Math.Max(previous.End, removalSpan.End)), string.Empty);
                continue;
            }

            changes.Add(new TextChange(removalSpan, string.Empty));
        }

        return text.WithChanges(changes);
    }

    /// <summary>
    /// The compiler reports unnecessary usings only for trees that are parsed with documentation comments. Projects
    /// without <c>GenerateDocumentationFile</c> parse with <see cref="DocumentationMode.None"/>, so this replaces the
    /// tree with one that has the same root and a documentation mode that reports them.
    /// </summary>
    private static async Task<(Compilation Compilation, SyntaxTree Tree)> EnableUnusedUsingDiagnosticsAsync(
        Compilation compilation,
        SyntaxTree tree,
        CancellationToken cancellationToken)
    {
        if (tree.Options.DocumentationMode != DocumentationMode.None)
        {
            return (compilation, tree);
        }

        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var options = ((CSharpParseOptions)tree.Options).WithDocumentationMode(DocumentationMode.Parse);
        var newTree = tree.WithRootAndOptions(root, options);

        return (compilation.ReplaceSyntaxTree(tree, newTree), newTree);
    }

    /// <summary>
    /// Gets the positions in the Razor document text of the unnecessary usings that the compiler reports for the
    /// generated tree. Usings that come from an imports file map to that file, so they are not included.
    /// </summary>
    private static HashSet<int> GetUnusedUsingPositions(
        Compilation compilation,
        SyntaxTree generatedTree,
        string sourceFilePath,
        SourceText sourceText,
        CancellationToken cancellationToken)
    {
        var positions = new HashSet<int>();
        var diagnostics = compilation.GetSemanticModel(generatedTree).GetDiagnostics(cancellationToken: cancellationToken);

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Id != UnnecessaryUsingDirectiveId)
            {
                continue;
            }

            var mappedSpan = diagnostic.Location.GetMappedLineSpan();
            if (!mappedSpan.HasMappedPath ||
                !PathEquals(mappedSpan.Path, sourceFilePath) ||
                mappedSpan.StartLinePosition.Line >= sourceText.Lines.Count)
            {
                continue;
            }

            positions.Add(sourceText.Lines.GetPosition(mappedSpan.StartLinePosition));
        }

        return positions;
    }

    /// <summary>
    /// Gets the usings that the imports files of the document declare. The default imports that Razor adds have no
    /// file, so they are not included.
    /// </summary>
    private static HashSet<(string Content, bool IsStatic)> GetImportedUsings(RazorCodeDocument codeDocument)
    {
        var importedUsings = new HashSet<(string Content, bool IsStatic)>();

        foreach (var importSyntaxTree in codeDocument.GetImportSyntaxTrees())
        {
            if (importSyntaxTree.Source.FilePath is null)
            {
                continue;
            }

            foreach (var directive in importSyntaxTree.Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>())
            {
                if (TryGetUsingKey(directive, out var key))
                {
                    importedUsings.Add(key);
                }
            }
        }

        return importedUsings;
    }

    /// <summary>
    /// Gets a key that is equal for two directives that import the same namespace, type, or alias. The key ignores
    /// whitespace, so <c>@using A.B</c> and <c>@using A . B</c> match.
    /// </summary>
    private static bool TryGetUsingKey(RazorUsingDirectiveSyntax directive, out (string Content, bool IsStatic) key)
    {
        foreach (var node in directive.DescendantNodes())
        {
            if (node.GetChunkGenerator() is AddImportChunkGenerator import)
            {
                key = (string.Concat(import.Namespace.Where(static c => !char.IsWhiteSpace(c))), import.IsStatic);
                return true;
            }
        }

        key = default;
        return false;
    }

    private static async Task<SyntaxTree> GetRequiredSyntaxTreeAsync(SourceGeneratedDocument document, CancellationToken cancellationToken)
        => await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Generated document '{document.Name}' did not have a syntax tree.");

    private static bool PathEquals(string path, string otherPath)
        => string.Equals(
            path.Replace('\\', '/'),
            otherPath.Replace('\\', '/'),
            StringComparison.OrdinalIgnoreCase);
}
