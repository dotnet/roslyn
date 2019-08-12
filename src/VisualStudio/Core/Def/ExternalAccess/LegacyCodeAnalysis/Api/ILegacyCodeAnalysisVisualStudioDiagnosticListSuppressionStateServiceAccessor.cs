// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api
{
    internal interface ILegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor
    {
        bool CanSuppressSelectedEntries { get; }
        bool CanSuppressSelectedEntriesInSource { get; }
        bool CanSuppressSelectedEntriesInSuppressionFiles { get; }
        bool CanRemoveSuppressionsSelectedEntries { get; }
    }
}
