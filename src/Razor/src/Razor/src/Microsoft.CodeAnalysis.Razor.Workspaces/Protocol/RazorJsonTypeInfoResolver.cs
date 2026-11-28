// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

[Export(ProtocolConstants.RoslynLspLanguagesContract, typeof(IJsonTypeInfoResolver)), Shared]
internal sealed class RazorJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorJsonTypeInfoResolver()
    {
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => ((IJsonTypeInfoResolver)RazorJsonSerializerContext.Default).GetTypeInfo(type, options);
}
