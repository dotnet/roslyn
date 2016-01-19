// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    [ExportOptionProvider, Shared]
    internal class RunTimeOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = SpecializedCollections.SingletonEnumerable(RunTimeOptions.FullSolutionAnalysis);

        public IEnumerable<IOption> GetOptions() => _options;
    }
}
