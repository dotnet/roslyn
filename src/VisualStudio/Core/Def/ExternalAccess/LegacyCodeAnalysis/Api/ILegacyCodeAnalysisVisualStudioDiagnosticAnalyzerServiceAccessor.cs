// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api
{
    internal interface ILegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor
    {
        IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy hierarchyOpt);
    }
}
