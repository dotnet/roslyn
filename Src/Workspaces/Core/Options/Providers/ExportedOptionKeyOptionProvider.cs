// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if MEF
using System.ComponentModel.Composition;
#endif
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !MEF
using Microsoft.CodeAnalysis.Composition;
#endif
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Options.Providers
{
#if MEF
    [ExportOptionProvider]
#endif
    internal class ExportedOptionProvider : IOptionProvider
    {
        private readonly IEnumerable<Lazy<IOption>> options;

#if MEF
        [ImportingConstructor]
        public ExportedOptionProvider([ImportMany] IEnumerable<Lazy<IOption>> options)
#else
        public ExportedOptionProvider(IEnumerable<Lazy<IOption>> options)
#endif
        {
            this.options = options;
        }

#if MEF
#else
        public ExportedOptionProvider(ExportSource exports)
        {
            this.options = exports.GetExports<IOption>();
        }
#endif

        public IEnumerable<IOption> GetOptions()
        {
            return options.Select(lazy => lazy.Value);
        }
    }
}
