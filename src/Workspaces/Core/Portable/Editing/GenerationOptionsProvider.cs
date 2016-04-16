// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editing
{
    [ExportOptionProvider, Shared]
    internal class GenerationOptionsProvider : IOptionProvider
    {
        private static readonly IEnumerable<IOption> s_options = ImmutableArray.Create<IOption>(
                GenerationOptions.PlaceSystemNamespaceFirst
                );

        public IEnumerable<IOption> GetOptions()
        {
            return s_options;
        }
    }
}