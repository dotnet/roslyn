// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class CodeActionsService(
    IDocumentMappingService documentMappingService,
    IEnumerable<IRazorCodeActionProvider> razorCodeActionProviders,
    IEnumerable<ICSharpCodeActionProvider> csharpCodeActionProviders,
    IEnumerable<IHtmlCodeActionProvider> htmlCodeActionProviders,
    LanguageServerFeatureOptions languageServerFeatureOptions) : ICodeActionsService
{
    private static readonly ImmutableHashSet<string> s_allAvailableCodeActionNames = GetAllAvailableCodeActionNames();

    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IEnumerable<IRazorCodeActionProvider> _razorCodeActionProviders = razorCodeActionProviders;
    private readonly IEnumerable<ICSharpCodeActionProvider> _csharpCodeActionProviders = csharpCodeActionProviders;
    private readonly IEnumerable<IHtmlCodeActionProvider> _htmlCodeActionProviders = htmlCodeActionProviders;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;

    public async Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(VSCodeActionParams request, IDocumentSnapshot documentSnapshot, RazorVSInternalCodeAction[] delegatedCodeActions, Uri? delegatedDocumentUri, bool supportsCodeActionResolve, CancellationToken cancellationToken)
    {
        var razorCodeActionContext = await GenerateRazorCodeActionContextAsync(request, documentSnapshot, delegatedDocumentUri, supportsCodeActionResolve, cancellationToken).ConfigureAwait(false);
        if (razorCodeActionContext is null)
        {
            return null;
        }

        delegatedCodeActions = razorCodeActionContext.LanguageKind switch
        {
            RazorLanguageKind.CSharp => ExtractCSharpCodeActionNamesFromData(delegatedCodeActions),
            RazorLanguageKind.Html => delegatedCodeActions,
            _ => []
        };

        var razorCodeActions = await GetRazorCodeActionsAsync(razorCodeActionContext, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var filteredCodeActions = await FilterDelegatedCodeActionsAsync(razorCodeActionContext, [.. delegatedCodeActions], cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        using var commandsOrCodeActions = new PooledArrayBuilder<SumType<Command, CodeAction>>();

        // Grouping the code actions causes VS to sort them into groups, rather than just alphabetically sorting them
        // by title. The latter is bad for us because it can put "Remove <div>" at the top in some locales, and our fully
        // qualify component code action at the bottom, depending on the users namespace.
        ConvertCodeActionsToSumType(razorCodeActions, "A-Razor");
        ConvertCodeActionsToSumType(filteredCodeActions, "B-Delegated");

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
        IDocumentSnapshot documentSnapshot,
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

    public async Task<VSCodeActionParams?> GetCSharpCodeActionsRequestAsync(IDocumentSnapshot documentSnapshot, VSCodeActionParams request, CancellationToken cancellationToken)
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

    private RazorVSInternalCodeAction[] ExtractCSharpCodeActionNamesFromData(RazorVSInternalCodeAction[] codeActions)
    {
        using var actions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            if (codeAction.Data is not JsonElement jsonData ||
                !jsonData.TryGetProperty("CustomTags", out var value) ||
                value.Deserialize<string[]>() is not [..] tags)
            {
                continue;
            }

            foreach (var tag in tags)
            {
                if (s_allAvailableCodeActionNames.Contains(tag))
                {
                    codeAction.Name = tag;
                    break;
                }
            }

            if (string.IsNullOrEmpty(codeAction.Name))
            {
                continue;
            }

            // In VS Code, Roslyn adds duplicate code actions for every code action, to implement Fix All functionality.
            // Until we implement support for that in the C# Extension, we want to filter them out.
            // https://github.com/dotnet/razor/issues/11832
            if (jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavours) &&
                fixAllFlavours.GetArrayLength() > 0)
            {
                continue;
            }

            actions.Add(codeAction);
        }

        return actions.ToArray();
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
        foreach (var provider in providers)
        {
            tasks.Add(provider.ProvideAsync(context, codeActions, cancellationToken));
        }

        return await ConsolidateCodeActionsFromProvidersAsync(tasks.ToImmutable(), cancellationToken).ConfigureAwait(false);
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

        using var codeActions = new PooledArrayBuilder<RazorVSInternalCodeAction>(capacity: tasks.Length);

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var result in results)
        {
            codeActions.AddRange(result);
        }

        return codeActions.ToImmutableOrderedByAndClear(static r => r.Order);
    }

    private static ImmutableHashSet<string> GetAllAvailableCodeActionNames()
    {
        using var _ = ArrayBuilderPool<string>.GetPooledObject(out var availableCodeActionNames);

        var refactoringProviderNames = typeof(RazorPredefinedCodeRefactoringProviderNames)
            .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.GetValue(null) as string)
            .WhereNotNull();
        var codeFixProviderNames = typeof(RazorPredefinedCodeFixProviderNames)
            .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.GetValue(null) as string)
            .WhereNotNull();

        availableCodeActionNames.AddRange(refactoringProviderNames);
        availableCodeActionNames.AddRange(codeFixProviderNames);
        availableCodeActionNames.Add(LanguageServerConstants.CodeActions.CodeActionFromVSCode);

        return availableCodeActionNames.ToImmutableHashSet();
    }

    public static void AdjustRequestRangeIfNecessary(VSCodeActionParams request)
    {
        // VS Provides `CodeActionParams.Context.SelectionRange` in addition to
        // `CodeActionParams.Range`. The `SelectionRange` is relative to where the
        // code action was invoked (ex. line 14, char 3) whereas the `Range` is
        // always at the start of the line (ex. line 14, char 0). We want to utilize
        // the relative positioning to ensure we provide code actions for the appropriate
        // context.
        //
        // We only do this if the Range contains the SelectionRange, or in other words if
        // the SelectionRange serves to better focus the Range. It is possible for the selection
        // to be on one line, and the code action request to be for an entirely different line
        // if the user is invoking from the lightbulb button directly, for example on hovering
        // over a diagnostic. In those cases, using SelectionRange would be wrong.
        //
        // Note: VS Code doesn't provide a `SelectionRange`.
        if (request.Context.SelectionRange is { } selectionRange &&
            request.Range.Contains(selectionRange))
        {
            request.Range = selectionRange;
        }
    }
}
