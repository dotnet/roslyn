// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class ComponentTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        private BindTagHelperProducer.Factory? _bindTagHelperProducerFactory;

        protected override void OnInitialized()
        {
            _bindTagHelperProducerFactory = GetRequiredFeature<BindTagHelperProducer.Factory>();
        }

        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            Assumed.NotNull(_bindTagHelperProducerFactory);

            _bindTagHelperProducerFactory.TryCreate(compilation, includeDocumentation, excludeHidden, out var producer);

            result = new ComponentTagHelperProducer((BindTagHelperProducer?)producer);
            return true;
        }
    }
}
