// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public byte[] OptionSetChecksumBytes;
        public byte[][] HostAnalyzerChecksumsByteArray;
        public string[] AnalyzerIds;

        public DiagnosticArguments()
        {
        }

        public DiagnosticArguments(
            bool reportSuppressedDiagnostics,
            bool logAnalyzerExecutionTime,
            ProjectId projectId,
            byte[] optionSetChecksum,
            ImmutableArray<byte[]> hostAnalyzerChecksums,
            string[] analyzerIds)
        {
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            LogAnalyzerExecutionTime = logAnalyzerExecutionTime;

            ProjectIdGuid = projectId.Id;
            ProjectIdDebugName = projectId.DebugName;

            OptionSetChecksumBytes = optionSetChecksum;
            HostAnalyzerChecksumsByteArray = hostAnalyzerChecksums.ToArray();
            AnalyzerIds = analyzerIds;
        }

        public ProjectId GetProjectId() => ProjectId.CreateFromSerialized(ProjectIdGuid, ProjectIdDebugName);
        public IEnumerable<Checksum> GetHostAnalyzerChecksums() => HostAnalyzerChecksumsByteArray.Select(b => new Checksum(b));
        public Checksum GetOptionSetChecksum() => new Checksum(OptionSetChecksumBytes);
    }
}