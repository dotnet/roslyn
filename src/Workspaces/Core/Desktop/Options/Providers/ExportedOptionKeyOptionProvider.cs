// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Options.Providers
{
    [ExportOptionProvider, Shared]
    internal class ExportedOptionProvider : IOptionProvider
    {
        private readonly IEnumerable<Lazy<IOption>> _options;

        [ImportingConstructor]
        public ExportedOptionProvider([ImportMany] IEnumerable<Lazy<IOption>> options)
        {
            _options = options;
        }

        public ImmutableArray<IOption> Options 
            => _options.Select(lazy => lazy.Value).ToImmutableArray();
    }
}
