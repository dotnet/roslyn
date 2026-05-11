// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class CodeActionResolveService(
    IEnumerable<IRazorCodeActionResolver> razorCodeActionResolvers,
    IEnumerable<ICSharpCodeActionResolver> csharpCodeActionResolvers,
    IEnumerable<IHtmlCodeActionResolver> htmlCodeActionResolvers,
    IClientSettingsManager clientSettingsManager,
    ILoggerFactory loggerFactory) : ICodeActionResolveService

{
    private readonly FrozenDictionary<string, IRazorCodeActionResolver> _razorCodeActionResolvers = CreateResolverMap(razorCodeActionResolvers);
    private readonly FrozenDictionary<string, ICSharpCodeActionResolver> _csharpCodeActionResolvers = CreateResolverMap(csharpCodeActionResolvers);
    private readonly FrozenDictionary<string, IHtmlCodeActionResolver> _htmlCodeActionResolvers = CreateResolverMap(htmlCodeActionResolvers);
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CodeActionResolveService>();

    public async Task<CodeAction> ResolveCodeActionAsync(DocumentContext documentContext, CodeAction request, CodeAction? resolvedDelegatedCodeAction, CancellationToken cancellationToken)
    {
        var resolutionParams = GetRazorCodeActionResolutionParams(request);

        var codeActionId = GetCodeActionId(resolutionParams);
        _logger.LogDebug($"Resolving workspace edit for action {codeActionId}.");

        // If it's a special "edit based code action" then the edit has been pre-computed and we
        // can extract the edit details and return to the client. This is only required for VSCode
        // as it does not support Command.Edit based code actions anymore.
        if (resolutionParams.Action == LanguageServerConstants.CodeActions.EditBasedCodeActionCommand)
        {
            request.Edit = (resolutionParams.Data as JsonElement?)?.Deserialize<WorkspaceEdit>();
            return request;
        }

        switch (resolutionParams.Language)
        {
            case RazorLanguageKind.Razor:
                return await ResolveRazorCodeActionAsync(
                    documentContext,
                    request,
                    resolutionParams,
                    cancellationToken).ConfigureAwait(false);
            case RazorLanguageKind.CSharp:
                return await ResolveCSharpCodeActionAsync(
                    documentContext,
                    resolvedDelegatedCodeAction.AssumeNotNull(),
                    resolutionParams,
                    cancellationToken).ConfigureAwait(false);
            case RazorLanguageKind.Html:
                return await ResolveHtmlCodeActionAsync(
                    documentContext,
                    resolvedDelegatedCodeAction.AssumeNotNull(),
                    resolutionParams,
                    cancellationToken).ConfigureAwait(false);
            default:
                _logger.LogError($"Invalid CodeAction.Data.Language. Received {codeActionId}.");
                return request;
        }
    }

    public static RazorCodeActionResolutionParams GetRazorCodeActionResolutionParams(CodeAction request)
    {
        if (request.Data is not JsonElement paramsObj)
        {
            throw new InvalidOperationException($"Invalid CodeAction Received '{request.Title}'.");
        }

        var resolutionParams = paramsObj.Deserialize<RazorCodeActionResolutionParams>();
        if (resolutionParams is null)
        {
            throw new InvalidOperationException($"request.Data should be convertible to {nameof(RazorCodeActionResolutionParams)}");
        }

        return resolutionParams;
    }

    private async Task<CodeAction> ResolveRazorCodeActionAsync(
        DocumentContext documentContext,
        CodeAction codeAction,
        RazorCodeActionResolutionParams resolutionParams,
        CancellationToken cancellationToken)
    {
        if (!_razorCodeActionResolvers.TryGetValue(resolutionParams.Action, out var resolver))
        {
            var codeActionId = GetCodeActionId(resolutionParams);
            _logger.LogWarning($"No resolver registered for {codeActionId}");
            Debug.Fail($"No resolver registered for {codeActionId}.");
            return codeAction;
        }

        if (resolutionParams.Data is not JsonElement data)
        {
            return codeAction;
        }

        var options = _clientSettingsManager.GetClientSettings().ToRazorFormattingOptions();
        var edit = await resolver.ResolveAsync(documentContext, data, options, cancellationToken).ConfigureAwait(false);
        codeAction.Edit = edit;
        return codeAction;
    }

    private async Task<CodeAction> ResolveCSharpCodeActionAsync(DocumentContext documentContext, CodeAction codeAction, RazorCodeActionResolutionParams resolutionParams, CancellationToken cancellationToken)
    {
        if (TryGetResolver(resolutionParams, _csharpCodeActionResolvers, out var resolver))
        {
            return await resolver.ResolveAsync(documentContext, codeAction, cancellationToken).ConfigureAwait(false);
        }

        return codeAction;
    }

    private async Task<CodeAction> ResolveHtmlCodeActionAsync(DocumentContext documentContext, CodeAction codeAction, RazorCodeActionResolutionParams resolutionParams, CancellationToken cancellationToken)
    {
        if (TryGetResolver(resolutionParams, _htmlCodeActionResolvers, out var resolver))
        {
            return await resolver.ResolveAsync(documentContext, codeAction, cancellationToken).ConfigureAwait(false);
        }

        return codeAction;
    }

    private bool TryGetResolver<TResolver>(RazorCodeActionResolutionParams resolutionParams, FrozenDictionary<string, TResolver> resolvers, [NotNullWhen(true)] out TResolver? resolver)
          where TResolver : ICodeActionResolver
    {
        if (!resolvers.TryGetValue(resolutionParams.Action, out resolver))
        {
            var codeActionId = GetCodeActionId(resolutionParams);
            _logger.LogWarning($"No resolver registered for {codeActionId}");
            Debug.Fail($"No resolver registered for {codeActionId}.");
            return false;
        }

        return resolver is not null;
    }

    private static FrozenDictionary<string, T> CreateResolverMap<T>(IEnumerable<T> codeActionResolvers)
        where T : ICodeActionResolver
    {
        using var _ = SpecializedPools.GetPooledStringDictionary<T>(out var resolverMap);

        foreach (var resolver in codeActionResolvers)
        {
            if (resolverMap.ContainsKey(resolver.Action))
            {
                Debug.Fail($"Duplicate resolver action for {resolver.Action} of type {typeof(T)}.");
            }

            resolverMap[resolver.Action] = resolver;
        }

        return resolverMap.ToFrozenDictionary();
    }

    private static string GetCodeActionId(RazorCodeActionResolutionParams resolutionParams) =>
        $"`{resolutionParams.Language}.{resolutionParams.Action}`";

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CodeActionResolveService instance)
    {
        public Task<CodeAction> ResolveRazorCodeActionAsync(DocumentContext documentContext, CodeAction codeAction, RazorCodeActionResolutionParams resolutionParams, CancellationToken cancellationToken)
            => instance.ResolveRazorCodeActionAsync(documentContext, codeAction, resolutionParams, cancellationToken);

        public Task<CodeAction> ResolveCSharpCodeActionAsync(DocumentContext documentContext, CodeAction codeAction, RazorCodeActionResolutionParams resolutionParams, CancellationToken cancellationToken)
            => instance.ResolveCSharpCodeActionAsync(documentContext, codeAction, resolutionParams, cancellationToken);

        public Task<CodeAction> ResolveHtmlCodeActionAsync(DocumentContext documentContext, CodeAction codeAction, RazorCodeActionResolutionParams resolutionParams, CancellationToken cancellationToken)
            => instance.ResolveCSharpCodeActionAsync(documentContext, codeAction, resolutionParams, cancellationToken);
    }
}
