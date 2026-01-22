// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Roslyn.LanguageServer.Protocol;

[ExportCSharpVisualBasicStatelessLspService(typeof(ICapabilitiesProvider), WellKnownLspServerKinds.RazorLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorInProcLanguageClientCapabilitiesProvider(DefaultCapabilitiesProvider defaultCapabilitiesProvider) : ICapabilitiesProvider
{
    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var capabilities = defaultCapabilitiesProvider.GetCapabilities(clientCapabilities);

        // Razor doesn't use workspace symbols, so disable to prevent duplicate results (with LiveshareLanguageClient) in liveshare.
        capabilities.WorkspaceSymbolProvider = false;

        if (capabilities is VSInternalServerCapabilities vsServerCapabilities)
        {
            vsServerCapabilities.SupportsDiagnosticRequests = true;
            vsServerCapabilities.SpellCheckingProvider = true;
            vsServerCapabilities.Experimental ??= new Dictionary<string, bool>();
            vsServerCapabilities.MapCodeProvider = true;
            var experimental = (Dictionary<string, bool>)vsServerCapabilities.Experimental;
            experimental[SimplifyMethodHandler.SimplifyMethodMethodName] = true;
            experimental[FormatNewFileHandler.FormatNewFileMethodName] = true;
            experimental[SemanticTokensRangesHandler.SemanticRangesMethodName] = true;

            var regexExpression = string.Join("|", InlineCompletionsHandler.BuiltInSnippets);
            var regex = new Regex(regexExpression, RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
            vsServerCapabilities.InlineCompletionOptions = new VSInternalInlineCompletionOptions
            {
                Pattern = regex
            };

            return vsServerCapabilities;
        }

        return capabilities;
    }
}