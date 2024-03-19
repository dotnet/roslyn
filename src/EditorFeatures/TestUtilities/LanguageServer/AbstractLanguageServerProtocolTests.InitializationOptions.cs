// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Roslyn.Test.Utilities
{
    public abstract partial class AbstractLanguageServerProtocolTests
    {
        internal readonly record struct InitializationOptions()
        {
            internal string[] SourceGeneratedMarkups { get; init; } = [];
            // Use this to specify the containing folders for each document.
            // Its count need to be same as documents' count.
            internal string[]? DocumentFileContainingFolders { get; init; } = null;
            internal LSP.ClientCapabilities ClientCapabilities { get; init; } = new LSP.ClientCapabilities();
            internal WellKnownLspServerKinds ServerKind { get; init; } = WellKnownLspServerKinds.AlwaysActiveVSLspServer;
            internal Action<IGlobalOptionService>? OptionUpdater { get; init; } = null;
            internal bool CallInitialized { get; init; } = true;
            internal object? ClientTarget { get; init; } = null;
            internal string? Locale { get; init; } = null;
            internal IEnumerable<DiagnosticAnalyzer>? AdditionalAnalyzers { get; init; } = null;
        }
    }
}
