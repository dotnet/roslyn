// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

// Razor.Workspaces is not a VS MEF component, so the in-process export lives here.
[Export(ProtocolConstants.RoslynLspLanguagesContract, typeof(IJsonTypeInfoResolver))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class CohostJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => ((IJsonTypeInfoResolver)RazorJsonSerializerContext.Default).GetTypeInfo(type, options);
}
