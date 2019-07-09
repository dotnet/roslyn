// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis
{
    [Export(typeof(ILegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor))]
    [Shared]
    internal sealed class LegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor
        : ILegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor
    {
        private readonly IVisualStudioDiagnosticListSuppressionStateService _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor(IVisualStudioDiagnosticListSuppressionStateService implementation)
        {
            _implementation = implementation;
        }

        public bool CanSuppressSelectedEntries => _implementation.CanSuppressSelectedEntries;
        public bool CanSuppressSelectedEntriesInSource => _implementation.CanSuppressSelectedEntriesInSource;
        public bool CanSuppressSelectedEntriesInSuppressionFiles => _implementation.CanSuppressSelectedEntriesInSuppressionFiles;
        public bool CanRemoveSuppressionsSelectedEntries => _implementation.CanRemoveSuppressionsSelectedEntries;
    }
}
