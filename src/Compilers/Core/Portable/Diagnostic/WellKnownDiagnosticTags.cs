// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public static class WellKnownDiagnosticTags
    {
        /// <summary>
        /// Indicates that the diagnostic is related to some unnecessary source code.
        /// </summary>
        public const string Unnecessary = "Unnecessary";

        /// <summary>
        /// Indicates that the diagnostic is related to edit and continue.
        /// </summary>
        public const string EditAndContinue = "EditAndContinue";

        /// <summary>
        /// Indicates that the diagnostic is related to build.
        /// </summary>
        public const string Build = "Build";

        /// <summary>
        /// Indicates that the diagnostic is reported by the compiler.
        /// </summary>
        public const string Compiler = "Compiler";

        /// <summary>
        /// Indicates that the diagnostic can be used for telemetry
        /// </summary>
        public const string Telemetry = "Telemetry";

        /// <summary>
        /// Indicates that the diagnostic is not configurable, i.e. it cannot be suppressed or filtered or have its severity changed.
        /// </summary>
        public const string NotConfigurable = "NotConfigurable";

        /// <summary>
        /// Indicates that the diagnostic is related to an exception thrown by a <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        public const string AnalyzerException = "AnalyzerException";
    }
}
