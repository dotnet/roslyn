// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;

namespace Microsoft.CodeAnalysis.AddMissingImports;

internal sealed class AddMissingImportsAnalysisResult(
    ImmutableArray<AddImportFixData> addImportFixData)
{
    public ImmutableArray<AddImportFixData> AddImportFixData { get; } = addImportFixData;
    public bool CanAddMissingImports => !AddImportFixData.IsEmpty;
}
