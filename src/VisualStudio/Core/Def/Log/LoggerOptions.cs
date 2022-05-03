// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class LoggerOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LoggerOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            EtwLoggerKey,
            TraceLoggerKey,
            OutputWindowLoggerKey);

        private const string LocalRegistryPath = @"Roslyn\Internal\Performance\Logger\";

        public static readonly Option2<bool> EtwLoggerKey = new(nameof(LoggerOptions), nameof(EtwLoggerKey), defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "EtwLogger"));

        public static readonly Option2<bool> TraceLoggerKey = new(nameof(LoggerOptions), nameof(TraceLoggerKey), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "TraceLogger"));

        public static readonly Option2<bool> OutputWindowLoggerKey = new(nameof(LoggerOptions), nameof(OutputWindowLoggerKey), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "OutputWindowLogger"));
    }
}
