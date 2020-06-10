// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        /// <summary>
        /// Indicates that the diagnostic is an obsolete diagnostic with a custom ID
        /// specified by the 'DiagnosticId' property on 'ObsoleteAttribute'.
        /// </summary>
        public const string CustomObsolete = nameof(CustomObsolete);
    }
}
