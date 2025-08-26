// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportCSharpVisualBasicLspServiceFactory(typeof(RazorStartupService), WellKnownLspServerKinds.Any), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorStartupServiceFactory(
    [Import(AllowDefault = true)] IUIContextActivationService? uIContextActivationService,
    [Import(AllowDefault = true)] Lazy<ICohostStartupService>? cohostStartupService,
    [Import(AllowDefault = true)] Lazy<AbstractRazorCohostLifecycleService>? razorCohostLifecycleService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new RazorStartupService(uIContextActivationService, cohostStartupService, razorCohostLifecycleService);
    }

    private class RazorStartupService(
        IUIContextActivationService? uIContextActivationService,
#pragma warning disable CS0618 // Type or member is obsolete
        Lazy<ICohostStartupService>? cohostStartupService,
#pragma warning restore CS0618 // Type or member is obsolete
        Lazy<AbstractRazorCohostLifecycleService>? razorCohostLifecycleService) : ILspService, IOnInitialized, IDisposable
    {
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private IDisposable? _cohostActivation;
        private IDisposable? _razorFilePresentActivation;

        public void Dispose()
        {
            if (razorCohostLifecycleService is { IsValueCreated: true, Value: var service })
            {
                service.Dispose();
            }

            _razorFilePresentActivation?.Dispose();
            _razorFilePresentActivation = null;
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

            if (cohostStartupService is null && razorCohostLifecycleService is null)
            {
                return Task.CompletedTask;
            }

            if (uIContextActivationService is null)
            {
                PreinitializeRazor();
                InitializeRazor();
            }
            else
            {
                // There are two initialization methods for Razor, which looks odd here, but are really controlled by UI contexts.
                // This method fires for any Roslyn project, but not all Roslyn projects are Razor projects, so the first UI context
                // triggers where there is a project with a Razor capability present in the solution, and the next is when a Razor file
                // is opened in the editor. ie these two lines look the same, but really they do different levels of initialization.
                _razorFilePresentActivation = uIContextActivationService.ExecuteWhenActivated(Constants.RazorCapabilityPresentUIContext, PreinitializeRazor);
                _cohostActivation = uIContextActivationService.ExecuteWhenActivated(Constants.RazorCohostingUIContext, InitializeRazor);
            }

            return Task.CompletedTask;

            void PreinitializeRazor()
            {
                this.PreinitializeRazorAsync(_disposalTokenSource.Token).ReportNonFatalErrorAsync();
            }

            void InitializeRazor()
            {
                this.InitializeRazorAsync(clientCapabilities, context, _disposalTokenSource.Token).ReportNonFatalErrorAsync();
            }
        }

        private async Task PreinitializeRazorAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            await TaskScheduler.Default.SwitchTo(alwaysYield: true);

            if (razorCohostLifecycleService is not null)
            {
                await razorCohostLifecycleService.Value.LspServerIntializedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task InitializeRazorAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            // The LSP server will dispose us when the server exits, but VS could decide to activate us later.
            // If a new instance of the server is created, a new instance of this class will be created and the
            // UIContext will already be active, so this method will be immediately called on the new instance.
            if (cancellationToken.IsCancellationRequested) return;

            await TaskScheduler.Default.SwitchTo(alwaysYield: true);

            using var languageScope = context.Logger.CreateLanguageContext(Constants.RazorLanguageName);

            var requestContext = new RazorCohostRequestContext(context);

            if (razorCohostLifecycleService is not null)
            {
                // If we have a cohost lifecycle service, fire post-initialization, which happens when the UIContext is activated.
                await razorCohostLifecycleService.Value.RazorActivatedAsync(clientCapabilities, requestContext, cancellationToken).ConfigureAwait(false);
            }

            if (cohostStartupService is not null)
            {
                // We use a string to pass capabilities to/from Razor to avoid version issues with the Protocol DLL
                var serializedClientCapabilities = JsonSerializer.Serialize(clientCapabilities, ProtocolConversions.LspJsonSerializerOptions);
                await cohostStartupService.Value.StartupAsync(serializedClientCapabilities, requestContext, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
