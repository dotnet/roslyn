// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis;

[Export(typeof(ILegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor))]
[Shared]
internal sealed class LegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor
    : ILegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor
{
    private readonly IVisualStudioDiagnosticListSuppressionStateService _implementation;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor(IVisualStudioDiagnosticListSuppressionStateService implementation)
        => _implementation = implementation;

    public bool CanSuppressSelectedEntries => _implementation.CanSuppressSelectedEntries;
    public bool CanSuppressSelectedEntriesInSource => _implementation.CanSuppressSelectedEntriesInSource;
    public bool CanSuppressSelectedEntriesInSuppressionFiles => _implementation.CanSuppressSelectedEntriesInSuppressionFiles;
    public bool CanRemoveSuppressionsSelectedEntries => _implementation.CanRemoveSuppressionsSelectedEntries;
}
