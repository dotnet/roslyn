// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem
{
    internal interface IRuleSetFile
    {
        event EventHandler UpdatedOnDisk;
        string FilePath { get; }
        Exception GetException();
        ReportDiagnostic GetGeneralDiagnosticOption();
        ImmutableDictionary<string, ReportDiagnostic> GetSpecificDiagnosticOptions();
    }
}
