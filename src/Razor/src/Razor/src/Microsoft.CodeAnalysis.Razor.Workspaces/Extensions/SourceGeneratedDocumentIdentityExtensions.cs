// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis;

internal static class SourceGeneratedDocumentIdentityExtensions
{
    public static bool IsRazorSourceGeneratedDocument(this SourceGeneratedDocumentIdentity identity)
    {
        return identity.Generator.TypeName == typeof(RazorSourceGenerator).FullName;
    }
}
