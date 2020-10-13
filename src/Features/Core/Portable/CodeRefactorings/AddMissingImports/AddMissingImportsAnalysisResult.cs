// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal sealed class AddMissingImportsAnalysisResult
    {
        public ImmutableArray<AddImportFixData> AddImportFixData { get; }
        public Document Document { get; }
        public TextSpan TextSpan { get; }
        public bool CanAddMissingImports { get; }

        public AddMissingImportsAnalysisResult(
            ImmutableArray<AddImportFixData> addImportFixData,
            Document document,
            TextSpan textSpan,
            bool canAddMissingImports)
        {
            AddImportFixData = addImportFixData;
            Document = document;
            TextSpan = textSpan;
            CanAddMissingImports = canAddMissingImports;

        }
    }
}
