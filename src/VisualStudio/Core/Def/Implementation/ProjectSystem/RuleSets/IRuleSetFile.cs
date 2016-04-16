// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
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
