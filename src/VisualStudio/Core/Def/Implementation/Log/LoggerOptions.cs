// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal static class LoggerOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Performance\Logger\";

        public static readonly Option<bool> EtwLoggerKey = new Option<bool>(nameof(LoggerOptions), nameof(EtwLoggerKey), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "EtwLogger"));

        public static readonly Option<bool> TraceLoggerKey = new Option<bool>(nameof(LoggerOptions), nameof(TraceLoggerKey), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "TraceLogger"));

        public static readonly Option<bool> OutputWindowLoggerKey = new Option<bool>(nameof(LoggerOptions), nameof(OutputWindowLoggerKey), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "OutputWindowLogger"));
    }

    [ExportOptionProvider, Shared]
    internal class LoggerOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public LoggerOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            LoggerOptions.EtwLoggerKey,
            LoggerOptions.TraceLoggerKey,
            LoggerOptions.OutputWindowLoggerKey);
    }

}
