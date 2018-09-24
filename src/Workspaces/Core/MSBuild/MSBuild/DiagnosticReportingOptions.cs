// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal readonly struct DiagnosticReportingOptions
    {
        public DiagnosticReportingMode OnPathFailure { get; }
        public DiagnosticReportingMode OnLoaderFailure { get; }

        public DiagnosticReportingOptions(
            DiagnosticReportingMode onPathFailure,
            DiagnosticReportingMode onLoaderFailure)
        {
            OnPathFailure = onPathFailure;
            OnLoaderFailure = onLoaderFailure;
        }

        public static DiagnosticReportingOptions IgnoreAll { get; }
            = new DiagnosticReportingOptions(DiagnosticReportingMode.Ignore, DiagnosticReportingMode.Ignore);

        public static DiagnosticReportingOptions ThrowForAll { get; }
            = new DiagnosticReportingOptions(DiagnosticReportingMode.Throw, DiagnosticReportingMode.Throw);
    }
}
