// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LoggerOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            LoggerOptions.EtwLoggerKey,
            LoggerOptions.TraceLoggerKey,
            LoggerOptions.OutputWindowLoggerKey);
    }
}
