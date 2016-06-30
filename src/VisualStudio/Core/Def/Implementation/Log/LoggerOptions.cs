// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal static class LoggerOptions
    {
        public const string FeatureName = "Performance/Loggers";
        private const string LocalRegistryPath = @"Roslyn\Internal\Performance\Logger\";

        [ExportOption]
        public static readonly Option<bool> EtwLoggerKey = new Option<bool>(FeatureName, "EtwLogger", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "EtwLogger"));

        [ExportOption]
        public static readonly Option<bool> TraceLoggerKey = new Option<bool>(FeatureName, "TraceLogger", defaultValue: false,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "TraceLoggerKey"));
    }
}
