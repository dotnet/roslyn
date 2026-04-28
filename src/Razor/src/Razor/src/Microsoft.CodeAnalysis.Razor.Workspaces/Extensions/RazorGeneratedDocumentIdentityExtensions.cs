// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis;

internal static class RazorGeneratedDocumentIdentityExtensions
{
    public static bool IsRazorSourceGeneratedDocument(this RazorGeneratedDocumentIdentity identity)
    {
        return identity.GeneratorTypeName == typeof(RazorSourceGenerator).FullName;
    }
}
