// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(ICodeActionsService)), Shared]
[method: ImportingConstructor]
internal sealed class CodeActionsService(
    IDocumentMappingService documentMappingService,
    [ImportMany] IEnumerable<IRazorCodeActionProvider> razorCodeActionProviders,
    [ImportMany] IEnumerable<ICSharpCodeActionProvider> csharpCodeActionProviders,
    [ImportMany] IEnumerable<IHtmlCodeActionProvider> htmlCodeActionProviders,
    LanguageServerFeatureOptions languageServerFeatureOptions,
    ILoggerFactory loggerFactory) : ICodeActionsService
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IEnumerable<IRazorCodeActionProvider> _razorCodeActionProviders = razorCodeActionProviders;
    private readonly IEnumerable<ICSharpCodeActionProvider> _csharpCodeActionProviders = csharpCodeActionProviders;
    private readonly IEnumerable<IHtmlCodeActionProvider> _htmlCodeActionProviders = htmlCodeActionProviders;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CodeActionsService>();

    public async Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(VSCodeActionParams request, RemoteDocumentSnapshot documentSnapshot, RazorVSInternalCodeAction[] delegatedCodeActions, Uri? delegatedDocumentUri, bool supportsCodeActionResolve, CancellationToken cancellationToken)
    {
        LogCodeActionTrace($"RemoteCodeActions.Entry: Document='{documentSnapshot.FilePath}', RequestRange='{FormatRange(request.Range)}', SelectionRange='{FormatRange(request.Context.SelectionRange)}', Diagnostics=[{FormatDiagnostics(request.Context.Diagnostics)}], DelegatedDocumentUri='{delegatedDocumentUri}', SupportsResolve={supportsCodeActionResolve}, DelegatedInputCount={delegatedCodeActions.Length}, DelegatedInput=[{FormatCodeActions(delegatedCodeActions)}]");

        var razorCodeActionContext = await GenerateRazorCodeActionContextAsync(request, documentSnapshot, delegatedDocumentUri, supportsCodeActionResolve, cancellationToken).ConfigureAwait(false);
        if (razorCodeActionContext is null)
        {
            LogCodeActionTrace($"RemoteCodeActions.NoContext: Document='{documentSnapshot.FilePath}', RequestRange='{FormatRange(request.Range)}'");
            return null;
        }

        LogCodeActionTrace($"RemoteCodeActions.Context: Document='{documentSnapshot.FilePath}', LanguageKind='{razorCodeActionContext.LanguageKind}', StartAbsoluteIndex={razorCodeActionContext.StartAbsoluteIndex}, EndAbsoluteIndex={razorCodeActionContext.EndAbsoluteIndex}, SupportsResolve={razorCodeActionContext.SupportsCodeActionResolve}");

        delegatedCodeActions = razorCodeActionContext.LanguageKind switch
        {
            RazorLanguageKind.CSharp => ExtractCSharpCodeActionNamesFromData(delegatedCodeActions),
            RazorLanguageKind.Html => delegatedCodeActions,
            _ => []
        };

        LogCodeActionTrace($"RemoteCodeActions.DelegatedAfterLanguageProcessing: LanguageKind='{razorCodeActionContext.LanguageKind}', Count={delegatedCodeActions.Length}, Actions=[{FormatCodeActions(delegatedCodeActions)}]");

        var razorCodeActions = await GetRazorCodeActionsAsync(razorCodeActionContext, cancellationToken).ConfigureAwait(false);
        LogCodeActionTrace($"RemoteCodeActions.RazorProviderResults: Count={razorCodeActions.Length}, Actions=[{FormatCodeActions(razorCodeActions)}]");

        cancellationToken.ThrowIfCancellationRequested();

        var filteredCodeActions = await FilterDelegatedCodeActionsAsync(razorCodeActionContext, [.. delegatedCodeActions], cancellationToken).ConfigureAwait(false);
        LogCodeActionTrace($"RemoteCodeActions.FilteredDelegatedResults: Count={filteredCodeActions.Length}, Actions=[{FormatCodeActions(filteredCodeActions)}]");

        cancellationToken.ThrowIfCancellationRequested();
        using var commandsOrCodeActions = new PooledArrayBuilder<SumType<Command, CodeAction>>();

        // Grouping the code actions causes VS to sort them into groups, rather than just alphabetically sorting them
        // by title. The latter is bad for us because it can put "Remove <div>" at the top in some locales, and our fully
        // qualify component code action at the bottom, depending on the users namespace.
        ConvertCodeActionsToSumType(razorCodeActions, "A-Razor");
        ConvertCodeActionsToSumType(filteredCodeActions, "B-Delegated");

        LogCodeActionTrace($"RemoteCodeActions.Final: Count={commandsOrCodeActions.Count}, RazorCount={razorCodeActions.Length}, DelegatedCount={filteredCodeActions.Length}");
        return commandsOrCodeActions.ToArray();

        void ConvertCodeActionsToSumType(ImmutableArray<RazorVSInternalCodeAction> codeActions, string groupName)
        {
            // We must cast the RazorCodeAction into a platform compliant code action
            // For VS (SupportsCodeActionResolve = true) this means just encapsulating the RazorCodeAction in the `CommandOrCodeAction` struct
            // For VS Code (SupportsCodeActionResolve = false) we must convert it into a CodeAction or Command before encapsulating in the `CommandOrCodeAction` struct.
            if (supportsCodeActionResolve)
            {
                foreach (var action in codeActions)
                {
                    // Make sure we honour the grouping that a delegated server may have created
                    action.Group = groupName + (action.Group ?? string.Empty);
                    commandsOrCodeActions.Add(action);
                }
            }
            else
            {
                foreach (var action in codeActions)
                {
                    commandsOrCodeActions.Add(action.AsVSCodeCommandOrCodeAction(request.TextDocument, delegatedDocumentUri));
                }
            }
        }
    }

    private async Task<RazorCodeActionContext?> GenerateRazorCodeActionContextAsync(
        VSCodeActionParams request,
        RemoteDocumentSnapshot documentSnapshot,
        Uri? delegatedDocumentUri,
        bool supportsCodeActionResolve,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = codeDocument.Source.Text;

        if (!sourceText.TryGetAbsoluteIndex(request.Range.Start, out var startLocation))
        {
            return null;
        }

        if (!sourceText.TryGetAbsoluteIndex(request.Range.End, out var endLocation))
        {
            endLocation = startLocation;
        }

        var languageKind = _documentMappingService.GetPositionInfo(codeDocument, startLocation).LanguageKind;
        var context = new RazorCodeActionContext(
            request,
            documentSnapshot,
            codeDocument,
            delegatedDocumentUri,
            startLocation,
            endLocation,
            languageKind,
            sourceText,
            _languageServerFeatureOptions.SupportsFileManipulation,
            supportsCodeActionResolve);

        return context;
    }

    public async Task<VSCodeActionParams?> GetCSharpCodeActionsRequestAsync(RemoteDocumentSnapshot documentSnapshot, VSCodeActionParams request, CancellationToken cancellationToken)
    {
        // For C# we have to map the ranges to the generated document
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        if (!_documentMappingService.TryMapToCSharpDocumentRange(csharpDocument, request.Range, out var projectedRange))
        {
            return null;
        }

        var newContext = request.Context;
        if (request.Context is VSInternalCodeActionContext { SelectionRange: not null } vsContext &&
            _documentMappingService.TryMapToCSharpDocumentRange(csharpDocument, vsContext.SelectionRange, out var selectionRange))
        {
            vsContext.SelectionRange = selectionRange;
            newContext = vsContext;
        }

        // @inherits projects onto the base type only (ie, just "Base" in `Component : Base`), but some Roslyn code actions are only
        // offered on the class declaration itself (ie, "Component" in above). In this case we widen the request to the whole declaration.
        if (await TryExpandInheritsDirectiveRangeToWholeDeclarationAsync(documentSnapshot, codeDocument, request.Range, projectedRange, cancellationToken).ConfigureAwait(false) is { } inheritsDirectiveRange)
        {
            projectedRange = inheritsDirectiveRange;

            if (newContext is VSInternalCodeActionContext inheritsVsContext)
            {
                inheritsVsContext.SelectionRange = inheritsDirectiveRange;
                newContext = inheritsVsContext;
            }
        }

        return new VSCodeActionParams
        {
            TextDocument = new VSTextDocumentIdentifier()
            {
                DocumentUri = request.TextDocument.DocumentUri,
                ProjectContext = request.TextDocument.ProjectContext
            },
            Context = newContext,
            Range = projectedRange,
        };
    }

    private static async Task<LspRange?> TryExpandInheritsDirectiveRangeToWholeDeclarationAsync(
        RemoteDocumentSnapshot documentSnapshot,
        RazorCodeDocument codeDocument,
        LspRange razorRange,
        LspRange projectedRange,
        CancellationToken cancellationToken)
    {
        var sourceText = codeDocument.Source.Text;
        var absoluteIndex = sourceText.GetRequiredAbsoluteIndex(razorRange.Start);
        var razorToken = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root.FindToken(absoluteIndex);
        if (razorToken.Parent?.FirstAncestorOrSelf<BaseRazorDirectiveSyntax>() is not RazorDirectiveSyntax directive ||
            !directive.IsDirective(InheritsDirective.Directive))
        {
            return null;
        }

        var csharpSyntaxTree = await documentSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var csharpRoot = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var csharpText = await csharpSyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var projectedStart = csharpText.GetRequiredAbsoluteIndex(projectedRange.Start);
        var projectedToken = csharpRoot.FindToken(projectedStart);
        var baseType = projectedToken.Parent?.FirstAncestorOrSelf<BaseTypeSyntax>();
        if (baseType is null)
        {
            return null;
        }

        var classDeclaration = baseType.Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null)
        {
            return null;
        }

        return csharpText.GetRange(TextSpan.FromBounds(classDeclaration.Identifier.SpanStart, baseType.Type.Span.End));
    }

    private RazorVSInternalCodeAction[] ExtractCSharpCodeActionNamesFromData(RazorVSInternalCodeAction[] codeActions)
    {
        using var actions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            if (codeAction.Data is not JsonElement jsonData ||
                !jsonData.TryGetProperty("CustomTags", out var value) ||
                value.Deserialize<string[]>() is not [..] tags)
            {
                LogCodeActionTrace($"RemoteCodeActions.ExtractName.DropNoCustomTags: Action={FormatCodeAction(codeAction)}");
                continue;
            }

            codeAction.Name = GetCodeActionName(tags);
            LogCodeActionTrace($"RemoteCodeActions.ExtractName.Tags: Title='{codeAction.Title}', Tags=[{string.Join(", ", tags)}], ExtractedName='{codeAction.Name ?? "<null>"}'");

            if (string.IsNullOrEmpty(codeAction.Name))
            {
                LogCodeActionTrace($"RemoteCodeActions.ExtractName.DropNoName: Action={FormatCodeAction(codeAction)}");
                continue;
            }

            // In VS Code, Roslyn adds duplicate code actions for every code action, to implement Fix All functionality.
            // Until we implement support for that in the C# Extension, we want to filter them out.
            // https://github.com/dotnet/razor/issues/11832
            if (jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavours) &&
                fixAllFlavours.GetArrayLength() > 0)
            {
                LogCodeActionTrace($"RemoteCodeActions.ExtractName.DropFixAll: Action={FormatCodeAction(codeAction)}, FixAllFlavors={fixAllFlavours.GetArrayLength()}");
                continue;
            }

            LogCodeActionTrace($"RemoteCodeActions.ExtractName.Keep: Action={FormatCodeAction(codeAction)}");
            actions.Add(codeAction);
        }

        return actions.ToArray();
    }

    private static string? GetCodeActionName(string[] tags)
    {
        foreach (var tag in tags)
        {
            // VS Code can send these type-accessibility actions with this synthetic marker instead of
            // a Roslyn provider name. Keep it so the later Razor-specific filtering can recognize the
            // special path and turn the delegated action into our AddUsing/FullyQualify experience.
            if (tag == LanguageServerConstants.CodeActions.CodeActionFromVSCode)
            {
                return tag;
            }
        }

        // Roslyn appends the provider name to CustomTags, and the other known custom tag in this flow
        // is the high-priority GUID tag. Walk backwards so inlined nested actions still resolve to the provider name.
        for (var i = tags.Length - 1; i >= 0; i--)
        {
            var tag = tags[i];
            if (!Guid.TryParse(tag, out _))
            {
                return tag;
            }
        }

        return null;
    }

    private async Task<ImmutableArray<RazorVSInternalCodeAction>> FilterDelegatedCodeActionsAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        if (context.LanguageKind == RazorLanguageKind.Razor)
        {
            return [];
        }

        var providers = context.LanguageKind switch
        {
            RazorLanguageKind.CSharp => _csharpCodeActionProviders,
            RazorLanguageKind.Html => _htmlCodeActionProviders,
            _ => Assumed.Unreachable<IEnumerable<ICodeActionProvider>>()
        };

        cancellationToken.ThrowIfCancellationRequested();

        using var tasks = new PooledArrayBuilder<Task<ImmutableArray<RazorVSInternalCodeAction>>>();
        using var providerNames = new PooledArrayBuilder<string>();
        foreach (var provider in providers)
        {
            providerNames.Add(provider.GetType().FullName ?? provider.GetType().Name);
            tasks.Add(provider.ProvideAsync(context, codeActions, cancellationToken));
        }

        var providerResults = await Task.WhenAll(tasks.ToImmutable()).ConfigureAwait(false);
        for (var i = 0; i < providerResults.Length; i++)
        {
            LogCodeActionTrace($"RemoteCodeActions.Filter.ProviderResult: Provider='{providerNames[i]}', Count={providerResults[i].Length}, Actions=[{FormatCodeActions(providerResults[i])}]");
        }

        return ConsolidateCodeActionsFromProviderResults(providerResults, cancellationToken);
    }

    private async Task<ImmutableArray<RazorVSInternalCodeAction>> GetRazorCodeActionsAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var tasks = new PooledArrayBuilder<Task<ImmutableArray<RazorVSInternalCodeAction>>>();

        foreach (var provider in _razorCodeActionProviders)
        {
            tasks.Add(provider.ProvideAsync(context, cancellationToken));
        }

        return await ConsolidateCodeActionsFromProvidersAsync(tasks.ToImmutable(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<RazorVSInternalCodeAction>> ConsolidateCodeActionsFromProvidersAsync(
        ImmutableArray<Task<ImmutableArray<RazorVSInternalCodeAction>>> tasks,
        CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return ConsolidateCodeActionsFromProviderResults(results, cancellationToken);
    }

    private static ImmutableArray<RazorVSInternalCodeAction> ConsolidateCodeActionsFromProviderResults(
        ImmutableArray<RazorVSInternalCodeAction>[] results,
        CancellationToken cancellationToken)
    {
        using var codeActions = new PooledArrayBuilder<RazorVSInternalCodeAction>(capacity: results.Length);

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var result in results)
        {
            codeActions.AddRange(result);
        }

        return codeActions.ToImmutableOrderedByAndClear(static r => r.Order);
    }

    private void LogCodeActionTrace(string message)
    {
        var fullMessage = $"RazorCodeActionTrace {message}";
        _logger.LogDebug(fullMessage);
        Trace.WriteLine(fullMessage);
    }

    private static string FormatCodeActions(ImmutableArray<RazorVSInternalCodeAction> codeActions)
        => codeActions.IsEmpty
            ? "<empty>"
            : string.Join("; ", codeActions.Select(static (action, index) => $"{index}: {FormatCodeAction(action)}"));

    private static string FormatCodeActions(RazorVSInternalCodeAction[]? codeActions)
    {
        if (codeActions is null)
        {
            return "<null>";
        }

        return codeActions.Length == 0
            ? "<empty>"
            : string.Join("; ", codeActions.Select(static (action, index) => $"{index}: {FormatCodeAction(action)}"));
    }

    private static string FormatCodeAction(RazorVSInternalCodeAction action)
        => $"Name='{action.Name ?? "<null>"}', Title='{action.Title}', Kind='{action.Kind?.ToString() ?? "<null>"}', Group='{action.Group ?? "<null>"}', Children={action.Children?.Length ?? 0}, Command='{action.Command?.CommandIdentifier ?? "<null>"}', Data={FormatData(action.Data)}";

    private static string FormatData(object? data)
    {
        if (data is null)
        {
            return "<null>";
        }

        if (data is not JsonElement jsonData)
        {
            return data.GetType().FullName ?? data.GetType().Name;
        }

        var customTags = jsonData.TryGetProperty("CustomTags", out var customTagsValue) && customTagsValue.ValueKind == JsonValueKind.Array
            ? $"CustomTags=[{string.Join(", ", customTagsValue.EnumerateArray().Select(static tag => tag.GetString()))}]"
            : "CustomTags=<missing>";

        var fixAllFlavors = jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavorsValue) && fixAllFlavorsValue.ValueKind == JsonValueKind.Array
            ? $"FixAllFlavors={fixAllFlavorsValue.GetArrayLength()}"
            : "FixAllFlavors=<missing>";

        return $"JsonElement{{ValueKind={jsonData.ValueKind}, {customTags}, {fixAllFlavors}}}";
    }

    private static string FormatDiagnostics(IEnumerable<LspDiagnostic>? diagnostics)
        => diagnostics is null
            ? "<null>"
            : string.Join("; ", diagnostics.Select(static (diagnostic, index) => $"{index}: Code='{diagnostic.Code}', Severity='{diagnostic.Severity}', Range='{FormatRange(diagnostic.Range)}', Message='{diagnostic.Message}'"));

    private static string FormatRange(LspRange? range)
        => range is null
            ? "<null>"
            : $"{range.Start.Line}:{range.Start.Character}-{range.End.Line}:{range.End.Character}";
}
