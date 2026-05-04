// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal sealed class StaticCompilationTagHelperFeature(Compilation compilation) : RazorEngineFeatureBase, ITagHelperFeature
    {
        private ITagHelperDiscoveryService? _discoveryService;
        private TagHelperDiscoverer? _discoverer;

        public TagHelperCollection GetTagHelpers(IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            if (_discoveryService is null)
            {
                return [];
            }

            if (_discoverer is null &&
                !_discoveryService.TryGetDiscoverer(compilation, out _discoverer))
            {
                return [];
            }

            return _discoverer.GetTagHelpers(assembly, cancellationToken);
        }

        TagHelperCollection ITagHelperFeature.GetTagHelpers(CancellationToken cancellationToken)
        {
            if (_discoveryService is null)
            {
                return [];
            }

            return _discoveryService.GetTagHelpers(compilation, cancellationToken);
        }

        protected override void OnInitialized()
        {
            _discoveryService = Engine.GetFeatures<ITagHelperDiscoveryService>().FirstOrDefault();
        }
    }
}
