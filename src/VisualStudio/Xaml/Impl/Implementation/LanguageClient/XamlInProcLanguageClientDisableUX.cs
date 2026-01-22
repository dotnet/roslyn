// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.Utilities;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml;

/// <summary>
/// XAML Language Server Client for LiveShare and Codespaces. Unused when
/// <see cref="XamlOptions.EnableLspIntelliSenseFeatureFlag"/> is turned on.
/// Remove this when we are ready to use LSP everywhere
/// </summary>
[DisableUserExperience(true)]
[ContentType(ContentTypeNames.XamlContentType)]
[Export(typeof(ILanguageClient))]
internal sealed class XamlInProcLanguageClientDisableUX : AbstractInProcLanguageClient
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
    public XamlInProcLanguageClientDisableUX(
        XamlLspServiceProvider lspServiceProvider,
        IGlobalOptionService globalOptions,
        ILspServiceLoggerFactory lspLoggerFactory,
        ExportProvider exportProvider)
        : base(lspServiceProvider, globalOptions, lspLoggerFactory, exportProvider)
    {
    }

    protected override ImmutableArray<string> SupportedLanguages => [StringConstants.XamlLanguageName];

    public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var isLspExperimentEnabled = GlobalOptions.GetOption(XamlOptions.EnableLspIntelliSenseFeatureFlag);
        var capabilities = isLspExperimentEnabled ? XamlCapabilities.None : XamlCapabilities.Current;

        // Only turn on CodeAction support for client scenarios. Hosts will get non-LSP lightbulbs automatically.
        capabilities.CodeActionProvider = new CodeActionOptions { CodeActionKinds = [CodeActionKind.QuickFix, CodeActionKind.Refactor], ResolveProvider = true };

        return capabilities;
    }

    /// <summary>
    /// Failures are catastrophic as liveshare guests will not have language features without this server.
    /// </summary>
    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.XamlLspServerDisableUX;
}
