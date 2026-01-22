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
/// Experimental XAML Language Server Client used everywhere when
/// <see cref="XamlOptions.EnableLspIntelliSenseFeatureFlag"/> is turned on.
/// </summary>
[ContentType(ContentTypeNames.XamlContentType)]
[Export(typeof(ILanguageClient))]
internal sealed class XamlInProcLanguageClient : AbstractInProcLanguageClient
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
    public XamlInProcLanguageClient(
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
        var isLspExperimentEnabled = IsXamlLspIntelliSenseEnabled();

        return isLspExperimentEnabled ? XamlCapabilities.Current : XamlCapabilities.None;
    }

    /// <summary>
    /// Failures are only catastrophic when this server is providing intellisense features.
    /// </summary>
    public override bool ShowNotificationOnInitializeFailed => IsXamlLspIntelliSenseEnabled();

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.XamlLspServer;

    private bool IsXamlLspIntelliSenseEnabled()
        => GlobalOptions.GetOption(XamlOptions.EnableLspIntelliSenseFeatureFlag);
}
