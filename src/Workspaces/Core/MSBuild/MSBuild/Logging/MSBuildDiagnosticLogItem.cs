// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.MSBuild.Logging
{
    internal class MSBuildDiagnosticLogItem : DiagnosticLogItem
    {
        public string FileName { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }

        public MSBuildDiagnosticLogItem(WorkspaceDiagnosticKind kind, string projectFilePath, string message, string fileName, int lineNumber, int columnNumber)
            : base(kind, message, projectFilePath)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public override string ToString()
            => $"{FileName}: ({LineNumber}, {ColumnNumber}): {Message}";
    }
}
