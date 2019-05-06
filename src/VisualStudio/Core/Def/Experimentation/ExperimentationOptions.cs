// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    internal static class ExperimentationOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\Experiment\";
    }

    [ExportOptionProvider, Shared]
    internal class ExperimentationOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray<IOption>.Empty;
    }
}
