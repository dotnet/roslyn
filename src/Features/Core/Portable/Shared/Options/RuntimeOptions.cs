// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    /// <summary>
    /// Options that aren't persisted. options here will be reset to default on new process.
    /// </summary>
    internal static class RuntimeOptions
    {
        public const string OptionName = "Runtime";

        public static readonly Option<bool> FullSolutionAnalysis = new Option<bool>(OptionName, "Full Solution Analysis", defaultValue: true);
    }
}
