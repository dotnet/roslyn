// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        public const string OptionName = "InternalDiagnosticsOptions";

        [ExportOption]
        public static readonly Option<bool> BlueSquiggleForBuildDiagnostic = new Option<bool>(OptionName, "Blue Squiggle For Build Diagnostic", defaultValue: false);
    }
}
