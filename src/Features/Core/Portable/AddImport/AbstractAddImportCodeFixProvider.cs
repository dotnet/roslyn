// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider
{
    private const int MaxResults = 5;

    private readonly IPackageInstallerService? _packageInstallerService;
    private readonly ISymbolSearchService? _symbolSearchService;

    /// <summary>
    /// Values for these parameters can be provided (during testing) for mocking purposes.
    /// </summary> 
    protected AbstractAddImportCodeFixProvider(
        IPackageInstallerService? packageInstallerService = null,
        ISymbolSearchService? symbolSearchService = null)
    {
        _packageInstallerService = packageInstallerService;
        _symbolSearchService = symbolSearchService;

        // Backdoor that allows this provider to use the high-priority bucket.
        this.CustomTags = this.CustomTags.Add(CodeAction.CanBeHighPriorityTag);
    }

    /// <summary>
    /// Add-using gets special privileges as being the most used code-action, along with being a core
    /// 'smart tag' feature in VS prior to us even having 'light bulbs'.  We want them to be computed
    /// first, ahead of everything else, and the main results should show up at the top of the list.
    /// </summary>
    protected override CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.High;

    public sealed override FixAllProvider? GetFixAllProvider()
    {
        // Currently Fix All is not supported for this provider
        // https://github.com/dotnet/roslyn/issues/34457
        return null;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var span = context.Span;
        var cancellationToken = context.CancellationToken;
        var diagnostics = context.Diagnostics;

        var addImportService = document.GetRequiredLanguageService<IAddImportFeatureService>();
        var services = document.Project.Solution.Services;
        var searchOptions = await document.GetSymbolSearchOptionsAsync(cancellationToken).ConfigureAwait(false);

        var symbolSearchService = _symbolSearchService ?? services.GetRequiredService<ISymbolSearchService>();

        var installerService = searchOptions.SearchNuGetPackages ?
            _packageInstallerService ?? services.GetService<IPackageInstallerService>() : null;

        var packageSources = installerService?.IsEnabled(document.Project.Id) == true
            ? installerService.TryGetPackageSources()
            : [];

        AddImportTrace.LogMessage($"AddImport ProviderRequest: Document='{document.FilePath ?? document.Name}', Project='{document.Project.Name}', Language='{document.Project.Language}', Span='{span.Start}..{span.End}', Diagnostics=[{FormatDiagnostics(diagnostics)}], InitialSearchOptions=[ReferencedProjects={searchOptions.SearchReferencedProjectSymbols}, UnreferencedProjectSources={searchOptions.SearchUnreferencedProjectSourceSymbols}, UnreferencedMetadata={searchOptions.SearchUnreferencedMetadataSymbols}, ReferenceAssemblies={searchOptions.SearchReferenceAssemblies}, NuGetPackages={searchOptions.SearchNuGetPackages}], InstallerService='{installerService?.GetType().FullName ?? "<null>"}', PackageSources={packageSources.Length}");

        if (packageSources.IsEmpty)
        {
            searchOptions = searchOptions with { SearchNuGetPackages = false };
            AddImportTrace.LogMessage($"AddImport ProviderSearchOptionsAdjusted: Document='{document.FilePath ?? document.Name}', Reason='No package sources', SearchNuGetPackages={searchOptions.SearchNuGetPackages}");
        }

        var addImportOptions = await document.GetAddImportOptionsAsync(
            searchOptions, cleanupDocument: true, cancellationToken).ConfigureAwait(false);

        AddImportTrace.LogMessage($"AddImport ProviderOptions: Document='{document.FilePath ?? document.Name}', CleanupDocument={addImportOptions.CleanupDocument}, CleanupOptionsType='{addImportOptions.CleanupOptions.GetType().FullName}', SearchOptions=[ReferencedProjects={addImportOptions.SearchOptions.SearchReferencedProjectSymbols}, UnreferencedProjectSources={addImportOptions.SearchOptions.SearchUnreferencedProjectSourceSymbols}, UnreferencedMetadata={addImportOptions.SearchOptions.SearchUnreferencedMetadataSymbols}, ReferenceAssemblies={addImportOptions.SearchOptions.SearchReferenceAssemblies}, NuGetPackages={addImportOptions.SearchOptions.SearchNuGetPackages}]");

        ImmutableArray<(Diagnostic Diagnostic, ImmutableArray<AddImportFixData> Fixes)> fixesForDiagnostic;
        try
        {
            fixesForDiagnostic = await addImportService.GetFixesForDiagnosticsAsync(
                document, span, diagnostics, MaxResults, symbolSearchService, addImportOptions, packageSources, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAddImportProviderException(
                $"ProviderFailure: Document='{document.FilePath ?? document.Name}', Project='{document.Project.Name}', Language='{document.Project.Language}', Span='{span.Start}..{span.End}', Diagnostics=[{FormatDiagnostics(diagnostics)}]",
                ex);
            throw;
        }

        AddImportTrace.LogMessage($"AddImport ProviderFixesForDiagnostics: Document='{document.FilePath ?? document.Name}', DiagnosticCount={fixesForDiagnostic.Length}, Fixes=[{string.Join("; ", fixesForDiagnostic.Select(static (entry, index) => $"{index}: Diagnostic='{entry.Diagnostic.Id}' Span='{entry.Diagnostic.Location.SourceSpan.Start}..{entry.Diagnostic.Location.SourceSpan.End}' FixCount={entry.Fixes.Length} Fixes=[{AddImportTrace.CreateFixSummary(entry.Fixes)}]"))}]");

        foreach (var (diagnostic, fixes) in fixesForDiagnostic)
        {
            // Limit the results returned since this will be displayed to the user
            var codeActions = addImportService.GetCodeActionsForFixes(document, fixes, installerService, MaxResults);
            AddImportTrace.LogMessage($"AddImport ProviderRegisterFixes: Document='{document.FilePath ?? document.Name}', Diagnostic='{diagnostic.Id}', DiagnosticSpan='{diagnostic.Location.SourceSpan.Start}..{diagnostic.Location.SourceSpan.End}', FixCount={fixes.Length}, CodeActionCount={codeActions.Length}, CodeActions=[{string.Join("; ", codeActions.Select(static (action, index) => $"{index}: Title='{action.Title}', EquivalenceKey='{action.EquivalenceKey ?? "<null>"}', Priority='{action.Priority}'"))}]");
            context.RegisterFixes(codeActions, diagnostic);
        }
    }

    private static string FormatDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        => diagnostics.IsEmpty
            ? "<empty>"
            : string.Join("; ", diagnostics.Select(static (diagnostic, index) =>
                $"{index}: Id='{diagnostic.Id}', Severity='{diagnostic.Severity}', Span='{diagnostic.Location.SourceSpan.Start}..{diagnostic.Location.SourceSpan.End}', Message='{diagnostic.GetMessage()}'"));

    private static void LogAddImportProviderException(string message, Exception exception)
        => AddImportTrace.LogException(message, exception);
}
