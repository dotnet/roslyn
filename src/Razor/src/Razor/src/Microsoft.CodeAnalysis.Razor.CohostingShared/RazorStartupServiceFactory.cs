// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportRazorLspServiceFactory(typeof(RazorStartupService)), Shared]
[method: ImportingConstructor]
internal sealed class RazorStartupServiceFactory(
    [Import(AllowDefault = true)] IUIContextActivationService? uIContextActivationService,
    [ImportMany] IEnumerable<Lazy<IRazorCohostStartupService>> lazyStartupServices) : ILspServiceFactory
#pragma warning restore RS0030 // Do not use banned APIs
{
    private static readonly Guid s_razorCohostingUIContext = new("6d5b86dc-6b8a-483b-ae30-098a3c7d6774");

    private readonly ImmutableArray<Lazy<IRazorCohostStartupService>> _lazyStartupServices = [.. lazyStartupServices];

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new RazorStartupService(uIContextActivationService, _lazyStartupServices);
    }

    private class RazorStartupService(
        IUIContextActivationService? uIContextActivationService,
        ImmutableArray<Lazy<IRazorCohostStartupService>> lazyStartupServices) : ILspService, IOnInitialized, IDisposable
    {
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private readonly ImmutableArray<Lazy<IRazorCohostStartupService>> _lazyStartupServices = lazyStartupServices;
        private IDisposable? _cohostActivation;

        public void Dispose()
        {
            _cohostActivation?.Dispose();
            _cohostActivation = null;
            _disposalTokenSource.Cancel();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            if (context.ServerKind is not (WellKnownLspServerKinds.AlwaysActiveVSLspServer or WellKnownLspServerKinds.CSharpVisualBasicLspServer))
            {
                // We have to register this class for Any server, but only want to run in the C# server in VS or VS Code
                return Task.CompletedTask;
            }

            if (uIContextActivationService is null)
            {
                InitializeRazor();
            }
            else
            {
                _cohostActivation = uIContextActivationService.ExecuteWhenActivated(s_razorCohostingUIContext, InitializeRazor);
            }

            return Task.CompletedTask;

            void InitializeRazor()
            {
                _ = InitializeRazorAndReportExceptionsAsync();
            }

            async Task InitializeRazorAndReportExceptionsAsync()
            {
                try
                {
                    await this.InitializeRazorAsync(clientCapabilities, context, _disposalTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.TraceException(ex);
                }
            }
        }

        private async Task InitializeRazorAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            // The LSP server will dispose us when the server exits, but VS could decide to activate us later.
            // If a new instance of the server is created, a new instance of this class will be created and the
            // UIContext will already be active, so this method will be immediately called on the new instance.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await TaskScheduler.Default.SwitchTo(alwaysYield: true);

            using var languageScope = context.Logger.CreateLanguageContext(LanguageInfoProvider.RazorLanguageName);
            var startupServices = _lazyStartupServices.SelectAndOrderByAsArray(p => p.Value, p => p.Order);
            var vsClientCapabilities = clientCapabilities.ToVSInternalClientCapabilities();

            foreach (var startupService in startupServices)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    context.Logger.LogInformation("Razor extension startup cancelled.");
                    return;
                }

                try
                {
                    await startupService.StartupAsync(vsClientCapabilities, context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Logger.LogException(ex, $"Error initializing Razor startup service '{startupService.GetType().Name}'");
                }
            }

            context.Logger.LogInformation("Razor extension startup finished.");
        }
    }
}
