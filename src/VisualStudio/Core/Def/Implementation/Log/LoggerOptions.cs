// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal static class LoggerOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Performance\Logger\";

        [ExportOption]
        public static readonly Option<bool> EtwLoggerKey = new Option<bool>(nameof(LoggerOptions), nameof(EtwLoggerKey), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "EtwLogger"));

        [ExportOption]
        public static readonly Option<bool> TraceLoggerKey = new Option<bool>(nameof(LoggerOptions), nameof(TraceLoggerKey), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "TraceLoggerKey"));
    }
}
