// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Remote.Razor.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class ExtractToComponentCodeActionResolver(RazorTranslateDiagnosticsService translateDiagnosticsService) : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.ExtractToNewComponent;

    public async Task<WorkspaceEdit?> ResolveAsync(RemoteDocumentSnapshot documentSnapshot, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var actionParams = JsonSerializer.Deserialize<ExtractToComponentCodeActionParams>(data.GetRawText());
        if (actionParams is null)
        {
            return null;
        }

        var componentDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var text = componentDocument.Source.Text;
        var path = FilePathNormalizer.Normalize(documentSnapshot.Uri.GetAbsoluteOrUNCPath());
        var directoryName = Path.GetDirectoryName(path).AssumeNotNull();
        var templatePath = Path.Combine(directoryName, "Component.razor");
        var componentPath = FileUtilities.GenerateUniquePath(templatePath, ".razor");
        var componentName = Path.GetFileNameWithoutExtension(componentPath);
        var newComponentUri = LspFactory.CreateFilePathUri(componentPath);

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        if (actionParams.Namespace is not null)
        {
            builder.AppendLine($"@namespace {actionParams.Namespace}");
            builder.AppendLine();
        }

        var syntaxTree = componentDocument.GetRequiredTagHelperRewrittenSyntaxTree();

        // Razor prefers local usings over identical imports, so diagnostics can't identify those
        // copies as redundant. The new component will inherit the same imports as the original.
        using var _1 = HashSetPool<string>.GetPooledObject(out var availableUsings);
        foreach (var import in componentDocument.GetImportSyntaxTrees())
        {
            foreach (var directive in import.GetUsingDirectives())
            {
                if (RazorSyntaxFacts.TryGetNamespaceFromDirective(directive, out var content))
                {
                    availableUsings.Add(content);
                }
            }
        }

        var usingDirectives = syntaxTree.GetUsingDirectives();
        var usingCount = 0;
        foreach (var usingDirective in usingDirectives)
        {
            if (RazorSyntaxFacts.TryGetNamespaceFromDirective(usingDirective, out var content) && !availableUsings.Add(content))
            {
                continue;
            }

            builder.AppendLine(usingDirective.ToString());
            usingCount++;
        }

        // If any using directives were added, add a newline before the extracted content.
        if (usingCount > 0)
        {
            builder.AppendLine();
        }

        var contentStart = builder.Length;
        var span = TextSpan.FromBounds(actionParams.Start, actionParams.End);
        FormattingUtilities.NaivelyUnindentSubstring(text, span, builder);

        var componentContent = builder.ToString();
        if (usingCount > 0)
        {
            componentContent = await RemoveUnusedUsingsAsync(documentSnapshot, componentDocument, componentPath, componentContent, contentStart, usingCount, cancellationToken).ConfigureAwait(false);
        }

        var removeRange = text.GetRange(actionParams.Start, actionParams.End);

        var documentChanges = new SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]
        {
            new CreateFile { DocumentUri = newComponentUri },
            new TextDocumentEdit
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = documentSnapshot.Uri },
                Edits =
                [
                    new TextEdit
                    {
                        NewText = $"<{componentName} />",
                        Range = removeRange,
                    }
                ],
            },
            new TextDocumentEdit
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = newComponentUri },
                Edits  =
                [
                    new TextEdit
                    {
                        NewText = componentContent,
                        Range = LspFactory.DefaultRange,
                    }
                ],
            }
        };

        return new WorkspaceEdit
        {
            DocumentChanges = documentChanges,
        };
    }

    private async Task<string> RemoveUnusedUsingsAsync(RemoteDocumentSnapshot documentSnapshot, RazorCodeDocument originalCodeDocument, string componentPath, string componentContent, int contentStart, int usingCount, CancellationToken cancellationToken)
    {
        // Generate the new component in a temporary project so usage is checked against the extracted
        // content, rather than the original component.
        var project = documentSnapshot.ProjectSnapshot.Project;
        // The new file doesn't have build metadata yet. Use the original's project-relative path so
        // the temporary component sees the same _Imports files.
        var sourcePath = originalCodeDocument.Source.RelativePath ?? documentSnapshot.FilePath;
        var componentName = Path.GetFileName(componentPath);
        var temporaryPath = Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, componentName);
        var componentDocument = project.AddAdditionalDocument(componentName, componentContent, filePath: temporaryPath);
        var snapshot = documentSnapshot.ProjectSnapshot.SolutionSnapshot.SnapshotManager.GetSnapshot(componentDocument);
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = codeDocument.Source.Text;

        var implDocument = await snapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        var implDiagnostics = await GetUsingDiagnosticsAsync(implDocument, cancellationToken).ConfigureAwait(false);
        LspDiagnostic[] declDiagnostics = [];
        if (codeDocument.GetCSharpDocument(declarationDocument: true) is { IsStubDocument: false })
        {
            var declDocument = await snapshot.GetGeneratedDocumentAsync(declarationDocument: true, cancellationToken).ConfigureAwait(false);
            declDiagnostics = await GetUsingDiagnosticsAsync(declDocument, cancellationToken).ConfigureAwait(false);
        }

        // Use the same filtering as Remove Unnecessary Directives, which accounts for component tags
        // and checks usage in both generated documents.
        var diagnostics = await translateDiagnosticsService.TranslateCSharpAsync(implDiagnostics, declDiagnostics, snapshot, cancellationToken).ConfigureAwait(false);
        var unusedDirectives = RemoteDiagnosticsService.UpdateUnusedDirectiveDiagnostics(codeDocument, diagnostics, supportsVisualStudioExtensions: false);

        using var changes = new PooledArrayBuilder<TextChange>();
        foreach (var directive in unusedDirectives)
        {
            if (directive.Start >= contentStart)
            {
                continue;
            }

            changes.Add(sourceText.GetTextChange(UsingDirectiveHelper.GetRemoveDirectiveEdit(sourceText, directive)));
        }

        if (changes.Count == usingCount)
        {
            // The blank line after the copied usings is no longer needed if none remain.
            changes.Add(new TextChange(new TextSpan(contentStart - Environment.NewLine.Length, Environment.NewLine.Length), string.Empty));
        }

        return sourceText.WithChanges(changes.ToImmutableAndClear()).ToString();
    }

    private static async Task<LspDiagnostic[]> GetUsingDiagnosticsAsync(Document document, CancellationToken cancellationToken)
    {
        // The temporary component isn't open in the host, so run the unused-import analyzer on its
        // generated C# directly. Don't hide failures as the host can't report diagnostics for this file.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var analyzerOptions = new CompilationWithAnalyzersOptions(
            document.Project.HostAnalyzerOptions,
            onAnalyzerException: null,
            concurrentAnalysis: false,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false,
            analyzerExceptionFilter: static _ => false);
        var compilation = semanticModel.Compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer()), analyzerOptions);
        var analyzerDiagnostics = await compilation.GetAnalyzerSemanticDiagnosticsAsync(semanticModel, filterSpan: null, cancellationToken).ConfigureAwait(false);
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        using var diagnostics = new PooledArrayBuilder<LspDiagnostic>();
        foreach (var diagnostic in analyzerDiagnostics)
        {
            if (diagnostic.Id == RemoveUnnecessaryImportsConstants.IDE0005_gen)
            {
                diagnostics.Add(ToLspDiagnostic(diagnostic, sourceText));
            }
        }

        // Pass CS1537 through so the diagnostic service can avoid removing usings when IDE0005 is ambiguous.
        foreach (var diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken))
        {
            if (diagnostic.Id == "CS1537")
            {
                diagnostics.Add(ToLspDiagnostic(diagnostic, sourceText));
            }
        }

        return diagnostics.ToArrayAndClear();

        static LspDiagnostic ToLspDiagnostic(Diagnostic diagnostic, SourceText sourceText)
            => new()
            {
                Code = diagnostic.Id,
                Message = diagnostic.GetMessage(),
                Range = sourceText.GetRange(diagnostic.Location.SourceSpan),
            };
    }
}
