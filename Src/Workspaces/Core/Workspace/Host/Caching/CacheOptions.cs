// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class CacheOptions
    {
        public const string FeatureName = "Cache Options";

        [ExportOption]
        public static readonly Option<long> TextCacheSize = new Option<long>(FeatureName, "Default Text Cache Size", defaultValue: TextCacheServiceFactory.CacheSize);

        [ExportOption]
        public static readonly Option<int> TextCacheCount = new Option<int>(FeatureName, "Default Minimum Text Count In The Cache", defaultValue: TextCacheServiceFactory.TextCount);

        [ExportOption]
        public static readonly Option<long> SyntaxTreeCacheSize = new Option<long>(FeatureName, "Default SyntaxTree Cache Size", defaultValue: SyntaxTreeCacheServiceFactory.CacheSize);

        [ExportOption]
        public static readonly Option<int> SyntaxTreeCacheCount = new Option<int>(FeatureName, "Default Minimum SyntaxTree Count In The Cache", defaultValue: SyntaxTreeCacheServiceFactory.TreeCount);

        [ExportOption]
        public static readonly Option<long> CompilationCacheSize = new Option<long>(FeatureName, "Default Compilation Cache Size", defaultValue: CompilationCacheServiceFactory.CacheSize);

        [ExportOption]
        public static readonly Option<int> CompilationCacheCount = new Option<int>(FeatureName, "Default Minimum Compilation Count In The Cache", defaultValue: CompilationCacheServiceFactory.CompilationCount);
    }
}
