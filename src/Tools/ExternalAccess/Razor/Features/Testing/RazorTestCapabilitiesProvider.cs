// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

[ExportCSharpVisualBasicStatelessLspService(typeof(ICapabilitiesProvider), WellKnownLspServerKinds.RazorLspServer), Shared]
[Export(typeof(RazorTestCapabilitiesProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorTestCapabilitiesProvider() : ICapabilitiesProvider
{
    public IRazorTestCapabilitiesProvider? RazorTestCapabilities { get; set; }
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        Contract.ThrowIfNull(RazorTestCapabilities, nameof(RazorTestCapabilities));
        Contract.ThrowIfNull(JsonSerializerOptions, nameof(JsonSerializerOptions));

        // To avoid exposing types from MS.VS.LanguageServer.Protocol types we serialize and deserialize the capabilities
        // so we can just pass string around. This is obviously not great for perf, but it is only used in Razor tests.
        var clientCapabilitiesJson = JsonSerializer.Serialize(clientCapabilities, JsonSerializerOptions);
        var serverCapabilitiesJson = RazorTestCapabilities.GetServerCapabilitiesJson(clientCapabilitiesJson);
        var serverCapabilities = JsonSerializer.Deserialize<VSInternalServerCapabilities>(serverCapabilitiesJson, JsonSerializerOptions);

        if (serverCapabilities is null)
        {
            throw new InvalidOperationException("Could not deserialize server capabilities as VSInternalServerCapabilities");
        }

        return serverCapabilities;
    }
}