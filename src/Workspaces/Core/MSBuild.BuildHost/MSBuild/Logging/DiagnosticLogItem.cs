// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild.Logging
{
    [DataContract]
    internal class DiagnosticLogItem
    {
        [DataMember(Order = 0)]
        public WorkspaceDiagnosticKind Kind { get; }

        [DataMember(Order = 1)]
        public string Message { get; }

        [DataMember(Order = 2)]
        public string ProjectFilePath { get; }

        public DiagnosticLogItem(WorkspaceDiagnosticKind kind, string message, string projectFilePath)
        {
            Kind = kind;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ProjectFilePath = projectFilePath ?? throw new ArgumentNullException(nameof(message));
        }

        public override string ToString() => Message;
    }
}
