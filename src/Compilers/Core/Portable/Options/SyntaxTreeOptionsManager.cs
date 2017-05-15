// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Type that caches syntax tree options coming from a given provider. May be shared between multiple
    /// <see cref="Compilation"/>s if they are using the same provider and thus have the same data.
    /// </summary>
    internal sealed class SyntaxTreeOptionsManager
    {
        private readonly SyntaxTreeOptionsProvider _provider;

        public SyntaxTreeOptionsManager(SyntaxTreeOptionsProvider provider)
        {
            _provider = provider;
        }

        public OptionSet GetOptionsForSyntaxTree(SyntaxTree tree)
        {
            // TODO: actually implement a cache
            return _provider.GetOptionsForSyntaxTreePath(tree.FilePath);
        }

        [Conditional("DEBUG")]
        internal void AssertCanReuseForCompilation(Compilation compilation)
        {
            Debug.Assert(object.Equals(_provider, compilation.Options.SyntaxTreeOptionsProvider));
        }
    }
}
