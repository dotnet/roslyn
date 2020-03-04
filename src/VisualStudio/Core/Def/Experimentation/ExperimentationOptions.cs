﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
