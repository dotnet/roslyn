// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class DefaultTagHelperProducer : TagHelperProducer
{
    private readonly DefaultTagHelperDescriptorFactory _factory;
    private readonly INamedTypeSymbol _iTagHelperType;

    private DefaultTagHelperProducer(DefaultTagHelperDescriptorFactory factory, INamedTypeSymbol iTagHelperType)
    {
        _factory = factory;
        _iTagHelperType = iTagHelperType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.Default;

    public override bool SupportsTypes => true;

    public override bool IsCandidateType(INamedTypeSymbol type)
        => type.IsTagHelper(_iTagHelperType);

    public override void AddTagHelpersForType(
        INamedTypeSymbol type,
        ref TagHelperCollection.RefBuilder results,
        CancellationToken cancellationToken)
    {
        if (_factory.CreateDescriptor(type) is { } descriptor)
        {
            results.Add(descriptor);
        }
    }
}
