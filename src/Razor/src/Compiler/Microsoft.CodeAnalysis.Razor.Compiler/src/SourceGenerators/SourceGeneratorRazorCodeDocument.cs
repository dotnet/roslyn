// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

/// <summary>
/// A wrapper for <see cref="RazorCodeDocument"/>
/// </summary>
/// <remarks>
/// The razor compiler modifies the <see cref="RazorCodeDocument"/> in place during the various phases,
/// meaning object identity is maintained even when the contents have changed.
/// 
/// We need to be able to identify from the source generator if a given code document was modified or 
/// returned unchanged. Rather than implementing deep equality on the <see cref="RazorCodeDocument"/> 
/// which can get expensive, we instead use a wrapper class. If the underlying document is unchanged we
/// return the original wrapper class. If the underlying  document is changed, we return a new instance
/// of the wrapper.
/// </remarks>
internal sealed class SourceGeneratorRazorCodeDocument(RazorCodeDocument razorCodeDocument)
{
    public RazorCodeDocument CodeDocument { get; } = razorCodeDocument;
}
