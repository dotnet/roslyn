// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public static class WellKnownDiagnosticTags
    {
        /// <summary>
        /// Indicates that the diagnostic is related to some unnecessary source code.
        /// </summary>
        public const string Unnecessary = nameof(Unnecessary);

        /// <summary>
        /// Indicates that the diagnostic is related to edit and continue.
        /// </summary>
        public const string EditAndContinue = nameof(EditAndContinue);

        /// <summary>
        /// Indicates that the diagnostic is related to build.
        /// </summary>
        public const string Build = nameof(Build);

        /// <summary>
        /// Indicates that the diagnostic is reported by the compiler.
        /// </summary>
        public const string Compiler = nameof(Compiler);

        /// <summary>
        /// Indicates that the diagnostic can be used for telemetry
        /// </summary>
        public const string Telemetry = nameof(Telemetry);

        /// <summary>
        /// Indicates that the diagnostic is not configurable, i.e. it cannot be suppressed or filtered or have its severity changed.
        /// </summary>
        public const string NotConfigurable = nameof(NotConfigurable);

        /// <summary>
        /// Indicates that the diagnostic is related to an exception thrown by a <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        public const string AnalyzerException = nameof(AnalyzerException);
    }
}
