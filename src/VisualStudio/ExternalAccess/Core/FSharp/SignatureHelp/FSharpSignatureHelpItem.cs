// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.SignatureHelp;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
#endif

internal class FSharpSignatureHelpItem
{
    /// <summary>
    /// True if this signature help item can have an unbounded number of arguments passed to it.
    /// If it is variadic then the last parameter will be considered selected, even if the
    /// selected parameter index strictly goes past the number of defined parameters for this
    /// item.
    /// </summary>
    public bool IsVariadic { get; }

    public ImmutableArray<TaggedText> PrefixDisplayParts { get; }
    public ImmutableArray<TaggedText> SuffixDisplayParts { get; }

    // TODO: This probably won't be sufficient for VB query signature help.  It has
    // arbitrary separators between parameters.
    public ImmutableArray<TaggedText> SeparatorDisplayParts { get; }

    public ImmutableArray<FSharpSignatureHelpParameter> Parameters { get; }

    public ImmutableArray<TaggedText> DescriptionParts { get; internal set; }

    public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; }

    private static readonly Func<CancellationToken, IEnumerable<TaggedText>> s_emptyDocumentationFactory = _ => [];

    public FSharpSignatureHelpItem(
        bool isVariadic,
        Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
        IEnumerable<TaggedText> prefixParts,
        IEnumerable<TaggedText> separatorParts,
        IEnumerable<TaggedText> suffixParts,
        IEnumerable<FSharpSignatureHelpParameter> parameters,
        IEnumerable<TaggedText> descriptionParts)
    {
        if (isVariadic && !parameters.Any())
        {
            throw new ArgumentException(FeaturesResources.Variadic_SignatureHelpItem_must_have_at_least_one_parameter);
        }

        this.IsVariadic = isVariadic;
        this.DocumentationFactory = documentationFactory ?? s_emptyDocumentationFactory;
        this.PrefixDisplayParts = prefixParts.ToImmutableArrayOrEmpty();
        this.SeparatorDisplayParts = separatorParts.ToImmutableArrayOrEmpty();
        this.SuffixDisplayParts = suffixParts.ToImmutableArrayOrEmpty();
        this.Parameters = parameters.ToImmutableArrayOrEmpty();
        this.DescriptionParts = descriptionParts.ToImmutableArrayOrEmpty();
    }

    internal IEnumerable<TaggedText> GetAllParts()
    {
        return
            PrefixDisplayParts.Concat(
            SeparatorDisplayParts.Concat(
            SuffixDisplayParts.Concat(
            Parameters.SelectMany(p => p.GetAllParts())).Concat(
            DescriptionParts)));
    }

    public override string ToString()
    {
        var prefix = string.Concat(PrefixDisplayParts);
        var suffix = string.Concat(SuffixDisplayParts);
        var parameters = string.Join(string.Concat(SeparatorDisplayParts), Parameters);
        var description = string.Concat(DescriptionParts);
        return string.Concat(prefix, parameters, suffix, description);
    }
}
