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
internal sealed class SourceGeneratorRazorCodeDocument(RazorCodeDocument razorCodeDocument, RazorProjectItem? sourceItem = null)
{
    public RazorCodeDocument CodeDocument { get; } = razorCodeDocument;

    /// <summary>
    /// The <see cref="RazorProjectItem"/> that originally produced <see cref="CodeDocument"/>, stored so
    /// that <see cref="SourceGeneratorProjectEngine.ProcessTagHelpers"/> can rebuild an unresolved IR
    /// when tag helpers change materially. The SG-side project engine wraps a filesystem that already
    /// contains the relevant imports, so the source item alone is sufficient to replay phases
    /// 0..decl-lowering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We don't snapshot the unresolved IR itself because <see cref="RazorCodeDocument"/> only
    /// provides shallow immutability -- its <c>With*</c> methods create new wrappers but share the
    /// underlying mutable IR. The tag helper resolution phase mutates the IR tree in place, so any
    /// retained reference to a "pre-resolution" code document becomes invalid once resolution runs.
    /// </para>
    /// <para>
    /// Phases 0..decl-lowering are tag-helper-independent and deterministic on stable input, so the
    /// replayed IR is equivalent to the original at the point of replay.
    /// </para>
    /// <para>
    /// Will be <see langword="null"/> when the wrapper wasn't produced by
    /// <see cref="SourceGeneratorProjectEngine.ProcessForDecl"/> (e.g. constructed directly by tests).
    /// </para>
    /// </remarks>
    public RazorProjectItem? SourceItem { get; } = sourceItem;
}
