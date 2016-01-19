// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    /// <summary>
    /// Options that aren't persisted. options here will be reset to default on new process.
    /// </summary>
    internal static class RunTimeOptions
    {
        public const string OptionName = "ServiceFeaturesOnOff";

        public static readonly Option<bool> FullSolutionAnalysis = new Option<bool>(OptionName, "FullSolutionAnalysisDiagnostic", defaultValue: true);
    }
}
