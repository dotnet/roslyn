// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class PersistentStorageOptions
    {
        public const string OptionName = "FeatureManager/Persistence";

        public static readonly Option<bool> Enabled = new Option<bool>(OptionName, "Enabled", defaultValue: true);

        public static readonly Option<bool> EsentPerformanceMonitor = new Option<bool>(OptionName, "Esent PerfMon", defaultValue: false);
    }
}
