// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class DiagnosticArguments
    {
        public bool ReportSuppressedDiagnostics;
        public bool LogAnalyzerExecutionTime;
        public Guid ProjectIdGuid;
        public string ProjectIdDebugName;
        public byte[][] HostAnalyzerChecksumsByteArray;
        public string[] AnalyzerIds;

        public ProjectId GetProjectId() => ProjectId.CreateFromSerialized(ProjectIdGuid, ProjectIdDebugName);
        public IEnumerable<Checksum> GetHostAnalyzerChecksums() => HostAnalyzerChecksumsByteArray.Select(b => new Checksum(b));
    }
}