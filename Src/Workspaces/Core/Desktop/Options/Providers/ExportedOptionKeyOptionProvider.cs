// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Options.Providers
{
    [ExportOptionProvider, Shared]
    internal class ExportedOptionProvider : IOptionProvider
    {
        private readonly IEnumerable<Lazy<IOption>> options;

        [ImportingConstructor]
        public ExportedOptionProvider([ImportMany] IEnumerable<Lazy<IOption>> options)
        {
            this.options = options;
        }

        public IEnumerable<IOption> GetOptions()
        {
            return options.Select(lazy => lazy.Value);
        }
    }
}
