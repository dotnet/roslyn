// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    internal static class ExperimentationOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\Experiment\";

        public static readonly Option<bool> SolutionStatusService_ForceDelay = new Option<bool>(nameof(ExperimentationOptions), nameof(SolutionStatusService_ForceDelay), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "SolutionStatusService_ForceDelay"));

        public static readonly Option<int> SolutionStatusService_DelayInMS = new Option<int>(nameof(ExperimentationOptions), nameof(SolutionStatusService_DelayInMS), defaultValue: 10000,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "SolutionStatusService_DelayInMS"));
    }

    [ExportOptionProvider, Shared]
    internal class ExperimentationOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            ExperimentationOptions.SolutionStatusService_ForceDelay,
            ExperimentationOptions.SolutionStatusService_DelayInMS);
    }
}
