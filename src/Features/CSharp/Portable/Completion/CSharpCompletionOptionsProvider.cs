// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    [ExportOptionProvider, Shared]
    internal class CSharpCompletionOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = SpecializedCollections.EmptyEnumerable<IOption>();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
